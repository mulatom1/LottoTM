using System.Text.Json.Serialization;


namespace LottoTM.Server.Api.Services.LottoOpenApi.DTOs;


public class GetDrawsLottoByDateResponse
{
    [JsonPropertyName("totalRows")]
    public int TotalRows { get; set; }

    [JsonPropertyName("items")]
    public List<GetDrawsLottoByDateResponseItem>? Items { get; set; }

    [JsonPropertyName("code")]
    public int Code { get; set; }
}