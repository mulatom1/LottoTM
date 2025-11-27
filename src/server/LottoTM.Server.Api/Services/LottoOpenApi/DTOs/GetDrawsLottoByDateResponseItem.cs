using System.Text.Json.Serialization;


namespace LottoTM.Server.Api.Services.LottoOpenApi.DTOs;


public class GetDrawsLottoByDateResponseItem
{
    [JsonPropertyName("results")]
    public List<GetDrawsLottoByDateResponseResult>? Results { get; set; }
}
