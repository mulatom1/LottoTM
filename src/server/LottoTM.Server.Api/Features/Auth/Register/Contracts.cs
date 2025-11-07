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
    /// Registration response with success message
    /// </summary>
    /// <param name="Message">Success message</param>
    public record Response(string Message);
}
