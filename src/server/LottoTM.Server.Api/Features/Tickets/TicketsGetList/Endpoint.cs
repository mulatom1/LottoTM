using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LottoTM.Server.Api.Features.Tickets.TicketsGetList;

/// <summary>
/// Endpoint definition for GET /api/tickets
/// Retrieves all tickets belonging to the authenticated user
/// </summary>
public static class Endpoint
{
    public static void AddEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("api/tickets",
            [Authorize] async (IMediator mediator) =>
            {
                var request = new Contracts.GetListRequest();
                var result = await mediator.Send(request);
                return Results.Ok(result);
            })
            .RequireAuthorization() // Requires JWT authentication
            .WithName("TicketsGetList")
            .WithTags("Tickets")
            .WithDescription("Retrieves all lottery tickets belonging to the authenticated user. Returns list with pagination metadata. Maximum 100 tickets per user.")
            .Produces<Contracts.GetListResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
            .WithOpenApi();
    }
}
