using MediatR;

namespace LottoTM.Server.Api.Features.Verification.Check;

/// <summary>
/// Contracts for the verification check endpoint
/// </summary>
public class Contracts
{
    /// <summary>
    /// Request to verify user tickets against lottery draws in a date range
    /// </summary>
    /// <param name="DateFrom">Start date of verification period (YYYY-MM-DD)</param>
    /// <param name="DateTo">End date of verification period (YYYY-MM-DD)</param>
    /// <param name="GroupName">Optional group name to filter tickets (partial match, case-insensitive). If provided, only tickets whose group name contains this text will be verified</param>
    public record Request(DateOnly DateFrom, DateOnly DateTo, string? GroupName) : IRequest<Response>;


    /// <summary>
    /// Response containing verification results for all user tickets
    /// </summary>    
    /// <param name="ExecutionTimeMs">Time taken to perform verification in milliseconds</param>
    /// <param name="DrawsResults">List of verification results for draws</param>
    /// <param name="TicketsResults">List of verification results for tickets</param>
    public record Response(
        long ExecutionTimeMs,
        List<DrawsResult> DrawsResults,
        List<TicketsResult> TicketsResults
    );

    
    
    /// <summary>
    /// Verification result for a single ticket
    /// </summary>
    /// <param name="TicketId">Ticket identifier</param>
    /// <param name="GroupName">Group name for organizing tickets (empty string if not assigned)</param>
    /// <param name="TicketNumbers">Array of 6 numbers from the ticket</param>
    public record TicketsResult(
        int TicketId,
        string GroupName,
        List<int> TicketNumbers        
    );


    public record WinningTicketResult(
        int TicketId,
        List<int> MatchingNumbers
    );

    /// <summary>
    /// Verification result for a single draw match
    /// </summary>
    /// <param name="DrawId">Draw identifier</param>
    /// <param name="DrawDate">Date of the draw</param>
    /// <param name="DrawSystemId">System identifier for the draw</param>
    /// <param name="LottoType">Type of lottery game (LOTTO, LOTTO PLUS, etc.)</param>
    /// <param name="DrawNumbers">Array of 6 numbers from the draw</param>
    /// <param name="TicketPrice">Price of the ticket (nullable)</param>
    /// <param name="WinPoolCount1">Number of winners for tier 1 (6 matches) - nullable</param>
    /// <param name="WinPoolAmount1">Prize amount for tier 1 (6 matches) - nullable</param>
    /// <param name="WinPoolCount2">Number of winners for tier 2 (5 matches) - nullable</param>
    /// <param name="WinPoolAmount2">Prize amount for tier 2 (5 matches) - nullable</param>
    /// <param name="WinPoolCount3">Number of winners for tier 3 (4 matches) - nullable</param>
    /// <param name="WinPoolAmount3">Prize amount for tier 3 (4 matches) - nullable</param>
    /// <param name="WinPoolCount4">Number of winners for tier 4 (3 matches) - nullable</param>
    /// <param name="WinPoolAmount4">Prize amount for tier 4 (3 matches) - nullable</param>
    /// <param name="WinningTicket">Dict key = ID winning ticket, value = conunt winning numbers</param>
    public record DrawsResult(
        int DrawId,
        DateOnly DrawDate,
        int DrawSystemId,
        string LottoType,
        List<int> DrawNumbers,
        decimal? TicketPrice,
        int? WinPoolCount1,
        decimal? WinPoolAmount1,
        int? WinPoolCount2,
        decimal? WinPoolAmount2,
        int? WinPoolCount3,
        decimal? WinPoolAmount3,
        int? WinPoolCount4,
        decimal? WinPoolAmount4,
        List<WinningTicketResult> WinningTicketsResult
    );
}
