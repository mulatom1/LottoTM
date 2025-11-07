using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LottoTM.Server.Api.Features.Tickets.GenerateSystem;

/// <summary>
/// Endpoint for generating 9 system lottery tickets covering all numbers 1-49.
/// Requires JWT authentication. User must have ≤91 existing tickets.
/// </summary>
public static class Endpoint
{
    public static void AddEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("api/tickets/generate-system",
            [Authorize] async (HttpContext httpContext, IMediator mediator) =>
            {
                // Pobranie UserId z JWT claims
                var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    return Results.Unauthorized();
                }

                // Utworzenie żądania
                var request = new Contracts.Request(userId);

                // Wysłanie do MediatR
                var result = await mediator.Send(request);

                // Zwrot 201 Created
                return Results.Created("api/tickets", result);
            })
        .RequireAuthorization() // Wymaga JWT tokenu
        .WithName("TicketsGenerateSystem")
        .WithTags("Tickets")
        .Produces<Contracts.Response>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
        .WithOpenApi(operation =>
        {
            operation.Summary = "Generowanie 9 zestawów systemowych";
            operation.Description =
                "Generuje 9 zestawów liczb LOTTO pokrywających wszystkie liczby 1-49. " +
                "Każdy zestaw zawiera 6 unikalnych liczb. Wymaga maksymalnie 91 istniejących zestawów.";
            return operation;
        });
    }
}
