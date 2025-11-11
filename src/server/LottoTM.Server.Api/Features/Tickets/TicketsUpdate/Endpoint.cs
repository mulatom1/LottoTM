using MediatR;

namespace LottoTM.Server.Api.Features.Tickets.TicketsUpdate;

/// <summary>
/// Endpoint registration for updating a ticket
/// </summary>
public static class Endpoint
{
    /// <summary>
    /// Registers the PUT /api/tickets/{id} endpoint
    /// </summary>
    public static void AddEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPut("api/tickets/{id:int}", async (
            int id,
            UpdateTicketRequest body,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var request = new Contracts.Request(id, body.GroupName, body.Numbers);
            var result = await mediator.Send(request, cancellationToken);
            return result;
        })
        .RequireAuthorization() // Requires JWT
        .WithName("TicketsUpdate")
        .WithTags("Tickets")
        .Produces<Contracts.Response>(StatusCodes.Status200OK)
        .Produces<Contracts.ValidationErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status500InternalServerError)
        .WithOpenApi(operation =>
        {
            operation.Summary = "Aktualizacja zestawu liczb";
            operation.Description = "Edycja istniejącego zestawu 6 liczb LOTTO (1-49) i opcjonalnie nazwy grupy. Wymaga autoryzacji JWT. Użytkownik może edytować tylko własne zestawy.";
            return operation;
        });
    }

    /// <summary>
    /// DTO for request body (JSON deserialization)
    /// </summary>
    public record UpdateTicketRequest(string? GroupName, int[] Numbers);
}
