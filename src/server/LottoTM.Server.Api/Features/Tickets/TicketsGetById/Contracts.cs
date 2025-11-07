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
    public record GetByIdRequest(int Id) : IRequest<GetByIdResponse>;

    /// <summary>
    /// Response containing ticket details
    /// </summary>
    /// <param name="Id">ID of the ticket</param>
    /// <param name="UserId">ID of the user who owns this ticket</param>
    /// <param name="Numbers">Array of 6 lottery numbers in order by position</param>
    /// <param name="CreatedAt">UTC timestamp when ticket was created</param>
    public record GetByIdResponse(
        int Id,
        int UserId,
        int[] Numbers,
        DateTime CreatedAt
    );
}
