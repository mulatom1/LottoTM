using MediatR;

namespace LottoTM.Server.Api.Features.Tickets.GenerateRandom;

public class Contracts
{
    /// <summary>
    /// Request for generating a random lottery ticket.
    /// UserId is extracted from JWT claims in the endpoint.
    /// </summary>
    public record Request(int UserId) : IRequest<Response>;

    /// <summary>
    /// Response with success message and generated ticket ID.
    /// </summary>
    public record Response(string Message, int TicketId);
}
