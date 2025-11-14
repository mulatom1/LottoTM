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
using Moq;

namespace LottoTM.Server.Api.Tests.Features.XLotto.ActualDraws;

/// <summary>
/// Integration tests for the XLotto ActualDraws endpoint
/// </summary>
public class EndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public EndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Test successful retrieval of actual draws with valid authentication and parameters
    /// </summary>
    [Fact]
    public async Task GetActualDraws_WithValidParameters_Returns200WithJsonData()
    {
        // Arrange
        var testDbName = "TestDb_ValidActualDraws_" + Guid.NewGuid();
        var mockXLottoService = new Mock<IXLottoService>();
        var expectedJsonData = "{\"Data\":[{\"DrawDate\":\"2025-01-15\",\"GameType\":\"LOTTO\",\"Numbers\":[3,12,25,31,42,48]}]}";
        
        mockXLottoService
            .Setup(s => s.GetActualDraws(It.IsAny<DateTime?>(), It.IsAny<string>()))
            .ReturnsAsync(expectedJsonData);

        var factory = CreateTestFactory(testDbName, mockXLottoService.Object);
        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/xlotto/actual-draws?date=2025-01-15&lottoType=LOTTO");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(result.GetProperty("success").GetBoolean());
        Assert.True(result.TryGetProperty("data", out var data));
        Assert.NotEqual(default(JsonElement), data);
        
        mockXLottoService.Verify(s => s.GetActualDraws(It.IsAny<DateTime?>(), "LOTTO"), Times.Once);
    }

    /// <summary>
    /// Test retrieval with default parameters (current date and LOTTO type)
    /// </summary>
    [Fact]
    public async Task GetActualDraws_WithDefaultParameters_Returns200()
    {
        // Arrange
        var testDbName = "TestDb_DefaultParams_" + Guid.NewGuid();
        var mockXLottoService = new Mock<IXLottoService>();
        var expectedJsonData = "{\"Data\":[{\"DrawDate\":\"2025-11-14\",\"GameType\":\"LOTTO\",\"Numbers\":[1,5,15,25,35,45]}]}";
        
        mockXLottoService
            .Setup(s => s.GetActualDraws(It.IsAny<DateTime?>(), It.IsAny<string>()))
            .ReturnsAsync(expectedJsonData);

        var factory = CreateTestFactory(testDbName, mockXLottoService.Object);
        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var today = DateTime.Today;
        var response = await client.GetAsync($"/api/xlotto/actual-draws?date={today:yyyy-MM-dd}&lottoType=LOTTO");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(result.GetProperty("success").GetBoolean());
    }

    /// <summary>
    /// Test retrieval for LOTTO PLUS game type
    /// </summary>
    [Fact]
    public async Task GetActualDraws_WithLottoPlusType_Returns200()
    {
        // Arrange
        var testDbName = "TestDb_LottoPlus_" + Guid.NewGuid();
        var mockXLottoService = new Mock<IXLottoService>();
        var expectedJsonData = "{\"Data\":[{\"DrawDate\":\"2025-01-15\",\"GameType\":\"LOTTO PLUS\",\"Numbers\":[2,8,18,28,38,48]}]}";
        
        mockXLottoService
            .Setup(s => s.GetActualDraws(It.IsAny<DateTime?>(), "LOTTO PLUS"))
            .ReturnsAsync(expectedJsonData);

        var factory = CreateTestFactory(testDbName, mockXLottoService.Object);
        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/xlotto/actual-draws?date=2025-01-15&lottoType=LOTTO%20PLUS");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        mockXLottoService.Verify(s => s.GetActualDraws(It.IsAny<DateTime?>(), "LOTTO PLUS"), Times.Once);
    }

    /// <summary>
    /// Test retrieval without authentication returns 401
    /// </summary>
    [Fact]
    public async Task GetActualDraws_WithoutAuthentication_Returns401Unauthorized()
    {
        // Arrange
        var testDbName = "TestDb_NoAuth_" + Guid.NewGuid();
        var mockXLottoService = new Mock<IXLottoService>();
        var factory = CreateTestFactory(testDbName, mockXLottoService.Object);
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/xlotto/actual-draws?date=2025-01-15&lottoType=LOTTO");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        mockXLottoService.Verify(s => s.GetActualDraws(It.IsAny<DateTime?>(), It.IsAny<string>()), Times.Never);
    }

    /// <summary>
    /// Test retrieval when service returns empty data
    /// </summary>
    [Fact]
    public async Task GetActualDraws_WithNoResultsFound_Returns200WithEmptyData()
    {
        // Arrange
        var testDbName = "TestDb_NoResults_" + Guid.NewGuid();
        var mockXLottoService = new Mock<IXLottoService>();
        var expectedJsonData = "{\"Data\":[]}";
        
        mockXLottoService
            .Setup(s => s.GetActualDraws(It.IsAny<DateTime?>(), It.IsAny<string>()))
            .ReturnsAsync(expectedJsonData);

        var factory = CreateTestFactory(testDbName, mockXLottoService.Object);
        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/xlotto/actual-draws?date=2025-01-15&lottoType=LOTTO");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(result.GetProperty("success").GetBoolean());
    }

    /// <summary>
    /// Test retrieval when service throws an exception returns 500
    /// </summary>
    [Fact]
    public async Task GetActualDraws_WhenServiceThrowsException_Returns500()
    {
        // Arrange
        var testDbName = "TestDb_ServiceError_" + Guid.NewGuid();
        var mockXLottoService = new Mock<IXLottoService>();
        
        mockXLottoService
            .Setup(s => s.GetActualDraws(It.IsAny<DateTime?>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Failed to fetch draw results from XLotto"));

        var factory = CreateTestFactory(testDbName, mockXLottoService.Object);
        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/xlotto/actual-draws?date=2025-01-15&lottoType=LOTTO");

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    /// <summary>
    /// Test retrieval with admin user permissions
    /// </summary>
    [Fact]
    public async Task GetActualDraws_WithAdminUser_Returns200()
    {
        // Arrange
        var testDbName = "TestDb_AdminUser_" + Guid.NewGuid();
        var mockXLottoService = new Mock<IXLottoService>();
        var expectedJsonData = "{\"Data\":[{\"DrawDate\":\"2025-01-15\",\"GameType\":\"LOTTO\",\"Numbers\":[1,2,3,4,5,6]}]}";
        
        mockXLottoService
            .Setup(s => s.GetActualDraws(It.IsAny<DateTime?>(), It.IsAny<string>()))
            .ReturnsAsync(expectedJsonData);

        var factory = CreateTestFactory(testDbName, mockXLottoService.Object);
        var userId = SeedTestUser(testDbName, "admin@example.com", "Password123!", true);

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "admin@example.com", true);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/xlotto/actual-draws?date=2025-01-15&lottoType=LOTTO");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(result.GetProperty("success").GetBoolean());
    }

    /// <summary>
    /// Test retrieval when GoogleGemini feature is disabled returns empty data
    /// </summary>
    [Fact]
    public async Task GetActualDraws_WhenFeatureDisabled_Returns200WithEmptyData()
    {
        // Arrange
        var testDbName = "TestDb_FeatureDisabled_" + Guid.NewGuid();
        var mockXLottoService = new Mock<IXLottoService>();

        // Service should NOT be called when feature is disabled
        mockXLottoService
            .Setup(s => s.GetActualDraws(It.IsAny<DateTime?>(), It.IsAny<string>()))
            .ReturnsAsync("{\"Data\":[{\"DrawDate\":\"2025-01-15\",\"GameType\":\"LOTTO\",\"Numbers\":[1,2,3,4,5,6]}]}");

        var factory = CreateTestFactory(testDbName, mockXLottoService.Object, featureEnabled: false);
        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/xlotto/actual-draws?date=2025-01-15&lottoType=LOTTO");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(result.GetProperty("success").GetBoolean());

        var data = result.GetProperty("data").GetString();
        Assert.NotNull(data);
        Assert.Contains("\"Data\":[]", data); // Should return empty data

        // Verify service was NOT called
        mockXLottoService.Verify(s => s.GetActualDraws(It.IsAny<DateTime?>(), It.IsAny<string>()), Times.Never);
    }

    /// <summary>
    /// Test retrieval when GoogleGemini feature is enabled calls service
    /// </summary>
    [Fact]
    public async Task GetActualDraws_WhenFeatureEnabled_CallsService()
    {
        // Arrange
        var testDbName = "TestDb_FeatureEnabled_" + Guid.NewGuid();
        var mockXLottoService = new Mock<IXLottoService>();
        var expectedJsonData = "{\"Data\":[{\"DrawDate\":\"2025-01-15\",\"GameType\":\"LOTTO\",\"Numbers\":[1,2,3,4,5,6]}]}";

        mockXLottoService
            .Setup(s => s.GetActualDraws(It.IsAny<DateTime?>(), It.IsAny<string>()))
            .ReturnsAsync(expectedJsonData);

        var factory = CreateTestFactory(testDbName, mockXLottoService.Object, featureEnabled: true);
        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/xlotto/actual-draws?date=2025-01-15&lottoType=LOTTO");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(result.GetProperty("success").GetBoolean());

        // Verify service WAS called
        mockXLottoService.Verify(s => s.GetActualDraws(It.IsAny<DateTime?>(), It.IsAny<string>()), Times.Once);
    }

    /// <summary>
    /// Creates a test factory with in-memory database and mocked XLotto service
    /// </summary>
    private WebApplicationFactory<Program> CreateTestFactory(string databaseName, IXLottoService? xLottoService = null, bool featureEnabled = true)
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

                // Replace XLottoService with mock if provided
                if (xLottoService != null)
                {
                    var xLottoServiceDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IXLottoService));
                    if (xLottoServiceDescriptor != null)
                    {
                        services.Remove(xLottoServiceDescriptor);
                    }
                    services.AddSingleton(xLottoService);
                }
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
