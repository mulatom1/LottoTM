using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LottoTM.Server.Api.Entities;
using LottoTM.Server.Api.Features.Verification.Check;
using LottoTM.Server.Api.Repositories;
using LottoTM.Server.Api.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LottoTM.Server.Api.Tests.Features.Verification.Check;

/// <summary>
/// Integration tests for the verification check endpoint (POST /api/verification/check)
/// </summary>
public class EndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public EndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Test successful verification with matching tickets
    /// </summary>
    [Fact]
    public async Task CheckVerification_WithMatchingTickets_Returns200WithResults()
    {
        // Arrange
        var testDbName = "TestDb_Check_Valid_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        // Create a ticket with numbers: 5, 14, 23, 29, 37, 41
        var ticketId = SeedTestTicket(factory, userId, new[] { 5, 14, 23, 29, 37, 41 });

        // Create a draw with 3 matching numbers: 14, 23, 37
        SeedTestDraw(factory, userId, DateOnly.Parse("2025-10-15"), new[] { 3, 14, 23, 31, 37, 48 });

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new Contracts.Request(
            DateOnly.Parse("2025-10-01"),
            DateOnly.Parse("2025-10-31")
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/verification/check", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<Contracts.Response>();
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalTickets);
        Assert.Equal(1, result.TotalDraws);
        Assert.Single(result.Results); // One ticket with hits

        var ticketResult = result.Results[0];
        Assert.Equal(ticketId, ticketResult.TicketId);
        Assert.NotNull(ticketResult.GroupName);
        Assert.Equal(6, ticketResult.TicketNumbers.Count);
        Assert.Single(ticketResult.Draws); // One draw with 3+ hits

        var drawResult = ticketResult.Draws[0];
        Assert.Equal("LOTTO", drawResult.LottoType);
        Assert.Equal(3, drawResult.Hits);
        Assert.Equal(3, drawResult.WinningNumbers.Count);
        Assert.Contains(14, drawResult.WinningNumbers);
        Assert.Contains(23, drawResult.WinningNumbers);
        Assert.Contains(37, drawResult.WinningNumbers);
    }

    /// <summary>
    /// Test verification with no matching tickets (less than 3 hits)
    /// </summary>
    [Fact]
    public async Task CheckVerification_WithNoMatches_ReturnsEmptyResults()
    {
        // Arrange
        var testDbName = "TestDb_Check_NoMatch_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        // Create a ticket with numbers that don't match
        SeedTestTicket(factory, userId, new[] { 1, 2, 3, 4, 5, 6 });

        // Create a draw with completely different numbers
        SeedTestDraw(factory, userId, DateOnly.Parse("2025-10-15"), new[] { 40, 41, 42, 43, 44, 45 });

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new Contracts.Request(
            DateOnly.Parse("2025-10-01"),
            DateOnly.Parse("2025-10-31")
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/verification/check", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<Contracts.Response>();
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalTickets);
        Assert.Equal(1, result.TotalDraws);
        Assert.Empty(result.Results); // No tickets with 3+ hits
    }

    /// <summary>
    /// Test verification with multiple tickets and draws
    /// </summary>
    [Fact]
    public async Task CheckVerification_WithMultipleTicketsAndDraws_ReturnsCorrectResults()
    {
        // Arrange
        var testDbName = "TestDb_Check_Multiple_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        // Create 3 tickets
        var ticket1Id = SeedTestTicket(factory, userId, new[] { 5, 14, 23, 29, 37, 41 });
        var ticket2Id = SeedTestTicket(factory, userId, new[] { 10, 20, 30, 40, 45, 48 });
        var ticket3Id = SeedTestTicket(factory, userId, new[] { 1, 2, 3, 4, 5, 6 });

        // Create 2 draws - first matches ticket1, second matches ticket2
        SeedTestDraw(factory, userId, DateOnly.Parse("2025-10-15"), new[] { 5, 14, 23, 31, 32, 33 });
        SeedTestDraw(factory, userId, DateOnly.Parse("2025-10-20"), new[] { 10, 20, 30, 35, 36, 37 });

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new Contracts.Request(
            DateOnly.Parse("2025-10-01"),
            DateOnly.Parse("2025-10-31")
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/verification/check", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<Contracts.Response>();
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalTickets);
        Assert.Equal(2, result.TotalDraws);
        Assert.Equal(2, result.Results.Count); // Two tickets with hits
    }

    /// <summary>
    /// Test with exactly 3 matching numbers (boundary condition)
    /// </summary>
    [Fact]
    public async Task CheckVerification_WithExactly3Matches_IncludesInResults()
    {
        // Arrange
        var testDbName = "TestDb_Check_Exactly3_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        SeedTestTicket(factory, userId, new[] { 5, 14, 23, 29, 37, 41 });

        // Create draw with exactly 3 matching numbers
        SeedTestDraw(factory, userId, DateOnly.Parse("2025-10-15"), new[] { 5, 14, 23, 31, 32, 33 });

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new Contracts.Request(
            DateOnly.Parse("2025-10-01"),
            DateOnly.Parse("2025-10-31")
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/verification/check", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<Contracts.Response>();
        Assert.NotNull(result);
        Assert.Single(result.Results);
        Assert.Equal(3, result.Results[0].Draws[0].Hits);
    }

    /// <summary>
    /// Test with only 2 matching numbers (should not be included)
    /// </summary>
    [Fact]
    public async Task CheckVerification_WithOnly2Matches_ExcludesFromResults()
    {
        // Arrange
        var testDbName = "TestDb_Check_Only2_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        SeedTestTicket(factory, userId, new[] { 5, 14, 23, 29, 37, 41 });

        // Create draw with only 2 matching numbers
        SeedTestDraw(factory, userId, DateOnly.Parse("2025-10-15"), new[] { 5, 14, 31, 32, 33, 34 });

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new Contracts.Request(
            DateOnly.Parse("2025-10-01"),
            DateOnly.Parse("2025-10-31")
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/verification/check", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<Contracts.Response>();
        Assert.NotNull(result);
        Assert.Empty(result.Results); // Should not include tickets with less than 3 hits
    }

    /// <summary>
    /// Test with date range validation - DateTo before DateFrom
    /// </summary>
    [Fact]
    public async Task CheckVerification_WithInvalidDateRange_Returns400()
    {
        // Arrange
        var testDbName = "TestDb_Check_InvalidRange_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new Contracts.Request(
            DateOnly.Parse("2025-10-31"),
            DateOnly.Parse("2025-10-01") // DateTo before DateFrom
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/verification/check", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Test with date range exceeding 31 days
    /// </summary>
    [Fact]
    public async Task CheckVerification_WithRangeExceeding31Days_Returns400()
    {
        // Arrange
        var testDbName = "TestDb_Check_RangeTooLarge_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new Contracts.Request(
            DateOnly.Parse("2025-10-01"),
            DateOnly.Parse("2025-11-05") // 35 days - exceeds 31 day limit
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/verification/check", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Test without authentication returns 401
    /// </summary>
    [Fact]
    public async Task CheckVerification_WithoutAuthentication_Returns401()
    {
        // Arrange
        var testDbName = "TestDb_Check_NoAuth_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);
        var client = factory.CreateClient();

        var request = new Contracts.Request(
            DateOnly.Parse("2025-10-01"),
            DateOnly.Parse("2025-10-31")
        );

        // Act - no authorization header
        var response = await client.PostAsJsonAsync("/api/verification/check", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Test with no tickets in database
    /// </summary>
    [Fact]
    public async Task CheckVerification_WithNoTickets_ReturnsEmptyResults()
    {
        // Arrange
        var testDbName = "TestDb_Check_NoTickets_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        // Create only draws, no tickets
        SeedTestDraw(factory, userId, DateOnly.Parse("2025-10-15"), new[] { 3, 14, 23, 31, 37, 48 });

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new Contracts.Request(
            DateOnly.Parse("2025-10-01"),
            DateOnly.Parse("2025-10-31")
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/verification/check", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<Contracts.Response>();
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalTickets);
        Assert.Equal(1, result.TotalDraws);
        Assert.Empty(result.Results);
    }

    /// <summary>
    /// Test with no draws in date range
    /// </summary>
    [Fact]
    public async Task CheckVerification_WithNoDrawsInRange_ReturnsEmptyResults()
    {
        // Arrange
        var testDbName = "TestDb_Check_NoDraws_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        SeedTestTicket(factory, userId, new[] { 5, 14, 23, 29, 37, 41 });

        // Create draw outside the requested date range
        SeedTestDraw(factory, userId, DateOnly.Parse("2025-09-15"), new[] { 3, 14, 23, 31, 37, 48 });

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new Contracts.Request(
            DateOnly.Parse("2025-10-01"),
            DateOnly.Parse("2025-10-31")
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/verification/check", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<Contracts.Response>();
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalTickets);
        Assert.Equal(0, result.TotalDraws);
        Assert.Empty(result.Results);
    }

    /// <summary>
    /// Test that user only sees their own tickets (isolation test)
    /// </summary>
    [Fact]
    public async Task CheckVerification_OnlyChecksCurrentUserTickets()
    {
        // Arrange
        var testDbName = "TestDb_Check_UserIsolation_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var user1Id = SeedTestUser(testDbName, "user1@example.com", "Password123!", false);
        var user2Id = SeedTestUser(testDbName, "user2@example.com", "Password123!", false);

        // Create tickets for both users
        SeedTestTicket(factory, user1Id, new[] { 5, 14, 23, 29, 37, 41 });
        SeedTestTicket(factory, user2Id, new[] { 10, 20, 30, 40, 45, 48 });

        // Create draw that matches both tickets
        SeedTestDraw(factory, user1Id, DateOnly.Parse("2025-10-15"), new[] { 5, 14, 23, 31, 32, 33 });

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, user1Id, "user1@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new Contracts.Request(
            DateOnly.Parse("2025-10-01"),
            DateOnly.Parse("2025-10-31")
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/verification/check", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<Contracts.Response>();
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalTickets); // Only user1's ticket
        Assert.Single(result.Results);
    }

    /// <summary>
    /// Test with both LOTTO and LOTTO PLUS draws
    /// </summary>
    [Fact]
    public async Task CheckVerification_WithBothLottoTypes_ReturnsCorrectLottoTypes()
    {
        // Arrange
        var testDbName = "TestDb_Check_BothTypes_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        // Create a ticket that matches both draws
        SeedTestTicket(factory, userId, new[] { 5, 14, 23, 29, 37, 41 });

        // Create LOTTO draw with 3 matching numbers: 5, 14, 23
        SeedTestDrawWithType(factory, userId, DateOnly.Parse("2025-10-15"), new[] { 5, 14, 23, 31, 32, 33 }, "LOTTO");

        // Create LOTTO PLUS draw with 3 matching numbers: 14, 23, 29
        SeedTestDrawWithType(factory, userId, DateOnly.Parse("2025-10-16"), new[] { 14, 23, 29, 35, 36, 38 }, "LOTTO PLUS");

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new Contracts.Request(
            DateOnly.Parse("2025-10-01"),
            DateOnly.Parse("2025-10-31")
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/verification/check", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<Contracts.Response>();
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalTickets);
        Assert.Equal(2, result.TotalDraws);
        Assert.Single(result.Results); // One ticket with hits

        var ticketResult = result.Results[0];
        Assert.Equal(2, ticketResult.Draws.Count); // Two draws with 3+ hits

        // Verify LOTTO draw
        var lottoDraw = ticketResult.Draws.FirstOrDefault(d => d.LottoType == "LOTTO");
        Assert.NotNull(lottoDraw);
        Assert.Equal(3, lottoDraw.Hits);

        // Verify LOTTO PLUS draw
        var lottoPlusDraw = ticketResult.Draws.FirstOrDefault(d => d.LottoType == "LOTTO PLUS");
        Assert.NotNull(lottoPlusDraw);
        Assert.Equal(3, lottoPlusDraw.Hits);
    }

    /// <summary>
    /// Test execution time is included in response
    /// </summary>
    [Fact]
    public async Task CheckVerification_IncludesExecutionTime()
    {
        // Arrange
        var testDbName = "TestDb_Check_ExecutionTime_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);
        SeedTestTicket(factory, userId, new[] { 5, 14, 23, 29, 37, 41 });
        SeedTestDraw(factory, userId, DateOnly.Parse("2025-10-15"), new[] { 5, 14, 23, 31, 37, 48 });

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new Contracts.Request(
            DateOnly.Parse("2025-10-01"),
            DateOnly.Parse("2025-10-31")
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/verification/check", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<Contracts.Response>();
        Assert.NotNull(result);
        Assert.True(result.ExecutionTimeMs >= 0);
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
    /// Helper method to seed a test ticket with specified numbers
    /// </summary>
    private static int SeedTestTicket(WebApplicationFactory<Program> factory, int userId, int[] numbers)
    {
        if (numbers.Length != 6)
        {
            throw new ArgumentException("Ticket must have exactly 6 numbers", nameof(numbers));
        }

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var ticket = new Ticket
        {
            UserId = userId,
            GroupName = $"TestTicket: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Tickets.Add(ticket);
        dbContext.SaveChanges();

        // Add numbers
        for (int i = 0; i < numbers.Length; i++)
        {
            var ticketNumber = new TicketNumber
            {
                TicketId = ticket.Id,
                Number = numbers[i],
                Position = (byte)(i + 1)
            };
            dbContext.TicketNumbers.Add(ticketNumber);
        }

        dbContext.SaveChanges();
        return ticket.Id;
    }

    /// <summary>
    /// Helper method to seed a test draw with specified numbers
    /// </summary>
    private static void SeedTestDraw(WebApplicationFactory<Program> factory, int userId, DateOnly drawDate, int[] numbers)
    {
        SeedTestDrawWithType(factory, userId, drawDate, numbers, "LOTTO");
    }

    /// <summary>
    /// Helper method to seed a test draw with specified numbers and lotto type
    /// </summary>
    private static void SeedTestDrawWithType(WebApplicationFactory<Program> factory, int userId, DateOnly drawDate, int[] numbers, string lottoType)
    {
        if (numbers.Length != 6)
        {
            throw new ArgumentException("Draw must have exactly 6 numbers", nameof(numbers));
        }

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var draw = new Draw
        {
            DrawDate = drawDate,
            LottoType = lottoType,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = userId
        };

        dbContext.Draws.Add(draw);
        dbContext.SaveChanges();

        // Add numbers
        for (int i = 0; i < numbers.Length; i++)
        {
            var drawNumber = new DrawNumber
            {
                DrawId = draw.Id,
                Number = numbers[i],
                Position = (byte)(i + 1)
            };
            dbContext.DrawNumbers.Add(drawNumber);
        }

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
}
