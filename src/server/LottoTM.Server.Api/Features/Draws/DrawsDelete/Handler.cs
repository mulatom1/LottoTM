using FluentValidation;
using LottoTM.Server.Api.Exceptions;
using LottoTM.Server.Api.Repositories;
using LottoTM.Server.Api.Services;
using MediatR;
using System.Security.Claims;

namespace LottoTM.Server.Api.Features.Draws.DrawsDelete;

/// <summary>
/// Handler for deleting a draw
/// Only administrators can delete draws
/// </summary>
public class Handler : IRequestHandler<Contracts.Request, Contracts.Response>
{
    private readonly ILogger<Handler> _logger;
    private readonly IValidator<Contracts.Request> _validator;
    private readonly AppDbContext _dbContext;
    private readonly IJwtService _jwtService;


    public Handler(
        ILogger<Handler> logger,
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
        var userId = await _jwtService.GetUserIdFromJwt();
        var isAdmin = await _jwtService.GetIsAdminFromJwt();
        if (!isAdmin)
        {
            _logger.LogWarning(
                "Użytkownik {UserId} próbował usunąć losowanie bez uprawnień administratora",
                userId
            );
            throw new ForbiddenException("Brak uprawnień do usuwania losowań");
        }

        // 3. Find the draw to delete
        var draw = await _dbContext.Draws.FindAsync(new object[] { request.Id }, cancellationToken);
        if (draw == null)
        {
            _logger.LogWarning(
                "Użytkownik {UserId} próbował usunąć nieistniejące losowanie {DrawId}",
                userId, request.Id);
            throw new NotFoundException($"Losowanie o ID {request.Id} nie istnieje");
        }

        // 4. Delete the draw (CASCADE DELETE will remove related DrawNumbers automatically)
        _logger.LogDebug(
            "Użytkownik {UserId}usuwa losowanie {DrawId} z daty {DrawDate}",
            userId, draw.Id, draw.DrawDate);

        _dbContext.Draws.Remove(draw);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Pomyślnie usunięto losowanie {DrawId}", request.Id);

        return new Contracts.Response("Losowanie usunięte pomyślnie");
    }
}
