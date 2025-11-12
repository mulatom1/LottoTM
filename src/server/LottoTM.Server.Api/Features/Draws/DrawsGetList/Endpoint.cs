using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LottoTM.Server.Api.Features.Draws.DrawsGetList;

/// <summary>
/// Minimal API endpoint for GET /api/draws
/// </summary>
public static class Endpoint
{
    /// <summary>
    /// Registers the GET /api/draws endpoint
    /// </summary>
    public static void AddEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("api/draws", async (
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            [FromQuery] string? sortBy,
            [FromQuery] string? sortOrder,
            IMediator mediator) =>
        {
            // Apply default values if parameters are not provided or are zero
            var request = new Contracts.Request(
                page ?? 1,
                pageSize ?? 20,
                string.IsNullOrWhiteSpace(sortBy) ? "drawDate" : sortBy,
                string.IsNullOrWhiteSpace(sortOrder) ? "desc" : sortOrder
            );

            var result = await mediator.Send(request);
            return Results.Ok(result);
        })
        .RequireAuthorization() // JWT authentication required
        .WithName("DrawsGetList")
        .WithTags("Draws")
        .Produces<Contracts.Response>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
        .WithOpenApi(operation =>
        {
            operation.Summary = "Get paginated list of lottery draws";
            operation.Description = "Retrieves a paginated list of all lottery draws with their numbers. " +
                                  "Supports sorting by draw date or creation date. " +
                                  "Requires JWT authentication.";
            return operation;
        });
    }
}
