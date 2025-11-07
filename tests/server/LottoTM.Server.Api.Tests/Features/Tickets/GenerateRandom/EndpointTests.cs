using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LottoTM.Server.Api.Entities;
using LottoTM.Server.Api.Features.Tickets.GenerateRandom;
using LottoTM.Server.Api.Repositories;
using LottoTM.Server.Api.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LottoTM.Server.Api.Tests.Features.Tickets.GenerateRandom;

/// <summary>
/// Integration tests for the GenerateRandomTicket endpoint (POST /api/tickets/generate-random)
/// </summary>
public class EndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public EndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Test successful generation of a random ticket
    /// </summary>
    [Fact]
    public async Task GenerateRandomTicket_WithValidUser_Returns201WithTicketData()
    {
        // Arrange
        var testDbName = "TestDb_GenerateRandom_Success_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.PostAsync("/api/tickets/generate-random", null);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<Contracts.Response>();
        Assert.NotNull(result);
        Assert.Equal("Zestaw wygenerowany pomyślnie", result.Message);
        Assert.True(result.TicketId > 0);

        // Verify Location header
        Assert.NotNull(response.Headers.Location);
        Assert.Contains($"/api/tickets/{result.TicketId}", response.Headers.Location.ToString());

        // Verify ticket was created in database
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var ticket = await dbContext.Tickets
            .Include(t => t.Numbers)
            .FirstOrDefaultAsync(t => t.Id == result.TicketId);

        Assert.NotNull(ticket);
        Assert.Equal(userId, ticket.UserId);
        Assert.Equal(6, ticket.Numbers.Count);

        // Verify all numbers are unique and in range 1-49
        var numbers = ticket.Numbers.Select(n => n.Number).ToList();
        Assert.Equal(6, numbers.Distinct().Count());
        Assert.All(numbers, n => Assert.InRange(n, 1, 49));

        // Verify positions are 1-6
        var positions = ticket.Numbers.Select(n => n.Position).OrderBy(p => p).ToList();
        Assert.Equal(new byte[] { 1, 2, 3, 4, 5, 6 }, positions);
    }

    /// <summary>
    /// Test multiple ticket generations create different numbers
    /// </summary>
    [Fact]
    public async Task GenerateRandomTicket_CalledMultipleTimes_CreatesUniqueTickets()
    {
        // Arrange
        var testDbName = "TestDb_GenerateRandom_Multiple_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act - Generate 3 tickets
        var ticketIds = new List<int>();
        for (int i = 0; i < 3; i++)
        {
            var response = await client.PostAsync("/api/tickets/generate-random", null);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<Contracts.Response>();
            Assert.NotNull(result);
            ticketIds.Add(result.TicketId);
        }

        // Assert - All ticket IDs are unique
        Assert.Equal(3, ticketIds.Distinct().Count());

        // Verify in database
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var tickets = await dbContext.Tickets
            .Include(t => t.Numbers)
            .Where(t => ticketIds.Contains(t.Id))
            .ToListAsync();

        Assert.Equal(3, tickets.Count);

        // Each ticket should have different number sets (statistically likely)
        var numberSets = tickets.Select(t =>
            string.Join(",", t.Numbers.OrderBy(n => n.Number).Select(n => n.Number))
        ).ToList();

        // At least 2 should be different (extremely unlikely all 3 are identical)
        Assert.True(numberSets.Distinct().Count() >= 2);
    }

    /// <summary>
    /// Test limit enforcement - user with 100 tickets cannot generate more
    /// </summary>
    [Fact]
    public async Task GenerateRandomTicket_WhenLimitReached_Returns400()
    {
        // Arrange
        var testDbName = "TestDb_GenerateRandom_Limit_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        // Seed 100 tickets to reach the limit
        SeedTestTickets(factory, userId, 100);

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.PostAsync("/api/tickets/generate-random", null);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("limit", content.ToLower());
        Assert.Contains("100", content);
    }

    /// <summary>
    /// Test user with 99 tickets can still generate one more
    /// </summary>
    [Fact]
    public async Task GenerateRandomTicket_WithUser99Tickets_Returns201()
    {
        // Arrange
        var testDbName = "TestDb_GenerateRandom_99Tickets_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        // Seed 99 tickets
        SeedTestTickets(factory, userId, 99);

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.PostAsync("/api/tickets/generate-random", null);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<Contracts.Response>();
        Assert.NotNull(result);
        Assert.True(result.TicketId > 0);

        // Verify total count is now 100
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var totalCount = await dbContext.Tickets.CountAsync(t => t.UserId == userId);
        Assert.Equal(100, totalCount);
    }

    /// <summary>
    /// Test without authentication returns 401
    /// </summary>
    [Fact]
    public async Task GenerateRandomTicket_WithoutAuthentication_Returns401()
    {
        // Arrange
        var testDbName = "TestDb_GenerateRandom_NoAuth_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);
        var client = factory.CreateClient();

        // Act - no authorization header
        var response = await client.PostAsync("/api/tickets/generate-random", null);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Test with invalid token returns 401
    /// </summary>
    [Fact]
    public async Task GenerateRandomTicket_WithInvalidToken_Returns401()
    {
        // Arrange
        var testDbName = "TestDb_GenerateRandom_InvalidToken_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");

        // Act
        var response = await client.PostAsync("/api/tickets/generate-random", null);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Test that different users can each generate their own tickets
    /// </summary>
    [Fact]
    public async Task GenerateRandomTicket_DifferentUsers_CreatesSeparateTickets()
    {
        // Arrange
        var testDbName = "TestDb_GenerateRandom_MultiUser_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var user1Id = SeedTestUser(testDbName, "user1@example.com", "Password123!", false);
        var user2Id = SeedTestUser(testDbName, "user2@example.com", "Password123!", false);

        var client = factory.CreateClient();

        // Act - User 1 generates ticket
        var token1 = GenerateTestToken(factory, user1Id, "user1@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token1);
        var response1 = await client.PostAsync("/api/tickets/generate-random", null);
        Assert.Equal(HttpStatusCode.Created, response1.StatusCode);

        // Act - User 2 generates ticket
        var token2 = GenerateTestToken(factory, user2Id, "user2@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token2);
        var response2 = await client.PostAsync("/api/tickets/generate-random", null);
        Assert.Equal(HttpStatusCode.Created, response2.StatusCode);

        // Assert - Verify in database
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user1Tickets = await dbContext.Tickets.CountAsync(t => t.UserId == user1Id);
        var user2Tickets = await dbContext.Tickets.CountAsync(t => t.UserId == user2Id);

        Assert.Equal(1, user1Tickets);
        Assert.Equal(1, user2Tickets);
    }

    /// <summary>
    /// Test that generated numbers are sorted in ascending order
    /// </summary>
    [Fact]
    public async Task GenerateRandomTicket_GeneratesNumbersInAscendingOrder()
    {
        // Arrange
        var testDbName = "TestDb_GenerateRandom_Sorted_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.PostAsync("/api/tickets/generate-random", null);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<Contracts.Response>();
        Assert.NotNull(result);

        // Assert - Verify numbers are sorted
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var ticket = await dbContext.Tickets
            .Include(t => t.Numbers)
            .FirstOrDefaultAsync(t => t.Id == result.TicketId);

        Assert.NotNull(ticket);

        var numbers = ticket.Numbers.OrderBy(n => n.Position).Select(n => n.Number).ToList();
        var sortedNumbers = numbers.OrderBy(n => n).ToList();

        Assert.Equal(sortedNumbers, numbers);
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
                CreatedAt = DateTime.UtcNow.AddMinutes(-i)
            };

            dbContext.Tickets.Add(ticket);
            dbContext.SaveChanges();

            // Add 6 dummy numbers for each ticket
            var ticketNumbers = new[]
            {
                new TicketNumber { TicketId = ticket.Id, Number = 1 + (i % 43), Position = 1 },
                new TicketNumber { TicketId = ticket.Id, Number = 5 + (i % 43), Position = 2 },
                new TicketNumber { TicketId = ticket.Id, Number = 10 + (i % 39), Position = 3 },
                new TicketNumber { TicketId = ticket.Id, Number = 20 + (i % 29), Position = 4 },
                new TicketNumber { TicketId = ticket.Id, Number = 30 + (i % 19), Position = 5 },
                new TicketNumber { TicketId = ticket.Id, Number = 40 + (i % 9), Position = 6 }
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
