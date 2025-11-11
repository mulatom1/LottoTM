using LottoTM.Server.Api.Entities;
using LottoTM.Server.Api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LottoTM.Server.Api.Services;

public class TicketService : ITicketService
{
    private readonly AppDbContext _context;
    private readonly ILogger<TicketService> _logger;

    public TicketService(AppDbContext context, ILogger<TicketService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<int> GetUserTicketCountAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _context.Tickets
            .Where(t => t.UserId == userId)
            .CountAsync(cancellationToken);
    }

    public int[] GenerateRandomNumbers()
    {
        // Używamy Random.Shared (C# 10+) dla thread-safety
        return Enumerable.Range(1, 49)
            .OrderBy(x => Random.Shared.Next())
            .Take(6)
            .OrderBy(x => x) // Sortowanie dla czytelności
            .ToArray();
    }

    public async Task<int> CreateTicketWithNumbersAsync(
        int userId,
        int[] numbers,
        CancellationToken cancellationToken = default)
    {
        // Walidacja parametrów
        if (numbers == null || numbers.Length != 6)
        {
            throw new ArgumentException("Numbers array must contain exactly 6 elements", nameof(numbers));
        }

        if (numbers.Any(n => n < 1 || n > 49))
        {
            throw new ArgumentException("All numbers must be in range 1-49", nameof(numbers));
        }

        if (numbers.Distinct().Count() != 6)
        {
            throw new ArgumentException("All numbers must be unique", nameof(numbers));
        }

        // Rozpoczęcie transakcji (EF Core implicit transaction przez SaveChangesAsync)
        var ticket = new Ticket
        {
            UserId = userId,
            GroupName = $"Random: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
            CreatedAt = DateTime.UtcNow
        };

        _context.Tickets.Add(ticket);

        // Zapisz ticket aby otrzymać wygenerowane Id
        await _context.SaveChangesAsync(cancellationToken);

        // Bulk insert liczb
        var ticketNumbers = numbers.Select((num, index) => new TicketNumber
        {
            TicketId = ticket.Id,
            Number = num,
            Position = (byte)(index + 1)
        }).ToList();

        await _context.TicketNumbers.AddRangeAsync(ticketNumbers, cancellationToken);

        // Zapis transakcyjny (COMMIT)
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created ticket {TicketId} for user {UserId} with numbers: {Numbers}",
            ticket.Id, userId, string.Join(", ", numbers));

        return ticket.Id;
    }
}
