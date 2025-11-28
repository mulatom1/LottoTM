using MediatR;

namespace LottoTM.Server.Api.Features.Draws.DrawsUpdate;

/// <summary>
/// Contracts (DTOs) for Update Draw endpoint
/// </summary>
public class Contracts
{
    /// <summary>
    /// Request for updating a lottery draw result
    /// </summary>
    public record Request : IRequest<IResult>
    {
        /// <summary>
        /// ID of the draw to update (from route parameter)
        /// </summary>
        public int Id { get; init; }

        /// <summary>
        /// New draw date (cannot be in the future)
        /// </summary>
        public DateOnly DrawDate { get; init; }

        public string LottoType { get; init; } = string.Empty;

        /// <summary>
        /// New array of 6 drawn numbers (1-49, unique)
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
    /// Response after successful draw update
    /// </summary>
    public record Response(string Message);
}
