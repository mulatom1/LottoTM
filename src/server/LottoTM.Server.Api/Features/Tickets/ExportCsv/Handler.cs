using LottoTM.Server.Api.Repositories;
using LottoTM.Server.Api.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace LottoTM.Server.Api.Features.Tickets.ExportCsv;

/// <summary>
/// Handler for ExportCsvRequest - implements business logic for exporting tickets to CSV
/// Fetches all user tickets and generates CSV file with format: Number1,Number2,Number3,Number4,Number5,Number6,GroupName
/// </summary>
public class ExportCsvHandler : IRequestHandler<Contracts.Request, Contracts.Response>
{
    private readonly ILogger<ExportCsvHandler> _logger;
    private readonly AppDbContext _dbContext;
    private readonly IJwtService _jwtService;

    private const string CsvHeader = "Number1,Number2,Number3,Number4,Number5,Number6,GroupName";

    public ExportCsvHandler(
        ILogger<ExportCsvHandler> logger,
        AppDbContext dbContext,
        IJwtService jwtService)
    {
        _logger = logger;
        _dbContext = dbContext;
        _jwtService = jwtService;
    }

    public async Task<Contracts.Response> Handle(
        Contracts.Request request,
        CancellationToken cancellationToken)
    {
        // 1. Extract UserId from JWT token
        var userId = await _jwtService.GetUserIdFromJwt();

        // 2. Fetch all tickets for the user with their numbers
        var tickets = await _dbContext.Tickets
            .Where(t => t.UserId == userId)
            .Include(t => t.Numbers)
            .OrderBy(t => t.Id)
            .ToListAsync(cancellationToken);

        // 3. Generate CSV content
        var csvBuilder = new StringBuilder();
        csvBuilder.AppendLine(CsvHeader);

        foreach (var ticket in tickets)
        {
            // Get numbers in position order
            var numbers = ticket.Numbers
                .OrderBy(n => n.Position)
                .Select(n => n.Number)
                .ToArray();

            // Build CSV row: Number1,Number2,Number3,Number4,Number5,Number6,GroupName
            var row = string.Join(",",
                numbers[0],
                numbers[1],
                numbers[2],
                numbers[3],
                numbers[4],
                numbers[5],
                ticket.GroupName ?? string.Empty
            );

            csvBuilder.AppendLine(row);
        }

        var csvContent = csvBuilder.ToString();
        var fileName = $"tickets_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";

        _logger.LogDebug("CSV export completed for user {UserId}. Exported {Count} tickets",
            userId, tickets.Count);

        return new Contracts.Response(csvContent, fileName);
    }
}
