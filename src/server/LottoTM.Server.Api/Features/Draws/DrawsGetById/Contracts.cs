using MediatR;

namespace LottoTM.Server.Api.Features.Draws.DrawsGetById;

public class Contracts
{
    /// <summary>
    /// Request to get a specific draw by ID
    /// </summary>
    /// <param name="Id">The draw ID</param>
    public record Request(int Id) : IRequest<Response?>;

    /// <summary>
    /// Response containing draw details
    /// </summary>
    /// <param name="Id">Draw identifier</param>
    /// <param name="DrawDate">Date of the lottery draw</param>
    /// <param name="LottoType">Type of lottery (LOTTO, LOTTO PLUS)</param>
    /// <param name="Numbers">Array of 6 drawn numbers (1-49), sorted by position</param>
    /// <param name="DrawSystemId">System identifier for the draw</param>
    /// <param name="TicketPrice">Ticket price for this draw</param>
    /// <param name="WinPoolCount1">Count of winners in tier 1 (6 matches)</param>
    /// <param name="WinPoolAmount1">Prize amount for tier 1</param>
    /// <param name="WinPoolCount2">Count of winners in tier 2 (5 matches)</param>
    /// <param name="WinPoolAmount2">Prize amount for tier 2</param>
    /// <param name="WinPoolCount3">Count of winners in tier 3 (4 matches)</param>
    /// <param name="WinPoolAmount3">Prize amount for tier 3</param>
    /// <param name="WinPoolCount4">Count of winners in tier 4 (3 matches)</param>
    /// <param name="WinPoolAmount4">Prize amount for tier 4</param>
    /// <param name="CreatedAt">Timestamp when the draw was created in the system</param>
    public record Response(
        int Id,
        DateTime DrawDate,
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
        decimal? WinPoolAmount4,
        DateTime CreatedAt
    );
}
