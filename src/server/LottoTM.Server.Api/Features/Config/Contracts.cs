using MediatR;

namespace LottoTM.Server.Api.Features.Config;

/// <summary>
/// Contracts for application configuration endpoint
/// </summary>
public class Contracts
{
    /// <summary>
    /// Request to get application configuration
    /// </summary>
    public record Request : IRequest<Response>;

    /// <summary>
    /// Response containing application configuration values
    /// </summary>
    /// <param name="VerificationMaxDays">Maximum number of days allowed for verification date range</param>
    public record Response(
        int VerificationMaxDays
    );
}
