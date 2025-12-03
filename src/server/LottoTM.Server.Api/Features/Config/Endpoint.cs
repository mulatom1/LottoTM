using MediatR;

namespace LottoTM.Server.Api.Features.Config;

/// <summary>
/// Endpoint registration for configuration retrieval
/// </summary>
public static class Endpoint
{
    public static void AddEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/config", async (IMediator mediator) =>
        {
            var request = new Contracts.Request();
            var response = await mediator.Send(request);
            return Results.Ok(response);
        })
        .WithName("GetConfig")
        .WithTags("Config")
        .WithOpenApi()
        .Produces<Contracts.Response>(StatusCodes.Status200OK);
    }
}
