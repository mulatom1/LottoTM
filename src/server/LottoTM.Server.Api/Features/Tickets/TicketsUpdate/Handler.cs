using FluentValidation;
using LottoTM.Server.Api.Entities;
using LottoTM.Server.Api.Repositories;
using LottoTM.Server.Api.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LottoTM.Server.Api.Features.Tickets.TicketsUpdate;

/// <summary>
/// Handler for updating a ticket with new numbers
/// </summary>
public class Handler : IRequestHandler<Contracts.Request, IResult>
{
    private readonly ILogger<Handler> _logger;
    private readonly IValidator<Contracts.Request> _validator;
    private readonly AppDbContext _context;
    private readonly IJwtService _jwtService;

    public Handler(
        ILogger<Handler> logger,
        IValidator<Contracts.Request> validator,
        AppDbContext context,
        IJwtService jwtService
        )
    {
        _logger = logger;
        _validator = validator;
        _context = context;
        _jwtService = jwtService;
    }

    public async Task<IResult> Handle(Contracts.Request request, CancellationToken cancellationToken)
    {
        // 1. FluentValidation
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray()
                );

            _logger.LogWarning(
                "Walidacja nie powiodła się dla aktualizacji zestawu {TicketId}: {Errors}",
                request.TicketId,
                string.Join(", ", errors.Keys)
            );

            return Results.BadRequest(new Contracts.ValidationErrorResponse(errors));
        }

        // 2. Get UserId from JWT
        var currentUserId = await _jwtService.GetUserIdFromJwt();

        // 3. Check if ticket exists with eager loading of numbers
        var ticket = await _context.Tickets
            .Include(t => t.Numbers)
            .FirstOrDefaultAsync(t => t.Id == request.TicketId, cancellationToken);

        if (ticket == null)
        {
            _logger.LogWarning(
                "Zestaw {TicketId} nie został znaleziony",
                request.TicketId
            );
            return Results.NotFound(new { error = "Zestaw nie został znaleziony" });
        }

        // 4. Check ownership
        if (ticket.UserId != currentUserId)
        {
            _logger.LogWarning(
                "Użytkownik {UserId} próbował edytować cudzy zestaw {TicketId} (właściciel: {OwnerId})",
                currentUserId,
                request.TicketId,
                ticket.UserId
            );
            return Results.Json(
                new { error = "Brak dostępu do tego zasobu" },
                statusCode: StatusCodes.Status403Forbidden
            );
        }

        // 5. Check uniqueness (excluding current ticket)
        var sortedNewNumbers = request.Numbers.OrderBy(n => n).ToArray();

        var existingTickets = await _context.Tickets
            .Where(t => t.UserId == currentUserId && t.Id != request.TicketId)
            .Include(t => t.Numbers)
            .AsNoTracking() // Read-only query for better performance
            .ToListAsync(cancellationToken);

        foreach (var existingTicket in existingTickets)
        {
            var sortedExisting = existingTicket.Numbers
                .OrderBy(n => n.Number)
                .Select(n => n.Number)
                .ToArray();

            if (sortedNewNumbers.SequenceEqual(sortedExisting))
            {
                _logger.LogWarning(
                    "Użytkownik {UserId} próbował zapisać duplikat zestawu (zestaw {ExistingTicketId} już zawiera te liczby)",
                    currentUserId,
                    existingTicket.Id
                );

                return Results.BadRequest(new Contracts.ValidationErrorResponse(
                    new Dictionary<string, string[]>
                    {
                        { "duplicate", new[] { "Taki zestaw już istnieje w Twoich zapisanych zestawach" } }
                    }
                ));
            }
        }

        // 6. Update ticket in transaction
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // Update GroupName if provided
            ticket.GroupName = request.GroupName ?? string.Empty;

            // Remove old numbers
            _context.TicketNumbers.RemoveRange(ticket.Numbers);

            // Add new numbers
            for (byte i = 0; i < request.Numbers.Length; i++)
            {
                var ticketNumber = new TicketNumber
                {
                    TicketId = ticket.Id,
                    Number = request.Numbers[i],
                    Position = (byte)(i + 1)
                };
                _context.TicketNumbers.Add(ticketNumber);
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Użytkownik {UserId} zaktualizował zestaw {TicketId} na liczby [{Numbers}]",
                currentUserId,
                request.TicketId,
                string.Join(", ", request.Numbers.OrderBy(n => n))
            );

            return Results.Ok(new Contracts.Response("Zestaw zaktualizowany pomyślnie"));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);

            _logger.LogError(
                ex,
                "Błąd podczas aktualizacji zestawu {TicketId} dla użytkownika {UserId}",
                request.TicketId,
                currentUserId
            );

            throw; // ExceptionHandlingMiddleware will handle 500
        }
    }
}
