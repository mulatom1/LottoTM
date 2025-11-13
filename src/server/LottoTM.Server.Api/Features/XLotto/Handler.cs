using MediatR;
using LottoTM.Server.Api.Services;

namespace LottoTM.Server.Api.Features.XLotto.ActualDraws;

/// <summary>
/// Handler for retrieving actual draws from XLotto website via Gemini API
/// </summary>
public class Handler : IRequestHandler<Contracts.Request, Contracts.Response>
{
    private readonly ILogger<Handler> _logger;
    private readonly IXLottoService _xLottoService;

    public Handler(
        IXLottoService xLottoService,
        ILogger<Handler> logger)
    {
        _logger = logger;
        _xLottoService = xLottoService;
    }

    public async Task<Contracts.Response> Handle(
        Contracts.Request request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Pobieranie aktualnych wyników z XLotto...");

        // Fetch actual draws from XLotto via Gemini API
        var jsonData = await _xLottoService.GetActualDraws(request.Date, request.LottoType);

        _logger.LogInformation("Pomyślnie pobrano wyniki z XLotto");

        return new Contracts.Response(jsonData);
    }
}
