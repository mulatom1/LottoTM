using LottoTM.Server.Api.Repositories;
using LottoTM.Server.Api.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LottoTM.Server.Api.Features.Tickets.TicketsDeleteAll;

/// <summary>
/// Handler for deleting all tickets for the authenticated user
/// </summary>
public class Handler : IRequestHandler<Contracts.Request, Contracts.Response>
{
    private readonly ILogger<Handler> _logger;
    private readonly AppDbContext _dbContext;
    private readonly IJwtService _jwtService;

    public Handler(
        ILogger<Handler> logger,
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
        // Get user ID from JWT
        var userId = await _jwtService.GetUserIdFromJwt();

        // Get all tickets for the user
        var tickets = await _dbContext.Tickets
            .Where(t => t.UserId == userId)
            .ToListAsync(cancellationToken);

        var count = tickets.Count;

        if (count == 0)
        {
            _logger.LogDebug("No tickets to delete for user {UserId}", userId);
            return new Contracts.Response("Brak zestawów do usunięcia", 0);
        }

        // Delete all tickets (cascade will delete associated TicketNumbers)
        _dbContext.Tickets.RemoveRange(tickets);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted {Count} tickets for user {UserId}", count, userId);

        return new Contracts.Response($"Usunięto {count} zestawów", count);
    }
}
