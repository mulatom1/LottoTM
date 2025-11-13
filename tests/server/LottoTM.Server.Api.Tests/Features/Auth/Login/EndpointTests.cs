using System.Net;
using System.Net.Http.Json;
using LottoTM.Server.Api.Entities;
using LottoTM.Server.Api.Features.Auth.Login;
using LottoTM.Server.Api.Repositories;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LottoTM.Server.Api.Tests.Features.Auth.Login.Tests;

/// <summary>
/// Integration tests for the login endpoint
/// </summary>
public class EndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public EndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Test successful login with valid credentials
    /// </summary>
    [Fact]
    public async Task Login_ValidCredentials_Returns200WithToken()
    {
        // Arrange
        var testDbName = "TestDb_ValidLogin_" + Guid.NewGuid();

        // Create factory with in-memory database
        var factory = CreateTestFactory(testDbName);
        
        // Seed database using the same service provider
        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            var passwordHash = BCrypt.Net.BCrypt.HashPassword("TestPassword123!");
            var user = new User
            {
                Email = "test@example.com",
                PasswordHash = passwordHash,
                IsAdmin = false,
                CreatedAt = DateTime.UtcNow
            };
            
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();
        }

        var client = factory.CreateClient();

        var loginRequest = new
        {
            email = "test@example.com",
            password = "TestPassword123!"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<Contracts.Response>();
        Assert.NotNull(result);
        Assert.NotEmpty(result.Token);
        Assert.Equal("test@example.com", result.Email);
        Assert.False(result.IsAdmin);
        Assert.True(result.ExpiresAt > DateTime.UtcNow);
    }

    /// <summary>
    /// Test login with admin user
    /// </summary>
    [Fact]
    public async Task Login_AdminUser_Returns200WithAdminFlag()
    {
        // Arrange
        var testDbName = "TestDb_AdminLogin_" + Guid.NewGuid();

        // Seed database first with a separate context
        SeedInMemoryDatabase(testDbName, "admin@example.com", "AdminPass123!", true);

        var factory = CreateTestFactory(testDbName);
        var client = factory.CreateClient();

        var loginRequest = new
        {
            email = "admin@example.com",
            password = "AdminPass123!"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<Contracts.Response>();
        Assert.NotNull(result);
        Assert.NotEmpty(result.Token);
        Assert.Equal("admin@example.com", result.Email);
        Assert.True(result.IsAdmin);
    }

    /// <summary>
    /// Test login with invalid email (user doesn't exist)
    /// </summary>
    [Fact]
    public async Task Login_InvalidEmail_Returns401()
    {
        // Arrange
        var testDbName = "TestDb_InvalidEmail_" + Guid.NewGuid();

        // Seed database first with a separate context
        SeedInMemoryDatabase(testDbName, "test@example.com", "TestPassword123!", false);

        var factory = CreateTestFactory(testDbName);
        var client = factory.CreateClient();

        var loginRequest = new
        {
            email = "nonexistent@example.com",
            password = "TestPassword123!"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.NotNull(result);
        Assert.Contains("error", result.Keys);
        Assert.Equal("Nieprawidłowy email lub hasło", result["error"]);
    }

    /// <summary>
    /// Test login with invalid password
    /// </summary>
    [Fact]
    public async Task Login_InvalidPassword_Returns401()
    {
        // Arrange
        var testDbName = "TestDb_InvalidPassword_" + Guid.NewGuid();

        // Seed database first with a separate context
        SeedInMemoryDatabase(testDbName, "test@example.com", "TestPassword123!", false);

        var factory = CreateTestFactory(testDbName);
        var client = factory.CreateClient();

        var loginRequest = new
        {
            email = "test@example.com",
            password = "WrongPassword!"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.NotNull(result);
        Assert.Contains("error", result.Keys);
        Assert.Equal("Nieprawidłowy email lub hasło", result["error"]);
    }

    /// <summary>
    /// Test login with empty email
    /// </summary>
    [Fact]
    public async Task Login_EmptyEmail_Returns400()
    {
        // Arrange
        var testDbName = "TestDb_EmptyEmail_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);
        var client = factory.CreateClient();

        var loginRequest = new
        {
            email = "",
            password = "TestPassword123!"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Test login with invalid email format
    /// </summary>
    [Fact]
    public async Task Login_InvalidEmailFormat_Returns400()
    {
        // Arrange
        var testDbName = "TestDb_InvalidEmailFormat_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);
        var client = factory.CreateClient();

        var loginRequest = new
        {
            email = "not-an-email",
            password = "TestPassword123!"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Test login with empty password
    /// </summary>
    [Fact]
    public async Task Login_EmptyPassword_Returns400()
    {
        // Arrange
        var testDbName = "TestDb_EmptyPassword_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);
        var client = factory.CreateClient();

        var loginRequest = new
        {
            email = "test@example.com",
            password = ""
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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
                    options.UseInMemoryDatabase(databaseName);
                });
            });
        });
    }

    /// <summary>
    /// Helper method to seed test user into an in-memory database
    /// </summary>
    private static void SeedInMemoryDatabase(string databaseName, string email, string password, bool isAdmin)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        using var dbContext = new AppDbContext(options);

        // Check if user already exists
        if (dbContext.Users.Any(u => u.Email == email))
        {
            return;
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
    }
}
