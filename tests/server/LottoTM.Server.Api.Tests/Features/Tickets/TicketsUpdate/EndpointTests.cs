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

namespace LottoTM.Server.Api.Tests.Features.Tickets.TicketsUpdate;

/// <summary>
/// Integration tests for the UpdateTicket endpoint
/// </summary>
public class EndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public EndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Test successful ticket update with valid data
    /// </summary>
    [Fact]
    public async Task UpdateTicket_WithValidData_Returns200OK()
    {
        // Arrange
        var testDbName = "TestDb_ValidUpdate_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        // Seed database with a user
        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        // Seed existing ticket
        var ticketId = SeedTestTicket(factory, userId, new[] { 1, 2, 3, 4, 5, 6 });

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new
        {
            numbers = new[] { 7, 15, 22, 33, 38, 45 }
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/tickets/{ticketId}", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.NotNull(result);
        Assert.Equal("Zestaw zaktualizowany pomyślnie", result["message"]);

        // Verify in database
        VerifyTicketInDatabase(factory, ticketId, new[] { 7, 15, 22, 33, 38, 45 });
    }

    /// <summary>
    /// Test ticket update without authentication returns 401
    /// </summary>
    [Fact]
    public async Task UpdateTicket_WithoutAuthentication_Returns401Unauthorized()
    {
        // Arrange
        var testDbName = "TestDb_NoAuth_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);
        var client = factory.CreateClient();

        var request = new
        {
            numbers = new[] { 7, 15, 22, 33, 38, 45 }
        };

        // Act
        var response = await client.PutAsJsonAsync("/api/tickets/1", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Test ticket update by different user returns 403
    /// </summary>
    [Fact]
    public async Task UpdateTicket_WithDifferentUser_Returns403Forbidden()
    {
        // Arrange
        var testDbName = "TestDb_DifferentUser_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        // Seed two users
        var user1Id = SeedTestUser(testDbName, "user1@example.com", "Password123!", false);
        var user2Id = SeedTestUser(testDbName, "user2@example.com", "Password123!", false);

        // Seed ticket for user1
        var ticketId = SeedTestTicket(factory, user1Id, new[] { 1, 2, 3, 4, 5, 6 });

        // Try to update with user2's token
        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, user2Id, "user2@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new
        {
            numbers = new[] { 7, 15, 22, 33, 38, 45 }
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/tickets/{ticketId}", request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>
    /// Test ticket update with non-existent ID returns 404
    /// </summary>
    [Fact]
    public async Task UpdateTicket_WithNonExistentId_Returns404NotFound()
    {
        // Arrange
        var testDbName = "TestDb_NotFound_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new
        {
            numbers = new[] { 7, 15, 22, 33, 38, 45 }
        };

        // Act
        var response = await client.PutAsJsonAsync("/api/tickets/99999", request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// Test ticket update with invalid number count returns 400
    /// </summary>
    [Fact]
    public async Task UpdateTicket_WithInvalidNumberCount_Returns400BadRequest()
    {
        // Arrange
        var testDbName = "TestDb_InvalidCount_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);
        var ticketId = SeedTestTicket(factory, userId, new[] { 1, 2, 3, 4, 5, 6 });

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new
        {
            numbers = new[] { 7, 15, 22, 33, 38 } // only 5 numbers
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/tickets/{ticketId}", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Test ticket update with numbers out of range returns 400
    /// </summary>
    [Fact]
    public async Task UpdateTicket_WithNumbersOutOfRange_Returns400BadRequest()
    {
        // Arrange
        var testDbName = "TestDb_OutOfRange_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);
        var ticketId = SeedTestTicket(factory, userId, new[] { 1, 2, 3, 4, 5, 6 });

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new
        {
            numbers = new[] { 0, 15, 22, 33, 38, 45 } // 0 is out of range
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/tickets/{ticketId}", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Test ticket update with duplicate numbers returns 400
    /// </summary>
    [Fact]
    public async Task UpdateTicket_WithDuplicateNumbers_Returns400BadRequest()
    {
        // Arrange
        var testDbName = "TestDb_Duplicates_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);
        var ticketId = SeedTestTicket(factory, userId, new[] { 1, 2, 3, 4, 5, 6 });

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new
        {
            numbers = new[] { 7, 7, 22, 33, 38, 45 } // duplicate 7
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/tickets/{ticketId}", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Test ticket update with duplicate ticket set returns 400
    /// </summary>
    [Fact]
    public async Task UpdateTicket_WithDuplicateTicketSet_Returns400BadRequest()
    {
        // Arrange
        var testDbName = "TestDb_DuplicateSet_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        // Seed two existing tickets
        var ticketId1 = SeedTestTicket(factory, userId, new[] { 1, 2, 3, 4, 5, 6 });
        var ticketId2 = SeedTestTicket(factory, userId, new[] { 7, 8, 9, 10, 11, 12 });

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Try to update ticketId1 to the same numbers as ticketId2
        var request = new
        {
            numbers = new[] { 7, 8, 9, 10, 11, 12 } // already exists in ticketId2
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/tickets/{ticketId1}", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Test ticket update with same numbers (no change) is allowed
    /// </summary>
    [Fact]
    public async Task UpdateTicket_WithSameNumbers_Returns200OK()
    {
        // Arrange
        var testDbName = "TestDb_SameNumbers_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);
        var ticketId = SeedTestTicket(factory, userId, new[] { 1, 2, 3, 4, 5, 6 });

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new
        {
            numbers = new[] { 1, 2, 3, 4, 5, 6 } // same numbers
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/tickets/{ticketId}", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// Test ticket update with numbers in different order (should be allowed)
    /// </summary>
    [Fact]
    public async Task UpdateTicket_WithDifferentOrder_Returns200OK()
    {
        // Arrange
        var testDbName = "TestDb_DifferentOrder_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);
        var ticketId = SeedTestTicket(factory, userId, new[] { 1, 2, 3, 4, 5, 6 });

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new
        {
            numbers = new[] { 6, 5, 4, 3, 2, 1 } // reversed order
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/tickets/{ticketId}", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Verify the new order is saved
        VerifyTicketInDatabase(factory, ticketId, new[] { 6, 5, 4, 3, 2, 1 });
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
    private static int SeedTestTicket(WebApplicationFactory<Program> factory, int userId, int[] numbers)
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
    /// Helper method to verify ticket was updated in database
    /// </summary>
    private static void VerifyTicketInDatabase(WebApplicationFactory<Program> factory, int ticketId, int[] expectedNumbers)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var ticket = dbContext.Tickets
            .Include(t => t.Numbers)
            .FirstOrDefault(t => t.Id == ticketId);

        Assert.NotNull(ticket);
        Assert.Equal(6, ticket.Numbers.Count);

        var actualNumbers = ticket.Numbers.OrderBy(n => n.Position).Select(n => n.Number).ToArray();
        Assert.Equal(expectedNumbers, actualNumbers);
    }
}
