using MediatR;

namespace LottoTM.Server.Api.Features.Draws.DrawsGetById;

/// <summary>
/// Endpoint definition for GET /api/draws/{id}
/// </summary>
public static class Endpoint
{
    public static void AddEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("api/draws/{id:int}", async (int id, IMediator mediator) =>
        {
            var request = new Contracts.Request(id);
            var result = await mediator.Send(request);

            return result != null
                ? Results.Ok(result)
                : Results.NotFound(new
                {
                    detail = "Losowanie o podanym ID nie istnieje"
                });
        })
        .RequireAuthorization() // Requires JWT token
        .WithName("DrawsGetById")
        .WithTags("Draws")
        .Produces<Contracts.Response>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status404NotFound)
        .WithOpenApi();
    }
}
