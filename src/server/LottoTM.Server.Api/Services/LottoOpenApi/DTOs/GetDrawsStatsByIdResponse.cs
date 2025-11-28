using System.Text.Json.Serialization;

namespace LottoTM.Server.Api.Services.LottoOpenApi.DTOs;

/// <summary>
/// Represents the response for a single draw's prize information.
/// </summary>
public class GetDrawsStatsByIdResponse
{
    /// <summary>
    /// A dictionary of prize levels (keys: "1", "2", "3", "4") to their prize information.
    /// </summary>
    [JsonPropertyName("prizes")]
    public Dictionary<string, GetDrawsStatsByIdResponsePrizeInfo>? Prizes { get; set; }

    /// <summary>
    /// The date and time of the draw.
    /// </summary>
    [JsonPropertyName("drawDate")]
    public DateTime DrawDate { get; set; }

    /// <summary>
    /// The system ID of the draw.
    /// </summary>
    [JsonPropertyName("drawSystemId")]
    public int? DrawSystemId { get; set; }

    /// <summary>
    /// The type of the game (e.g., "Lotto", "LottoPlus", "SuperSzansa").
    /// </summary>
    [JsonPropertyName("gameType")]
    public string? GameType { get; set; }

    /// <summary>
    /// Indicates whether the prizes are empty.
    /// </summary>
    [JsonPropertyName("prizesEmpty")]
    public bool PrizesEmpty { get; set; }
}