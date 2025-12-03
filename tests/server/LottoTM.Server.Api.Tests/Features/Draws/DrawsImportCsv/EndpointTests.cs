using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LottoTM.Server.Api.Entities;
using LottoTM.Server.Api.Repositories;
using LottoTM.Server.Api.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text;

namespace LottoTM.Server.Api.Tests.Features.Draws.DrawsImportCsv;

/// <summary>
/// Integration tests for the DrawsImportCsv endpoint
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
    public async Task ImportDrawsCsv_WithValidData_Returns200OK()
    {
        // Arrange
        var testDbName = "TestDb_ValidImport_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName, true); // Feature enabled

        var userId = SeedTestUser(testDbName, "admin@example.com", "Password123!", true);

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "admin@example.com", true);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create CSV content
        var csvContent = @"DrawDate,LottoType,DrawSystemId,TicketPrice,WinPoolCount1,WinPoolAmount1,WinPoolCount2,WinPoolAmount2,WinPoolCount3,WinPoolAmount3,WinPoolCount4,WinPoolAmount4,Number1,Number2,Number3,Number4,Number5,Number6
2025-01-15,LOTTO,7001,3.00,1,1000000,5,50000,100,5000,500,500,3,12,25,31,42,48
2025-01-16,LOTTO,7002,3.00,,,,,,,,,5,10,15,20,25,30";

        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(csvContent));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        content.Add(fileContent, "file", "draws.csv");

        // Act
        var response = await client.PostAsync("/api/draws/import-csv", content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ImportCsvResponse>();
        Assert.NotNull(result);
        Assert.Equal(2, result.Imported);
        Assert.Equal(0, result.Rejected);
        Assert.Empty(result.Errors);

        // Verify in database
        VerifyDrawsInDatabase(factory, 2);
    }

    /// <summary>
    /// Test CSV import with invalid header
    /// </summary>
    [Fact]
    public async Task ImportDrawsCsv_WithInvalidHeader_Returns400BadRequest()
    {
        // Arrange
        var testDbName = "TestDb_InvalidHeader_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName, true);

        var userId = SeedTestUser(testDbName, "admin@example.com", "Password123!", true);

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "admin@example.com", true);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Invalid CSV header
        var csvContent = @"Invalid,Header,Format
2025-01-15,LOTTO,7001";

        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(csvContent));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        content.Add(fileContent, "file", "draws.csv");

        // Act
        var response = await client.PostAsync("/api/draws/import-csv", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Test CSV import with numbers out of range
    /// </summary>
    [Fact]
    public async Task ImportDrawsCsv_WithNumbersOutOfRange_RejectsRow()
    {
        // Arrange
        var testDbName = "TestDb_OutOfRange_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName, true);

        var userId = SeedTestUser(testDbName, "admin@example.com", "Password123!", true);

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "admin@example.com", true);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // CSV with number out of range (50)
        var csvContent = @"DrawDate,LottoType,DrawSystemId,TicketPrice,WinPoolCount1,WinPoolAmount1,WinPoolCount2,WinPoolAmount2,WinPoolCount3,WinPoolAmount3,WinPoolCount4,WinPoolAmount4,Number1,Number2,Number3,Number4,Number5,Number6
2025-01-15,LOTTO,7001,3.00,,,,,,,,,3,12,25,31,42,50";

        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(csvContent));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        content.Add(fileContent, "file", "draws.csv");

        // Act
        var response = await client.PostAsync("/api/draws/import-csv", content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ImportCsvResponse>();
        Assert.NotNull(result);
        Assert.Equal(0, result.Imported);
        Assert.Equal(1, result.Rejected);
        Assert.Single(result.Errors);
    }

    /// <summary>
    /// Test CSV import when feature is disabled
    /// </summary>
    [Fact]
    public async Task ImportDrawsCsv_WhenFeatureDisabled_Returns404NotFound()
    {
        // Arrange
        var testDbName = "TestDb_FeatureDisabled_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName, false); // Feature disabled

        var userId = SeedTestUser(testDbName, "admin@example.com", "Password123!", true);

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "admin@example.com", true);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var csvContent = @"DrawDate,LottoType,DrawSystemId,TicketPrice,WinPoolCount1,WinPoolAmount1,WinPoolCount2,WinPoolAmount2,WinPoolCount3,WinPoolAmount3,WinPoolCount4,WinPoolAmount4,Number1,Number2,Number3,Number4,Number5,Number6
2025-01-15,LOTTO,7001,3.00,,,,,,,,,3,12,25,31,42,48";

        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(csvContent));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        content.Add(fileContent, "file", "draws.csv");

        // Act
        var response = await client.PostAsync("/api/draws/import-csv", content);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// Test CSV import without authentication
    /// </summary>
    [Fact]
    public async Task ImportDrawsCsv_WithoutAuthentication_Returns401Unauthorized()
    {
        // Arrange
        var testDbName = "TestDb_NoAuth_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName, true);
        var client = factory.CreateClient();

        var csvContent = @"DrawDate,LottoType,DrawSystemId,TicketPrice,WinPoolCount1,WinPoolAmount1,WinPoolCount2,WinPoolAmount2,WinPoolCount3,WinPoolAmount3,WinPoolCount4,WinPoolAmount4,Number1,Number2,Number3,Number4,Number5,Number6
2025-01-15,LOTTO,7001,3.00,,,,,,,,,3,12,25,31,42,48";

        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(csvContent));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        content.Add(fileContent, "file", "draws.csv");

        // Act
        var response = await client.PostAsync("/api/draws/import-csv", content);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Creates a test factory with in-memory database
    /// </summary>
    private WebApplicationFactory<Program> CreateTestFactory(string databaseName, bool featureEnabled)
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
                    ["Features:DrawImportExport:Enable"] = featureEnabled.ToString()
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
    /// Helper method to seed test draw into database
    /// </summary>
    private static void SeedTestDraw(WebApplicationFactory<Program> factory, int userId, DateOnly drawDate, string lottoType, int[] numbers)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var draw = new Draw
        {
            DrawDate = drawDate,
            LottoType = lottoType,
            DrawSystemId = 7000,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = userId
        };

        dbContext.Draws.Add(draw);
        dbContext.SaveChanges();

        var drawNumbers = numbers
            .Select((number, index) => new DrawNumber
            {
                DrawId = draw.Id,
                Number = number,
                Position = (byte)(index + 1)
            })
            .ToList();

        dbContext.DrawNumbers.AddRange(drawNumbers);
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
    /// Helper method to verify draws count in database
    /// </summary>
    private static void VerifyDrawsInDatabase(WebApplicationFactory<Program> factory, int expectedCount)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var drawsCount = dbContext.Draws.Count();
        Assert.Equal(expectedCount, drawsCount);
    }

    /// <summary>
    /// Response model for import CSV endpoint
    /// </summary>
    private class ImportCsvResponse
    {
        public int Imported { get; set; }
        public int Rejected { get; set; }
        public List<ImportError> Errors { get; set; } = new();
    }

    /// <summary>
    /// Error model for import CSV response
    /// </summary>
    private class ImportError
    {
        public int Row { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
