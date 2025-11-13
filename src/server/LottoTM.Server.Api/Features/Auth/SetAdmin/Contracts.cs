using MediatR;

namespace LottoTM.Server.Api.Features.Auth.SetAdmin;

/// <summary>
/// Contracts for SetAdmin endpoint
/// TEMPORARY: This endpoint is only for MVP. Will be replaced with proper admin management in production.
/// </summary>
public class Contracts
{
    /// <summary>
    /// SetAdmin request with user email
    /// </summary>
    /// <param name="Email">User's email address</param>
    public record Request(
        string Email
    ) : IRequest<Response>;

    /// <summary>
    /// SetAdmin response with updated user information
    /// </summary>
    /// <param name="UserId">Unique user identifier</param>
    /// <param name="Email">User's email address</param>
    /// <param name="IsAdmin">Updated admin flag value (toggled)</param>
    /// <param name="UpdatedAt">Timestamp when the admin status was updated (UTC)</param>
    public record Response(
         int UserId,
         string Email,
         bool IsAdmin,
         DateTime UpdatedAt
     );
}
