using MediatR;

namespace LottoTM.Server.Api.Features.Tickets.TicketsGetList;

/// <summary>
/// Data contracts for getting list of tickets
/// </summary>
public class Contracts
{
    /// <summary>
    /// Request to get list of tickets for authenticated user
    /// </summary>
    /// <param name="GroupName">Optional partial match filter by group name (case-sensitive, max 100 chars). Uses LIKE/Contains search.</param>
    public record Request(string? GroupName = null) : IRequest<Response>;

    /// <summary>
    /// Response containing list of tickets with pagination metadata
    /// </summary>
    /// <param name="Tickets">List of tickets belonging to the user</param>
    /// <param name="TotalCount">Total number of pages (always 0 or 1 in MVP)</param>
    /// <param name="Limit">Maximum tickets allowed per user (100)</param>
    public record Response(
        List<TicketDto> Tickets,
        int TotalCount,
        int Limit
    );

    /// <summary>
    /// DTO for a single ticket in the list
    /// </summary>
    /// <param name="Id">Ticket ID</param>
    /// <param name="UserId">User ID who owns this ticket</param>
    /// <param name="GroupName">Optional group name for organizing tickets</param>
    /// <param name="Numbers">Array of 6 lottery numbers ordered by position</param>
    /// <param name="CreatedAt">UTC timestamp when ticket was created</param>
    public record TicketDto(
        int Id,
        int UserId,
        string GroupName,
        int[] Numbers,
        DateTime CreatedAt
    );
}
