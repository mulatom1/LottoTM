using MediatR;

namespace LottoTM.Server.Api.Features.Tickets.GenerateRandom;

public class Contracts
{
    /// <summary>
    /// Request for generating a random lottery ticket.
    /// </summary>
    public record Request() : IRequest<Response>;

    /// <summary>
    /// Response with success message and generated ticket ID.
    /// </summary>
    public record Response(
        int[] Numbers
     );
}
