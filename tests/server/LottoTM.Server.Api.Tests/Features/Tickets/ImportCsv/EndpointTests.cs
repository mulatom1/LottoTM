using System.Net;
using System.Net.Http.Headers;
using System.Text;
using LottoTM.Server.Api.Entities;
using LottoTM.Server.Api.Repositories;
using LottoTM.Server.Api.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;

namespace LottoTM.Server.Api.Tests.Features.Tickets.ImportCsv;

/// <summary>
/// Integration tests for the ImportCsv endpoint
/// </summary>
public class EndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public EndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Test successful CSV import with valid data
    /// </summary>
    [Fact]
    public async Task ImportCsv_WithValidData_Returns200Ok()
    {
        // Arrange
        var testDbName = "TestDb_ValidImport_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName, featureFlagEnabled: true);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var csvContent = """
            Number1,Number2,Number3,Number4,Number5,Number6,GroupName
            5,14,23,29,37,41,Group1
            1,2,3,4,5,6,Group2
            10,20,30,40,45,49
            """;

        var formContent = CreateMultipartFormContent(csvContent, "tickets.csv");

        // Act
        var response = await client.PostAsync("/api/tickets/import-csv", formContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.NotNull(result);
        Assert.Equal(3, int.Parse(result["imported"].ToString()!));
        Assert.Equal(0, int.Parse(result["rejected"].ToString()!));

        // Verify in database
        VerifyTicketsInDatabase(factory, userId, 3);
    }

    /// <summary>
    /// Test CSV import with some invalid rows
    /// </summary>
    [Fact]
    public async Task ImportCsv_WithPartiallyInvalidData_ReturnsImportedAndRejected()
    {
        // Arrange
        var testDbName = "TestDb_PartialImport_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName, featureFlagEnabled: true);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var csvContent = """
            Number1,Number2,Number3,Number4,Number5,Number6,GroupName
            5,14,23,29,37,41,Valid
            1,2,3,4,5,60,Invalid-OutOfRange
            10,10,30,40,45,49,Invalid-Duplicate
            1,2,3,4,5,6,Valid
            """;

        var formContent = CreateMultipartFormContent(csvContent, "tickets.csv");

        // Act
        var response = await client.PostAsync("/api/tickets/import-csv", formContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.NotNull(result);
        Assert.Equal(2, int.Parse(result["imported"].ToString()!));
        Assert.Equal(2, int.Parse(result["rejected"].ToString()!));
    }

    /// <summary>
    /// Test CSV import with invalid header returns 400
    /// </summary>
    [Fact]
    public async Task ImportCsv_WithInvalidHeader_Returns400BadRequest()
    {
        // Arrange
        var testDbName = "TestDb_InvalidHeader_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName, featureFlagEnabled: true);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var csvContent = """
            Num1,Num2,Num3,Num4,Num5,Num6,Group
            5,14,23,29,37,41,Group1
            """;

        var formContent = CreateMultipartFormContent(csvContent, "tickets.csv");

        // Act
        var response = await client.PostAsync("/api/tickets/import-csv", formContent);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Test CSV import with non-CSV file returns 400
    /// </summary>
    [Fact]
    public async Task ImportCsv_WithNonCsvFile_Returns400BadRequest()
    {
        // Arrange
        var testDbName = "TestDb_NonCsv_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName, featureFlagEnabled: true);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var formContent = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("not a csv"));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        formContent.Add(fileContent, "file", "test.json");

        // Act
        var response = await client.PostAsync("/api/tickets/import-csv", formContent);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Test CSV import with file exceeding size limit returns 400
    /// </summary>
    [Fact]
    public async Task ImportCsv_WithFileTooLarge_Returns400BadRequest()
    {
        // Arrange
        var testDbName = "TestDb_FileTooLarge_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName, featureFlagEnabled: true);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create content larger than 1MB
        var largeContent = new string('x', 1048577);
        var formContent = CreateMultipartFormContent(largeContent, "large.csv");

        // Act
        var response = await client.PostAsync("/api/tickets/import-csv", formContent);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Test CSV import when limit exceeded returns 400
    /// </summary>
    [Fact]
    public async Task ImportCsv_WhenLimitExceeded_Returns400BadRequest()
    {
        // Arrange
        var testDbName = "TestDb_LimitExceeded_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName, featureFlagEnabled: true);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        // Seed 99 tickets (leaving only 1 slot)
        for (int i = 0; i < 99; i++)
        {
            SeedTestTicket(factory, userId, new[] { 1 + i % 44, 2 + i % 44, 3 + i % 44, 4 + i % 44, 5 + i % 44, 6 + i % 44 });
        }

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Try to import 2 tickets (exceeds available slots)
        // Use numbers that won't be duplicates of seeded tickets
        var csvContent = """
            Number1,Number2,Number3,Number4,Number5,Number6,GroupName
            7,15,24,30,38,42,Group1
            8,16,25,31,39,43,Group2
            """;

        var formContent = CreateMultipartFormContent(csvContent, "tickets.csv");

        // Act
        var response = await client.PostAsync("/api/tickets/import-csv", formContent);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Test CSV import with duplicate ticket in CSV returns partial import
    /// </summary>
    [Fact]
    public async Task ImportCsv_WithDuplicateInCsv_RejectsOnlyDuplicate()
    {
        // Arrange
        var testDbName = "TestDb_DuplicateInCsv_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName, featureFlagEnabled: true);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var csvContent = """
            Number1,Number2,Number3,Number4,Number5,Number6,GroupName
            5,14,23,29,37,41,Group1
            5,14,23,29,37,41,Group1-Duplicate
            1,2,3,4,5,6,Group2
            """;

        var formContent = CreateMultipartFormContent(csvContent, "tickets.csv");

        // Act
        var response = await client.PostAsync("/api/tickets/import-csv", formContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.NotNull(result);
        Assert.Equal(2, int.Parse(result["imported"].ToString()!));
        Assert.Equal(1, int.Parse(result["rejected"].ToString()!));
    }

    /// <summary>
    /// Test CSV import with duplicate existing ticket rejects duplicate
    /// </summary>
    [Fact]
    public async Task ImportCsv_WithDuplicateExistingTicket_RejectsDuplicate()
    {
        // Arrange
        var testDbName = "TestDb_DuplicateExisting_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName, featureFlagEnabled: true);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        // Seed existing ticket
        SeedTestTicket(factory, userId, new[] { 5, 14, 23, 29, 37, 41 });

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var csvContent = """
            Number1,Number2,Number3,Number4,Number5,Number6,GroupName
            41,5,29,14,37,23,Group1-SameNumbers-DifferentOrder
            1,2,3,4,5,6,Group2
            """;

        var formContent = CreateMultipartFormContent(csvContent, "tickets.csv");

        // Act
        var response = await client.PostAsync("/api/tickets/import-csv", formContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.NotNull(result);
        Assert.Equal(1, int.Parse(result["imported"].ToString()!));
        Assert.Equal(1, int.Parse(result["rejected"].ToString()!));
    }

    /// <summary>
    /// Test CSV import without authentication returns 401
    /// </summary>
    [Fact]
    public async Task ImportCsv_WithoutAuthentication_Returns401Unauthorized()
    {
        // Arrange
        var testDbName = "TestDb_NoAuth_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName, featureFlagEnabled: true);
        var client = factory.CreateClient();

        var csvContent = """
            Number1,Number2,Number3,Number4,Number5,Number6,GroupName
            5,14,23,29,37,41,Group1
            """;

        var formContent = CreateMultipartFormContent(csvContent, "tickets.csv");

        // Act
        var response = await client.PostAsync("/api/tickets/import-csv", formContent);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Test CSV import when feature flag is disabled returns 404
    /// </summary>
    [Fact]
    public async Task ImportCsv_WhenFeatureDisabled_Returns404NotFound()
    {
        // Arrange
        var testDbName = "TestDb_FeatureDisabled_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName, featureFlagEnabled: false);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var csvContent = """
            Number1,Number2,Number3,Number4,Number5,Number6,GroupName
            5,14,23,29,37,41,Group1
            """;

        var formContent = CreateMultipartFormContent(csvContent, "tickets.csv");

        // Act
        var response = await client.PostAsync("/api/tickets/import-csv", formContent);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// Creates a test factory with in-memory database and configurable feature flag
    /// </summary>
    private WebApplicationFactory<Program> CreateTestFactory(string databaseName, bool featureFlagEnabled)
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.Sources.Clear();

                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = "Server=dummy;Database=dummy;Integrated Security=True;",
                    ["Jwt:Key"] = "ThisIsASecretKeyForTestingPurposesOnly123456",
                    ["Jwt:Issuer"] = "TestIssuer",
                    ["Jwt:Audience"] = "TestAudience",
                    ["Jwt:ExpiryInMinutes"] = "1440",
                    ["Swagger:Enabled"] = "false",
                    ["Features:TicketImportExport:Enable"] = featureFlagEnabled.ToString()
                });
            });

            builder.ConfigureServices(services =>
            {
                var descriptorsToRemove = services.Where(d =>
                    d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                    d.ServiceType == typeof(DbContextOptions) ||
                    d.ServiceType == typeof(AppDbContext) ||
                    d.ImplementationType?.FullName?.Contains("SqlServer") == true ||
                    d.ServiceType.FullName?.Contains("EntityFrameworkCore") == true)
                    .ToList();

                foreach (var descriptor in descriptorsToRemove)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase(databaseName)
                        .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning));
                });
            });
        });
    }

    /// <summary>
    /// Helper method to create multipart form content with CSV file
    /// </summary>
    private static MultipartFormDataContent CreateMultipartFormContent(string csvContent, string fileName)
    {
        var formContent = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(csvContent));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        formContent.Add(fileContent, "file", fileName);
        return formContent;
    }

    /// <summary>
    /// Helper method to seed test user into an in-memory database
    /// </summary>
    private static int SeedTestUser(string databaseName, string email, string password, bool isAdmin)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        using var dbContext = new AppDbContext(options);

        var existingUser = dbContext.Users.FirstOrDefault(u => u.Email == email);
        if (existingUser != null)
        {
            return existingUser.Id;
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
        var user = new User
        {
            Email = email,
            PasswordHash = passwordHash,
            IsAdmin = isAdmin,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Users.Add(user);
        dbContext.SaveChanges();

        return user.Id;
    }

    /// <summary>
    /// Helper method to seed test ticket into an in-memory database
    /// </summary>
    private static void SeedTestTicket(WebApplicationFactory<Program> factory, int userId, int[] numbers)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var ticket = new Ticket
        {
            UserId = userId,
            GroupName = "Seeded Ticket",
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Tickets.Add(ticket);
        dbContext.SaveChanges();

        var ticketNumbers = numbers
            .Select((number, index) => new TicketNumber
            {
                TicketId = ticket.Id,
                Number = number,
                Position = (byte)(index + 1)
            })
            .ToList();

        dbContext.TicketNumbers.AddRange(ticketNumbers);
        dbContext.SaveChanges();
    }

    /// <summary>
    /// Helper method to generate JWT token for testing
    /// </summary>
    private static string GenerateTestToken(WebApplicationFactory<Program> factory, int userId, string email, bool isAdmin)
    {
        using var scope = factory.Services.CreateScope();
        var jwtService = scope.ServiceProvider.GetRequiredService<IJwtService>();
        return jwtService.GenerateToken(userId, email, isAdmin, out _);
    }

    /// <summary>
    /// Helper method to verify tickets were created in database
    /// </summary>
    private static void VerifyTicketsInDatabase(WebApplicationFactory<Program> factory, int userId, int expectedCount)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var count = dbContext.Tickets.Count(t => t.UserId == userId);
        Assert.Equal(expectedCount, count);
    }
}
