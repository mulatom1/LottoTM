using MediatR;

namespace LottoTM.Server.Api.Features.ApiVersion;

public static class Endpoint
{
    public static void AddEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("api/version", async (IMediator mediator) =>
        {
            var request = new Contracts.Request();
            var result = await mediator.Send(request);
            return Results.Ok(result);
        })
        .WithName("ApiVersion")
        .WithTags("Version")
        .Produces<Contracts.Response>(StatusCodes.Status200OK)
        .WithOpenApi();
    }
}
