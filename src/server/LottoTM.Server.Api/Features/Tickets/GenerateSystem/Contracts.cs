using MediatR;

namespace LottoTM.Server.Api.Features.Tickets.GenerateSystem;

public class Contracts
{
    /// <summary>
    /// Request for generating 9 system tickets covering all numbers 1-49.
    /// </summary>
    public record Request() : IRequest<Response>;

    /// <summary>
    /// Response containing success message and details of generated tickets.
    /// </summary>
    /// <param name="Message">Success message</param>
    /// <param name="GeneratedCount">Number of tickets generated (always 9)</param>
    /// <param name="Tickets">List of generated ticket details</param>
    public record Response(
        List<TicketDto> Tickets
    );

    /// <summary>
    /// DTO representing a single generated ticket.
    /// </summary>
    /// <param name="Id">Unique ticket identifier (int)</param>
    /// <param name="GroupName">Group name for organizing tickets</param>
    /// <param name="Numbers">6 lottery numbers (sorted by position)</param>
    /// <param name="CreatedAt">UTC timestamp of creation</param>
    public record TicketDto(
        int[] Numbers
    );
}
