using LottoTM.Server.Api.Entities;
using LottoTM.Server.Api.Features.Auth.Register;
using LottoTM.Server.Api.Repositories;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace LottoTM.Server.Api.Tests.Features.Auth.Register.Tests;

/// <summary>
/// Integration tests for the user registration endpoint
/// </summary>
public class EndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public EndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Test successful registration with valid credentials
    /// </summary>
    [Fact]
    public async Task Register_WithValidData_Returns201Created()
    {
        // Arrange
        var testDbName = "TestDb_ValidRegister_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);
        var client = factory.CreateClient();

        var request = new Contracts.Request(
            Email: $"test{Guid.NewGuid()}@example.com",
            Password: "SecurePass123!",
            ConfirmPassword: "SecurePass123!"
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<Contracts.Response>();
        Assert.NotNull(result);

        // Verify user was created in database
        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            Assert.NotNull(user);
            Assert.Equal(request.Email, user.Email);
            Assert.False(user.IsAdmin);
            Assert.True(BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash));
        }
    }

    /// <summary>
    /// Test registration with duplicate email
    /// </summary>
    [Fact]
    public async Task Register_WithDuplicateEmail_Returns400BadRequest()
    {
        // Arrange
        var email = $"duplicate{Guid.NewGuid()}@example.com";

        var testDbName = "TestDb_DuplicateEmail_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);
        
        // Seed database with existing user through the factory's service provider
        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var passwordHash = BCrypt.Net.BCrypt.HashPassword("ExistingPass123!");
            var existingUser = new User
            {
                Email = email,
                PasswordHash = passwordHash,
                IsAdmin = false,
                CreatedAt = DateTime.UtcNow
            };
            dbContext.Users.Add(existingUser);
            await dbContext.SaveChangesAsync();
        }

        var client = factory.CreateClient();

        var request = new Contracts.Request(
            Email: email,
            Password: "AnotherPass456!",
            ConfirmPassword: "AnotherPass456!"
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Test registration with invalid email format
    /// </summary>
    [Fact]
    public async Task Register_WithInvalidEmail_Returns400BadRequest()
    {
        // Arrange
        var testDbName = "TestDb_InvalidEmail_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);
        var client = factory.CreateClient();

        var request = new Contracts.Request(
            Email: "invalid-email-format",
            Password: "SecurePass123!",
            ConfirmPassword: "SecurePass123!"
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Test registration with password too short
    /// </summary>
    [Fact]
    public async Task Register_WithShortPassword_Returns400BadRequest()
    {
        // Arrange
        var testDbName = "TestDb_ShortPassword_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);
        var client = factory.CreateClient();

        var request = new Contracts.Request(
            Email: $"test{Guid.NewGuid()}@example.com",
            Password: "Short1!", // Only 7 characters
            ConfirmPassword: "Short1!"
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Test registration with password missing uppercase letter
    /// </summary>
    [Fact]
    public async Task Register_WithPasswordNoUppercase_Returns400BadRequest()
    {
        // Arrange
        var testDbName = "TestDb_NoUppercase_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);
        var client = factory.CreateClient();

        var request = new Contracts.Request(
            Email: $"test{Guid.NewGuid()}@example.com",
            Password: "lowercase123!",
            ConfirmPassword: "lowercase123!"
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Test registration with password missing digit
    /// </summary>
    [Fact]
    public async Task Register_WithPasswordNoDigit_Returns400BadRequest()
    {
        // Arrange
        var testDbName = "TestDb_NoDigit_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);
        var client = factory.CreateClient();

        var request = new Contracts.Request(
            Email: $"test{Guid.NewGuid()}@example.com",
            Password: "NoDigitPass!",
            ConfirmPassword: "NoDigitPass!"
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Test registration with password missing special character
    /// </summary>
    [Fact]
    public async Task Register_WithPasswordNoSpecialChar_Returns400BadRequest()
    {
        // Arrange
        var testDbName = "TestDb_NoSpecialChar_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);
        var client = factory.CreateClient();

        var request = new Contracts.Request(
            Email: $"test{Guid.NewGuid()}@example.com",
            Password: "NoSpecial123",
            ConfirmPassword: "NoSpecial123"
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Test registration with mismatched passwords
    /// </summary>
    [Fact]
    public async Task Register_WithMismatchedPasswords_Returns400BadRequest()
    {
        // Arrange
        var testDbName = "TestDb_MismatchedPass_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);
        var client = factory.CreateClient();

        var request = new Contracts.Request(
            Email: $"test{Guid.NewGuid()}@example.com",
            Password: "SecurePass123!",
            ConfirmPassword: "DifferentPass456!"
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Test registration with empty email
    /// </summary>
    [Fact]
    public async Task Register_WithEmptyEmail_Returns400BadRequest()
    {
        // Arrange
        var testDbName = "TestDb_EmptyEmail_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);
        var client = factory.CreateClient();

        var request = new Contracts.Request(
            Email: "",
            Password: "SecurePass123!",
            ConfirmPassword: "SecurePass123!"
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Test registration with empty password
    /// </summary>
    [Fact]
    public async Task Register_WithEmptyPassword_Returns400BadRequest()
    {
        // Arrange
        var testDbName = "TestDb_EmptyPassword_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);
        var client = factory.CreateClient();

        var request = new Contracts.Request(
            Email: $"test{Guid.NewGuid()}@example.com",
            Password: "",
            ConfirmPassword: ""
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Test registration with empty confirm password
    /// </summary>
    [Fact]
    public async Task Register_WithEmptyConfirmPassword_Returns400BadRequest()
    {
        // Arrange
        var testDbName = "TestDb_EmptyConfirm_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);
        var client = factory.CreateClient();

        var request = new Contracts.Request(
            Email: $"test{Guid.NewGuid()}@example.com",
            Password: "SecurePass123!",
            ConfirmPassword: ""
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", request);

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
                // Clear all configuration sources
                config.Sources.Clear();

                // Add only required configuration
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = "U2VydmVyPWR1bW15O0RhdGFiYXNlPWR1bW15O0ludGVncmF0ZWQgU2VjdXJpdHk9VHJ1ZTs=",
                    ["Jwt:Key"] = "ThisIsASecretKeyForTestingPurposesOnly123456",
                    ["Jwt:Issuer"] = "TestIssuer",
                    ["Jwt:Audience"] = "TestAudience",
                    ["Jwt:ExpiryInMinutes"] = "1440",
                    ["Swagger:Enabled"] = "false",
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
}
