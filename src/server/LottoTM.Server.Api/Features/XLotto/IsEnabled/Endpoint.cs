using MediatR;

namespace LottoTM.Server.Api.Features.XLotto.IsEnabled;

/// <summary>
/// Endpoint definition for GET /api/xlotto/is-enabled
/// </summary>
public static class Endpoint
{
    public static void AddEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("api/xlotto/is-enabled", async (IMediator mediator) =>
        {
            var request = new Contracts.Request();
            var result = await mediator.Send(request);

            return Results.Ok(new
            {
                success = true,
                data = result.IsEnabled
            });
        })
        .RequireAuthorization() // Requires JWT token
        .WithName("XLottoIsEnabled")
        .WithTags("XLotto")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .WithOpenApi();
    }
}
