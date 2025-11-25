using FluentValidation;
using LottoTM.Server.Api.Entities;
using LottoTM.Server.Api.Repositories;
using LottoTM.Server.Api.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace LottoTM.Server.Api.Features.Tickets.ImportCsv;

/// <summary>
/// Handler for ImportCsvRequest - implements business logic for mass ticket import from CSV
/// Parses CSV file, validates tickets, checks limits and uniqueness, performs batch insert
/// </summary>
public class ImportCsvHandler : IRequestHandler<Contracts.Request, Contracts.Response>
{
    private readonly ILogger<ImportCsvHandler> _logger;
    private readonly IValidator<Contracts.Request> _validator;
    private readonly AppDbContext _dbContext;
    private readonly IJwtService _jwtService;

    private const string ExpectedHeader = "Number1,Number2,Number3,Number4,Number5,Number6,GroupName";

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

        // 2. Extract UserId from JWT token
        var userId = await _jwtService.GetUserIdFromJwt();

        // 3. Check available slots (100 - current count)
        var availableSlots = await GetAvailableSlotsAsync(userId, cancellationToken);

        // 4. Parse CSV file
        var (ticketsToImport, errors) = await ParseCsvFileAsync(request.File, userId, cancellationToken);

        // 5. Check if number of tickets exceeds available slots
        if (ticketsToImport.Count > availableSlots)
        {
            throw new ValidationException($"Osiągnięto limit 100 zestawów. Dostępne: {availableSlots} zestawów. Plik zawiera {ticketsToImport.Count + errors.Count} wierszy.");
        }

        // 6. Batch insert validated tickets
        var imported = await ImportTicketsAsync(userId, ticketsToImport, cancellationToken);

        _logger.LogDebug("CSV import completed for user {UserId}. Imported: {Imported}, Rejected: {Rejected}",
            userId, imported, errors.Count);

        return new Contracts.Response(imported, errors.Count, errors);
    }

    /// <summary>
    /// Calculates number of available slots for new tickets (max 100 per user)
    /// </summary>
    private async Task<int> GetAvailableSlotsAsync(int userId, CancellationToken cancellationToken)
    {
        var currentCount = await _dbContext.Tickets
            .CountAsync(t => t.UserId == userId, cancellationToken);

        return 100 - currentCount;
    }

    /// <summary>
    /// Parses CSV file and validates each row
    /// Returns list of valid tickets to import and list of errors
    /// </summary>
    private async Task<(List<TicketToImport>, List<Contracts.ImportError>)> ParseCsvFileAsync(
        Microsoft.AspNetCore.Http.IFormFile file,
        int userId,
        CancellationToken cancellationToken)
    {
        var ticketsToImport = new List<TicketToImport>();
        var errors = new List<Contracts.ImportError>();

        using var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8);
        var csvContent = await reader.ReadToEndAsync(cancellationToken);
        var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // Validate header
        if (lines.Length == 0 || lines[0].Trim() != ExpectedHeader)
        {
            throw new ValidationException($"Nieprawidłowy format nagłówka. Oczekiwano: {ExpectedHeader}");
        }

        // Fetch existing tickets for uniqueness validation
        var existingTickets = await _dbContext.Tickets
            .Where(t => t.UserId == userId)
            .Include(t => t.Numbers)
            .ToListAsync(cancellationToken);

        // Parse data rows
        for (int i = 1; i < lines.Length; i++)
        {
            var rowNumber = i; // 1-based row number (excluding header)
            var line = lines[i].Trim();

            try
            {
                var columns = line.Split(',');

                // Parse 6 numbers
                if (columns.Length < 6)
                {
                    errors.Add(new Contracts.ImportError(rowNumber, "Wymagane 6 liczb"));
                    continue;
                }

                var numbers = new int[6];
                for (int j = 0; j < 6; j++)
                {
                    if (!int.TryParse(columns[j].Trim(), out numbers[j]))
                    {
                        throw new Exception($"Nieprawidłowa wartość w kolumnie {j + 1}");
                    }
                }

                // Validate range (1-49)
                if (numbers.Any(n => n < 1 || n > 49))
                {
                    var invalidNumber = numbers.First(n => n < 1 || n > 49);
                    errors.Add(new Contracts.ImportError(rowNumber, $"Invalid number range: {invalidNumber}"));
                    continue;
                }

                // Validate uniqueness within set
                if (numbers.Distinct().Count() != 6)
                {
                    errors.Add(new Contracts.ImportError(rowNumber, "Numbers must be unique in set"));
                    continue;
                }

                // Parse GroupName (optional)
                var groupName = columns.Length > 6 ? columns[6].Trim() : string.Empty;

                // Check for duplicate ticket
                var numbersSorted = numbers.OrderBy(n => n).ToArray();
                var isDuplicate = existingTickets.Any(ticket =>
                {
                    var existingNumbers = ticket.Numbers.OrderBy(n => n.Number).Select(n => n.Number).ToArray();
                    return numbersSorted.SequenceEqual(existingNumbers);
                });

                // Also check against already parsed tickets in current import
                var isDuplicateInCurrentImport = ticketsToImport.Any(t => t.Numbers.OrderBy(n => n).SequenceEqual(numbersSorted));

                if (isDuplicate || isDuplicateInCurrentImport)
                {
                    errors.Add(new Contracts.ImportError(rowNumber, "Duplicate ticket"));
                    continue;
                }

                ticketsToImport.Add(new TicketToImport(numbers, groupName));
            }
            catch (Exception ex)
            {
                errors.Add(new Contracts.ImportError(rowNumber, ex.Message));
            }
        }

        return (ticketsToImport, errors);
    }

    /// <summary>
    /// Performs batch insert of validated tickets in a single transaction
    /// </summary>
    private async Task<int> ImportTicketsAsync(
        int userId,
        List<TicketToImport> ticketsToImport,
        CancellationToken cancellationToken)
    {
        if (ticketsToImport.Count == 0)
        {
            return 0;
        }

        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var allTickets = new List<Ticket>();

            foreach (var ticketData in ticketsToImport)
            {
                var ticket = new Ticket
                {
                    UserId = userId,
                    GroupName = ticketData.GroupName,
                    CreatedAt = DateTime.UtcNow
                };
                allTickets.Add(ticket);
            }

            _dbContext.Tickets.AddRange(allTickets);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // Now we have ticket.Id for each ticket
            var allTicketNumbers = new List<TicketNumber>();

            for (int i = 0; i < allTickets.Count; i++)
            {
                var ticket = allTickets[i];
                var ticketData = ticketsToImport[i];

                var ticketNumbers = ticketData.Numbers.Select((number, index) => new TicketNumber
                {
                    TicketId = ticket.Id,
                    Number = number,
                    Position = (byte)(index + 1)
                }).ToList();

                allTicketNumbers.AddRange(ticketNumbers);
            }

            _dbContext.TicketNumbers.AddRange(allTicketNumbers);
            await _dbContext.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return allTickets.Count;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Internal model for ticket data parsed from CSV
    /// </summary>
    private record TicketToImport(int[] Numbers, string GroupName);
}
