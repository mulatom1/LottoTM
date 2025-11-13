using MediatR;

namespace LottoTM.Server.Api.Features.XLotto.ActualDraws;

public class Contracts
{
    /// <summary>
    /// Request to get actual draws from XLotto
    /// </summary>
    public record Request(DateTime Date, string LottoType) : IRequest<Response>;

    /// <summary>
    /// Response containing JSON data from Gemini LLM
    /// </summary>
    /// <param name="JsonData">Raw JSON response from Gemini API containing draw results</param>
    public record Response(string JsonData);
}
