using MediatR;

namespace LottoTM.Server.Api.Features.Draws.DrawsDelete;

/// <summary>
/// Contracts for DELETE /api/draws/{id} endpoint
/// </summary>
public class Contracts
{
    /// <summary>
    /// Request to delete a draw by ID
    /// </summary>
    /// <param name="Id">Draw ID to delete</param>
    public record Request(int Id) : IRequest<Response>;

    /// <summary>
    /// Response after successful draw deletion
    /// </summary>
    /// <param name="Message">Success message</param>
    public record Response(string Message);
}
