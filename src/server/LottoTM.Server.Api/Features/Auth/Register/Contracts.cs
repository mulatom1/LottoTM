using MediatR;

namespace LottoTM.Server.Api.Features.Auth.Register;

/// <summary>
/// Contracts for user registration endpoint
/// </summary>
public class Contracts
{
    /// <summary>
    /// Registration request with user credentials
    /// </summary>
    /// <param name="Email">User's email address</param>
    /// <param name="Password">User's password (plain text)</param>
    /// <param name="ConfirmPassword">Password confirmation for validation</param>
    public record Request(
        string Email,
        string Password,
        string ConfirmPassword
    ) : IRequest<Response>;

    /// <summary>
    /// Registration response containing JWT token and user information
    /// </summary>
    /// <param name="Token">JWT bearer token for authentication</param>
    /// <param name="UserId">Unique user identifier</param>
    /// <param name="Email">User's email address</param>
    /// <param name="IsAdmin">Flag indicating if user has admin privileges</param>
    /// <param name="ExpiresAt">Token expiration timestamp (UTC)</param>
    public record Response(
         string Token,
         int UserId,
         string Email,
         bool IsAdmin,
         DateTime ExpiresAt
     );
}
