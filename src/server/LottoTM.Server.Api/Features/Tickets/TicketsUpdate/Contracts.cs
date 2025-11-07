using MediatR;

namespace LottoTM.Server.Api.Features.Tickets.TicketsUpdate;

/// <summary>
/// Contracts (DTOs) for updating a ticket
/// </summary>
public class Contracts
{
    /// <summary>
    /// Request to update a ticket with new numbers.
    /// Contains ticket ID (from route) and new numbers (from body).
    /// </summary>
    public record Request(int TicketId, int[] Numbers) : IRequest<IResult>;

    /// <summary>
    /// Response for successful update
    /// </summary>
    public record Response(string Message);

    /// <summary>
    /// Response for validation errors
    /// </summary>
    public record ValidationErrorResponse(Dictionary<string, string[]> Errors);
}
