using System.Security.Claims;
using FluentValidation;
using LottoTM.Server.Api.Entities;
using LottoTM.Server.Api.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LottoTM.Server.Api.Features.Draws.DrawsCreate;

/// <summary>
/// Handler for processing CreateDrawRequest
/// Creates a new lottery draw with validation and transaction support
/// </summary>
public class CreateDrawHandler : IRequestHandler<Contracts.CreateDrawRequest, Contracts.CreateDrawResponse>
{
    private readonly AppDbContext _dbContext;
    private readonly IValidator<Contracts.CreateDrawRequest> _validator;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<CreateDrawHandler> _logger;

    public CreateDrawHandler(
        AppDbContext dbContext,
        IValidator<Contracts.CreateDrawRequest> validator,
        IHttpContextAccessor httpContextAccessor,
        ILogger<CreateDrawHandler> logger)
    {
        _dbContext = dbContext;
        _validator = validator;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    /// <summary>
    /// Handles the create draw request by:
    /// 1. Validating input data with FluentValidation
    /// 2. Extracting user ID from JWT claims
    /// 3. Checking if draw for the date already exists
    /// 4. Creating Draw and DrawNumbers in a transaction
    /// </summary>
    public async Task<Contracts.CreateDrawResponse> Handle(
        Contracts.CreateDrawRequest request,
        CancellationToken cancellationToken)
    {
        // 1. Walidacja FluentValidation
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // 2. Pobierz UserId z JWT claims
        var currentUserId = int.Parse(
            _httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier)!
        );

        _logger.LogInformation(
            "Creating draw for date {DrawDate} with type {LottoType} by user {UserId}",
            request.DrawDate, request.LottoType, currentUserId
        );

        // 3. Sprawdź czy losowanie na daną datę i typ gry już istnieje (unique combination)
        var existingDraw = await _dbContext.Draws
            .AnyAsync(d => d.DrawDate == request.DrawDate && d.LottoType == request.LottoType, cancellationToken);

        if (existingDraw)
        {
            _logger.LogWarning(
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

            _logger.LogInformation(
                "Draw {DrawId} created successfully for date {DrawDate}",
                draw.Id, draw.DrawDate
            );

            return new Contracts.CreateDrawResponse("Losowanie utworzone pomyślnie");
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
