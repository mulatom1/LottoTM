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

        /// <summary>
        /// New array of 6 drawn numbers (1-49, unique)
        /// </summary>
        public int[] Numbers { get; init; } = Array.Empty<int>();
    }

    /// <summary>
    /// Response after successful draw update
    /// </summary>
    public record Response(string Message);
}
