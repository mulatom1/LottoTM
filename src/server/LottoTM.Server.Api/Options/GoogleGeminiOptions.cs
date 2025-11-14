namespace LottoTM.Server.Api.Options;

/// <summary>
/// Configuration options for Google Gemini API integration
/// </summary>
public class GoogleGeminiOptions
{
    /// <summary>
    /// Gets or sets the Google Gemini API key (base64 encoded)
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Gemini model to use
    /// </summary>
    public string Model { get; set; } = "gemini-2.0-flash";

    /// <summary>
    /// Gets or sets whether the Google Gemini feature is enabled
    /// </summary>
    public bool Enable { get; set; } = false;
}
