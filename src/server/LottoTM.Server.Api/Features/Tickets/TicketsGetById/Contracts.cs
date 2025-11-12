using MediatR;

namespace LottoTM.Server.Api.Features.Tickets.TicketsGetById;

/// <summary>
/// Data contracts for getting ticket by ID
/// </summary>
public class Contracts
{
    /// <summary>
    /// Request to get a ticket by ID
    /// </summary>
    /// <param name="Id">ID of the ticket to retrieve (must be > 0)</param>
    public record Request(int Id) : IRequest<Response>;

    /// <summary>
    /// Response containing ticket details
    /// </summary>
    /// <param name="Id">ID of the ticket</param>
    /// <param name="UserId">ID of the user who owns this ticket</param>
    /// <param name="GroupName">Optional group name for organizing tickets</param>
    /// <param name="Numbers">Array of 6 lottery numbers in order by position</param>
    /// <param name="CreatedAt">UTC timestamp when ticket was created</param>
    public record Response(
        int Id,
        int UserId,
        string GroupName,
        int[] Numbers,
        DateTime CreatedAt
    );
}
