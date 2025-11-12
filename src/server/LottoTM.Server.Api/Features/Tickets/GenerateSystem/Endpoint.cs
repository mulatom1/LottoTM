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
            [Authorize] async (IMediator mediator) =>
            {
                // Utworzenie żądania
                var request = new Contracts.Request();

                // Wysłanie do MediatR
                var result = await mediator.Send(request);

                // Zwrot 200 OK
                return Results.Ok(result);
            })
        .RequireAuthorization() // Wymaga JWT tokenu
        .WithName("TicketsGenerateSystem")
        .WithTags("Tickets")
        .Produces<Contracts.Response>(StatusCodes.Status200OK)
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
