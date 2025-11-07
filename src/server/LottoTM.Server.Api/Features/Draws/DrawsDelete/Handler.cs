using FluentValidation;
using LottoTM.Server.Api.Exceptions;
using LottoTM.Server.Api.Repositories;
using MediatR;
using System.Security.Claims;

namespace LottoTM.Server.Api.Features.Draws.DrawsDelete;

/// <summary>
/// Handler for deleting a draw
/// Only administrators can delete draws
/// </summary>
public class Handler : IRequestHandler<Contracts.Request, Contracts.Response>
{
    private readonly AppDbContext _dbContext;
    private readonly IValidator<Contracts.Request> _validator;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<Handler> _logger;

    public Handler(
        AppDbContext dbContext,
        IValidator<Contracts.Request> validator,
        IHttpContextAccessor httpContextAccessor,
        ILogger<Handler> logger)
    {
        _dbContext = dbContext;
        _validator = validator;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<Contracts.Response> Handle(
        Contracts.Request request,
        CancellationToken cancellationToken)
    {
        // 1. Validate request
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Walidacja nieudana dla DeleteDraw: {Errors}",
                string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
            throw new ValidationException(validationResult.Errors);
        }

        // 2. Get UserId from JWT claims
        var userIdString = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userIdString))
        {
            _logger.LogWarning("Próba usunięcia losowania bez tokenu JWT");
            throw new UnauthorizedAccessException("Brak tokenu autoryzacji");
        }

        var userId = int.Parse(userIdString);

        // 3. Get user and check IsAdmin permission
        var user = await _dbContext.Users.FindAsync(new object[] { userId }, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Użytkownik {UserId} nie istnieje", userId);
            throw new UnauthorizedAccessException("Użytkownik nie istnieje");
        }

        if (!user.IsAdmin)
        {
            _logger.LogWarning(
                "Użytkownik {UserId} ({Email}) próbował usunąć losowanie bez uprawnień administratora",
                userId, user.Email);
            throw new ForbiddenException(
                "Brak uprawnień administratora. Tylko administratorzy mogą usuwać losowania.");
        }

        // 4. Find the draw to delete
        var draw = await _dbContext.Draws.FindAsync(new object[] { request.Id }, cancellationToken);

        if (draw == null)
        {
            _logger.LogWarning(
                "Użytkownik {UserId} próbował usunąć nieistniejące losowanie {DrawId}",
                userId, request.Id);
            throw new NotFoundException($"Losowanie o ID {request.Id} nie istnieje");
        }

        // 5. Delete the draw (CASCADE DELETE will remove related DrawNumbers automatically)
        _logger.LogInformation(
            "Użytkownik {UserId} ({Email}) usuwa losowanie {DrawId} z daty {DrawDate}",
            userId, user.Email, draw.Id, draw.DrawDate);

        _dbContext.Draws.Remove(draw);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Pomyślnie usunięto losowanie {DrawId}", request.Id);

        return new Contracts.Response("Losowanie usunięte pomyślnie");
    }
}
