using System.Net;
using System.Net.Http.Headers;
using LottoTM.Server.Api.Entities;
using LottoTM.Server.Api.Repositories;
using LottoTM.Server.Api.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text;

namespace LottoTM.Server.Api.Tests.Features.Draws.DrawsExportCsv;

/// <summary>
/// Integration tests for the DrawsExportCsv endpoint
/// </summary>
public class EndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public EndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Test successful CSV export with draws in database
    /// </summary>
    [Fact]
    public async Task ExportDrawsCsv_WithDrawsInDatabase_Returns200OK()
    {
        // Arrange
        var testDbName = "TestDb_ValidExport_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName, true); // Feature enabled

        var userId = SeedTestUser(testDbName, "admin@example.com", "Password123!", true);

        // Seed test draws
        SeedTestDraw(factory, userId, DateOnly.Parse("2025-01-15"), "LOTTO", new[] { 3, 12, 25, 31, 42, 48 });
        SeedTestDraw(factory, userId, DateOnly.Parse("2025-01-16"), "LOTTO", new[] { 5, 10, 15, 20, 25, 30 });

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "admin@example.com", true);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/draws/export-csv");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);

        var csvContent = await response.Content.ReadAsStringAsync();
        Assert.NotEmpty(csvContent);

        // Verify CSV structure
        var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.True(lines.Length >= 3); // Header + 2 draws

        // Verify header
        var header = lines[0].Trim();
        Assert.Contains("DrawDate", header);
        Assert.Contains("LottoType", header);
        Assert.Contains("DrawSystemId", header);
        Assert.Contains("Number1", header);
        Assert.Contains("Number6", header);

        // Verify data rows
        Assert.Contains("2025-01-15", lines[1]);
        Assert.Contains("2025-01-16", lines[2]);
    }

    /// <summary>
    /// Test CSV export with empty database
    /// </summary>
    [Fact]
    public async Task ExportDrawsCsv_WithEmptyDatabase_ReturnsHeaderOnly()
    {
        // Arrange
        var testDbName = "TestDb_EmptyExport_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName, true);

        var userId = SeedTestUser(testDbName, "admin@example.com", "Password123!", true);

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "admin@example.com", true);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/draws/export-csv");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var csvContent = await response.Content.ReadAsStringAsync();
        var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // Should only have header line
        Assert.Single(lines);
        Assert.Contains("DrawDate", lines[0]);
    }

    /// <summary>
    /// Test CSV export with complete draw data including win pools
    /// </summary>
    [Fact]
    public async Task ExportDrawsCsv_WithCompleteDrawData_ExportsAllFields()
    {
        // Arrange
        var testDbName = "TestDb_CompleteExport_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName, true);

        var userId = SeedTestUser(testDbName, "admin@example.com", "Password123!", true);

        // Seed draw with complete data
        SeedCompleteTestDraw(factory, userId, DateOnly.Parse("2025-01-15"), "LOTTO",
            new[] { 3, 12, 25, 31, 42, 48 }, 3.00m, 1, 1000000m);

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "admin@example.com", true);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/draws/export-csv");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var csvContent = await response.Content.ReadAsStringAsync();
        var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // Verify complete data in CSV
        Assert.Contains("3.00", lines[1]); // TicketPrice
        Assert.Contains("1000000", lines[1]); // WinPoolAmount1
    }

    /// <summary>
    /// Test CSV export when feature is disabled
    /// </summary>
    [Fact]
    public async Task ExportDrawsCsv_WhenFeatureDisabled_Returns404NotFound()
    {
        // Arrange
        var testDbName = "TestDb_FeatureDisabled_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName, false); // Feature disabled

        var userId = SeedTestUser(testDbName, "admin@example.com", "Password123!", true);

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "admin@example.com", true);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/draws/export-csv");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// Test CSV export without authentication
    /// </summary>
    [Fact]
    public async Task ExportDrawsCsv_WithoutAuthentication_Returns401Unauthorized()
    {
        // Arrange
        var testDbName = "TestDb_NoAuth_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName, true);
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/draws/export-csv");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Test CSV export with multiple draw types
    /// </summary>
    [Fact]
    public async Task ExportDrawsCsv_WithMultipleDrawTypes_ExportsAllTypes()
    {
        // Arrange
        var testDbName = "TestDb_MultipleTypes_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName, true);

        var userId = SeedTestUser(testDbName, "admin@example.com", "Password123!", true);

        // Seed draws with different types
        SeedTestDraw(factory, userId, DateOnly.Parse("2025-01-15"), "LOTTO", new[] { 3, 12, 25, 31, 42, 48 });
        SeedTestDraw(factory, userId, DateOnly.Parse("2025-01-15"), "LOTTO PLUS", new[] { 5, 10, 15, 20, 25, 30 });

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "admin@example.com", true);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/draws/export-csv");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var csvContent = await response.Content.ReadAsStringAsync();

        // Verify both types are exported
        Assert.Contains("LOTTO", csvContent);
        Assert.Contains("LOTTO PLUS", csvContent);
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
    /// Helper method to seed test user into database
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
    /// Helper method to seed basic test draw
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
    /// Helper method to seed complete test draw with all fields
    /// </summary>
    private static void SeedCompleteTestDraw(WebApplicationFactory<Program> factory, int userId,
        DateOnly drawDate, string lottoType, int[] numbers, decimal ticketPrice, int winPoolCount1, decimal winPoolAmount1)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var draw = new Draw
        {
            DrawDate = drawDate,
            LottoType = lottoType,
            DrawSystemId = 7000,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = userId,
            TicketPrice = ticketPrice,
            WinPoolCount1 = winPoolCount1,
            WinPoolAmount1 = winPoolAmount1
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
}
