using MediatR;

namespace LottoTM.Server.Api.Features.Draws.DrawsUpdate;

/// <summary>
/// Minimal API endpoint for updating lottery draw results
/// </summary>
public static class Endpoint
{
    /// <summary>
    /// Registers the UpdateDraw endpoint in the application
    /// PUT /api/draws/{id} - Requires JWT authentication and admin privileges
    /// </summary>
    public static void AddEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPut("api/draws/{id:int}", async (int id, UpdateDrawDto dto, IMediator mediator) =>
        {
            var request = new Contracts.Request
            {
                Id = id,
                LottoType = dto.LottoType,
                DrawDate = dto.DrawDate,
                Numbers = dto.Numbers,
                DrawSystemId = dto.DrawSystemId,
                TicketPrice = dto.TicketPrice,
                WinPoolCount1 = dto.WinPoolCount1,
                WinPoolAmount1 = dto.WinPoolAmount1,
                WinPoolCount2 = dto.WinPoolCount2,
                WinPoolAmount2 = dto.WinPoolAmount2,
                WinPoolCount3 = dto.WinPoolCount3,
                WinPoolAmount3 = dto.WinPoolAmount3,
                WinPoolCount4 = dto.WinPoolCount4,
                WinPoolAmount4 = dto.WinPoolAmount4
            };

            return await mediator.Send(request);
        })
        .RequireAuthorization()
        .WithName("DrawsUpdate")
        .WithTags("Draws")
        .Produces<Contracts.Response>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .WithOpenApi();
    }

    /// <summary>
    /// DTO for binding request body (without Id, which comes from route)
    /// </summary>
    public record UpdateDrawDto(
        DateOnly DrawDate,
        string LottoType,
        int[] Numbers,
        int DrawSystemId,
        decimal? TicketPrice,
        int? WinPoolCount1,
        decimal? WinPoolAmount1,
        int? WinPoolCount2,
        decimal? WinPoolAmount2,
        int? WinPoolCount3,
        decimal? WinPoolAmount3,
        int? WinPoolCount4,
        decimal? WinPoolAmount4
    );
}
