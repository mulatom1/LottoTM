using MediatR;

namespace LottoTM.Server.Api.Features.Tickets.TicketsCreate;

/// <summary>
/// Data contracts for Tickets feature - manages user lottery number sets
/// </summary>
public class Contracts
{
    /// <summary>
    /// Request to create a new lottery ticket with 6 unique numbers
    /// </summary>
    /// <param name="Numbers">Array of 6 unique numbers in range 1-49</param>
    public record CreateTicketRequest(int[] Numbers) : IRequest<CreateTicketResponse>;

    /// <summary>
    /// Response after successful ticket creation
    /// </summary>
    /// <param name="Id">ID of the created ticket</param>
    /// <param name="Message">Success message</param>
    public record CreateTicketResponse(int Id, string Message);
}
