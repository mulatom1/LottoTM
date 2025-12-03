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
    /// 5. Updating Draw and DrawNumbers in a transaction
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
            _logger.LogDebug(
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
            _logger.LogDebug("Draw {DrawId} not found for update", request.Id);

            return Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found",
                detail: "Losowanie o podanym ID nie istnieje"
            );
        }

        // 5. Aktualizacja transakcyjna
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            _logger.LogDebug("Updating draw {DrawId} by admin {UserId}", request.Id, userId);

            // Update Draw metadata
            draw.DrawDate = request.DrawDate;

            // Update LottoType
            draw.LottoType = request.LottoType;

            // Update DrawSystemId
            draw.DrawSystemId = request.DrawSystemId;

            // Update prize pool information
            draw.TicketPrice = request.TicketPrice;
            draw.WinPoolCount1 = request.WinPoolCount1;
            draw.WinPoolAmount1 = request.WinPoolAmount1;
            draw.WinPoolCount2 = request.WinPoolCount2;
            draw.WinPoolAmount2 = request.WinPoolAmount2;
            draw.WinPoolCount3 = request.WinPoolCount3;
            draw.WinPoolAmount3 = request.WinPoolAmount3;
            draw.WinPoolCount4 = request.WinPoolCount4;
            draw.WinPoolAmount4 = request.WinPoolAmount4;

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

            _logger.LogDebug("Draw {DrawId} updated successfully", request.Id);

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
