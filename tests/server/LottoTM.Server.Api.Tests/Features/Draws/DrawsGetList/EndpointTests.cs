using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LottoTM.Server.Api.Entities;
using LottoTM.Server.Api.Features.Draws.DrawsGetList;
using LottoTM.Server.Api.Repositories;
using LottoTM.Server.Api.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LottoTM.Server.Api.Tests.Features.Draws.DrawsGetList;

/// <summary>
/// Integration tests for the GetDraws endpoint (GET /api/draws)
/// </summary>
public class EndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public EndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Test successful retrieval of draws with valid parameters
    /// </summary>
    [Fact]
    public async Task GetDraws_WithValidParameters_Returns200WithPaginatedData()
    {
        // Arrange
        var testDbName = "TestDb_GetDraws_Valid_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        // Seed database with a user and draws
        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);
        SeedTestDraws(factory, userId, 5); // Create 5 test draws

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/draws?page=1&pageSize=10&sortBy=drawDate&sortOrder=desc");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<Contracts.Response>();
        Assert.NotNull(result);
        Assert.NotNull(result.Draws);
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(1, result.TotalPages);
        Assert.Equal(5, result.Draws.Count);

        // Verify each draw has 6 numbers
        foreach (var draw in result.Draws)
        {
            Assert.Equal(6, draw.Numbers.Length);
        }
    }

    /// <summary>
    /// Test retrieval with default parameters
    /// </summary>
    [Fact]
    public async Task GetDraws_WithDefaultParameters_Returns200()
    {
        // Arrange
        var testDbName = "TestDb_GetDraws_Default_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);
        SeedTestDraws(factory, userId, 3);

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act - no query parameters, should use defaults
        var response = await client.GetAsync("/api/draws");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<Contracts.Response>();
        Assert.NotNull(result);
        Assert.Equal(1, result.Page); // Default page
        Assert.Equal(20, result.PageSize); // Default pageSize
    }

    /// <summary>
    /// Test pagination with page 2
    /// </summary>
    [Fact]
    public async Task GetDraws_WithPage2_ReturnsSecondPage()
    {
        // Arrange
        var testDbName = "TestDb_GetDraws_Page2_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);
        SeedTestDraws(factory, userId, 25); // Create 25 draws

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/draws?page=2&pageSize=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<Contracts.Response>();
        Assert.NotNull(result);
        Assert.Equal(2, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(3, result.TotalPages); // 25 / 10 = 3 pages
        Assert.Equal(10, result.Draws.Count);
    }

    /// <summary>
    /// Test sorting by createdAt ascending
    /// </summary>
    [Fact]
    public async Task GetDraws_WithSortByCreatedAtAsc_ReturnsCorrectOrder()
    {
        // Arrange
        var testDbName = "TestDb_GetDraws_Sort_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);
        SeedTestDraws(factory, userId, 3);

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/draws?sortBy=createdAt&sortOrder=asc");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<Contracts.Response>();
        Assert.NotNull(result);
        Assert.True(result.Draws.Count > 0);

        // Verify ascending order by CreatedAt
        for (int i = 0; i < result.Draws.Count - 1; i++)
        {
            Assert.True(result.Draws[i].CreatedAt <= result.Draws[i + 1].CreatedAt);
        }
    }

    /// <summary>
    /// Test with pageSize greater than 100 returns 400
    /// </summary>
    [Fact]
    public async Task GetDraws_WithPageSizeGreaterThan100_Returns400()
    {
        // Arrange
        var testDbName = "TestDb_GetDraws_PageSizeTooLarge_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/draws?pageSize=101");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Test with page less than 1 returns 400
    /// </summary>
    [Fact]
    public async Task GetDraws_WithPageLessThan1_Returns400()
    {
        // Arrange
        var testDbName = "TestDb_GetDraws_PageTooSmall_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/draws?page=0");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Test with invalid sortBy parameter returns 400
    /// </summary>
    [Fact]
    public async Task GetDraws_WithInvalidSortBy_Returns400()
    {
        // Arrange
        var testDbName = "TestDb_GetDraws_InvalidSortBy_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/draws?sortBy=invalidField");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Test with invalid sortOrder parameter returns 400
    /// </summary>
    [Fact]
    public async Task GetDraws_WithInvalidSortOrder_Returns400()
    {
        // Arrange
        var testDbName = "TestDb_GetDraws_InvalidSortOrder_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/draws?sortOrder=invalid");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Test without authentication returns 401
    /// </summary>
    [Fact]
    public async Task GetDraws_WithoutAuthentication_Returns401()
    {
        // Arrange
        var testDbName = "TestDb_GetDraws_NoAuth_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);
        var client = factory.CreateClient();

        // Act - no authorization header
        var response = await client.GetAsync("/api/draws");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Test with empty database returns empty list
    /// </summary>
    [Fact]
    public async Task GetDraws_WithEmptyDatabase_ReturnsEmptyList()
    {
        // Arrange
        var testDbName = "TestDb_GetDraws_Empty_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/draws");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<Contracts.Response>();
        Assert.NotNull(result);
        Assert.Empty(result.Draws);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.TotalPages);
    }

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
    /// Helper method to seed multiple test draws into the database
    /// </summary>
    private static void SeedTestDraws(WebApplicationFactory<Program> factory, int userId, int count)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var baseDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-count));

        for (int i = 0; i < count; i++)
        {
            var draw = new Draw
            {
                DrawDate = baseDate.AddDays(i),
                LottoType = "LOTTO",
                CreatedAt = DateTime.UtcNow.AddMinutes(i), // Stagger creation times for sorting tests
                CreatedByUserId = userId
            };

            dbContext.Draws.Add(draw);
            dbContext.SaveChanges();

            // Add 6 numbers for each draw
            var drawNumbers = new[]
            {
                new DrawNumber { DrawId = draw.Id, Number = 3 + i, Position = 1 },
                new DrawNumber { DrawId = draw.Id, Number = 12 + i, Position = 2 },
                new DrawNumber { DrawId = draw.Id, Number = 25 + i, Position = 3 },
                new DrawNumber { DrawId = draw.Id, Number = 31 + i, Position = 4 },
                new DrawNumber { DrawId = draw.Id, Number = 42 + i % 7, Position = 5 },
                new DrawNumber { DrawId = draw.Id, Number = 48 + i % 2, Position = 6 }
            };

            dbContext.DrawNumbers.AddRange(drawNumbers);
            dbContext.SaveChanges();
        }
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
