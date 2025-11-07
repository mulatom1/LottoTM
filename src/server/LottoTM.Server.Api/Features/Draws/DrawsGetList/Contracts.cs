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
    public record GetDrawsRequest(
        int Page = 1,
        int PageSize = 20,
        string SortBy = "drawDate",
        string SortOrder = "desc"
    ) : IRequest<GetDrawsResponse>;

    /// <summary>
    /// Response containing paginated list of draws with metadata
    /// </summary>
    /// <param name="Draws">List of draw DTOs</param>
    /// <param name="TotalCount">Total number of draws in database</param>
    /// <param name="Page">Current page number</param>
    /// <param name="PageSize">Items per page</param>
    /// <param name="TotalPages">Total number of pages</param>
    public record GetDrawsResponse(
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
    /// <param name="Numbers">Array of 6 drawn numbers (sorted by position)</param>
    /// <param name="CreatedAt">Timestamp when draw was created</param>
    public record DrawDto(
        int Id,
        DateTime DrawDate,
        int[] Numbers,
        DateTime CreatedAt
    );
}
