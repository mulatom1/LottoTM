using MediatR;

namespace LottoTM.Server.Api.Features.Tickets.TicketsDeleteAll;

/// <summary>
/// Endpoint for deleting all tickets for the authenticated user
/// DELETE /api/tickets/all
/// </summary>
public static class Endpoint
{
    public static void AddEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/tickets/all", async (
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var request = new Contracts.Request();
            var response = await mediator.Send(request, cancellationToken);

            return Results.Ok(response);
        })
        .RequireAuthorization()
        .WithName("TicketsDeleteAll")
        .WithTags("Tickets")
        .Produces<Contracts.Response>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized);
    }
}
