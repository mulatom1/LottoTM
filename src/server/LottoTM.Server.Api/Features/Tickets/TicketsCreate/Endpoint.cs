using MediatR;
using Microsoft.AspNetCore.Authorization;

namespace LottoTM.Server.Api.Features.Tickets.TicketsCreate;

/// <summary>
/// Endpoint registration for Tickets feature
/// </summary>
public static class Endpoint
{
    /// <summary>
    /// Registers the POST /api/tickets endpoint
    /// Requires JWT authentication
    /// </summary>
    public static void AddEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("api/tickets", [Authorize] async (
            Contracts.CreateTicketRequest request,
            IMediator mediator) =>
        {
            var result = await mediator.Send(request);
            return Results.Created($"/api/tickets/{result.Id}", result);
        })
        .RequireAuthorization() // Wymaga JWT tokenu
        .WithName("TicketsCreate")
        .WithTags("Tickets")
        .Produces<Contracts.CreateTicketResponse>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .WithOpenApi();
    }
}
