using MediatR;

namespace LottoTM.Server.Api.Features.Draws.DrawsDelete;

/// <summary>
/// Endpoint definition for DELETE /api/draws/{id}
/// </summary>
public static class Endpoint
{
    /// <summary>
    /// Registers the DELETE draw endpoint
    /// </summary>
    public static void AddEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapDelete("api/draws/{id:int}", async (int id, IMediator mediator) =>
        {
            var request = new Contracts.Request(id);
            var result = await mediator.Send(request);
            return Results.Ok(result);
        })
        .RequireAuthorization() // Requires JWT token
        .WithName("DrawsDelete")
        .WithTags("Draws")
        .Produces<Contracts.Response>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithOpenApi();
    }
}
