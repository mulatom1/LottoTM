using System.Text.Json.Serialization;


namespace LottoTM.Server.Api.Services.LottoOpenApi.DTOs;


public class GetDrawsLottoByDateResponseResult
{
    [JsonPropertyName("drawDate")]
    public DateTime? DrawDate { get; set; }

    [JsonPropertyName("drawSystemId")]
    public int DrawSystemId { get; set; }

    [JsonPropertyName("gameType")]
    public string? GameType { get; set; }

    [JsonPropertyName("resultsJson")]
    public int[]? ResultsJson { get; set; }
}
