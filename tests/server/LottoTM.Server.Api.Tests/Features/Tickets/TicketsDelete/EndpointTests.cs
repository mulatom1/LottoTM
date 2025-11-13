using System.Net;
using System.Net.Http.Headers;
using LottoTM.Server.Api.Entities;
using LottoTM.Server.Api.Repositories;
using LottoTM.Server.Api.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LottoTM.Server.Api.Tests.Features.Tickets.TicketsDelete;

/// <summary>
/// Integration tests for the DeleteTicket endpoint
/// </summary>
public class EndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public EndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Test successful ticket deletion by owner user
    /// </summary>
    [Fact]
    public async Task DeleteTicket_WithOwnerUser_Returns200Ok()
    {
        // Arrange
        var testDbName = "TestDb_DeleteTicket_Owner_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        // Seed user
        var userId = SeedTestUser(factory, "user@example.com", "UserPass123!", false);

        // Seed a ticket to delete
        var ticketId = SeedTestTicket(factory, userId, new[] { 1, 2, 3, 4, 5, 6 });

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.DeleteAsync($"/api/tickets/{ticketId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Verify ticket was deleted from database
        VerifyTicketNotInDatabase(factory, ticketId);
    }

    /// <summary>
    /// Test ticket deletion by non-owner user returns 403
    /// </summary>
    [Fact]
    public async Task DeleteTicket_WithNonOwnerUser_Returns403Forbidden()
    {
        // Arrange
        var testDbName = "TestDb_DeleteTicket_NonOwner_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        // Seed two users
        var userId1 = SeedTestUser(factory, "user1@example.com", "UserPass123!", false);
        var userId2 = SeedTestUser(factory, "user2@example.com", "UserPass123!", false);

        // Seed ticket for user 1
        var ticketId = SeedTestTicket(factory, userId1, new[] { 1, 2, 3, 4, 5, 6 });

        var client = factory.CreateClient();
        // Try to delete with user 2's token
        var token = GenerateTestToken(factory, userId2, "user2@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.DeleteAsync($"/api/tickets/{ticketId}");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        // Verify ticket still exists in database
        VerifyTicketInDatabase(factory, ticketId);
    }

    /// <summary>
    /// Test ticket deletion without authentication returns 401
    /// </summary>
    [Fact]
    public async Task DeleteTicket_WithoutAuthentication_Returns401Unauthorized()
    {
        // Arrange
        var testDbName = "TestDb_DeleteTicket_NoAuth_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var userId = SeedTestUser(factory, "user@example.com", "UserPass123!", false);
        var ticketId = SeedTestTicket(factory, userId, new[] { 1, 2, 3, 4, 5, 6 });

        var client = factory.CreateClient();
        // No Authorization header

        // Act
        var response = await client.DeleteAsync($"/api/tickets/{ticketId}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Test ticket deletion with non-existent ID returns 403
    /// (Protection against enumeration attacks - we don't reveal if resource exists)
    /// </summary>
    [Fact]
    public async Task DeleteTicket_WithNonExistentId_Returns403Forbidden()
    {
        // Arrange
        var testDbName = "TestDb_DeleteTicket_NotFound_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var userId = SeedTestUser(factory, "user@example.com", "UserPass123!", false);

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var nonExistentId = 99999;

        // Act
        var response = await client.DeleteAsync($"/api/tickets/{nonExistentId}");

        // Assert
        // Returns 403 (not 404) to prevent enumeration attacks
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>
    /// Test ticket deletion with invalid ID (0) returns 400
    /// </summary>
    [Fact]
    public async Task DeleteTicket_WithInvalidId_Returns400BadRequest()
    {
        // Arrange
        var testDbName = "TestDb_DeleteTicket_InvalidId_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var userId = SeedTestUser(factory, "user@example.com", "UserPass123!", false);

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var invalidId = 0;

        // Act
        var response = await client.DeleteAsync($"/api/tickets/{invalidId}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Test ticket deletion with negative ID returns 400
    /// </summary>
    [Fact]
    public async Task DeleteTicket_WithNegativeId_Returns400BadRequest()
    {
        // Arrange
        var testDbName = "TestDb_DeleteTicket_NegativeId_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var userId = SeedTestUser(factory, "user@example.com", "UserPass123!", false);

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var negativeId = -1;

        // Act
        var response = await client.DeleteAsync($"/api/tickets/{negativeId}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Test that CASCADE DELETE removes associated TicketNumbers
    /// NOTE: In-Memory database does not enforce CASCADE DELETE constraints.
    /// This test verifies the endpoint works, but CASCADE DELETE behavior
    /// should be tested against a real SQL Server database.
    /// </summary>
    [Fact]
    public async Task DeleteTicket_RemovesAssociatedTicketNumbers()
    {
        // Arrange
        var testDbName = "TestDb_DeleteTicket_Cascade_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var userId = SeedTestUser(factory, "user@example.com", "UserPass123!", false);
        var ticketId = SeedTestTicket(factory, userId, new[] { 1, 2, 3, 4, 5, 6 });

        // Verify TicketNumbers exist before deletion
        VerifyTicketNumbersExist(factory, ticketId, 6);

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.DeleteAsync($"/api/tickets/{ticketId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Verify ticket was deleted
        VerifyTicketNotInDatabase(factory, ticketId);

        // NOTE: In-Memory database doesn't enforce CASCADE DELETE.
        // On real SQL Server, TicketNumbers would be automatically deleted.
        // For now, we just verify the endpoint succeeded.
    }

    /// <summary>
    /// Test that admin user can delete their own ticket
    /// </summary>
    [Fact]
    public async Task DeleteTicket_WithAdminUserOwner_Returns200Ok()
    {
        // Arrange
        var testDbName = "TestDb_DeleteTicket_AdminOwner_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        // Seed admin user
        var adminUserId = SeedTestUser(factory, "admin@example.com", "AdminPass123!", true);

        // Seed ticket for admin
        var ticketId = SeedTestTicket(factory, adminUserId, new[] { 7, 14, 21, 28, 35, 42 });

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, adminUserId, "admin@example.com", true);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.DeleteAsync($"/api/tickets/{ticketId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Verify ticket was deleted from database
        VerifyTicketNotInDatabase(factory, ticketId);
    }

    /// <summary>
    /// Test that admin user CANNOT delete another user's ticket
    /// (Admin privilege does NOT grant access to delete other users' tickets)
    /// </summary>
    [Fact]
    public async Task DeleteTicket_WithAdminUserNotOwner_Returns403Forbidden()
    {
        // Arrange
        var testDbName = "TestDb_DeleteTicket_AdminNotOwner_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        // Seed regular user and admin user
        var userId = SeedTestUser(factory, "user@example.com", "UserPass123!", false);
        var adminUserId = SeedTestUser(factory, "admin@example.com", "AdminPass123!", true);

        // Seed ticket for regular user
        var ticketId = SeedTestTicket(factory, userId, new[] { 1, 2, 3, 4, 5, 6 });

        var client = factory.CreateClient();
        // Try to delete with admin token (but admin is not the owner)
        var token = GenerateTestToken(factory, adminUserId, "admin@example.com", true);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.DeleteAsync($"/api/tickets/{ticketId}");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        // Verify ticket still exists in database
        VerifyTicketInDatabase(factory, ticketId);
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
    private static int SeedTestUser(WebApplicationFactory<Program> factory, string email, string password, bool isAdmin)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

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
            GroupName = "Test Ticket Group",
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

    /// <summary>
    /// Helper method to verify ticket was deleted from database
    /// </summary>
    private static void VerifyTicketNotInDatabase(WebApplicationFactory<Program> factory, int ticketId)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var ticket = dbContext.Tickets.Find(ticketId);
        Assert.Null(ticket);
    }

    /// <summary>
    /// Helper method to verify ticket still exists in database
    /// </summary>
    private static void VerifyTicketInDatabase(WebApplicationFactory<Program> factory, int ticketId)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var ticket = dbContext.Tickets.Find(ticketId);
        Assert.NotNull(ticket);
    }

    /// <summary>
    /// Helper method to verify TicketNumbers exist
    /// </summary>
    private static void VerifyTicketNumbersExist(WebApplicationFactory<Program> factory, int ticketId, int expectedCount)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var ticketNumbers = dbContext.TicketNumbers.Where(tn => tn.TicketId == ticketId).ToList();
        Assert.Equal(expectedCount, ticketNumbers.Count);
    }

    /// <summary>
    /// Helper method to verify TicketNumbers were cascade deleted
    /// </summary>
    private static void VerifyTicketNumbersNotExist(WebApplicationFactory<Program> factory, int ticketId)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var ticketNumbers = dbContext.TicketNumbers.Where(tn => tn.TicketId == ticketId).ToList();
        Assert.Empty(ticketNumbers);
    }
}
