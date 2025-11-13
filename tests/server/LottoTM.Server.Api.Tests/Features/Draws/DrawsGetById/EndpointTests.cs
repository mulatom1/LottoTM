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

namespace LottoTM.Server.Api.Tests.Features.Draws.DrawsGetById;

/// <summary>
/// Integration tests for the GetDrawById endpoint
/// </summary>
public class EndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public EndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Test successful retrieval of a draw by valid ID
    /// </summary>
    [Fact]
    public async Task GetDrawById_WithValidId_Returns200AndDrawDetails()
    {
        // Arrange
        var testDbName = "TestDb_GetValidDraw_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        // Seed database with a user and a draw
        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);
        var drawId = SeedTestDraw(factory, userId, DateOnly.Parse("2025-01-15"), new[] { 3, 12, 25, 31, 42, 48 });

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync($"/api/draws/{drawId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<DrawResponse>();
        Assert.NotNull(result);
        Assert.Equal(drawId, result.Id);
        Assert.Equal(6, result.Numbers.Length);
        Assert.Equal(new[] { 3, 12, 25, 31, 42, 48 }, result.Numbers);
        Assert.NotEqual(default(DateTime), result.CreatedAt);
    }

    /// <summary>
    /// Test retrieval of non-existent draw returns 404
    /// </summary>
    [Fact]
    public async Task GetDrawById_WithNonExistentId_Returns404NotFound()
    {
        // Arrange
        var testDbName = "TestDb_NonExistent_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/draws/99999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.NotNull(result);
        Assert.Contains("detail", result.Keys);
        Assert.Equal("Losowanie o podanym ID nie istnieje", result["detail"]);
    }

    /// <summary>
    /// Test retrieval without authentication returns 401
    /// </summary>
    [Fact]
    public async Task GetDrawById_WithoutAuthentication_Returns401Unauthorized()
    {
        // Arrange
        var testDbName = "TestDb_NoAuth_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/draws/1");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Test retrieval with invalid ID (zero) returns 400
    /// </summary>
    [Fact]
    public async Task GetDrawById_WithInvalidIdZero_Returns400BadRequest()
    {
        // Arrange
        var testDbName = "TestDb_InvalidId_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/draws/0");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Test retrieval with negative ID returns 400
    /// </summary>
    [Fact]
    public async Task GetDrawById_WithNegativeId_Returns400BadRequest()
    {
        // Arrange
        var testDbName = "TestDb_NegativeId_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/draws/-1");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Test that numbers are returned in correct order by position
    /// </summary>
    [Fact]
    public async Task GetDrawById_ReturnsNumbersInCorrectOrder()
    {
        // Arrange
        var testDbName = "TestDb_NumberOrder_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        // Create draw with numbers not in natural order
        var drawId = SeedTestDraw(factory, userId, DateOnly.Parse("2025-01-20"), new[] { 48, 31, 3, 42, 12, 25 });

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync($"/api/draws/{drawId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<DrawResponse>();
        Assert.NotNull(result);
        // Numbers should be in the order they were inserted (by position)
        Assert.Equal(new[] { 48, 31, 3, 42, 12, 25 }, result.Numbers);
    }

    /// <summary>
    /// Response DTO for GetDrawById endpoint
    /// </summary>
    private record DrawResponse(int Id, DateTime DrawDate, int[] Numbers, DateTime CreatedAt);

    /// <summary>
    /// Creates a test factory with in-memory database
    /// </summary>
    private WebApplicationFactory<Program> CreateTestFactory(string databaseName)
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.Sources.Clear();

                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = "U2VydmVyPWR1bW15O0RhdGFiYXNlPWR1bW15O0ludGVncmF0ZWQgU2VjdXJpdHk9VHJ1ZTs=",
                    ["Jwt:Key"] = "ThisIsASecretKeyForTestingPurposesOnly123456",
                    ["Jwt:Issuer"] = "TestIssuer",
                    ["Jwt:Audience"] = "TestAudience",
                    ["Jwt:ExpiryInMinutes"] = "1440",
                    ["Swagger:Enabled"] = "false"
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

        // Check if user already exists
        var existingUser = dbContext.Users.FirstOrDefault(u => u.Email == email);
        if (existingUser != null)
        {
            return existingUser.Id;
        }

        // Create user with hashed password using BCrypt
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
    /// Helper method to seed test draw into an in-memory database
    /// </summary>
    private static int SeedTestDraw(WebApplicationFactory<Program> factory, int userId, DateOnly drawDate, int[] numbers)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var draw = new Draw
        {
            DrawDate = drawDate,
            LottoType = "LOTTO",
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

        return draw.Id;
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
