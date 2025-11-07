using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LottoTM.Server.Api.Features.Tickets.GenerateRandom;

public static class Endpoint
{
    public static void AddEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("api/tickets/generate-random",
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

                // Zwrot 201 Created z Location header
                return Results.Created(
                    $"/api/tickets/{result.TicketId}",
                    result);
            })
        .RequireAuthorization() // Wymaga JWT tokenu
        .WithName("TicketsGenerateRandom")
        .WithTags("Tickets")
        .Produces<Contracts.Response>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status500InternalServerError)
        .WithOpenApi(operation =>
        {
            operation.Summary = "Generuje losowy zestaw liczb LOTTO";
            operation.Description = "Generuje pojedynczy losowy zestaw 6 unikalnych liczb (1-49) " +
                                   "i zapisuje go jako nowy ticket użytkownika. " +
                                   "Maksymalny limit: 100 zestawów na użytkownika.";
            return operation;
        });
    }
}
