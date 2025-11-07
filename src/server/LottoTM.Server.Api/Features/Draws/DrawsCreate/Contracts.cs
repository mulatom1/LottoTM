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
    public record CreateDrawRequest : IRequest<CreateDrawResponse>
    {
        /// <summary>
        /// Date of the lottery draw (YYYY-MM-DD format)
        /// Must not be in the future and must be unique globally
        /// </summary>
        public DateOnly DrawDate { get; init; }

        /// <summary>
        /// Array of exactly 6 numbers in range 1-49, all unique
        /// </summary>
        public int[] Numbers { get; init; } = Array.Empty<int>();
    }

    /// <summary>
    /// Response after successfully creating a draw
    /// </summary>
    /// <param name="Message">Success message</param>
    public record CreateDrawResponse(
        string Message
    );
}
