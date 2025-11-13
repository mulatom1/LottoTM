using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LottoTM.Server.Api.Entities;
using LottoTM.Server.Api.Features.Tickets.TicketsGetList;
using LottoTM.Server.Api.Repositories;
using LottoTM.Server.Api.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LottoTM.Server.Api.Tests.Features.Tickets.TicketsGetList;

/// <summary>
/// Integration tests for the GetTickets endpoint (GET /api/tickets)
/// </summary>
public class EndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public EndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Test successful retrieval of tickets with valid authentication
    /// </summary>
    [Fact]
    public async Task GetTickets_WithValidAuthentication_Returns200WithTicketsList()
    {
        // Arrange
        var testDbName = "TestDb_GetTickets_Valid_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        // Seed database with a user and tickets
        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);
        SeedTestTickets(factory, userId, 5); // Create 5 test tickets

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/tickets");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<Contracts.Response>();
        Assert.NotNull(result);
        Assert.NotNull(result.Tickets);
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(100, result.Limit);
        Assert.Equal(5, result.Tickets.Count);

        // Verify each ticket has 6 numbers
        foreach (var ticket in result.Tickets)
        {
            Assert.Equal(6, ticket.Numbers.Length);
            Assert.Equal(userId, ticket.UserId);
        }
    }

    /// <summary>
    /// Test that tickets are sorted by CreatedAt descending (newest first)
    /// </summary>
    [Fact]
    public async Task GetTickets_ReturnsTicketsSortedByCreatedAtDescending()
    {
        // Arrange
        var testDbName = "TestDb_GetTickets_Sorting_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);
        SeedTestTickets(factory, userId, 3);

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/tickets");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<Contracts.Response>();
        Assert.NotNull(result);
        Assert.True(result.Tickets.Count > 1);

        // Verify descending order by CreatedAt
        for (int i = 0; i < result.Tickets.Count - 1; i++)
        {
            Assert.True(result.Tickets[i].CreatedAt >= result.Tickets[i + 1].CreatedAt);
        }
    }

    /// <summary>
    /// Test without authentication returns 401
    /// </summary>
    [Fact]
    public async Task GetTickets_WithoutAuthentication_Returns401()
    {
        // Arrange
        var testDbName = "TestDb_GetTickets_NoAuth_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);
        var client = factory.CreateClient();

        // Act - no authorization header
        var response = await client.GetAsync("/api/tickets");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Test with invalid token returns 401
    /// </summary>
    [Fact]
    public async Task GetTickets_WithInvalidToken_Returns401()
    {
        // Arrange
        var testDbName = "TestDb_GetTickets_InvalidToken_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid_token");

        // Act
        var response = await client.GetAsync("/api/tickets");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Test with empty database returns empty list (not 404)
    /// </summary>
    [Fact]
    public async Task GetTickets_WithNoTickets_ReturnsEmptyList()
    {
        // Arrange
        var testDbName = "TestDb_GetTickets_Empty_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);
        // Don't seed any tickets

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/tickets");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<Contracts.Response>();
        Assert.NotNull(result);
        Assert.Empty(result.Tickets);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(100, result.Limit);
    }

    /// <summary>
    /// Test data isolation - user can only see their own tickets
    /// </summary>
    [Fact]
    public async Task GetTickets_OnlyReturnsTicketsForCurrentUser()
    {
        // Arrange
        var testDbName = "TestDb_GetTickets_Isolation_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var user1Id = SeedTestUser(testDbName, "user1@example.com", "Password123!", false);
        var user2Id = SeedTestUser(testDbName, "user2@example.com", "Password123!", false);

        SeedTestTickets(factory, user1Id, 3); // User 1 has 3 tickets
        SeedTestTickets(factory, user2Id, 5); // User 2 has 5 tickets

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, user1Id, "user1@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/tickets");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<Contracts.Response>();
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalCount); // Only user1's tickets
        Assert.All(result.Tickets, ticket => Assert.Equal(user1Id, ticket.UserId));
    }

    /// <summary>
    /// Test with maximum number of tickets (100)
    /// </summary>
    [Fact]
    public async Task GetTickets_WithMaximumTickets_ReturnsAllTickets()
    {
        // Arrange
        var testDbName = "TestDb_GetTickets_MaxTickets_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);
        SeedTestTickets(factory, userId, 100); // Create 100 test tickets

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/tickets");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<Contracts.Response>();
        Assert.NotNull(result);
        Assert.Equal(100, result.TotalCount);
        Assert.Equal(100, result.Tickets.Count);
        Assert.Equal(100, result.Limit);
    }

    /// <summary>
    /// Test that numbers are returned in correct order by position
    /// </summary>
    [Fact]
    public async Task GetTickets_ReturnsNumbersInCorrectOrder()
    {
        // Arrange
        var testDbName = "TestDb_GetTickets_NumberOrder_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        // Create a ticket with specific numbers
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var ticket = new Ticket
        {
            UserId = userId,
            GroupName = "Test Group",
            CreatedAt = DateTime.UtcNow
        };
        dbContext.Tickets.Add(ticket);
        dbContext.SaveChanges();

        // Add numbers in specific positions (not in number order)
        var expectedNumbers = new[] { 42, 7, 23, 15, 31, 49 }; // Position 1-6
        for (byte pos = 1; pos <= 6; pos++)
        {
            dbContext.TicketNumbers.Add(new TicketNumber
            {
                TicketId = ticket.Id,
                Number = expectedNumbers[pos - 1],
                Position = pos
            });
        }
        dbContext.SaveChanges();

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/tickets");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<Contracts.Response>();
        Assert.NotNull(result);
        Assert.Single(result.Tickets);
        Assert.Equal(expectedNumbers, result.Tickets[0].Numbers);
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
    /// Helper method to seed multiple test tickets into the database
    /// </summary>
    private static void SeedTestTickets(WebApplicationFactory<Program> factory, int userId, int count)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        for (int i = 0; i < count; i++)
        {
            var ticket = new Ticket
            {
                UserId = userId,
                GroupName = $"Test Group {i + 1}",
                CreatedAt = DateTime.UtcNow.AddMinutes(-count + i) // Stagger creation times for sorting tests
            };

            dbContext.Tickets.Add(ticket);
            dbContext.SaveChanges();

            // Add 6 numbers for each ticket
            var ticketNumbers = new[]
            {
                new TicketNumber { TicketId = ticket.Id, Number = 3 + i, Position = 1 },
                new TicketNumber { TicketId = ticket.Id, Number = 12 + i, Position = 2 },
                new TicketNumber { TicketId = ticket.Id, Number = 25 + (i % 20), Position = 3 },
                new TicketNumber { TicketId = ticket.Id, Number = 31 + (i % 15), Position = 4 },
                new TicketNumber { TicketId = ticket.Id, Number = 37 + (i % 10), Position = 5 },
                new TicketNumber { TicketId = ticket.Id, Number = 42 + (i % 7), Position = 6 }
            };

            dbContext.TicketNumbers.AddRange(ticketNumbers);
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
