using LottoTM.Server.Api.Services.LottoOpenApi.DTOs;
using System.Text.Json;

namespace LottoTM.Server.Api.Services.LottoOpenApi;

/// <summary>
/// Service for fetching latest draw results from Lotto Open API
/// </summary>
public partial class LottoOpenApiService : ILottoOpenApiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<LottoOpenApiService> _logger;

    private readonly string errorUrlMessage = "LottoOpenApi URL not configured";

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
    public async Task<GetDrawsLottoByDateResponse> GetDrawsLottoByDate(DateOnly? date = null)
    {
        try
        {
            if (date == null) date = DateOnly.FromDateTime(DateTime.Today);

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "LottoTM.Server.Api.LottoOpenApiService/1.0");
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            var apiKey = _configuration.GetValue("LottoOpenApi:ApiKey", "");
            if (!string.IsNullOrEmpty(apiKey)) httpClient.DefaultRequestHeaders.Add("secret", apiKey);

            var url = _configuration.GetValue("LottoOpenApi:Url", "");
            if (string.IsNullOrEmpty(url))
            {
                _logger.LogError(errorUrlMessage);
                throw new InvalidOperationException(errorUrlMessage);
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
            _logger.LogDebug("{JsonContent}", jsonContent);

            return JsonSerializer.Deserialize<GetDrawsLottoByDateResponse>(jsonContent)
                   ?? new GetDrawsLottoByDateResponse()
                   {
                       Code = 0,
                       TotalRows = 0,
                       Items = []
                   };
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

    public async Task<string> GetActualInfo()
    {
        try
        {
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
                _logger.LogError(errorUrlMessage);
                throw new InvalidOperationException(errorUrlMessage);
            }

            var apiUrl = $"{url}/api/open/v1/lotteries/info?gameType=Lotto";
            _logger.LogDebug("Fetching INFO from Lotto Open API: {Url}", apiUrl);

            var response = await httpClient.GetAsync(apiUrl);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Lotto Open API returned non-success status: {StatusCode}", response.StatusCode);
                throw new InvalidOperationException($"Lotto Open API returned status: {response.StatusCode}");
            }

            var jsonContent = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("Received response from Lotto Open API");
            _logger.LogDebug("{JsonContent}", jsonContent);

            return jsonContent;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed while fetching draw results");
            throw new InvalidOperationException("Failed to fetch INFO results from Lotto Open API", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse JSON response");
            throw new InvalidOperationException("Failed to parse API response", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching INFO results");
            throw;
        }
    }
}
