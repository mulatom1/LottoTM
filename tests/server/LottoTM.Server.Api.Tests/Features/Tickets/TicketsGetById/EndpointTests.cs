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

namespace LottoTM.Server.Api.Tests.Features.Tickets.TicketsGetById;

/// <summary>
/// Integration tests for the GetTicketById endpoint
/// </summary>
public class EndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public EndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Test successful retrieval of a ticket by valid ID
    /// </summary>
    [Fact]
    public async Task GetTicketById_WithValidId_Returns200AndTicketDetails()
    {
        // Arrange
        var testDbName = "TestDb_GetValidTicket_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        // Seed database with a user and a ticket
        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);
        var ticketId = SeedTestTicket(factory, userId, new[] { 5, 14, 23, 29, 37, 41 });

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync($"/api/tickets/{ticketId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<TicketResponse>();
        Assert.NotNull(result);
        Assert.Equal(ticketId, result.Id);
        Assert.Equal(userId, result.UserId);
        Assert.Equal(6, result.Numbers.Length);
        Assert.Equal(new[] { 5, 14, 23, 29, 37, 41 }, result.Numbers);
        Assert.NotEqual(default(DateTime), result.CreatedAt);
    }

    /// <summary>
    /// Test retrieval of non-existent ticket returns 404
    /// </summary>
    [Fact]
    public async Task GetTicketById_WithNonExistentId_Returns404NotFound()
    {
        // Arrange
        var testDbName = "TestDb_NonExistent_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/tickets/99999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();
        Assert.NotNull(result);
        Assert.Equal("Zestaw o podanym ID nie istnieje", result.Detail);
    }

    /// <summary>
    /// Test retrieval without authentication returns 401
    /// </summary>
    [Fact]
    public async Task GetTicketById_WithoutAuthentication_Returns401Unauthorized()
    {
        // Arrange
        var testDbName = "TestDb_NoAuth_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/tickets/1");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Test retrieval with invalid ID (zero) returns 400
    /// </summary>
    [Fact]
    public async Task GetTicketById_WithInvalidIdZero_Returns400BadRequest()
    {
        // Arrange
        var testDbName = "TestDb_InvalidId_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/tickets/0");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Test retrieval with negative ID returns 400
    /// </summary>
    [Fact]
    public async Task GetTicketById_WithNegativeId_Returns400BadRequest()
    {
        // Arrange
        var testDbName = "TestDb_NegativeId_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/tickets/-1");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Test that attempting to access another user's ticket returns 403 Forbidden
    /// (security by obscurity - don't reveal if resource exists)
    /// </summary>
    [Fact]
    public async Task GetTicketById_WhenNotOwner_Returns403Forbidden()
    {
        // Arrange
        var testDbName = "TestDb_NotOwner_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        // Create two users
        var user1Id = SeedTestUser(testDbName, "user1@example.com", "Password123!", false);
        var user2Id = SeedTestUser(testDbName, "user2@example.com", "Password123!", false);

        // User 1 creates a ticket
        var ticketId = SeedTestTicket(factory, user1Id, new[] { 5, 14, 23, 29, 37, 41 });

        // User 2 tries to access User 1's ticket
        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, user2Id, "user2@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync($"/api/tickets/{ticketId}");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();
        Assert.NotNull(result);
        Assert.Equal("Nie masz uprawnień do tego zasobu", result.Detail);
    }

    /// <summary>
    /// Test that numbers are returned in correct order by position
    /// </summary>
    [Fact]
    public async Task GetTicketById_ReturnsNumbersInCorrectOrder()
    {
        // Arrange
        var testDbName = "TestDb_NumberOrder_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        // Create ticket with numbers not in natural order
        var ticketId = SeedTestTicket(factory, userId, new[] { 48, 31, 3, 42, 12, 25 });

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync($"/api/tickets/{ticketId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<TicketResponse>();
        Assert.NotNull(result);
        // Numbers should be in the order they were inserted (by position)
        Assert.Equal(new[] { 48, 31, 3, 42, 12, 25 }, result.Numbers);
    }

    /// <summary>
    /// Response DTO for GetTicketById endpoint
    /// </summary>
    private record TicketResponse(int Id, int UserId, int[] Numbers, DateTime CreatedAt);

    /// <summary>
    /// ProblemDetails response DTO for error responses
    /// </summary>
    private record ProblemDetailsResponse(string? Type, string? Title, int? Status, string? Detail);

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
    private static int SeedTestTicket(WebApplicationFactory<Program> factory, int userId, int[] numbers)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var ticket = new Ticket
        {
            UserId = userId,
            GroupName = $"Manual: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
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

        return ticket.Id;
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
