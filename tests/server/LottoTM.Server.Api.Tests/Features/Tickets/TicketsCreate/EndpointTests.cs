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

namespace LottoTM.Server.Api.Tests.Features.Tickets.TicketsCreate;

/// <summary>
/// Integration tests for the CreateTicket endpoint
/// </summary>
public class EndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public EndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Test successful ticket creation with valid data
    /// </summary>
    [Fact]
    public async Task CreateTicket_WithValidData_Returns201Created()
    {
        // Arrange
        var testDbName = "TestDb_ValidTicket_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        // Seed database with a user
        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new
        {
            numbers = new[] { 5, 14, 23, 29, 37, 41 }
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/tickets", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.NotNull(result);
        Assert.Equal("Zestaw utworzony pomyślnie", result["message"].ToString());
        Assert.Contains("/api/tickets/", response.Headers.Location?.ToString());

        // Verify in database
        VerifyTicketInDatabase(factory, userId, new[] { 5, 14, 23, 29, 37, 41 });
    }

    /// <summary>
    /// Test ticket creation with invalid number count returns 400
    /// </summary>
    [Fact]
    public async Task CreateTicket_WithInvalidNumberCount_Returns400BadRequest()
    {
        // Arrange
        var testDbName = "TestDb_InvalidCount_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new
        {
            numbers = new[] { 5, 14, 23, 29 } // only 4 numbers
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/tickets", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Test ticket creation with numbers out of range returns 400
    /// </summary>
    [Fact]
    public async Task CreateTicket_WithNumbersOutOfRange_Returns400BadRequest()
    {
        // Arrange
        var testDbName = "TestDb_OutOfRange_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new
        {
            numbers = new[] { 0, 14, 23, 29, 37, 41 } // 0 is out of range
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/tickets", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Test ticket creation with duplicate numbers returns 400
    /// </summary>
    [Fact]
    public async Task CreateTicket_WithDuplicateNumbers_Returns400BadRequest()
    {
        // Arrange
        var testDbName = "TestDb_Duplicates_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new
        {
            numbers = new[] { 5, 5, 23, 29, 37, 41 } // duplicate 5
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/tickets", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Test ticket creation with duplicate set returns 400
    /// Tests that order-independent comparison works
    /// </summary>
    [Fact]
    public async Task CreateTicket_WithDuplicateSet_Returns400BadRequest()
    {
        // Arrange
        var testDbName = "TestDb_DuplicateSet_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        // Seed existing ticket
        SeedTestTicket(factory, userId, new[] { 5, 14, 23, 29, 37, 41 });

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new
        {
            numbers = new[] { 41, 5, 29, 14, 37, 23 } // same numbers, different order
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/tickets", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Test ticket creation when limit is reached returns 400
    /// </summary>
    [Fact]
    public async Task CreateTicket_WhenLimitReached_Returns400BadRequest()
    {
        // Arrange
        var testDbName = "TestDb_LimitReached_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        // Seed 100 tickets to reach the limit
        for (int i = 0; i < 100; i++)
        {
            SeedTestTicket(factory, userId, new[] { 1 + i % 44, 2 + i % 44, 3 + i % 44, 4 + i % 44, 5 + i % 44, 6 + i % 44 });
        }

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new
        {
            numbers = new[] { 10, 20, 30, 40, 45, 49 }
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/tickets", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Test ticket creation without authentication returns 401
    /// </summary>
    [Fact]
    public async Task CreateTicket_WithoutAuthentication_Returns401Unauthorized()
    {
        // Arrange
        var testDbName = "TestDb_NoAuth_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);
        var client = factory.CreateClient();

        var request = new
        {
            numbers = new[] { 5, 14, 23, 29, 37, 41 }
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/tickets", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Test that users can only see their own tickets (data isolation)
    /// </summary>
    [Fact]
    public async Task CreateTicket_WithDifferentUser_AllowsDuplicateSet()
    {
        // Arrange
        var testDbName = "TestDb_DataIsolation_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var user1Id = SeedTestUser(testDbName, "user1@example.com", "Password123!", false);
        var user2Id = SeedTestUser(testDbName, "user2@example.com", "Password123!", false);

        // Seed ticket for user 1
        SeedTestTicket(factory, user1Id, new[] { 5, 14, 23, 29, 37, 41 });

        // User 2 tries to create the same set
        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, user2Id, "user2@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new
        {
            numbers = new[] { 5, 14, 23, 29, 37, 41 }
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/tickets", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode); // Should succeed for different user
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
    /// Helper method to seed test ticket into an in-memory database
    /// </summary>
    private static void SeedTestTicket(WebApplicationFactory<Program> factory, int userId, int[] numbers)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var ticket = new Ticket
        {
            UserId = userId,
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

    /// <summary>
    /// Helper method to verify ticket was created in database
    /// </summary>
    private static void VerifyTicketInDatabase(WebApplicationFactory<Program> factory, int userId, int[] expectedNumbers)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var ticket = dbContext.Tickets
            .Include(t => t.Numbers)
            .FirstOrDefault(t => t.UserId == userId);

        Assert.NotNull(ticket);
        Assert.Equal(6, ticket.Numbers.Count);

        var actualNumbers = ticket.Numbers.OrderBy(n => n.Position).Select(n => n.Number).ToArray();
        Assert.Equal(expectedNumbers, actualNumbers);
    }
}
