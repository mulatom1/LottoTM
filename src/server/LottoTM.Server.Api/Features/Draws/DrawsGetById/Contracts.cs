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
    /// <param name="Numbers">Array of 6 drawn numbers (1-49), sorted by position</param>
    /// <param name="CreatedAt">Timestamp when the draw was created in the system</param>
    public record Response(
        int Id,
        DateTime DrawDate,
        int[] Numbers,
        DateTime CreatedAt
    );
}
