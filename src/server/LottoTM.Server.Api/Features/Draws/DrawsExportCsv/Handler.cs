using LottoTM.Server.Api.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;

namespace LottoTM.Server.Api.Features.Draws.DrawsExportCsv;

/// <summary>
/// Handler for DrawsExportCsvRequest - implements business logic for exporting draw results to CSV
/// Fetches all draws and generates CSV file with format: DrawDate,LottoType,DrawSystemId,TicketPrice,WinPoolCount1,WinPoolAmount1,WinPoolCount2,WinPoolAmount2,WinPoolCount3,WinPoolAmount3,WinPoolCount4,WinPoolAmount4,Number1,Number2,Number3,Number4,Number5,Number6
/// </summary>
public class ExportCsvHandler : IRequestHandler<Contracts.Request, Contracts.Response>
{
    private readonly ILogger<ExportCsvHandler> _logger;
    private readonly AppDbContext _dbContext;

    private const string CsvHeader = "DrawDate,LottoType,DrawSystemId,TicketPrice,WinPoolCount1,WinPoolAmount1,WinPoolCount2,WinPoolAmount2,WinPoolCount3,WinPoolAmount3,WinPoolCount4,WinPoolAmount4,Number1,Number2,Number3,Number4,Number5,Number6";

    public ExportCsvHandler(
        ILogger<ExportCsvHandler> logger,
        AppDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<Contracts.Response> Handle(
        Contracts.Request request,
        CancellationToken cancellationToken)
    {
        // Fetch all draws with their numbers
        var draws = await _dbContext.Draws
            .Include(d => d.Numbers)
            .OrderBy(d => d.DrawDate)
            .ThenBy(d => d.LottoType)
            .ToListAsync(cancellationToken);

        // Generate CSV content
        var csvBuilder = new StringBuilder();
        csvBuilder.AppendLine(CsvHeader);

        foreach (var draw in draws)
        {
            // Get numbers in position order
            var numbers = draw.Numbers
                .OrderBy(n => n.Position)
                .Select(n => n.Number)
                .ToArray();

            // Build CSV row
            var row = string.Join(",",
                draw.DrawDate.ToString("yyyy-MM-dd"),
                draw.LottoType,
                draw.DrawSystemId,
                draw.TicketPrice?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                draw.WinPoolCount1?.ToString() ?? string.Empty,
                draw.WinPoolAmount1?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                draw.WinPoolCount2?.ToString() ?? string.Empty,
                draw.WinPoolAmount2?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                draw.WinPoolCount3?.ToString() ?? string.Empty,
                draw.WinPoolAmount3?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                draw.WinPoolCount4?.ToString() ?? string.Empty,
                draw.WinPoolAmount4?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                numbers.Length > 0 ? numbers[0].ToString() : string.Empty,
                numbers.Length > 1 ? numbers[1].ToString() : string.Empty,
                numbers.Length > 2 ? numbers[2].ToString() : string.Empty,
                numbers.Length > 3 ? numbers[3].ToString() : string.Empty,
                numbers.Length > 4 ? numbers[4].ToString() : string.Empty,
                numbers.Length > 5 ? numbers[5].ToString() : string.Empty
            );

            csvBuilder.AppendLine(row);
        }

        var csvContent = csvBuilder.ToString();
        var fileName = $"draws_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";

        _logger.LogDebug("CSV export completed. Exported {Count} draws", draws.Count);

        return new Contracts.Response(csvContent, fileName);
    }
}
