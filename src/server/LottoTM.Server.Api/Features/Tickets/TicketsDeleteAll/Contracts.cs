using MediatR;

namespace LottoTM.Server.Api.Features.Tickets.TicketsDeleteAll;

/// <summary>
/// Data contracts for deleting all user tickets
/// </summary>
public class Contracts
{
    /// <summary>
    /// Request to delete all tickets for the authenticated user
    /// </summary>
    public record Request() : IRequest<Response>;

    /// <summary>
    /// Response after deleting all tickets
    /// </summary>
    /// <param name="Message">Success message</param>
    /// <param name="DeletedCount">Number of tickets deleted</param>
    public record Response(string Message, int DeletedCount);
}
