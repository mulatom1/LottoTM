using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LottoTM.Server.Api.Features.Verification.Check;

/// <summary>
/// Minimal API endpoint for POST /api/verification/check
/// </summary>
public static class Endpoint
{
    /// <summary>
    /// Registers the POST /api/verification/check endpoint
    /// </summary>
    public static void AddEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("api/verification/check", async (
            [FromBody] Contracts.Request request,
            IMediator mediator) =>
        {
            var result = await mediator.Send(request);
            return Results.Ok(result);
        })
        .RequireAuthorization() // JWT authentication required
        .WithName("VerificationCheck")
        .WithTags("Verification")
        .Produces<Contracts.Response>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
        .WithOpenApi(operation =>
        {
            operation.Summary = "Verify user tickets against lottery draws";
            operation.Description = "Compares all user tickets against lottery draw results in the specified date range. " +
                                  "Returns tickets with 3 or more matching numbers. " +
                                  "Maximum date range is 31 days. " +
                                  "Requires JWT authentication.";
            return operation;
        });
    }
}
