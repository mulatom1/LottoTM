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

namespace LottoTM.Server.Api.Tests.Features.Tickets.ExportCsv;

/// <summary>
/// Integration tests for the ExportCsv endpoint
/// </summary>
public class EndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public EndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Test successful CSV export with tickets
    /// </summary>
    [Fact]
    public async Task ExportCsv_WithTickets_Returns200OkWithCsvContent()
    {
        // Arrange
        var testDbName = "TestDb_ValidExport_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName, featureFlagEnabled: true);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        // Seed test tickets
        SeedTestTicket(factory, userId, new[] { 5, 14, 23, 29, 37, 41 }, "Group1");
        SeedTestTicket(factory, userId, new[] { 1, 2, 3, 4, 5, 6 }, "Group2");
        SeedTestTicket(factory, userId, new[] { 10, 20, 30, 40, 45, 49 }, "");

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/tickets/export-csv");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
        Assert.NotNull(response.Content.Headers.ContentDisposition?.FileNameStar);
        Assert.Contains(".csv", response.Content.Headers.ContentDisposition?.FileNameStar);

        var csvContent = await response.Content.ReadAsStringAsync();
        Assert.Contains("Number1,Number2,Number3,Number4,Number5,Number6,GroupName", csvContent);
        Assert.Contains("5,14,23,29,37,41,Group1", csvContent);
        Assert.Contains("1,2,3,4,5,6,Group2", csvContent);
        Assert.Contains("10,20,30,40,45,49", csvContent);

        // Verify CSV has exactly 4 lines (header + 3 tickets)
        var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(4, lines.Length);
    }

    /// <summary>
    /// Test CSV export with no tickets returns empty CSV with header only
    /// </summary>
    [Fact]
    public async Task ExportCsv_WithNoTickets_Returns200OkWithHeaderOnly()
    {
        // Arrange
        var testDbName = "TestDb_NoTickets_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName, featureFlagEnabled: true);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/tickets/export-csv");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);

        var csvContent = await response.Content.ReadAsStringAsync();
        Assert.Contains("Number1,Number2,Number3,Number4,Number5,Number6,GroupName", csvContent);

        // Verify CSV has only header
        var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(1, lines.Length);
    }

    /// <summary>
    /// Test CSV export with special characters in GroupName
    /// </summary>
    [Fact]
    public async Task ExportCsv_WithSpecialCharactersInGroupName_ReturnsCorrectCsv()
    {
        // Arrange
        var testDbName = "TestDb_SpecialChars_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName, featureFlagEnabled: true);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        // Seed ticket with special characters
        SeedTestTicket(factory, userId, new[] { 5, 14, 23, 29, 37, 41 }, "Grupa Testowa ĄĘŚ");

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/tickets/export-csv");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var csvContent = await response.Content.ReadAsStringAsync();
        Assert.Contains("Grupa Testowa ĄĘŚ", csvContent);
    }

    /// <summary>
    /// Test CSV export verifies data isolation between users
    /// </summary>
    [Fact]
    public async Task ExportCsv_OnlyExportsCurrentUserTickets()
    {
        // Arrange
        var testDbName = "TestDb_DataIsolation_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName, featureFlagEnabled: true);

        var user1Id = SeedTestUser(testDbName, "user1@example.com", "Password123!", false);
        var user2Id = SeedTestUser(testDbName, "user2@example.com", "Password123!", false);

        // Seed tickets for both users
        SeedTestTicket(factory, user1Id, new[] { 5, 14, 23, 29, 37, 41 }, "User1Ticket");
        SeedTestTicket(factory, user2Id, new[] { 1, 2, 3, 4, 5, 6 }, "User2Ticket");

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, user1Id, "user1@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/tickets/export-csv");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var csvContent = await response.Content.ReadAsStringAsync();

        // Should contain only user1's ticket
        Assert.Contains("User1Ticket", csvContent);
        Assert.DoesNotContain("User2Ticket", csvContent);

        // Verify exactly 2 lines (header + 1 ticket)
        var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
    }

    /// <summary>
    /// Test CSV export verifies numbers are in correct position order
    /// </summary>
    [Fact]
    public async Task ExportCsv_PreservesNumberPositions()
    {
        // Arrange
        var testDbName = "TestDb_PositionOrder_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName, featureFlagEnabled: true);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        // Seed ticket with specific order
        SeedTestTicket(factory, userId, new[] { 49, 1, 30, 2, 40, 3 }, "OrderTest");

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/tickets/export-csv");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var csvContent = await response.Content.ReadAsStringAsync();

        // Verify the numbers are in the exact order they were stored
        Assert.Contains("49,1,30,2,40,3,OrderTest", csvContent);
    }

    /// <summary>
    /// Test CSV export without authentication returns 401
    /// </summary>
    [Fact]
    public async Task ExportCsv_WithoutAuthentication_Returns401Unauthorized()
    {
        // Arrange
        var testDbName = "TestDb_NoAuth_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName, featureFlagEnabled: true);
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/tickets/export-csv");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Test CSV export when feature flag is disabled returns 404
    /// </summary>
    [Fact]
    public async Task ExportCsv_WhenFeatureDisabled_Returns404NotFound()
    {
        // Arrange
        var testDbName = "TestDb_FeatureDisabled_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName, featureFlagEnabled: false);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/tickets/export-csv");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// Test CSV export with many tickets returns correct count
    /// </summary>
    [Fact]
    public async Task ExportCsv_WithManyTickets_ReturnsAllTickets()
    {
        // Arrange
        var testDbName = "TestDb_ManyTickets_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName, featureFlagEnabled: true);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        // Seed 50 tickets
        for (int i = 0; i < 50; i++)
        {
            SeedTestTicket(factory, userId, new[] { 1 + i % 44, 2 + i % 44, 3 + i % 44, 4 + i % 44, 5 + i % 44, 6 + i % 44 }, $"Group{i}");
        }

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/tickets/export-csv");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var csvContent = await response.Content.ReadAsStringAsync();

        // Verify CSV has 51 lines (header + 50 tickets)
        var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(51, lines.Length);
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
    private static void SeedTestTicket(WebApplicationFactory<Program> factory, int userId, int[] numbers, string groupName)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var ticket = new Ticket
        {
            UserId = userId,
            GroupName = groupName,
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
}
