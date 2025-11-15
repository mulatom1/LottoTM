namespace LottoTM.Server.Api.Services;

/// <summary>
/// Interface for Lotto Open API service to fetch latest draw results
/// </summary>
public interface ILottoOpenApiService
{
    /// <summary>
    /// Gets the actual draw results from Lotto Open API
    /// </summary>
    /// <param name="date">Date of the draw to fetch. If null, uses today's date.</param>
    /// <param name="lottoType">Type of lottery (LOTTO or LOTTO PLUS). Default is "LOTTO".</param>
    /// <returns>JSON string containing draw results in DrawsResponse format</returns>
    Task<string> GetActualDraws(DateTime? date = null, string lottoType = "LOTTO");
}
