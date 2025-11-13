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

namespace LottoTM.Server.Api.Tests.Features.Draws.DrawsUpdate;

/// <summary>
/// Integration tests for the UpdateDraw endpoint
/// </summary>
public class EndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public EndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Test successful draw update with valid data by admin
    /// </summary>
    [Fact]
    public async Task UpdateDraw_WithValidDataAndAdmin_Returns200OK()
    {
        // Arrange
        var testDbName = "TestDb_ValidUpdate_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        // Seed database with an admin user
        var adminUserId = SeedTestUser(testDbName, "admin@example.com", "Password123!", true);

        // Seed existing draw
        var drawId = SeedTestDraw(factory, adminUserId, DateOnly.Parse("2025-01-15"), new[] { 1, 2, 3, 4, 5, 6 });

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, adminUserId, "admin@example.com", true);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new
        {
            lottoType = "LOTTO",
            drawDate = "2025-01-20",
            numbers = new[] { 3, 12, 25, 31, 42, 48 }
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/draws/{drawId}", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.NotNull(result);
        Assert.Equal("Losowanie zaktualizowane pomyślnie", result["message"]);

        // Verify in database
        VerifyDrawInDatabase(factory, drawId, DateOnly.Parse("2025-01-20"), new[] { 3, 12, 25, 31, 42, 48 });
    }

    /// <summary>
    /// Test draw update without authentication returns 401
    /// </summary>
    [Fact]
    public async Task UpdateDraw_WithoutAuthentication_Returns401Unauthorized()
    {
        // Arrange
        var testDbName = "TestDb_NoAuth_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);
        var client = factory.CreateClient();

        var request = new
        {
            drawDate = "2025-01-20",
            numbers = new[] { 3, 12, 25, 31, 42, 48 }
        };

        // Act
        var response = await client.PutAsJsonAsync("/api/draws/1", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Test draw update by non-admin user returns 403
    /// </summary>
    [Fact]
    public async Task UpdateDraw_WithNonAdminUser_Returns403Forbidden()
    {
        // Arrange
        var testDbName = "TestDb_NonAdmin_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        // Seed regular user (not admin)
        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        // Seed admin user and draw
        var adminUserId = SeedTestUser(testDbName, "admin@example.com", "Password123!", true);
        var drawId = SeedTestDraw(factory, adminUserId, DateOnly.Parse("2025-01-15"), new[] { 1, 2, 3, 4, 5, 6 });

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new
        {
            lottoType = "LOTTO",
            drawDate = "2025-01-20",
            numbers = new[] { 3, 12, 25, 31, 42, 48 }
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/draws/{drawId}", request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>
    /// Test draw update with non-existent ID returns 404
    /// </summary>
    [Fact]
    public async Task UpdateDraw_WithNonExistentId_Returns404NotFound()
    {
        // Arrange
        var testDbName = "TestDb_NotFound_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var adminUserId = SeedTestUser(testDbName, "admin@example.com", "Password123!", true);

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, adminUserId, "admin@example.com", true);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new
        {
            lottoType = "LOTTO",
            drawDate = "2025-01-20",
            numbers = new[] { 3, 12, 25, 31, 42, 48 }
        };

        // Act
        var response = await client.PutAsJsonAsync("/api/draws/99999", request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// Test draw update with future date returns 400
    /// </summary>
    [Fact]
    public async Task UpdateDraw_WithFutureDate_Returns400BadRequest()
    {
        // Arrange
        var testDbName = "TestDb_FutureDate_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var adminUserId = SeedTestUser(testDbName, "admin@example.com", "Password123!", true);
        var drawId = SeedTestDraw(factory, adminUserId, DateOnly.Parse("2025-01-15"), new[] { 1, 2, 3, 4, 5, 6 });

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, adminUserId, "admin@example.com", true);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new
        {
            drawDate = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd"),
            numbers = new[] { 3, 12, 25, 31, 42, 48 }
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/draws/{drawId}", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Test draw update with invalid number count returns 400
    /// </summary>
    [Fact]
    public async Task UpdateDraw_WithInvalidNumberCount_Returns400BadRequest()
    {
        // Arrange
        var testDbName = "TestDb_InvalidCount_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var adminUserId = SeedTestUser(testDbName, "admin@example.com", "Password123!", true);
        var drawId = SeedTestDraw(factory, adminUserId, DateOnly.Parse("2025-01-15"), new[] { 1, 2, 3, 4, 5, 6 });

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, adminUserId, "admin@example.com", true);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new
        {
            drawDate = "2025-01-20",
            numbers = new[] { 3, 12, 25, 31, 42 } // tylko 5 liczb
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/draws/{drawId}", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Test draw update with numbers out of range returns 400
    /// </summary>
    [Fact]
    public async Task UpdateDraw_WithNumbersOutOfRange_Returns400BadRequest()
    {
        // Arrange
        var testDbName = "TestDb_OutOfRange_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var adminUserId = SeedTestUser(testDbName, "admin@example.com", "Password123!", true);
        var drawId = SeedTestDraw(factory, adminUserId, DateOnly.Parse("2025-01-15"), new[] { 1, 2, 3, 4, 5, 6 });

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, adminUserId, "admin@example.com", true);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new
        {
            drawDate = "2025-01-20",
            numbers = new[] { 0, 12, 25, 31, 42, 48 } // 0 poza zakresem
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/draws/{drawId}", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Test draw update with duplicate numbers returns 400
    /// </summary>
    [Fact]
    public async Task UpdateDraw_WithDuplicateNumbers_Returns400BadRequest()
    {
        // Arrange
        var testDbName = "TestDb_Duplicates_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var adminUserId = SeedTestUser(testDbName, "admin@example.com", "Password123!", true);
        var drawId = SeedTestDraw(factory, adminUserId, DateOnly.Parse("2025-01-15"), new[] { 1, 2, 3, 4, 5, 6 });

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, adminUserId, "admin@example.com", true);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new
        {
            drawDate = "2025-01-20",
            numbers = new[] { 3, 3, 25, 31, 42, 48 } // duplikat 3
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/draws/{drawId}", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Test draw update with duplicate date returns 400
    /// </summary>
    [Fact]
    public async Task UpdateDraw_WithDuplicateDate_Returns400BadRequest()
    {
        // Arrange
        var testDbName = "TestDb_DuplicateDate_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var adminUserId = SeedTestUser(testDbName, "admin@example.com", "Password123!", true);

        // Seed two existing draws
        var drawId1 = SeedTestDraw(factory, adminUserId, DateOnly.Parse("2025-01-15"), new[] { 1, 2, 3, 4, 5, 6 });
        var drawId2 = SeedTestDraw(factory, adminUserId, DateOnly.Parse("2025-01-20"), new[] { 7, 8, 9, 10, 11, 12 });

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, adminUserId, "admin@example.com", true);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Try to update drawId1 to the same date as drawId2
        var request = new
        {
            drawDate = "2025-01-20", // już istnieje w drawId2
            numbers = new[] { 3, 12, 25, 31, 42, 48 }
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/draws/{drawId1}", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Test draw update with same date (no change) is allowed
    /// </summary>
    [Fact]
    public async Task UpdateDraw_WithSameDate_Returns200OK()
    {
        // Arrange
        var testDbName = "TestDb_SameDate_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var adminUserId = SeedTestUser(testDbName, "admin@example.com", "Password123!", true);
        var drawId = SeedTestDraw(factory, adminUserId, DateOnly.Parse("2025-01-15"), new[] { 1, 2, 3, 4, 5, 6 });

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, adminUserId, "admin@example.com", true);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new
        {
            lottoType = "LOTTO",
            drawDate = "2025-01-15", // ta sama data
            numbers = new[] { 3, 12, 25, 31, 42, 48 } // tylko liczby się zmieniają
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/draws/{drawId}", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
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

    /// <summary>
    /// Helper method to verify draw was updated in database
    /// </summary>
    private static void VerifyDrawInDatabase(WebApplicationFactory<Program> factory, int drawId, DateOnly drawDate, int[] expectedNumbers)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var draw = dbContext.Draws
            .Include(d => d.Numbers)
            .FirstOrDefault(d => d.Id == drawId);

        Assert.NotNull(draw);
        Assert.Equal(drawDate, draw.DrawDate);
        Assert.Equal(6, draw.Numbers.Count);

        var actualNumbers = draw.Numbers.OrderBy(n => n.Position).Select(n => n.Number).ToArray();
        Assert.Equal(expectedNumbers, actualNumbers);
    }
}
