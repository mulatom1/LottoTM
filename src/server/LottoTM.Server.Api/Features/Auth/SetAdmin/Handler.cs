using FluentValidation;
using LottoTM.Server.Api.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LottoTM.Server.Api.Features.Auth.SetAdmin;

/// <summary>
/// Handler for SetAdmin requests
/// Toggles the IsAdmin flag for a user
/// TEMPORARY: This endpoint is only for MVP. Will be replaced with proper admin management in production.
/// </summary>
public class SetAdminHandler : IRequestHandler<Contracts.Request, Contracts.Response>
{
    private readonly ILogger<SetAdminHandler> _logger;
    private readonly IValidator<Contracts.Request> _validator;
    private readonly AppDbContext _dbContext;

    public SetAdminHandler(
        ILogger<SetAdminHandler> logger,
        IValidator<Contracts.Request> validator,
        AppDbContext dbContext)
    {
        _logger = logger;
        _validator = validator;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Handles SetAdmin request by:
    /// 1. Validating the request
    /// 2. Finding the user by email
    /// 3. Toggling the IsAdmin flag
    /// 4. Saving to database
    /// 5. Logging the change
    /// 6. Returning the response
    /// </summary>
    public async Task<Contracts.Response> Handle(
        Contracts.Request request,
        CancellationToken cancellationToken)
    {
        // 1. Validate the request
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // 2. Find the user by email
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (user == null)
        {
            throw new ValidationException($"Użytkownik z emailem {request.Email} nie istnieje");
        }

        // 3. Toggle the IsAdmin flag
        user.IsAdmin = !user.IsAdmin;
        var updatedAt = DateTime.UtcNow;

        // 4. Save to database
        await _dbContext.SaveChangesAsync(cancellationToken);

        // 5. Log the change
        _logger.LogDebug(
            "Admin status toggled for user {Email} (ID: {UserId}). New IsAdmin value: {IsAdmin}",
            user.Email,
            user.Id,
            user.IsAdmin);

        // 6. Return success response
        return new Contracts.Response(
            UserId: user.Id,
            Email: user.Email,
            IsAdmin: user.IsAdmin,
            UpdatedAt: updatedAt
        );
    }
}
