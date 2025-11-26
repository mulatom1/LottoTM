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
    /// <param name="Results">List of verification results for each ticket with hits</param>
    /// <param name="TotalTickets">Total number of tickets checked</param>
    /// <param name="TotalDraws">Total number of draws checked</param>
    /// <param name="ExecutionTimeMs">Time taken to perform verification in milliseconds</param>
    public record Response(
        List<TicketVerificationResult> Results,
        int TotalTickets,
        int TotalDraws,
        long ExecutionTimeMs
    );

    /// <summary>
    /// Verification result for a single ticket
    /// </summary>
    /// <param name="TicketId">Ticket identifier</param>
    /// <param name="GroupName">Group name for organizing tickets (empty string if not assigned)</param>
    /// <param name="TicketNumbers">Array of 6 numbers from the ticket</param>
    /// <param name="Draws">List of draws where this ticket had hits (3 or more matching numbers)</param>
    public record TicketVerificationResult(
        int TicketId,
        string GroupName,
        List<int> TicketNumbers,
        List<DrawVerificationResult> Draws
    );

    /// <summary>
    /// Verification result for a single draw match
    /// </summary>
    /// <param name="DrawId">Draw identifier</param>
    /// <param name="DrawDate">Date of the draw</param>
    /// <param name="LottoType">Type of lottery game (LOTTO, LOTTO PLUS, etc.)</param>
    /// <param name="DrawNumbers">Array of 6 numbers from the draw</param>
    /// <param name="Hits">Number of matching numbers between ticket and draw</param>
    /// <param name="WinningNumbers">List of the actual matching numbers</param>
    public record DrawVerificationResult(
        int DrawId,
        DateOnly DrawDate,
        string LottoType,
        List<int> DrawNumbers,
        int Hits,
        List<int> WinningNumbers
    );
}
