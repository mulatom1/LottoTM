using FluentValidation;
using LottoTM.Server.Api.Entities;
using LottoTM.Server.Api.Repositories;
using LottoTM.Server.Api.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;

namespace LottoTM.Server.Api.Features.Draws.DrawsImportCsv;

/// <summary>
/// Handler for DrawsImportCsvRequest - implements business logic for mass draw import from CSV
/// Parses CSV file, validates draws, checks uniqueness, performs batch insert
/// </summary>
public class ImportCsvHandler : IRequestHandler<Contracts.Request, Contracts.Response>
{
    private readonly ILogger<ImportCsvHandler> _logger;
    private readonly IValidator<Contracts.Request> _validator;
    private readonly AppDbContext _dbContext;
    private readonly IJwtService _jwtService;

    private const string ExpectedHeader = "DrawDate,LottoType,DrawSystemId,TicketPrice,WinPoolCount1,WinPoolAmount1,WinPoolCount2,WinPoolAmount2,WinPoolCount3,WinPoolAmount3,WinPoolCount4,WinPoolAmount4,Number1,Number2,Number3,Number4,Number5,Number6";

    public ImportCsvHandler(
        ILogger<ImportCsvHandler> logger,
        IValidator<Contracts.Request> validator,
        AppDbContext dbContext,
        IJwtService jwtService)
    {
        _logger = logger;
        _validator = validator;
        _dbContext = dbContext;
        _jwtService = jwtService;
    }

    public async Task<Contracts.Response> Handle(
        Contracts.Request request,
        CancellationToken cancellationToken)
    {
        // 1. Validate request using FluentValidation
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // 2. Extract UserId from JWT token (admin only)
        var userId = await _jwtService.GetUserIdFromJwt();

        // 3. Parse CSV file
        var (drawsToImport, errors) = await ParseCsvFileAsync(request.File, userId, cancellationToken);

        // 4. Batch insert validated draws
        var imported = await ImportDrawsAsync(drawsToImport, cancellationToken);

        _logger.LogDebug("CSV import completed. Imported: {Imported}, Rejected: {Rejected}",
            imported, errors.Count);

        return new Contracts.Response(imported, errors.Count, errors);
    }

    /// <summary>
    /// Parses CSV file and validates each row
    /// Returns list of valid draws to import and list of errors
    /// </summary>
    private async Task<(List<DrawToImport>, List<Contracts.ImportError>)> ParseCsvFileAsync(
        Microsoft.AspNetCore.Http.IFormFile file,
        int userId,
        CancellationToken cancellationToken)
    {
        var drawsToImport = new List<DrawToImport>();
        var errors = new List<Contracts.ImportError>();

        using var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8);
        var csvContent = await reader.ReadToEndAsync(cancellationToken);
        var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // Validate header
        if (lines.Length == 0 || lines[0].Trim() != ExpectedHeader)
        {
            throw new ValidationException($"Nieprawidłowy format nagłówka. Oczekiwano: {ExpectedHeader}");
        }

        // Parse data rows
        for (int i = 1; i < lines.Length; i++)
        {
            var rowNumber = i; // 1-based row number (excluding header)
            var line = lines[i].Trim();

            try
            {
                var columns = line.Split(',');

                if (columns.Length < 18)
                {
                    errors.Add(new Contracts.ImportError(rowNumber, "Wymagane 18 kolumn"));
                    continue;
                }

                // Parse DrawDate
                if (!DateOnly.TryParse(columns[0].Trim(), out var drawDate))
                {
                    errors.Add(new Contracts.ImportError(rowNumber, "Nieprawidłowy format daty DrawDate"));
                    continue;
                }

                // Parse LottoType
                var lottoType = columns[1].Trim();
                if (string.IsNullOrWhiteSpace(lottoType))
                {
                    errors.Add(new Contracts.ImportError(rowNumber, "LottoType jest wymagany"));
                    continue;
                }

                // Parse DrawSystemId
                if (!int.TryParse(columns[2].Trim(), out var drawSystemId))
                {
                    errors.Add(new Contracts.ImportError(rowNumber, "Nieprawidłowa wartość DrawSystemId"));
                    continue;
                }

                // Parse TicketPrice (nullable)
                decimal? ticketPrice = null;
                if (!string.IsNullOrWhiteSpace(columns[3].Trim()))
                {
                    if (decimal.TryParse(columns[3].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedPrice))
                    {
                        ticketPrice = parsedPrice;
                    }
                }

                // Parse WinPool data (all nullable)
                int? winPoolCount1 = ParseNullableInt(columns[4]);
                decimal? winPoolAmount1 = ParseNullableDecimal(columns[5]);
                int? winPoolCount2 = ParseNullableInt(columns[6]);
                decimal? winPoolAmount2 = ParseNullableDecimal(columns[7]);
                int? winPoolCount3 = ParseNullableInt(columns[8]);
                decimal? winPoolAmount3 = ParseNullableDecimal(columns[9]);
                int? winPoolCount4 = ParseNullableInt(columns[10]);
                decimal? winPoolAmount4 = ParseNullableDecimal(columns[11]);

                // Parse 6 numbers (columns 12-17)
                var numbers = new int[6];
                for (int j = 0; j < 6; j++)
                {
                    if (!int.TryParse(columns[12 + j].Trim(), out numbers[j]))
                    {
                        throw new Exception($"Nieprawidłowa wartość w kolumnie Number{j + 1}");
                    }
                }

                // Validate range (1-49)
                if (numbers.Any(n => n < 1 || n > 49))
                {
                    var invalidNumber = numbers.First(n => n < 1 || n > 49);
                    errors.Add(new Contracts.ImportError(rowNumber, $"Liczba spoza zakresu 1-49: {invalidNumber}"));
                    continue;
                }

                // Validate uniqueness within set
                if (numbers.Distinct().Count() != 6)
                {
                    errors.Add(new Contracts.ImportError(rowNumber, "Liczby muszą być unikalne w zestawie"));
                    continue;
                }

                drawsToImport.Add(new DrawToImport(
                    drawDate,
                    lottoType,
                    drawSystemId,
                    ticketPrice,
                    winPoolCount1,
                    winPoolAmount1,
                    winPoolCount2,
                    winPoolAmount2,
                    winPoolCount3,
                    winPoolAmount3,
                    winPoolCount4,
                    winPoolAmount4,
                    numbers
                ));
            }
            catch (Exception ex)
            {
                errors.Add(new Contracts.ImportError(rowNumber, ex.Message));
            }
        }

        return (drawsToImport, errors);
    }

    private static int? ParseNullableInt(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return int.TryParse(value.Trim(), out var result) ? result : null;
    }

    private static decimal? ParseNullableDecimal(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return decimal.TryParse(value.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var result) ? result : null;
    }

    /// <summary>
    /// Performs batch insert of validated draws in a single transaction
    /// </summary>
    private async Task<int> ImportDrawsAsync(
        List<DrawToImport> drawsToImport,
        CancellationToken cancellationToken)
    {
        if (drawsToImport.Count == 0)
        {
            return 0;
        }

        // Get current user from JWT
        var userId = await _jwtService.GetUserIdFromJwt();
        var currentTimestamp = DateTime.UtcNow;

        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var allDraws = new List<Draw>();

            foreach (var drawData in drawsToImport)
            {
                var draw = new Draw
                {
                    DrawSystemId = drawData.DrawSystemId,
                    DrawDate = drawData.DrawDate,
                    LottoType = drawData.LottoType,
                    CreatedAt = currentTimestamp,
                    CreatedByUserId = userId,
                    TicketPrice = drawData.TicketPrice,
                    WinPoolCount1 = drawData.WinPoolCount1,
                    WinPoolAmount1 = drawData.WinPoolAmount1,
                    WinPoolCount2 = drawData.WinPoolCount2,
                    WinPoolAmount2 = drawData.WinPoolAmount2,
                    WinPoolCount3 = drawData.WinPoolCount3,
                    WinPoolAmount3 = drawData.WinPoolAmount3,
                    WinPoolCount4 = drawData.WinPoolCount4,
                    WinPoolAmount4 = drawData.WinPoolAmount4
                };
                allDraws.Add(draw);
            }

            _dbContext.Draws.AddRange(allDraws);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // Now we have draw.Id for each draw
            var allDrawNumbers = new List<DrawNumber>();

            for (int i = 0; i < allDraws.Count; i++)
            {
                var draw = allDraws[i];
                var drawData = drawsToImport[i];

                var drawNumbers = drawData.Numbers.Select((number, index) => new DrawNumber
                {
                    DrawId = draw.Id,
                    Number = number,
                    Position = (byte)(index + 1)
                }).ToList();

                allDrawNumbers.AddRange(drawNumbers);
            }

            _dbContext.DrawNumbers.AddRange(allDrawNumbers);
            await _dbContext.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return allDraws.Count;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Internal model for draw data parsed from CSV
    /// </summary>
    private record DrawToImport(
        DateOnly DrawDate,
        string LottoType,
        int DrawSystemId,
        decimal? TicketPrice,
        int? WinPoolCount1,
        decimal? WinPoolAmount1,
        int? WinPoolCount2,
        decimal? WinPoolAmount2,
        int? WinPoolCount3,
        decimal? WinPoolAmount3,
        int? WinPoolCount4,
        decimal? WinPoolAmount4,
        int[] Numbers
    );
}
