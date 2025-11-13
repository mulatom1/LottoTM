using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LottoTM.Server.Api.Features.Auth.SetAdmin;

/// <summary>
/// Minimal API endpoint for toggling user admin status
/// TEMPORARY: This endpoint is only for MVP. Will be replaced with proper admin management in production.
/// </summary>
public static class Endpoint
{
    /// <summary>
    /// Registers the SetAdmin endpoint
    /// PUT /api/auth/setadmin - Requires authentication
    /// WARNING: This is a temporary MVP endpoint and should be replaced with proper admin management
    /// </summary>
    public static void AddEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPut("api/auth/setadmin", async (
            [FromBody] SetAdminBodyRequest body,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            // Create request from body (only email)
            var request = new Contracts.Request(body.Email);
            var result = await mediator.Send(request, cancellationToken);
            return Results.Ok(result);
        })
        .RequireAuthorization()
        .WithName("AuthSetAdmin")
        .WithTags("Auth")
        .Produces<Contracts.Response>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
        .WithOpenApi(operation =>
        {
            operation.Summary = "[TEMPORARY MVP] Toggle user admin status";
            operation.Description = "Toggles the IsAdmin flag for a user. This is a temporary endpoint for MVP only and will be replaced with proper admin management in production.";
            return operation;
        });
    }

    /// <summary>
    /// Request body for SetAdmin endpoint
    /// </summary>
    /// <param name="Email">User's email address</param>
    public record SetAdminBodyRequest(string Email);
}
