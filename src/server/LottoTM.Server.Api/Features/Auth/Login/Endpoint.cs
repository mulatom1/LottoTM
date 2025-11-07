using MediatR;

namespace LottoTM.Server.Api.Features.Auth.Login;

/// <summary>
/// Minimal API endpoint for user authentication
/// </summary>
public static class Endpoint
{
    /// <summary>
    /// Registers the login endpoint in the application
    /// POST /api/auth/login - Public endpoint (no authentication required)
    /// </summary>
    public static void AddEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("api/auth/login", async (
            Contracts.LoginRequest request,
            IMediator mediator) =>
        {
            try
            {
                var result = await mediator.Send(request);
                return Results.Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                // Return 401 for invalid credentials
                return Results.Json(
                    new { error = ex.Message },
                    statusCode: StatusCodes.Status401Unauthorized
                );
            }
        })
        .WithName("AuthLogin")
        .WithTags("Auth")
        .Produces<Contracts.LoginResponse>(StatusCodes.Status200OK)
        .Produces<object>(StatusCodes.Status400BadRequest)
        .Produces<object>(StatusCodes.Status401Unauthorized)
        .AllowAnonymous()
        .WithOpenApi();
    }
}
