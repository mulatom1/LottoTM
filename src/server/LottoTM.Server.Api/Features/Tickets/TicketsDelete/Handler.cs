using FluentValidation;
using LottoTM.Server.Api.Exceptions;
using LottoTM.Server.Api.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LottoTM.Server.Api.Features.Tickets.TicketsDelete;

public class DeleteTicketHandler : IRequestHandler<Contracts.Request, Contracts.Response>
{
    private readonly AppDbContext _dbContext;
    private readonly IValidator<Contracts.Request> _validator;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<DeleteTicketHandler> _logger;

    public DeleteTicketHandler(
        AppDbContext dbContext,
        IValidator<Contracts.Request> validator,
        IHttpContextAccessor httpContextAccessor,
        ILogger<DeleteTicketHandler> logger)
    {
        _dbContext = dbContext;
        _validator = validator;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<Contracts.Response> Handle(Contracts.Request request, CancellationToken cancellationToken)
    {
        // 1. Walidacja żądania
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // 2. Pobranie UserId z JWT tokenu
        var httpContext = _httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HttpContext is not available");

        var userIdClaim = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User ID not found in token");

        if (!int.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid User ID format in token");
        }

        // 3. Znalezienie zestawu z weryfikacją własności
        // Jedno zapytanie SQL: WHERE Id = @id AND UserId = @userId
        var ticket = await _dbContext.Tickets
            .FirstOrDefaultAsync(
                t => t.Id == request.Id && t.UserId == userId,
                cancellationToken);

        // 4. Weryfikacja istnienia i własności zasobu
        if (ticket == null)
        {
            // Ochrona przed IDOR i enumeration attacks:
            // Nie ujawniamy czy zasób nie istnieje czy należy do innego użytkownika
            _logger.LogWarning(
                "User {UserId} attempted to delete ticket {TicketId} - access denied (ticket not found or belongs to another user)",
                userId,
                request.Id);

            throw new ForbiddenException("Brak dostępu do zasobu");
        }

        // 5. Usunięcie zestawu (CASCADE DELETE automatycznie usuwa TicketNumbers)
        _dbContext.Tickets.Remove(ticket);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "User {UserId} successfully deleted ticket {TicketId}",
            userId,
            request.Id);

        // 6. Zwrot odpowiedzi sukcesu
        return new Contracts.Response("Zestaw usunięty pomyślnie");
    }
}
