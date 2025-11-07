using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LottoTM.Server.Api.Features.Auth.Register;

/// <summary>
/// Minimal API endpoint for user registration
/// </summary>
public static class Endpoint
{
    /// <summary>
    /// Registers the user registration endpoint
    /// POST /api/auth/register - Public endpoint (no authentication required)
    /// </summary>
    public static void AddEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("api/auth/register", async (
            Contracts.Request request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(request, cancellationToken);
            return Results.Created($"/api/auth/register", result);
        })
        .WithName("AuthRegister")
        .WithTags("Auth")
        .Produces<Contracts.Response>(StatusCodes.Status201Created)
        .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
        .AllowAnonymous()
        .WithOpenApi();
    }
}
