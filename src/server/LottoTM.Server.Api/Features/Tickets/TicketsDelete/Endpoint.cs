using MediatR;

namespace LottoTM.Server.Api.Features.Tickets.TicketsDelete;

public static class Endpoint
{
    /// <summary>
    /// Rejestruje endpoint DELETE /api/tickets/{id}
    /// </summary>
    public static void AddEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapDelete("api/tickets/{id}", async (
            int id,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var request = new Contracts.Request(id);
            var result = await mediator.Send(request, cancellationToken);
            return Results.Ok(result);
        })
        .RequireAuthorization() // Wymaga JWT tokenu
        .WithName("TicketsDelete")
        .WithTags("Tickets")
        .Produces<Contracts.Response>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .WithOpenApi(operation =>
        {
            operation.Summary = "Usuwa zestaw liczb LOTTO użytkownika";
            operation.Description = "Usuwa zestaw liczb wraz z powiązanymi liczbami (CASCADE DELETE). Wymaga autoryzacji JWT.";
            return operation;
        });
    }
}
