using Microsoft.Extensions.Options;
using LottoTM.Server.Api.Options;
using LottoTM.Server.Api.Repositories;
using LottoTM.Server.Api.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LottoTM.Server.Api.Services;

public class LottoWorker : BackgroundService
{
    private readonly ILogger<LottoWorker> _logger;
    private readonly LottoWorkerOptions _options;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IConfiguration _configuration;

    public LottoWorker(
        ILogger<LottoWorker> logger,
        IOptions<LottoWorkerOptions> options,
        IConfiguration configuration,
        IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _options = options.Value;
        _serviceScopeFactory = serviceScopeFactory;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogDebug("LottoWorker started. Enabled: {Enabled}, Time window: {StartTime} - {EndTime}, Interval: {Interval} minutes",
            _options.Enable, _options.StartTime, _options.EndTime, _options.IntervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            // Ping to keep the API alive
            await PingForApiVersion(stoppingToken);

            var now = DateTime.Now;
            var currentTime = now.TimeOfDay;


            // Sprawdź czy worker jest włączony i czy jesteśmy w przedziale czasowym
            if (_options.Enable && currentTime >= _options.StartTime && currentTime <= _options.EndTime)
            {
                _logger.LogDebug("LottoWorker is in active time window at: {time}", now);

                // Sprawd� czy losowania na dzie� ju� istniej�
                using var scope = _serviceScopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var targetDate = DateOnly.FromDateTime(DateTime.Today);

                var lottoExists = await dbContext.Draws
                    .AnyAsync(d => d.DrawDate == targetDate && d.LottoType == "LOTTO", stoppingToken);
                var lottoPlusExists = await dbContext.Draws
                    .AnyAsync(d => d.DrawDate == targetDate && d.LottoType == "LOTTO PLUS", stoppingToken);

                if (lottoExists && lottoPlusExists)
                {
                    _logger.LogDebug("Draws for date {Date} already exist (LOTTO and LOTTO PLUS), skipping fetch", targetDate);
                }
                else
                {
                    _logger.LogDebug("Draws for date {Date} do not exist or are incomplete. LOTTO exists: {LottoExists}, LOTTO PLUS exists: {LottoPlusExists}",
                        targetDate, lottoExists, lottoPlusExists);
                    await FetchAndSaveDrawsFromLottoOpenApi(stoppingToken);
                }
            }

            if (!_options.Enable)
            {
                _logger.LogDebug("LottoWorker is disabled (LottoWorker:Enable = false)");
            }
            else
            {
                _logger.LogDebug("LottoWorker outside active window.");
            }
            await Task.Delay(_options.IntervalMinutes * 1000 * 60, stoppingToken);
        }
    }

    private async Task PingForApiVersion(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();

            var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient();

            var url = _configuration.GetValue("ApiUrl", "");
            if (string.IsNullOrEmpty(url))
            {
                _logger.LogDebug("API URL is not configured. Skipping ping.");
                return;
            }
            _logger.LogDebug($"Pinging {url}/api/version to keep the website alive");

            var response = await httpClient.GetAsync($"{url}/api/version", stoppingToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Successfully pinged Status: {StatusCode}", response.StatusCode);
            }
            else
            {
                _logger.LogDebug("Ping to returned non-success status: {StatusCode}", response.StatusCode);
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request exception while pinging PingForApiVersion");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while pinging PingForApiVersion");
        }
    }
    
    private async Task FetchAndSaveDrawsFromLottoOpenApi(CancellationToken stoppingToken)
    {
        _logger.LogDebug("Executing FetchAndSaveDrawsFromLottoOpenApi at: {time}", DateTime.Now);

        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var lottoOpenApiService = scope.ServiceProvider.GetRequiredService<ILottoOpenApiService>();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Pobierz wyniki losowa� z API
            var today = DateTime.Today;
            _logger.LogDebug("Fetching draws for date: {date}", today);

            // Pobierz wszystkie typy losowa� (LOTTO i LOTTO PLUS są zwracane razem)
            var jsonResults = await lottoOpenApiService.GetActualDraws(today, "LOTTO");
            await SaveInDatabase(jsonResults, dbContext, stoppingToken);

            _logger.LogDebug("FetchAndSaveDrawsFromLottoOpenApi completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while executing FetchAndSaveDrawsFromLottoOpenApi");
        }
    }

    private async Task SaveInDatabase(string jsonResults, AppDbContext dbContext, CancellationToken stoppingToken)
    {
        try
        {
            var drawsData = JsonSerializer.Deserialize<DrawsResponse>(jsonResults);

            if (drawsData?.Data == null || drawsData.Data.Count == 0)
            {
                _logger.LogDebug("No draw results found in the response");
                return;
            }

            foreach (var drawData in drawsData.Data)
            {
                try
                {
                    _logger.LogDebug("Processing draw: Date={DrawDate}, Type={GameType}, Numbers={Numbers}",
                        drawData.DrawDate, drawData.GameType, string.Join(",", drawData.Numbers));

                    var drawDate = DateOnly.Parse(drawData.DrawDate);

                    // Sprawd� czy losowanie na dan� dat� i typ ju� istnieje
                    var existingDraw = await dbContext.Draws
                        .AnyAsync(d => d.DrawDate == drawDate && d.LottoType == drawData.GameType, stoppingToken);

                    if (existingDraw)
                    {
                        _logger.LogDebug("Draw for date {DrawDate} with type {LottoType} already exists, skipping",
                            drawDate, drawData.GameType);
                        continue;
                    }

                    // Walidacja liczb
                    if (drawData.Numbers.Length != 6)
                    {
                        _logger.LogDebug("Invalid number count for draw {DrawDate}: expected 6, got {Count}",
                            drawDate, drawData.Numbers.Length);
                        continue;
                    }

                    if (drawData.Numbers.Any(n => n < 1 || n > 49))
                    {
                        _logger.LogDebug("Invalid numbers for draw {DrawDate}: numbers must be between 1-49", drawDate);
                        continue;
                    }

                    if (drawData.Numbers.Distinct().Count() != 6)
                    {
                        _logger.LogDebug("Duplicate numbers found in draw {DrawDate}", drawDate);
                        continue;
                    }

                    // Transakcja: INSERT Draw + DrawNumbers
                    using var transaction = await dbContext.Database.BeginTransactionAsync(stoppingToken);

                    try
                    {
                        // Dodaj Draw (CreatedByUserId = null dla worker-generated draws)
                        var draw = new Draw
                        {
                            DrawDate = drawDate,
                            LottoType = drawData.GameType,
                            CreatedAt = DateTime.UtcNow,
                            CreatedByUserId = null // Worker nie ma userId
                        };
                        dbContext.Draws.Add(draw);
                        await dbContext.SaveChangesAsync(stoppingToken);

                        // Dodaj DrawNumbers
                        var drawNumbers = drawData.Numbers
                            .Select((number, index) => new DrawNumber
                            {
                                DrawId = draw.Id,
                                Number = number,
                                Position = (byte)(index + 1)
                            })
                            .ToList();

                        dbContext.DrawNumbers.AddRange(drawNumbers);
                        await dbContext.SaveChangesAsync(stoppingToken);

                        await transaction.CommitAsync(stoppingToken);

                        _logger.LogDebug("Draw {DrawId} saved successfully: Date={DrawDate}, Type={GameType}",
                            draw.Id, drawDate, drawData.GameType);
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync(stoppingToken);
                        _logger.LogError(ex, "Failed to save draw: Date={DrawDate}, Type={GameType}",
                            drawDate, drawData.GameType);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to process draw: Date={DrawDate}, Type={GameType}",
                        drawData.DrawDate, drawData.GameType);
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize draw results JSON: {Json}", jsonResults);
        }
    }

    // DTOs for deserializing draw results
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
}