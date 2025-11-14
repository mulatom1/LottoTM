using MediatR;

namespace LottoTM.Server.Api.Features.XLotto.ActualDraws;

/// <summary>
/// Endpoint definition for GET /api/xlotto/actual-draws
/// </summary>
public static class Endpoint
{
    public static void AddEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("api/xlotto/actual-draws", async (DateTime date, string lottoType, IMediator mediator) =>
        {
            var request = new Contracts.Request(date, lottoType);
            var result = await mediator.Send(request);

            return Results.Ok(new
            {
                success = true,
                data = result.JsonData
            });
        })
        .RequireAuthorization() // Requires JWT token
        .WithName("XLottoActualDraws")
        .WithTags("XLotto")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status500InternalServerError)
        .WithOpenApi();
    }
}
