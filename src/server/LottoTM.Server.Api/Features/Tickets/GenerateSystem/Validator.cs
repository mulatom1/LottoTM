using FluentValidation;
using LottoTM.Server.Api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LottoTM.Server.Api.Features.Tickets.GenerateSystem;

/// <summary>
/// Validates that user has sufficient space for 9 new system tickets.
/// Maximum allowed tickets per user: 100
/// System generation requires: 9 slots available (≤91 existing tickets)
/// </summary>
public class Validator : AbstractValidator<Contracts.Request>
{
    private readonly AppDbContext _context;

    public Validator(AppDbContext context)
    {
        _context = context;

        RuleFor(x => x.UserId)
            .GreaterThan(0)
            .WithMessage("UserId musi być większy od 0");

        RuleFor(x => x)
            .CustomAsync(async (request, context, cancellationToken) =>
            {
                // Check current ticket count
                var currentCount = await _context.Tickets
                    .Where(t => t.UserId == request.UserId)
                    .CountAsync(cancellationToken);

                // Validate: must have ≤91 tickets (space for 9 more)
                const int maxTickets = 100;
                const int systemTicketsCount = 9;
                const int maxAllowedBeforeGeneration = maxTickets - systemTicketsCount; // 91

                if (currentCount > maxAllowedBeforeGeneration)
                {
                    var availableSlots = maxTickets - currentCount;
                    var ticketsToDelete = systemTicketsCount - availableSlots;

                    context.AddFailure("limit",
                        $"Brak miejsca na {systemTicketsCount} zestawów. " +
                        $"Masz {currentCount}/{maxTickets} zestawów. " +
                        $"Usuń co najmniej {ticketsToDelete} zestawów.");
                }
            });
    }
}
