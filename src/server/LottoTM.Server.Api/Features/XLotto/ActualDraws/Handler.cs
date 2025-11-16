using MediatR;
using LottoTM.Server.Api.Options;
using LottoTM.Server.Api.Services;
using Microsoft.Extensions.Options;

namespace LottoTM.Server.Api.Features.XLotto.ActualDraws;

/// <summary>
/// Handler for retrieving actual draws from XLotto website via Gemini API
/// </summary>
public class Handler : IRequestHandler<Contracts.Request, Contracts.Response>
{
    private readonly ILogger<Handler> _logger;
    private readonly IXLottoService _xLottoService;
    private readonly GoogleGeminiOptions _googleGeminiOptions;

    public Handler(
        IXLottoService xLottoService,
        IOptions<GoogleGeminiOptions> googleGeminiOptions,
        ILogger<Handler> logger)
    {
        _logger = logger;
        _xLottoService = xLottoService;
        _googleGeminiOptions = googleGeminiOptions.Value;
    }

    public async Task<Contracts.Response> Handle(
        Contracts.Request request,
        CancellationToken cancellationToken)
    {
        // Check if GoogleGemini feature is enabled
        if (!_googleGeminiOptions.Enable)
        {
            _logger.LogWarning("GoogleGemini feature is disabled. Returning empty data.");
            return new Contracts.Response("{\"Data\":[]}");
        }

        _logger.LogDebug("Pobieranie aktualnych wyników z XLotto...");

        // Fetch actual draws from XLotto via Gemini API
        var jsonData = await _xLottoService.GetActualDraws(request.Date, request.LottoType);

        _logger.LogDebug("Pomyślnie pobrano wyniki z XLotto");

        return new Contracts.Response(jsonData);
    }
}
