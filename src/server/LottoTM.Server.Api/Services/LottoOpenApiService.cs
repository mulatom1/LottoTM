using System.Text.Json;
using System.Text.Json.Serialization;

namespace LottoTM.Server.Api.Services;

/// <summary>
/// Service for fetching latest draw results from Lotto Open API
/// </summary>
public class LottoOpenApiService : ILottoOpenApiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<LottoOpenApiService> _logger;

    public LottoOpenApiService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<LottoOpenApiService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
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

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "LottoTM.Server.Api.LottoOpenApiService/1.0");
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            var apiKey = _configuration.GetValue("LottoOpenApi:ApiKey", "");
            if (!string.IsNullOrEmpty(apiKey))
            {
                httpClient.DefaultRequestHeaders.Add("secret", apiKey);
            }

            var url = _configuration.GetValue("LottoOpenApi:Url", "");
            if (string.IsNullOrEmpty(url))
            {
                _logger.LogError("LottoOpenApi URL not configured");
                throw new InvalidOperationException("LottoOpenApi URL not configured");
            }

            var apiUrl = $"{url}/api/open/v1/lotteries/draw-results/by-date-per-game?drawDate={date:yyyy-MM-dd}&gameType=Lotto&size=10&sort=drawDate&order=DESC&index=1";
            _logger.LogDebug("Fetching draws from Lotto Open API: {Url}", apiUrl);

            var response = await httpClient.GetAsync(apiUrl);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Lotto Open API returned non-success status: {StatusCode}", response.StatusCode);
                throw new InvalidOperationException($"Lotto Open API returned status: {response.StatusCode}");
            }

            var jsonContent = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("Received response from Lotto Open API");

            // Deserialize the Lotto Open API response
            var lottoOpenApiResponse = JsonSerializer.Deserialize<LottoOpenApiResponse>(jsonContent);

            if (lottoOpenApiResponse?.Items == null || lottoOpenApiResponse.Items.Count == 0)
            {
                _logger.LogDebug("No draw items found in Lotto Open API response");
                return CreateEmptyDrawsResponse();
            }

            // Transform to DrawsResponse format and process each game type
            var drawDataList = new List<DrawData>();

            foreach (var item in lottoOpenApiResponse.Items)
            {
                if (item.Results == null || item.Results.Count == 0)
                {
                    _logger.LogDebug("No results found for game type {GameType}", item.GameType);
                    continue;
                }

                var result = item.Results[0]; // Take first result

                // Map gameType to LottoType format
                var mappedLottoType = item.GameType?.ToUpper() == "LOTTO" ? "LOTTO" 
                    : item.GameType?.ToUpper() == "LOTTOPLUS" ? "LOTTO PLUS"
                    : item.GameType ?? string.Empty;

                var drawData = new DrawData
                {
                    DrawDate = result.DrawDate?.ToString("yyyy-MM-dd") ?? string.Empty,
                    GameType = mappedLottoType,
                    Numbers = result.ResultsJson ?? Array.Empty<int>()
                };

                drawDataList.Add(drawData);
            }

            // Create DrawsResponse format
            var drawsResponse = new DrawsResponse
            {
                Data = drawDataList
            };

            // Serialize to JSON
            var jsonResults = JsonSerializer.Serialize(drawsResponse);
            _logger.LogDebug("Successfully fetched and transformed draw results from Lotto Open API");

            return jsonResults;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed while fetching draw results");
            throw new InvalidOperationException("Failed to fetch draw results from Lotto Open API", ex);
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
    /// Creates an empty draws response JSON string
    /// </summary>
    private static string CreateEmptyDrawsResponse()
    {
        return JsonSerializer.Serialize(new DrawsResponse { Data = new List<DrawData>() });
    }

    // DTOs for transforming to common format
    private class DrawsResponse
    {
        public List<DrawData>? Data { get; set; }
    }

    private class DrawData
    {
        public string DrawDate { get; set; } = string.Empty;
        public string GameType { get; set; } = string.Empty;
        public int[] Numbers { get; set; } = Array.Empty<int>();
    }

    // DTOs for deserializing Lotto Open API response
    private class LottoOpenApiResponse
    {
        [JsonPropertyName("totalRows")]
        public int TotalRows { get; set; }

        [JsonPropertyName("items")]
        public List<LottoOpenApiItem>? Items { get; set; }

        [JsonPropertyName("code")]
        public int Code { get; set; }
    }

    private class LottoOpenApiItem
    {
        [JsonPropertyName("drawSystemId")]
        public int DrawSystemId { get; set; }

        [JsonPropertyName("drawDate")]
        public DateTime? DrawDate { get; set; }

        [JsonPropertyName("gameType")]
        public string? GameType { get; set; }

        [JsonPropertyName("results")]
        public List<LottoOpenApiResult>? Results { get; set; }
    }

    private class LottoOpenApiResult
    {
        [JsonPropertyName("drawDate")]
        public DateTime? DrawDate { get; set; }

        [JsonPropertyName("drawSystemId")]
        public int DrawSystemId { get; set; }

        [JsonPropertyName("gameType")]
        public string? GameType { get; set; }

        [JsonPropertyName("resultsJson")]
        public int[]? ResultsJson { get; set; }

        [JsonPropertyName("specialResults")]
        public object[]? SpecialResults { get; set; }
    }
}
