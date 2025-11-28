using System.Text.Json.Serialization;

namespace LottoTM.Server.Api.Services.LottoOpenApi.DTOs;

/// <summary>
/// Represents the prize information for a specific prize level.
/// </summary>
public class GetDrawsStatsByIdResponsePrizeInfo
{
    /// <summary>
    /// The number of prizes awarded at this level.
    /// </summary>
    [JsonPropertyName("prize")]
    public int Prize { get; set; }

    /// <summary>
    /// The prize value amount.
    /// </summary>
    [JsonPropertyName("prizeValue")]
    public decimal PrizeValue { get; set; }
}
