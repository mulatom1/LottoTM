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
            DateOnly.Parse("2025-10-31"),
            null // No group filter
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/verification/check", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<Contracts.Response>();
        Assert.NotNull(result);
        Assert.Single(result.TicketsResults); // One ticket
        Assert.Single(result.DrawsResults); // One draw

        var ticketResult = result.TicketsResults[0];
        Assert.Equal(ticketId, ticketResult.TicketId);
        Assert.NotNull(ticketResult.GroupName);
        Assert.Equal(6, ticketResult.TicketNumbers.Count);

        var drawResult = result.DrawsResults[0];
        Assert.Equal("LOTTO", drawResult.LottoType);
        Assert.Single(drawResult.WinningTicketsResult); // One winning ticket
        
        var winningTicket = drawResult.WinningTicketsResult[0];
        Assert.Equal(ticketId, winningTicket.TicketId);
        Assert.Equal(3, winningTicket.MatchingNumbers.Count);
        Assert.Contains(14, winningTicket.MatchingNumbers);
        Assert.Contains(23, winningTicket.MatchingNumbers);
        Assert.Contains(37, winningTicket.MatchingNumbers);

        // Verify draw fields
        Assert.True(drawResult.DrawSystemId > 0);
        Assert.Equal(3.00m, drawResult.TicketPrice);
        Assert.Equal(2, drawResult.WinPoolCount1);
        Assert.Equal(5000000.00m, drawResult.WinPoolAmount1);
        Assert.Equal(15, drawResult.WinPoolCount2);
        Assert.Equal(50000.00m, drawResult.WinPoolAmount2);
        Assert.Equal(120, drawResult.WinPoolCount3);
        Assert.Equal(500.00m, drawResult.WinPoolAmount3);
        Assert.Equal(850, drawResult.WinPoolCount4);
        Assert.Equal(20.00m, drawResult.WinPoolAmount4);
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
            DateOnly.Parse("2025-10-31"),
            null // No group filter
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/verification/check", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<Contracts.Response>();
        Assert.NotNull(result);
        Assert.Single(result.TicketsResults); // Ticket exists
        Assert.Single(result.DrawsResults); // Draw exists
        Assert.Empty(result.DrawsResults[0].WinningTicketsResult); // No winning tickets (less than 3 matches)
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
            DateOnly.Parse("2025-10-31"),
            null // No group filter
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/verification/check", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<Contracts.Response>();
        Assert.NotNull(result);
        Assert.Equal(3, result.TicketsResults.Count); // Three tickets
        Assert.Equal(2, result.DrawsResults.Count); // Two draws
        
        // Each draw should have one winning ticket
        Assert.All(result.DrawsResults, d => Assert.Single(d.WinningTicketsResult));
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

        var ticketId = SeedTestTicket(factory, userId, new[] { 5, 14, 23, 29, 37, 41 });

        // Create draw with exactly 3 matching numbers
        SeedTestDraw(factory, userId, DateOnly.Parse("2025-10-15"), new[] { 5, 14, 23, 31, 32, 33 });

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new Contracts.Request(
            DateOnly.Parse("2025-10-01"),
            DateOnly.Parse("2025-10-31"),
            null // No group filter
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/verification/check", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<Contracts.Response>();
        Assert.NotNull(result);
        Assert.Single(result.DrawsResults);
        Assert.Single(result.DrawsResults[0].WinningTicketsResult);
        Assert.Equal(3, result.DrawsResults[0].WinningTicketsResult[0].MatchingNumbers.Count);
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
            DateOnly.Parse("2025-10-31"),
            null // No group filter
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/verification/check", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<Contracts.Response>();
        Assert.NotNull(result);
        Assert.Empty(result.DrawsResults[0].WinningTicketsResult); // Should not include tickets with less than 3 matches
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
            DateOnly.Parse("2025-10-01"), // DateTo before DateFrom
            null // No group filter
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/verification/check", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Test with date range exceeding configured maximum days (31 days by default)
    /// </summary>
    [Fact]
    public async Task CheckVerification_WithRangeExceedingConfiguredDays_Returns400()
    {
        // Arrange
        var testDbName = "TestDb_Check_RangeTooLarge_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new Contracts.Request(
            DateOnly.Parse("2025-01-01"),
            DateOnly.Parse("2025-02-15"), // 45 days - exceeds 31 day limit configured in Features:Verification:Days
            null // No group filter
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
            DateOnly.Parse("2025-10-31"),
            null // No group filter
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
            DateOnly.Parse("2025-10-31"),
            null // No group filter
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/verification/check", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<Contracts.Response>();
        Assert.NotNull(result);
        Assert.Empty(result.TicketsResults);
        Assert.Single(result.DrawsResults);
        Assert.Empty(result.DrawsResults[0].WinningTicketsResult);
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
            DateOnly.Parse("2025-10-31"),
            null // No group filter
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/verification/check", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<Contracts.Response>();
        Assert.NotNull(result);
        Assert.Single(result.TicketsResults);
        Assert.Empty(result.DrawsResults);
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
            DateOnly.Parse("2025-10-31"),
            null // No group filter
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/verification/check", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<Contracts.Response>();
        Assert.NotNull(result);
        Assert.Single(result.TicketsResults); // Only user1's ticket
        Assert.Single(result.DrawsResults[0].WinningTicketsResult);
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
        var ticketId = SeedTestTicket(factory, userId, new[] { 5, 14, 23, 29, 37, 41 });

        // Create LOTTO draw with 3 matching numbers: 5, 14, 23
        SeedTestDrawWithType(factory, userId, DateOnly.Parse("2025-10-15"), new[] { 5, 14, 23, 31, 32, 33 }, "LOTTO");

        // Create LOTTO PLUS draw with 3 matching numbers: 14, 23, 29
        SeedTestDrawWithType(factory, userId, DateOnly.Parse("2025-10-16"), new[] { 14, 23, 29, 35, 36, 38 }, "LOTTO PLUS");

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new Contracts.Request(
            DateOnly.Parse("2025-10-01"),
            DateOnly.Parse("2025-10-31"),
            null // No group filter
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/verification/check", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<Contracts.Response>();
        Assert.NotNull(result);
        Assert.Single(result.TicketsResults);
        Assert.Equal(2, result.DrawsResults.Count); // Two draws

        // Verify LOTTO draw
        var lottoDraw = result.DrawsResults.FirstOrDefault(d => d.LottoType == "LOTTO");
        Assert.NotNull(lottoDraw);
        Assert.Single(lottoDraw.WinningTicketsResult);
        Assert.Equal(3, lottoDraw.WinningTicketsResult[0].MatchingNumbers.Count);
        Assert.True(lottoDraw.DrawSystemId > 0);
        Assert.NotNull(lottoDraw.TicketPrice);

        // Verify LOTTO PLUS draw
        var lottoPlusDraw = result.DrawsResults.FirstOrDefault(d => d.LottoType == "LOTTO PLUS");
        Assert.NotNull(lottoPlusDraw);
        Assert.Single(lottoPlusDraw.WinningTicketsResult);
        Assert.Equal(3, lottoPlusDraw.WinningTicketsResult[0].MatchingNumbers.Count);
        Assert.True(lottoPlusDraw.DrawSystemId > 0);
        Assert.NotNull(lottoPlusDraw.TicketPrice);
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
            DateOnly.Parse("2025-10-31"),
            null // No group filter
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
    /// Test filtering by GroupName - partial match (contains) - only tickets with group names containing the search text are checked
    /// </summary>
    [Fact]
    public async Task CheckVerification_WithGroupNameFilter_UsesPartialMatch()
    {
        // Arrange
        var testDbName = "TestDb_Check_GroupFilter_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        // Create tickets with different group names
        var ticket1Id = SeedTestTicketWithGroup(factory, userId, new[] { 5, 14, 23, 29, 37, 41 }, "Ulubione");
        var ticket2Id = SeedTestTicketWithGroup(factory, userId, new[] { 10, 20, 30, 40, 45, 48 }, "Testowe");
        var ticket3Id = SeedTestTicketWithGroup(factory, userId, new[] { 1, 2, 3, 4, 5, 6 }, "Ulubione 2024");

        // Create a draw that matches all tickets
        SeedTestDraw(factory, userId, DateOnly.Parse("2025-10-15"), new[] { 5, 14, 23, 31, 32, 33 });

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new Contracts.Request(
            DateOnly.Parse("2025-10-01"),
            DateOnly.Parse("2025-10-31"),
            "ubi" // Partial match - should find "Ulubione" and "Ulubione 2024"
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/verification/check", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<Contracts.Response>();
        Assert.NotNull(result);
        Assert.Equal(2, result.TicketsResults.Count); // Only 2 tickets containing "ubi"
        Assert.Single(result.DrawsResults);
        Assert.All(result.TicketsResults, r => Assert.Contains("ubi", r.GroupName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Test filtering by GroupName - case-insensitive matching
    /// </summary>
    [Fact]
    public async Task CheckVerification_WithGroupNameFilter_CaseInsensitive()
    {
        // Arrange
        var testDbName = "TestDb_Check_GroupCaseInsensitive_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        // Create tickets with mixed case group names
        var ticket1Id = SeedTestTicketWithGroup(factory, userId, new[] { 5, 14, 23, 29, 37, 41 }, "Ulubione");
        var ticket2Id = SeedTestTicketWithGroup(factory, userId, new[] { 10, 20, 30, 40, 45, 48 }, "TESTOWE");

        // Create a draw that matches first ticket
        SeedTestDraw(factory, userId, DateOnly.Parse("2025-10-15"), new[] { 5, 14, 23, 31, 32, 33 });

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new Contracts.Request(
            DateOnly.Parse("2025-10-01"),
            DateOnly.Parse("2025-10-31"),
            "ULU" // Uppercase search should match "Ulubione"
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/verification/check", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<Contracts.Response>();
        Assert.NotNull(result);
        Assert.Single(result.TicketsResults); // Only 1 ticket matches (case-insensitive)
        Assert.Single(result.DrawsResults);
        Assert.Single(result.DrawsResults[0].WinningTicketsResult);
        Assert.Equal("Ulubione", result.TicketsResults[0].GroupName);
    }

    /// <summary>
    /// Test filtering by non-matching partial GroupName returns empty results
    /// </summary>
    [Fact]
    public async Task CheckVerification_WithNonExistentGroupName_ReturnsEmptyResults()
    {
        // Arrange
        var testDbName = "TestDb_Check_NonExistentGroup_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        // Create tickets with group name "Ulubione"
        SeedTestTicketWithGroup(factory, userId, new[] { 5, 14, 23, 29, 37, 41 }, "Ulubione");

        // Create a draw that matches the ticket
        SeedTestDraw(factory, userId, DateOnly.Parse("2025-10-15"), new[] { 5, 14, 23, 31, 32, 33 });

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new Contracts.Request(
            DateOnly.Parse("2025-10-01"),
            DateOnly.Parse("2025-10-31"),
            "xyz" // No group contains "xyz"
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/verification/check", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<Contracts.Response>();
        Assert.NotNull(result);
        Assert.Empty(result.TicketsResults); // No tickets match the filter
        Assert.Single(result.DrawsResults);
        Assert.Empty(result.DrawsResults[0].WinningTicketsResult);
    }

    /// <summary>
    /// Test with empty GroupName checks all tickets
    /// </summary>
    [Fact]
    public async Task CheckVerification_WithEmptyGroupName_ChecksAllTickets()
    {
        // Arrange
        var testDbName = "TestDb_Check_EmptyGroup_" + Guid.NewGuid();
        var factory = CreateTestFactory(testDbName);

        var userId = SeedTestUser(testDbName, "user@example.com", "Password123!", false);

        // Create tickets with different groups
        SeedTestTicketWithGroup(factory, userId, new[] { 5, 14, 23, 29, 37, 41 }, "Ulubione");
        SeedTestTicketWithGroup(factory, userId, new[] { 10, 20, 30, 40, 45, 48 }, "Testowe");

        // Create a draw that matches both tickets
        SeedTestDraw(factory, userId, DateOnly.Parse("2025-10-15"), new[] { 5, 14, 23, 31, 32, 33 });

        var client = factory.CreateClient();
        var token = GenerateTestToken(factory, userId, "user@example.com", false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new Contracts.Request(
            DateOnly.Parse("2025-10-01"),
            DateOnly.Parse("2025-10-31"),
            "" // Empty group name should check all
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/verification/check", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<Contracts.Response>();
        Assert.NotNull(result);
        Assert.Equal(2, result.TicketsResults.Count); // All tickets checked
        Assert.Single(result.DrawsResults);
        Assert.Single(result.DrawsResults[0].WinningTicketsResult); // Only one ticket has 3+ matches
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
                    ["Swagger:Enabled"] = "false",
                    ["Features:Verification:Days"] = "31"
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
    /// Helper method to seed a test ticket with specified numbers and group name
    /// </summary>
    private static int SeedTestTicketWithGroup(WebApplicationFactory<Program> factory, int userId, int[] numbers, string groupName)
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
            GroupName = groupName,
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
            DrawSystemId = 20250000 + drawDate.DayNumber, // Generate unique DrawSystemId based on date
            LottoType = lottoType,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = userId,
            // Add test data for prize pools
            TicketPrice = 3.00m,
            WinPoolCount1 = 2,
            WinPoolAmount1 = 5000000.00m,
            WinPoolCount2 = 15,
            WinPoolAmount2 = 50000.00m,
            WinPoolCount3 = 120,
            WinPoolAmount3 = 500.00m,
            WinPoolCount4 = 850,
            WinPoolAmount4 = 20.00m
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
