using LottoTM.Server.Api.Entities;
using LottoTM.Server.Api.Options;
using LottoTM.Server.Api.Repositories;
using LottoTM.Server.Api.Services.LottoOpenApi;
using LottoTM.Server.Api.Services.LottoOpenApi.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;


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
        _logger.LogDebug("LottoWorker started. Enabled: {Enabled}, Time window: {StartTime} - {EndTime}, Interval: {Interval} minutes, InWeek: {InWeek}",
            _options.Enable, _options.StartTime, _options.EndTime, _options.IntervalMinutes, string.Join(",", _options.InWeek));


        DateOnly lastArchRunTime = await GetLastArchRunTime(stoppingToken);


        while (!stoppingToken.IsCancellationRequested)
        {
            // Ping to keep the API alive
            await PingForApiVersion(stoppingToken);

            var now = DateTime.Now;
            var currentTime = now.TimeOfDay;
            var currentDayAbbrev = GetDayAbbreviation(now.DayOfWeek);


            // Sprawdź czy worker jest włączony, czy jesteśmy w przedziale czasowym i czy dzień jest w InWeek
            if (_options.Enable 
                && _options.InWeek.Contains(currentDayAbbrev) 
                && currentTime >= _options.StartTime 
                && currentTime <= _options.EndTime)
            {
                _logger.LogDebug("LottoWorker is in active time window at: {Time} on day {Day}", now, currentDayAbbrev);

                // Sprawd� czy losowania na dzie� ju� istniej�
                using var scope = _serviceScopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var targetDate = DateOnly.FromDateTime(DateTime.Today);

                var lottoExists = await dbContext.Draws.AnyAsync(d => d.DrawDate == targetDate && d.LottoType == "Lotto", stoppingToken);
                var lottoPlusExists = await dbContext.Draws.AnyAsync(d => d.DrawDate == targetDate && d.LottoType == "LottoPlus", stoppingToken);

                if (lottoExists && lottoPlusExists)
                {
                    _logger.LogDebug("Draws for date {Date} already exist (LOTTO and LOTTO PLUS), skipping fetch", targetDate);
                }
                else
                {
                    _logger.LogDebug(
                        "Draws for date {Date} do not exist or are incomplete. LOTTO exists: {LottoExists}, LOTTO PLUS exists: {LottoPlusExists}",
                        targetDate, lottoExists, lottoPlusExists);
                    await FetchAndSaveDrawsFromLottoOpenApi(stoppingToken);
                }
            }

            ////Archiwum
            //if (_options.Enable && lastArchRunTime.Year >= 1957)
            //{
            //    lastArchRunTime = lastArchRunTime.AddDays(-1);

            //    if (_options.InWeek.Contains(GetDayAbbreviation(lastArchRunTime.DayOfWeek)))
            //        await FetchAndSaveArchDrawsFromLottoOpenApi(lastArchRunTime, stoppingToken);
            //}

            //Wygrane
            if (_options.Enable)
            {
                await FetchAndSaveStatsDrawsFromLottoOpenApi(stoppingToken);
            }

            if (!_options.Enable)
            {
                _logger.LogDebug("LottoWorker is disabled (LottoWorker:Enable = false)");
            }
            else
            {
                _logger.LogDebug("LottoWorker outside active window.");
            }

            await Task.Delay((int)(_options.IntervalMinutes * 1000) * 60, stoppingToken);
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
        _logger.LogDebug("Executing FetchAndSaveDrawsFromLottoOpenApi at: {Time}", DateTime.Now);

        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var lottoOpenApiService = scope.ServiceProvider.GetRequiredService<ILottoOpenApiService>();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var today = DateOnly.FromDateTime(DateTime.Today);
            _logger.LogDebug("Fetching draws for date: {Date}", today);

            var response = await lottoOpenApiService.GetDrawsLottoByDate(today);
            if (response == null || response.TotalRows == 0)
            {
                _logger.LogDebug("No response received from LottoOpenApiService for date: {Date}", today);
                return;
            }

            await SaveInDatabase(response, dbContext, stoppingToken);

            _logger.LogDebug("FetchAndSaveDrawsFromLottoOpenApi completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while executing FetchAndSaveDrawsFromLottoOpenApi");
        }
    }

    //private async Task FetchAndSaveArchDrawsFromLottoOpenApi(DateOnly date, CancellationToken stoppingToken)
    //{
    //    _logger.LogDebug("Executing FetchAndSaveArchDrawsFromLottoOpenApi at: {Time}", DateTime.Now);

    //    try
    //    {
    //        using var scope = _serviceScopeFactory.CreateScope();
    //        var lottoOpenApiService = scope.ServiceProvider.GetRequiredService<ILottoOpenApiService>();
    //        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    //        _logger.LogDebug("Fetching arch draws for date: {Date}", date);

    //        var response = await lottoOpenApiService.GetDrawsLottoByDate(date);
    //        if (response == null || response.TotalRows == 0)
    //        {
    //            _logger.LogDebug("No response received from LottoOpenApiService for date: {Date}", date);
    //            return;
    //        }

    //        await SaveInDatabase(response, dbContext, stoppingToken);

    //        _logger.LogDebug("FetchAndSaveDrawsFromLottoOpenApi completed successfully");
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Error occurred while executing FetchAndSaveDrawsFromLottoOpenApi");
    //    }
    //}

    private async Task FetchAndSaveStatsDrawsFromLottoOpenApi(CancellationToken stoppingToken)
    {
        _logger.LogDebug("Executing FetchAndSaveStatsDrawsFromLottoOpenApi at: {Time}", DateTime.Now);

        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var lottoOpenApiService = scope.ServiceProvider.GetRequiredService<ILottoOpenApiService>();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var draw = await dbContext.Draws
                .Where(d => d.DrawSystemId > 0 && d.WinPoolCount1 == null)
                .OrderByDescending(d => d.DrawDate)
                .FirstOrDefaultAsync(stoppingToken);
            if (draw == null)
            {
                _logger.LogDebug("No draws found that require stats update.");
                return;
            }

            _logger.LogDebug("Fetching stats of draw for: {Id}", draw.DrawSystemId);

            var response = await lottoOpenApiService.GetDrawsStatsById(draw.DrawSystemId);
            if (response == null)
            {
                _logger.LogDebug("No response received from LottoOpenApiService for: {Id}", draw.DrawSystemId);
                return;
            }

            await SaveInDatabase(response, dbContext, stoppingToken);

            _logger.LogDebug("FetchAndSaveDrawsFromLottoOpenApi completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while executing FetchAndSaveDrawsFromLottoOpenApi");
        }
    }

    private async Task<DateOnly> GetLastArchRunTime(CancellationToken stoppingToken)
    {
        _logger.LogDebug("Executing GetLastArchRunTime at: {Time}", DateTime.Now);

        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var lastDate = await dbContext.Draws.MinAsync(d => d.DrawDate, stoppingToken);
            _logger.LogDebug("GetLastArchRunTime completed successfully {Time}", lastDate);

            return lastDate;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while executing FetchAndSaveDrawsFromLottoOpenApi");
            return DateOnly.FromDateTime(DateTime.Today);
        }
    }

    private async Task SaveInDatabase(GetDrawsLottoByDateResponse response, AppDbContext dbContext, CancellationToken stoppingToken)
    {
        if (response.Items == null || response.Items.Count == 0)
        {
            _logger.LogDebug("No draw items to process in the response");
            return;
        }

        try
        {
            foreach (var result in response.Items ?? [])
            {
                foreach (var item in result.Results ?? [])
                {
                    try
                    {

                        _logger.LogDebug(
                            "Processing draw: Date={DrawDate}, Type={GameType}, Numbers={Numbers}",
                            item.DrawDate, item.GameType, string.Join(",", item?.ResultsJson ?? []));

                        var drawDate = DateOnly.FromDateTime(item.DrawDate.Value);

                        // Check if draw for the date and type already exists
                        var existingDraw = await dbContext.Draws.AnyAsync(d => d.DrawDate == drawDate && d.LottoType == item.GameType, stoppingToken);
                        if (existingDraw)
                        {
                            _logger.LogDebug(
                                "Draw for date {DrawDate} with type {LottoType} already exists, skipping",
                                drawDate, item.GameType);
                            continue;
                        }

                        // Validate numbers
                        if (item.ResultsJson == null || item.ResultsJson.Length != 6)
                        {
                            _logger.LogDebug("Invalid number count for draw {DrawDate}: expected 6, got {Count}",
                                drawDate, item.ResultsJson?.Length ?? 0);
                            continue;
                        }

                        if (item.ResultsJson.Any(n => n < 1 || n > 49))
                        {
                            _logger.LogDebug("Invalid numbers for draw {DrawDate}: numbers must be between 1-49", drawDate);
                            continue;
                        }

                        if (item.ResultsJson.Distinct().Count() != 6)
                        {
                            _logger.LogDebug("Duplicate numbers found in draw {DrawDate}", drawDate);
                            continue;
                        }

                        // Transaction: INSERT Draw + DrawNumbers
                        using var transaction = await dbContext.Database.BeginTransactionAsync(stoppingToken);

                        try
                        {
                            // Add Draw (CreatedByUserId = null for worker-generated draws)
                            var draw = new Draw
                            {
                                DrawSystemId = item.DrawSystemId,
                                DrawDate = drawDate,
                                LottoType = item.GameType ?? "UNKNOWN",
                                CreatedAt = DateTime.UtcNow,
                                CreatedByUserId = null, // Worker doesn't have userId
                                TicketPrice = item.GameType == "Lotto" ? _options.TicketPriceLotto : _options.TicketPriceLottoPlus,
                            };
                            dbContext.Draws.Add(draw);
                            await dbContext.SaveChangesAsync(stoppingToken);

                            // Add DrawNumbers
                            var drawNumbers = item.ResultsJson
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
                                draw.Id, drawDate, item.GameType);
                        }
                        catch (Exception ex)
                        {
                            await transaction.RollbackAsync(stoppingToken);
                            _logger.LogError(ex, "Failed to save draw: Date={DrawDate}, Type={GameType}",
                                drawDate, item.GameType);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to process draw: Date={DrawDate}, Type={GameType}",
                            item?.DrawDate, item?.GameType);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process draw results");
        }
    }

    private async Task SaveInDatabase(List<GetDrawsStatsByIdResponse> response, AppDbContext dbContext, CancellationToken stoppingToken)
    {
        if (response == null)
        {
            _logger.LogDebug("No stats of draw items to process in the response");
            return;
        }

        try
        {
            foreach (var pos in response ?? [])
            {
                if (pos == null) continue;

                var draw = await dbContext.Draws.FirstOrDefaultAsync(d => d.DrawSystemId == pos.DrawSystemId && d.LottoType == pos.GameType, stoppingToken);
                if (draw == null)
                    draw = await dbContext.Draws.FirstOrDefaultAsync(d => d.DrawDate == DateOnly.FromDateTime(pos.DrawDate) && d.LottoType == pos.GameType && d.WinPoolAmount1 == null, stoppingToken);
                if (draw == null) continue;

                draw.WinPoolCount1 = pos.Prizes?.GetValueOrDefault("1", new GetDrawsStatsByIdResponsePrizeInfo()).Prize ?? 0;
                draw.WinPoolAmount1 = pos.Prizes?.GetValueOrDefault("1", new GetDrawsStatsByIdResponsePrizeInfo()).PrizeValue ?? 0.0m;
                draw.WinPoolCount2 = pos.Prizes?.GetValueOrDefault("2", new GetDrawsStatsByIdResponsePrizeInfo()).Prize ?? 0;
                draw.WinPoolAmount2 = pos.Prizes?.GetValueOrDefault("2", new GetDrawsStatsByIdResponsePrizeInfo()).PrizeValue ?? 0.0m;
                draw.WinPoolCount3 = pos.Prizes?.GetValueOrDefault("3", new GetDrawsStatsByIdResponsePrizeInfo()).Prize ?? 0;
                draw.WinPoolAmount3 = pos.Prizes?.GetValueOrDefault("3", new GetDrawsStatsByIdResponsePrizeInfo()).PrizeValue ?? 0.0m;
                draw.WinPoolCount4 = pos.Prizes?.GetValueOrDefault("4", new GetDrawsStatsByIdResponsePrizeInfo()).Prize ?? 0;
                draw.WinPoolAmount4 = pos.Prizes?.GetValueOrDefault("4", new GetDrawsStatsByIdResponsePrizeInfo()).PrizeValue ?? 0.0m;

                dbContext.Draws.Update(draw);
                await dbContext.SaveChangesAsync(stoppingToken);

                _logger.LogDebug("Database update Draw: {Draw}", draw);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process stats of draw results");
        }
    }

    private static string GetDayAbbreviation(DayOfWeek dayOfWeek)
    {
        return dayOfWeek switch
        {
            DayOfWeek.Monday => "PN",
            DayOfWeek.Tuesday => "WT",
            DayOfWeek.Wednesday => "SR",
            DayOfWeek.Thursday => "CZ",
            DayOfWeek.Friday => "PT",
            DayOfWeek.Saturday => "SO",
            DayOfWeek.Sunday => "ND",
            _ => throw new ArgumentOutOfRangeException(nameof(dayOfWeek), dayOfWeek, null)
        };
    }
}