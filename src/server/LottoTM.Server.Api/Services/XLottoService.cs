using System.Text;
using System.Text.Json;

namespace LottoTM.Server.Api.Services;

/// <summary>
/// Service for fetching latest draw results from XLotto website using Google Gemini API
/// </summary>
public class XLottoService : IXLottoService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<XLottoService> _logger;

    public XLottoService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<XLottoService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
        
        // Register the code pages encoding provider to support iso-8859-2 and other encodings
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    /// <inheritdoc/>
    public async Task<string> GetActualDraws(DateTime? date = null, string lottoType = "LOTTO")
    {
        try
        {
            if (date == null)
            {
                date = DateTime.Today;
            }

            // Step 1: Fetch the XLotto website content
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            
            _logger.LogInformation("Fetching XLotto website content...");
            var url = _configuration.GetValue("XLotto:Url", "");
            if (string.IsNullOrEmpty(url))
            {
                _logger.LogError("XLotto URL not configured");
                throw new InvalidOperationException("XLotto URL not configured");
            }
            var response = await httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to fetch XLotto website content. Status Code: {StatusCode}", response.StatusCode);
                throw new InvalidOperationException($"Failed to fetch XLotto website content. Status Code: {response.StatusCode}");
            }

            // Read content as byte array first, then decode with proper encoding
            var contentBytes = await response.Content.ReadAsByteArrayAsync();
            var encoding = GetEncodingFromContentType(response.Content.Headers.ContentType?.CharSet) ?? Encoding.UTF8;
            var htmlContent = encoding.GetString(contentBytes);
            
            _logger.LogInformation("Successfully fetched XLotto website content. Size: {Size} bytes", htmlContent.Length);

            // Step 2: Send content to Google Gemini API
            var geminiApiKeyBase64 = _configuration["GoogleGemini:ApiKey"]
                ?? throw new InvalidOperationException("Google Gemini API Key not configured");

            var geminiApiKey = Encoding.UTF8.GetString(Convert.FromBase64String(geminiApiKeyBase64));

            var geminiModel = _configuration["GoogleGemini:Model"] ?? "gemini-2.0-flash";
            var geminiApiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{geminiModel}:generateContent?key={geminiApiKey}";

            var prompt = 
                  $"Przeszukaj zawartość contentu i podaj tylko wyniki dla typu gry {lottoType} na dzień {date?.ToString("yyyy-MM-dd") ?? "najnowszy"}. "
                + "interesuje mnie wynik tylko na podany dzien, a nie starsze wyniki z poprzednich dni. "
                + @"Wynik oddaj w formacie w JSON: { ""Data"": [ { ""DrawDate"": ""2025-01-01"", ""GameType"": """ + lottoType + @""", ""Numbers"": [1, 2, 3, 4, 5, 6 ] } ] }."
                + @"Jeśli nie możesz znaleźć wyników na daną datę, zwróć pustą tablicę w polu Data, taką jak poniżej: { ""Data"": [ ] } i nie zwracaj niczego innego.Tylko JSON bez białych znaków.";

            var geminiRequest = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = $"{prompt}\n\nHTML Content:\n{htmlContent}" }
                        }
                    }
                }
            };

            var geminiHttpClient = _httpClientFactory.CreateClient();
            var geminiRequestContent = new StringContent(
                JsonSerializer.Serialize(geminiRequest),
                Encoding.UTF8,
                "application/json");

            _logger.LogInformation("Sending request to Google Gemini API...");
            var geminiResponse = await geminiHttpClient.PostAsync(geminiApiUrl, geminiRequestContent);
            geminiResponse.EnsureSuccessStatusCode();

            var geminiResponseContent = await geminiResponse.Content.ReadAsStringAsync();
            _logger.LogInformation("Received response from Google Gemini API");

            // Step 3: Extract the text from Gemini response
            var geminiResult = JsonSerializer.Deserialize<JsonElement>(geminiResponseContent);
            var generatedText = geminiResult
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            if (string.IsNullOrEmpty(generatedText))
            {
                throw new InvalidOperationException("Gemini API returned empty response");
            }

            _logger.LogInformation("Successfully extracted draw results from Gemini response");
            
            // Clean up the response (remove markdown code blocks if present)
            var cleanedText = generatedText.Trim();
            if (cleanedText.StartsWith("```json"))
            {
                cleanedText = cleanedText.Substring(7);
            }
            if (cleanedText.StartsWith("```"))
            {
                cleanedText = cleanedText.Substring(3);
            }
            if (cleanedText.EndsWith("```"))
            {
                cleanedText = cleanedText.Substring(0, cleanedText.Length - 3);
            }
            cleanedText = cleanedText.Trim();

            // Validate it's valid JSON
            JsonSerializer.Deserialize<JsonElement>(cleanedText);

            return cleanedText;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed while fetching draw results");
            throw new InvalidOperationException("Failed to fetch draw results from XLotto or Gemini API", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse JSON response");
            throw new InvalidOperationException("Failed to parse API response", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching draw results");
            throw;
        }
    }

    /// <summary>
    /// Gets the encoding from the content type charset
    /// </summary>
    private static Encoding? GetEncodingFromContentType(string? charset)
    {
        if (string.IsNullOrEmpty(charset))
            return null;

        try
        {
            return Encoding.GetEncoding(charset);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }
}
