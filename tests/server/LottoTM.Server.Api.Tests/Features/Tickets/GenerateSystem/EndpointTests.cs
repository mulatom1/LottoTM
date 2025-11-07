using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LottoTM.Server.Api.Entities;
using LottoTM.Server.Api.Features.Tickets.GenerateSystem;
using LottoTM.Server.Api.Repositories;
using LottoTM.Server.Api.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LottoTM.Server.Api.Tests.Features.Tickets.GenerateSystem;

/// <summary>
/// Integration tests for the GenerateSystemTickets endpoint (POST /api/tickets/generate-system)
/// </summary>
public class EndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public EndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Test successful generation of 9 system tickets
    /// </summary>
    [Fact]
    public async Task GenerateSystemTickets_WithValidUser_Returns201With9Tickets()
    {
        // Arrange
        var testDbName = "TestDb_GenerateSystem_Success_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.PostAsync("/api/tickets/generate-system", null);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<Contracts.Response>();
        Assert.NotNull(result);
        Assert.Equal("9 zestawów wygenerowanych i zapisanych pomyślnie", result.Message);
        Assert.Equal(9, result.GeneratedCount);
        Assert.Equal(9, result.Tickets.Count);

        // Verify each ticket has 6 unique numbers
        foreach (var ticket in result.Tickets)
        {
            Assert.Equal(6, ticket.Numbers.Count);
            Assert.Equal(6, ticket.Numbers.Distinct().Count());
            Assert.All(ticket.Numbers, n => Assert.InRange(n, 1, 49));
        }

        // Verify coverage - all numbers 1-49 should appear
        var allNumbers = result.Tickets.SelectMany(t => t.Numbers).Distinct().OrderBy(n => n).ToList();
        Assert.Equal(49, allNumbers.Count);
        Assert.Equal(1, allNumbers.First());
        Assert.Equal(49, allNumbers.Last());

        // Verify in database
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var ticketsInDb = await dbContext.Tickets
            .Include(t => t.Numbers)
            .Where(t => t.UserId == userId)
            .ToListAsync();

        Assert.Equal(9, ticketsInDb.Count);
        Assert.All(ticketsInDb, t => Assert.Equal(6, t.Numbers.Count));
    }

    /// <summary>
    /// Test that all numbers 1-49 are covered by the generated tickets
    /// </summary>
    [Fact]
    public async Task GenerateSystemTickets_CoversAllNumbers1To49()
    {
        // Arrange
        var testDbName = "TestDb_GenerateSystem_Coverage_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.PostAsync("/api/tickets/generate-system", null);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<Contracts.Response>();
        Assert.NotNull(result);

        // Collect all numbers from all tickets
        var allNumbers = result.Tickets
            .SelectMany(t => t.Numbers)
            .Distinct()
            .OrderBy(n => n)
            .ToList();

        // Verify complete coverage
        Assert.Equal(49, allNumbers.Count);
        var expectedNumbers = Enumerable.Range(1, 49).ToList();
        Assert.Equal(expectedNumbers, allNumbers);
    }

    /// <summary>
    /// Test multiple generations create different ticket sets
    /// </summary>
    [Fact]
    public async Task GenerateSystemTickets_CalledTwice_CreatesDifferentSets()
    {
        // Arrange
        var testDbName = "TestDb_GenerateSystem_Different_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act - Generate first set
        var response1 = await client.PostAsync("/api/tickets/generate-system", null);
        Assert.Equal(HttpStatusCode.Created, response1.StatusCode);
        var result1 = await response1.Content.ReadFromJsonAsync<Contracts.Response>();

        // Act - Generate second set
        var response2 = await client.PostAsync("/api/tickets/generate-system", null);
        Assert.Equal(HttpStatusCode.Created, response2.StatusCode);
        var result2 = await response2.Content.ReadFromJsonAsync<Contracts.Response>();

        // Assert - Ticket IDs should be different
        Assert.NotNull(result1);
        Assert.NotNull(result2);

        var ids1 = result1.Tickets.Select(t => t.Id).OrderBy(id => id).ToList();
        var ids2 = result2.Tickets.Select(t => t.Id).OrderBy(id => id).ToList();

        Assert.NotEqual(ids1, ids2);

        // Verify database has 18 tickets total
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var totalTickets = await dbContext.Tickets.CountAsync(t => t.UserId == userId);
        Assert.Equal(18, totalTickets);
    }

    /// <summary>
    /// Test limit enforcement - user with 92 tickets cannot generate 9 more
    /// </summary>
    [Fact]
    public async Task GenerateSystemTickets_WhenOver91Tickets_Returns400()
    {
        // Arrange
        var testDbName = "TestDb_GenerateSystem_Limit_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        // Seed 92 tickets (over the limit of 91)
        SeedTestTickets(factory, userId, 92);

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.PostAsync("/api/tickets/generate-system", null);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("limit", content.ToLower());
        Assert.Contains("92", content);
        Assert.Contains("100", content);
    }

    /// <summary>
    /// Test user with exactly 91 tickets can still generate 9 more
    /// </summary>
    [Fact]
    public async Task GenerateSystemTickets_WithExactly91Tickets_Returns201()
    {
        // Arrange
        var testDbName = "TestDb_GenerateSystem_91Tickets_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        // Seed exactly 91 tickets (boundary case)
        SeedTestTickets(factory, userId, 91);

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.PostAsync("/api/tickets/generate-system", null);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<Contracts.Response>();
        Assert.NotNull(result);
        Assert.Equal(9, result.GeneratedCount);

        // Verify total count is now 100 (91 + 9)
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var totalCount = await dbContext.Tickets.CountAsync(t => t.UserId == userId);
        Assert.Equal(100, totalCount);
    }

    /// <summary>
    /// Test without authentication returns 401
    /// </summary>
    [Fact]
    public async Task GenerateSystemTickets_WithoutAuthentication_Returns401()
    {
        // Arrange
        var testDbName = "TestDb_GenerateSystem_NoAuth_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);
        var client = factory.CreateClient();

        // Act - no authorization header
        var response = await client.PostAsync("/api/tickets/generate-system", null);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Test with invalid token returns 401
    /// </summary>
    [Fact]
    public async Task GenerateSystemTickets_WithInvalidToken_Returns401()
    {
        // Arrange
        var testDbName = "TestDb_GenerateSystem_InvalidToken_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");

        // Act
        var response = await client.PostAsync("/api/tickets/generate-system", null);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Test that different users can generate their own system tickets independently
    /// </summary>
    [Fact]
    public async Task GenerateSystemTickets_DifferentUsers_CreatesSeparateSets()
    {
        // Arrange
        var testDbName = "TestDb_GenerateSystem_MultiUser_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var user1Id = SeedTestUser(testDbName, "user1@example.com", "Password123!", false);
        var user2Id = SeedTestUser(testDbName, "user2@example.com", "Password123!", false);

        var client = factory.CreateClient();

        // Act - User 1 generates system tickets
        var token1 = GenerateTestToken(factory, user1Id, "user1@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token1);
        var response1 = await client.PostAsync("/api/tickets/generate-system", null);
        Assert.Equal(HttpStatusCode.Created, response1.StatusCode);

        // Act - User 2 generates system tickets
        var token2 = GenerateTestToken(factory, user2Id, "user2@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token2);
        var response2 = await client.PostAsync("/api/tickets/generate-system", null);
        Assert.Equal(HttpStatusCode.Created, response2.StatusCode);

        // Assert - Verify in database
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user1Tickets = await dbContext.Tickets.CountAsync(t => t.UserId == user1Id);
        var user2Tickets = await dbContext.Tickets.CountAsync(t => t.UserId == user2Id);

        Assert.Equal(9, user1Tickets);
        Assert.Equal(9, user2Tickets);
    }

    /// <summary>
    /// Test that CreatedAt timestamps are properly set
    /// </summary>
    [Fact]
    public async Task GenerateSystemTickets_SetsProperTimestamps()
    {
        // Arrange
        var testDbName = "TestDb_GenerateSystem_Timestamps_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var beforeCall = DateTime.UtcNow;

        // Act
        var response = await client.PostAsync("/api/tickets/generate-system", null);

        var afterCall = DateTime.UtcNow;

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<Contracts.Response>();
        Assert.NotNull(result);

        // All tickets should have the same CreatedAt timestamp
        var timestamps = result.Tickets.Select(t => t.CreatedAt).Distinct().ToList();
        Assert.Single(timestamps);

        var createdAt = timestamps.First();
        Assert.True(createdAt >= beforeCall && createdAt <= afterCall);
    }

    /// <summary>
    /// Test that numbers in each ticket are in valid range and unique
    /// </summary>
    [Fact]
    public async Task GenerateSystemTickets_EachTicketHasValidNumbers()
    {
        // Arrange
        var testDbName = "TestDb_GenerateSystem_ValidNumbers_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.PostAsync("/api/tickets/generate-system", null);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<Contracts.Response>();
        Assert.NotNull(result);

        foreach (var ticket in result.Tickets)
        {
            // Exactly 6 numbers
            Assert.Equal(6, ticket.Numbers.Count);

            // All numbers are unique within the ticket
            Assert.Equal(6, ticket.Numbers.Distinct().Count());

            // All numbers in range 1-49
            Assert.All(ticket.Numbers, n => Assert.InRange(n, 1, 49));
        }

        // Verify all tickets in database with positions
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var ticketsInDb = await dbContext.Tickets
            .Include(t => t.Numbers)
            .Where(t => t.UserId == userId)
            .ToListAsync();

        Assert.Equal(9, ticketsInDb.Count);

        foreach (var ticketInDb in ticketsInDb)
        {
            Assert.Equal(6, ticketInDb.Numbers.Count);

            var positions = ticketInDb.Numbers.Select(n => n.Position).OrderBy(p => p).ToList();
            Assert.Equal(new byte[] { 1, 2, 3, 4, 5, 6 }, positions);
        }
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
