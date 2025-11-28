using MediatR;

namespace LottoTM.Server.Api.Features.Draws.DrawsGetList;

/// <summary>
/// Contracts for GET /api/draws endpoint (retrieving paginated list of draws)
/// </summary>
public class Contracts
{
    /// <summary>
    /// Request to get a paginated list of draws with sorting options
    /// </summary>
    /// <param name="Page">Page number (starting from 1, default: 1)</param>
    /// <param name="PageSize">Number of items per page (1-100, default: 20)</param>
    /// <param name="SortBy">Field to sort by: "drawDate" or "createdAt" (default: "drawDate")</param>
    /// <param name="SortOrder">Sort order: "asc" or "desc" (default: "desc")</param>
    public record Request(
        DateOnly? DateFrom = null,
        DateOnly? DateTo = null,
        int Page = 1,
        int PageSize = 20,
        string SortBy = "drawDate",
        string SortOrder = "desc"
    ) : IRequest<Response>;

    /// <summary>
    /// Response containing paginated list of draws with metadata
    /// </summary>
    /// <param name="Draws">List of draw DTOs</param>
    /// <param name="TotalCount">Total number of draws in database</param>
    /// <param name="Page">Current page number</param>
    /// <param name="PageSize">Items per page</param>
    /// <param name="TotalPages">Total number of pages</param>
    public record Response(
        List<DrawDto> Draws,
        int TotalCount,
        int Page,
        int PageSize,
        int TotalPages
    );

    /// <summary>
    /// Data Transfer Object for a single draw with its numbers
    /// </summary>
    /// <param name="Id">Draw identifier</param>
    /// <param name="DrawDate">Date of the lottery draw</param>
    /// <param name="LottoType">Type of lottery (LOTTO, LOTTO PLUS)</param>
    /// <param name="Numbers">Array of 6 drawn numbers (sorted by position)</param>
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
    /// <param name="CreatedAt">Timestamp when draw was created</param>
    public record DrawDto(
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
