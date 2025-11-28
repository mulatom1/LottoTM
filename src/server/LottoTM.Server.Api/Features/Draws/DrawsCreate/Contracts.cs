using MediatR;

namespace LottoTM.Server.Api.Features.Draws.DrawsCreate;

/// <summary>
/// Contracts (DTOs) for Draw management endpoints
/// </summary>
public class Contracts
{
    /// <summary>
    /// Request to create a new lottery draw result
    /// </summary>
    public record Request : IRequest<Response>
    {
        /// <summary>
        /// Date of the lottery draw (YYYY-MM-DD format)
        /// Must not be in the future and must be unique globally
        /// </summary>
        public DateOnly DrawDate { get; init; }

        public string LottoType { get; init; } = string.Empty;

        /// <summary>
        /// Array of exactly 6 numbers in range 1-49, all unique
        /// </summary>
        public int[] Numbers { get; init; } = Array.Empty<int>();

        /// <summary>
        /// System identifier for the draw (required)
        /// </summary>
        public int DrawSystemId { get; init; }

        /// <summary>
        /// Ticket price for this draw (optional)
        /// </summary>
        public decimal? TicketPrice { get; init; }

        /// <summary>
        /// Count of winners in tier 1 (6 matches) - optional
        /// </summary>
        public int? WinPoolCount1 { get; init; }

        /// <summary>
        /// Prize amount for tier 1 (6 matches) - optional
        /// </summary>
        public decimal? WinPoolAmount1 { get; init; }

        /// <summary>
        /// Count of winners in tier 2 (5 matches) - optional
        /// </summary>
        public int? WinPoolCount2 { get; init; }

        /// <summary>
        /// Prize amount for tier 2 (5 matches) - optional
        /// </summary>
        public decimal? WinPoolAmount2 { get; init; }

        /// <summary>
        /// Count of winners in tier 3 (4 matches) - optional
        /// </summary>
        public int? WinPoolCount3 { get; init; }

        /// <summary>
        /// Prize amount for tier 3 (4 matches) - optional
        /// </summary>
        public decimal? WinPoolAmount3 { get; init; }

        /// <summary>
        /// Count of winners in tier 4 (3 matches) - optional
        /// </summary>
        public int? WinPoolCount4 { get; init; }

        /// <summary>
        /// Prize amount for tier 4 (3 matches) - optional
        /// </summary>
        public decimal? WinPoolAmount4 { get; init; }
    }

    /// <summary>
    /// Response after successfully creating a draw
    /// </summary>
    /// <param name="Message">Success message</param>
    public record Response(
        string Message
    );
}
