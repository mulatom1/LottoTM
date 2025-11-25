using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LottoTM.Server.Api.Entities;
using LottoTM.Server.Api.Features.Tickets.TicketsDeleteAll;
using LottoTM.Server.Api.Repositories;
using LottoTM.Server.Api.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LottoTM.Server.Api.Tests.Features.Tickets.TicketsDeleteAll;

/// <summary>
/// Integration tests for the TicketsDeleteAll endpoint (DELETE /api/tickets/all)
/// </summary>
public class EndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public EndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Test successful deletion of all tickets for authenticated user
    /// </summary>
    [Fact]
    public async Task DeleteAllTickets_WithValidUserAndTickets_Returns200WithDeletedCount()
    {
        // Arrange
        var testDbName = "TestDb_DeleteAll_Success_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);
        SeedTestTickets(factory, userId, 5);

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.DeleteAsync("/api/tickets/all");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<Contracts.Response>();
        Assert.NotNull(result);
        Assert.Equal(5, result.DeletedCount);
        Assert.Contains("5", result.Message);
        Assert.NotEmpty(result.Message);

        // Verify tickets were actually deleted
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var remainingTickets = await dbContext.Tickets.Where(t => t.UserId == userId).CountAsync();
        Assert.Equal(0, remainingTickets);
    }

    /// <summary>
    /// Test deletion when user has no tickets
    /// </summary>
    [Fact]
    public async Task DeleteAllTickets_WithNoTickets_Returns200WithZeroCount()
    {
        // Arrange
        var testDbName = "TestDb_DeleteAll_NoTickets_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);
        // Don't seed any tickets

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.DeleteAsync("/api/tickets/all");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<Contracts.Response>();
        Assert.NotNull(result);
        Assert.Equal(0, result.DeletedCount);
        Assert.NotEmpty(result.Message);
    }

    /// <summary>
    /// Test deletion only deletes tickets for the authenticated user
    /// </summary>
    [Fact]
    public async Task DeleteAllTickets_OnlyDeletesOwnTickets_LeavesOtherUsersTickets()
    {
        // Arrange
        var testDbName = "TestDb_DeleteAll_OwnTicketsOnly_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var user1Id = SeedTestUser(testDbName, "user1@example.com", "Password123!", false);
        var user2Id = SeedTestUser(testDbName, "user2@example.com", "Password123!", false);

        SeedTestTickets(factory, user1Id, 3);
        SeedTestTickets(factory, user2Id, 4);

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, user1Id, "user1@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.DeleteAsync("/api/tickets/all");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<Contracts.Response>();
        Assert.NotNull(result);
        Assert.Equal(3, result.DeletedCount);

        // Verify user1's tickets were deleted
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user1Tickets = await dbContext.Tickets.Where(t => t.UserId == user1Id).CountAsync();
        Assert.Equal(0, user1Tickets);

        // Verify user2's tickets remain intact
        var user2Tickets = await dbContext.Tickets.Where(t => t.UserId == user2Id).CountAsync();
        Assert.Equal(4, user2Tickets);
    }

    /// <summary>
    /// Test deletion with large number of tickets
    /// </summary>
    [Fact]
    public async Task DeleteAllTickets_WithManyTickets_Returns200WithCorrectCount()
    {
        // Arrange
        var testDbName = "TestDb_DeleteAll_ManyTickets_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);
        SeedTestTickets(factory, userId, 20);

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.DeleteAsync("/api/tickets/all");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<Contracts.Response>();
        Assert.NotNull(result);
        Assert.Equal(20, result.DeletedCount);
        Assert.Contains("20", result.Message);
        Assert.NotEmpty(result.Message);

        // Verify all tickets were deleted
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var remainingTickets = await dbContext.Tickets.Where(t => t.UserId == userId).CountAsync();
        Assert.Equal(0, remainingTickets);
    }

    /// <summary>
    /// Test without authentication returns 401
    /// </summary>
    [Fact]
    public async Task DeleteAllTickets_WithoutAuthentication_Returns401()
    {
        // Arrange
        var testDbName = "TestDb_DeleteAll_NoAuth_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);
        var client = factory.CreateClient();

        // Act - no authorization header
        var response = await client.DeleteAsync("/api/tickets/all");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Test with invalid token returns 401
    /// </summary>
    [Fact]
    public async Task DeleteAllTickets_WithInvalidToken_Returns401()
    {
        // Arrange
        var testDbName = "TestDb_DeleteAll_InvalidToken_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");

        // Act
        var response = await client.DeleteAsync("/api/tickets/all");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Test that associated TicketNumbers are also deleted (cascade delete)
    /// </summary>
    [Fact]
    public async Task DeleteAllTickets_AlsoDeletesAssociatedTicketNumbers()
    {
        // Arrange
        var testDbName = "TestDb_DeleteAll_CascadeDelete_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);
        SeedTestTickets(factory, userId, 3);

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Verify TicketNumbers exist before deletion
        int ticketNumbersCountBefore;
        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var ticketIds = await dbContext.Tickets
                .Where(t => t.UserId == userId)
                .Select(t => t.Id)
                .ToListAsync();
            
            ticketNumbersCountBefore = await dbContext.TicketNumbers
                .Where(tn => ticketIds.Contains(tn.TicketId))
                .CountAsync();
            
            Assert.Equal(18, ticketNumbersCountBefore); // 3 tickets * 6 numbers each
        }

        // Act
        var response = await client.DeleteAsync("/api/tickets/all");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Verify TicketNumbers were deleted
        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var remainingTicketNumbers = await dbContext.TicketNumbers
                .Where(tn => dbContext.Tickets
                    .Where(t => t.UserId == userId)
                    .Select(t => t.Id)
                    .Contains(tn.TicketId))
                .CountAsync();
            
            Assert.Equal(0, remainingTicketNumbers);
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
                    ["ConnectionStrings:DefaultConnection"] = "Server=dummy;Database=dummy;Integrated Security=True;",
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
                GroupName = $"Seeded Ticket {i + 1}",
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
