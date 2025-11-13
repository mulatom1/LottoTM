namespace LottoTM.Server.Api.Services;

/// <summary>
/// Interface for XLotto service to fetch latest draw results
/// </summary>
public interface IXLottoService
{
    /// <summary>
    /// Gets the actual draw results from XLotto website
    /// </summary>
    /// <returns>JSON string containing latest LOTTO and LOTTO PLUS results</returns>
    Task<string> GetActualDraws(DateTime? date = null, string lottoType = "LOTTO");
}
