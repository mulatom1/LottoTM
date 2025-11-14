using MediatR;
using LottoTM.Server.Api.Options;
using Microsoft.Extensions.Options;

namespace LottoTM.Server.Api.Features.XLotto.IsEnabled;

/// <summary>
/// Handler for checking if XLotto feature is enabled
/// </summary>
public class Handler : IRequestHandler<Contracts.Request, Contracts.Response>
{
    private readonly GoogleGeminiOptions _googleGeminiOptions;

    public Handler(IOptions<GoogleGeminiOptions> googleGeminiOptions)
    {
        _googleGeminiOptions = googleGeminiOptions.Value;
    }

    public Task<Contracts.Response> Handle(
        Contracts.Request request,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(new Contracts.Response(_googleGeminiOptions.Enable));
    }
}
