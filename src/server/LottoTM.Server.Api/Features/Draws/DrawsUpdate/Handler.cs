using FluentValidation;
using LottoTM.Server.Api.Entities;
using LottoTM.Server.Api.Exceptions;
using LottoTM.Server.Api.Repositories;
using LottoTM.Server.Api.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LottoTM.Server.Api.Features.Draws.DrawsUpdate;

/// <summary>
/// Handler for processing UpdateDrawRequest
/// Updates an existing lottery draw with validation and transaction support
/// </summary>
public class UpdateDrawHandler : IRequestHandler<Contracts.Request, IResult>
{
    private readonly ILogger<UpdateDrawHandler> _logger;
    private readonly IValidator<Contracts.Request> _validator;
    private readonly AppDbContext _dbContext;
    private readonly IJwtService _jwtService;

    public UpdateDrawHandler(
        ILogger<UpdateDrawHandler> logger,
        IValidator<Contracts.Request> validator,
        AppDbContext dbContext,
        IJwtService jwtService)
    {
        _logger = logger;
        _validator = validator;
        _dbContext = dbContext;
        _jwtService = jwtService;
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

        // 2. Get UserId from JWT claims
        var userId = await _jwtService.GetUserIdFromJwt();
        var isAdmin = await _jwtService.GetIsAdminFromJwt();

        // 3. Sprawdzenie uprawnień administratora
        if (!isAdmin)
        {
            _logger.LogWarning(
                "Użytkownik {UserId} próbował aktualizować losowanie bez uprawnień administratora",
                userId
            );
            throw new ForbiddenException("Brak uprawnień do aktualizacji losowań");
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

        // 5. Sprawdzenie unikalności kombinacji (DrawDate, LottoType) - jeśli zmieniona
        if (draw.DrawDate != request.DrawDate || draw.LottoType != request.LottoType)
        {
            var dateExists = await _dbContext.Draws
                .AnyAsync(d => d.DrawDate == request.DrawDate && d.LottoType == request.LottoType && d.Id != request.Id, cancellationToken);

            if (dateExists)
            {
                _logger.LogWarning("Draw with date {DrawDate} and type {LottoType} already exists for another draw", 
                    request.DrawDate, request.LottoType);

                return Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Bad Request",
                    detail: $"Losowanie typu {request.LottoType} na datę {request.DrawDate} już istnieje w systemie"
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

            // Update LottoType
            draw.LottoType = request.LottoType;

            // Update userID
            draw.CreatedByUserId = userId;

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
