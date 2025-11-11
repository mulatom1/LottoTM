using MediatR;

namespace LottoTM.Server.Api.Features.Draws.DrawsCreate;

/// <summary>
/// Minimal API endpoint for managing lottery draw results
/// </summary>
public static class Endpoint
{
    /// <summary>
    /// Registers the CreateDraw endpoint in the application
    /// POST /api/draws - Requires JWT authentication
    /// </summary>
    public static void AddEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("api/draws", async (
            Contracts.CreateDrawRequest request,
            IMediator mediator) =>
        {
            var result = await mediator.Send(request);
            return Results.Created($"/api/draws", result);
        })
        .RequireAuthorization()
        .WithName("DrawsCreate")
        .WithTags("Draws")
        .Produces<Contracts.CreateDrawResponse>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status401Unauthorized)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .WithOpenApi();
    }
}
