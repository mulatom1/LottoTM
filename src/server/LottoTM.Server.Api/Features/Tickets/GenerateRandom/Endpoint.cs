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
        .WithName("TicketsGenerateRandom")
        .WithTags("Tickets")
        .Produces<Contracts.Response>(StatusCodes.Status200OK)
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
