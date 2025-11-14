using MediatR;

namespace LottoTM.Server.Api.Features.XLotto.IsEnabled;

public class Contracts
{
    /// <summary>
    /// Request to check if XLotto feature is enabled
    /// </summary>
    public record Request() : IRequest<Response>;

    /// <summary>
    /// Response indicating whether XLotto feature is enabled
    /// </summary>
    /// <param name="IsEnabled">True if the feature is enabled, false otherwise</param>
    public record Response(bool IsEnabled);
}
