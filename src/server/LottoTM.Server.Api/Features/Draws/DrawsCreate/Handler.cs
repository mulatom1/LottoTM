using FluentValidation;
using LottoTM.Server.Api.Entities;
using LottoTM.Server.Api.Repositories;
using LottoTM.Server.Api.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LottoTM.Server.Api.Features.Draws.DrawsCreate;

/// <summary>
/// Handler for processing CreateDrawRequest
/// Creates a new lottery draw with validation and transaction support
/// </summary>
public class CreateDrawHandler : IRequestHandler<Contracts.Request, Contracts.Response>
{
    private readonly ILogger<CreateDrawHandler> _logger;
    private readonly IValidator<Contracts.Request> _validator;
    private readonly AppDbContext _dbContext;
    private readonly IJwtService _jwtService;


    public CreateDrawHandler(
        ILogger<CreateDrawHandler> logger,
        IValidator<Contracts.Request> validator,
        AppDbContext dbContext,
        IJwtService jwtService
    )
    {
        _logger = logger;
        _validator = validator;
        _dbContext = dbContext;
        _jwtService = jwtService;
    }

    /// <summary>
    /// Handles the create draw request by:
    /// 1. Validating input data with FluentValidation
    /// 2. Extracting user ID from JWT claims
    /// 3. Checking if draw for the date already exists
    /// 4. Creating Draw and DrawNumbers in a transaction
    /// </summary>
    public async Task<Contracts.Response> Handle(
        Contracts.Request request,
        CancellationToken cancellationToken)
    {
        // 1. Walidacja FluentValidation
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // 2. Pobierz UserId z JWT claims
        var currentUserId = await _jwtService.GetUserIdFromJwt();
        var isAdmin = await _jwtService.GetIsAdminFromJwt();
        if (!isAdmin)
        {
            _logger.LogDebug(
                "User {UserId} attempted to create a draw without admin privileges",
                currentUserId
            );
            throw new UnauthorizedAccessException("Brak uprawnień do tworzenia losowań");
        }

        // 3. Sprawdź czy losowanie na daną datę i typ gry już istnieje (unique combination)
        var existingDraw = await _dbContext.Draws
            .AnyAsync(d => d.DrawDate == request.DrawDate && d.LottoType == request.LottoType, cancellationToken);
        if (existingDraw)
        {
            _logger.LogDebug(
                "Draw for date {DrawDate} with type {LottoType} already exists",
                request.DrawDate, request.LottoType
            );

            var errors = new List<FluentValidation.Results.ValidationFailure>
            {
                new("DrawDate", $"Losowanie typu {request.LottoType} na datę {request.DrawDate} już istnieje")
            };
            throw new ValidationException(errors);
        }

        // 4. Transakcja: INSERT Draws + DrawNumbers
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // 4a. Dodaj Draw
            var draw = new Draw
            {
                DrawDate = request.DrawDate,
                LottoType = request.LottoType,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = currentUserId
            };
            _dbContext.Draws.Add(draw);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // 4b. Bulk insert DrawNumbers (6 rekordów)
            var drawNumbers = request.Numbers
                .Select((number, index) => new DrawNumber
                {
                    DrawId = draw.Id,
                    Number = number,
                    Position = (byte)(index + 1)
                })
                .ToList();

            _dbContext.DrawNumbers.AddRange(drawNumbers);
            await _dbContext.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            _logger.LogDebug(
                "Draw {DrawId} created successfully for date {DrawDate}",
                draw.Id, draw.DrawDate
            );

            return new Contracts.Response("Losowanie utworzone pomyślnie");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);

            _logger.LogError(ex,
                "Error creating draw for date {DrawDate}",
                request.DrawDate
            );

            throw;
        }
    }
}
