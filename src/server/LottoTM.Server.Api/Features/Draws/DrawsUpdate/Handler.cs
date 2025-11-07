using System.Security.Claims;
using FluentValidation;
using LottoTM.Server.Api.Entities;
using LottoTM.Server.Api.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LottoTM.Server.Api.Features.Draws.DrawsUpdate;

/// <summary>
/// Handler for processing UpdateDrawRequest
/// Updates an existing lottery draw with validation and transaction support
/// </summary>
public class UpdateDrawHandler : IRequestHandler<Contracts.Request, IResult>
{
    private readonly AppDbContext _dbContext;
    private readonly IValidator<Contracts.Request> _validator;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<UpdateDrawHandler> _logger;

    public UpdateDrawHandler(
        AppDbContext dbContext,
        IValidator<Contracts.Request> validator,
        IHttpContextAccessor httpContextAccessor,
        ILogger<UpdateDrawHandler> logger)
    {
        _dbContext = dbContext;
        _validator = validator;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    /// <summary>
    /// Handles the update draw request by:
    /// 1. Validating input data with FluentValidation
    /// 2. Extracting user ID and admin status from JWT claims
    /// 3. Checking if user is admin
    /// 4. Finding the draw to update
    /// 5. Checking if new draw date is unique (if changed)
    /// 6. Updating Draw and DrawNumbers in a transaction
    /// </summary>
    public async Task<IResult> Handle(Contracts.Request request, CancellationToken cancellationToken)
    {
        // 1. Walidacja FluentValidation
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // 2. Pobranie użytkownika z JWT claims
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User == null)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized",
                detail: "Brak tokenu JWT"
            );
        }

        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var isAdminClaim = httpContext.User.FindFirst("IsAdmin")?.Value;

        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized",
                detail: "Nieprawidłowy token JWT"
            );
        }

        var isAdmin = bool.TryParse(isAdminClaim, out var adminResult) && adminResult;

        // 3. Sprawdzenie uprawnień administratora
        if (!isAdmin)
        {
            _logger.LogWarning("User {UserId} attempted to update draw {DrawId} without admin privileges",
                userId, request.Id);

            return Results.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Forbidden",
                detail: "Brak uprawnień administratora. Tylko administratorzy mogą edytować losowania."
            );
        }

        // 4. Pobranie losowania z bazy danych (z eager loading liczb)
        var draw = await _dbContext.Draws
            .Include(d => d.Numbers.OrderBy(n => n.Position))
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken);

        if (draw == null)
        {
            _logger.LogWarning("Draw {DrawId} not found for update", request.Id);

            return Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found",
                detail: "Losowanie o podanym ID nie istnieje"
            );
        }

        // 5. Sprawdzenie unikalności daty (jeśli zmieniona)
        if (draw.DrawDate != request.DrawDate)
        {
            var dateExists = await _dbContext.Draws
                .AnyAsync(d => d.DrawDate == request.DrawDate && d.Id != request.Id, cancellationToken);

            if (dateExists)
            {
                _logger.LogWarning("Draw date {DrawDate} already exists for another draw", request.DrawDate);

                return Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Bad Request",
                    detail: "Losowanie z podaną datą już istnieje w systemie"
                );
            }
        }

        // 6. Aktualizacja transakcyjna
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            _logger.LogInformation("Updating draw {DrawId} by admin {UserId}", request.Id, userId);

            // Update Draw metadata
            draw.DrawDate = request.DrawDate;

            // Delete old numbers (EF Core tracking handles CASCADE)
            _dbContext.DrawNumbers.RemoveRange(draw.Numbers);

            // Add new numbers with positions 1-6
            for (int i = 0; i < request.Numbers.Length; i++)
            {
                _dbContext.DrawNumbers.Add(new DrawNumber
                {
                    DrawId = draw.Id,
                    Number = request.Numbers[i],
                    Position = (byte)(i + 1)
                });
            }

            // Save changes and commit transaction
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Draw {DrawId} updated successfully", request.Id);

            return Results.Ok(new Contracts.Response("Losowanie zaktualizowane pomyślnie"));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Failed to update draw {DrawId}", request.Id);
            throw;
        }
    }
}
