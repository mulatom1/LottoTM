using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using LottoTM.Server.Api.Entities;
using LottoTM.Server.Api.Repositories;
using LottoTM.Server.Api.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LottoTM.Server.Api.Tests.Features.XLotto.IsEnabled;

/// <summary>
/// Integration tests for the XLotto IsEnabled endpoint
/// </summary>
public class EndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public EndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Test successful retrieval when feature is enabled
    /// </summary>
    [Fact]
    public async Task GetIsEnabled_WhenFeatureEnabled_Returns200WithTrue()
    {
        // Arrange
        var testDbName = "TestDb_IsEnabledTrue_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName, featureEnabled: true);
        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/xlotto/is-enabled");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(result.GetProperty("success").GetBoolean());
        Assert.True(result.GetProperty("data").GetBoolean());
    }

    /// <summary>
    /// Test successful retrieval when feature is disabled
    /// </summary>
    [Fact]
    public async Task GetIsEnabled_WhenFeatureDisabled_Returns200WithFalse()
    {
        // Arrange
        var testDbName = "TestDb_IsEnabledFalse_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName, featureEnabled: false);
        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/xlotto/is-enabled");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(result.GetProperty("success").GetBoolean());
        Assert.False(result.GetProperty("data").GetBoolean());
    }

    /// <summary>
    /// Test that endpoint requires authentication
    /// </summary>
    [Fact]
    public async Task GetIsEnabled_WithoutAuthentication_Returns401Unauthorized()
    {
        // Arrange
        var testDbName = "TestDb_IsEnabledNoAuth_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName, featureEnabled: true);
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/xlotto/is-enabled");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Test that endpoint works for admin users
    /// </summary>
    [Fact]
    public async Task GetIsEnabled_WithAdminUser_Returns200()
    {
        // Arrange
        var testDbName = "TestDb_IsEnabledAdmin_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName, featureEnabled: true);
        var userId = SeedTestUser(testDbName, "admin@example.com", "Password123!", true);

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "admin@example.com", true);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/xlotto/is-enabled");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(result.GetProperty("success").GetBoolean());
        Assert.True(result.GetProperty("data").GetBoolean());
    }

    /// <summary>
    /// Test that endpoint works for non-admin users (no admin permission required)
    /// </summary>
    [Fact]
    public async Task GetIsEnabled_WithNonAdminUser_Returns200()
    {
        // Arrange
        var testDbName = "TestDb_IsEnabledNonAdmin_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName, featureEnabled: true);
        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/xlotto/is-enabled");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(result.GetProperty("success").GetBoolean());
    }

    /// <summary>
    /// Test that endpoint response structure is correct
    /// </summary>
    [Fact]
    public async Task GetIsEnabled_ResponseStructure_IsCorrect()
    {
        // Arrange
        var testDbName = "TestDb_IsEnabledStructure_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName, featureEnabled: true);
        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/xlotto/is-enabled");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Verify structure
        Assert.True(result.TryGetProperty("success", out var success));
        Assert.Equal(JsonValueKind.True, success.ValueKind);

        Assert.True(result.TryGetProperty("data", out var data));
        Assert.True(data.ValueKind == JsonValueKind.True || data.ValueKind == JsonValueKind.False);
    }


    /// <summary>
    /// Test multiple consecutive calls return consistent results
    /// </summary>
    [Fact]
    public async Task GetIsEnabled_MultipleCalls_ReturnConsistentResults()
    {
        // Arrange
        var testDbName = "TestDb_IsEnabledConsistency_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName, featureEnabled: true);
        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response1 = await client.GetAsync("/api/xlotto/is-enabled");
        var response2 = await client.GetAsync("/api/xlotto/is-enabled");
        var response3 = await client.GetAsync("/api/xlotto/is-enabled");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response3.StatusCode);

        var result1 = await response1.Content.ReadFromJsonAsync<JsonElement>();
        var result2 = await response2.Content.ReadFromJsonAsync<JsonElement>();
        var result3 = await response3.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(result1.GetProperty("data").GetBoolean(), result2.GetProperty("data").GetBoolean());
        Assert.Equal(result2.GetProperty("data").GetBoolean(), result3.GetProperty("data").GetBoolean());
    }

    /// <summary>
    /// Creates a test factory with in-memory database and feature flag configuration
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
                    ["GoogleGemini:ApiKey"] = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("test-api-key")),
                    ["GoogleGemini:Model"] = "gemini-2.0-flash",
                    ["GoogleGemini:Enable"] = featureEnabled.ToString(),
                    ["Swagger:Enabled"] = "false"
                });
            });

            builder.ConfigureServices(services =>
            {
                // Remove existing DbContext
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
    /// Helper method to generate JWT token for testing
    /// </summary>
    private static string GenerateTestToken(WebApplicationFactory<Program> factory, int userId, string email, bool isAdmin)
    {
        using var scope = factory.Services.CreateScope();
        var jwtService = scope.ServiceProvider.GetRequiredService<IJwtService>();
        return jwtService.GenerateToken(userId, email, isAdmin, out _);
    }
}
