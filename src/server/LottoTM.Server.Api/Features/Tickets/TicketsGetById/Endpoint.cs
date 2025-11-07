using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LottoTM.Server.Api.Features.Tickets.TicketsGetById;

/// <summary>
/// Endpoint definition for GET /api/tickets/{id}
/// Retrieves a single ticket by ID with ownership verification
/// </summary>
public static class Endpoint
{
    public static void AddEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("api/tickets/{id:int}",
            [Authorize] async (int id, IMediator mediator) =>
            {
                var request = new Contracts.GetByIdRequest(id);
                var result = await mediator.Send(request);
                return Results.Ok(result);
            })
            .RequireAuthorization() // Requires JWT authentication
            .WithName("TicketsGetById")
            .WithTags("Tickets")
            .WithDescription("Retrieves a single lottery ticket by ID. The ticket must belong to the authenticated user.")
            .Produces<Contracts.GetByIdResponse>(StatusCodes.Status200OK)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .WithOpenApi();
    }
}
