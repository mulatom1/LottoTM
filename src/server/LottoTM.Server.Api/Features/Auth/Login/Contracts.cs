using MediatR;

namespace LottoTM.Server.Api.Features.Auth.Login;

public class Contracts
{
    /// <summary>
    /// Login request containing user credentials
    /// </summary>
    public record Request : IRequest<Response>
    {
        /// <summary>
        /// User's email address
        /// </summary>
        public string Email { get; init; } = string.Empty;

        /// <summary>
        /// User's password (plain text - will be verified against hashed password)
        /// </summary>
        public string Password { get; init; } = string.Empty;
    }

    /// <summary>
    /// Login response containing JWT token and user information
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
