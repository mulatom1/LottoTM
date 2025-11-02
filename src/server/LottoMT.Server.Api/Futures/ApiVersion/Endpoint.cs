namespace LottoMT.Server.Api.Futures.ApiVersion;

public static class Endpoint
{
    public static void AddEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("api/version", (IConfiguration configuration) =>
        {
            var response = new Contracts.Response(configuration.GetValue<string>("ApiVersion") ?? "");
            return Results.Ok(response);
        })
        .WithName("GetApiVersion")
        .Produces<string>(StatusCodes.Status200OK)
        .WithOpenApi();
    }
}
