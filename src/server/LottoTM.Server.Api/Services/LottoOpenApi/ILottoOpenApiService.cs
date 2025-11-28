using LottoTM.Server.Api.Services.LottoOpenApi.DTOs;


namespace LottoTM.Server.Api.Services.LottoOpenApi;


public interface ILottoOpenApiService
{
    Task<GetDrawsLottoByDateResponse> GetDrawsLottoByDate(DateOnly? date = null);

    Task<List<GetDrawsStatsByIdResponse>> GetDrawsStatsById(int drawSystemId);

    Task<string> GetActualInfo();
}
