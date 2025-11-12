using FluentValidation;
using FluentValidation.Results;
using LottoTM.Server.Api.Entities;
using LottoTM.Server.Api.Repositories;
using LottoTM.Server.Api.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LottoTM.Server.Api.Features.Auth.Register;

/// <summary>
/// Handler for user registration requests
/// Creates new user account with BCrypt password hashing
/// </summary>
public class RegisterHandler : IRequestHandler<Contracts.Request, Contracts.Response>
{
    private readonly ILogger<RegisterHandler> _logger;
    private readonly IValidator<Contracts.Request> _validator;
    private readonly AppDbContext _dbContext;
    private readonly IJwtService _jwtService;

    public RegisterHandler(
        ILogger<RegisterHandler> logger,
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
    /// Handles user registration by:
    /// 1. Validating the request
    /// 2. Hashing the password with BCrypt
    /// 3. Creating a new User entity
    /// 4. Saving to database
    /// 5. Logging the registration
    /// 6. Generating JWT token
    /// 7. Returning the response
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

        // 2. Hash the password using BCrypt (10 rounds)
        // BCrypt automatically generates a random salt
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 10);

        // 3. Create new user entity
        var user = new User
        {
            Email = request.Email,
            PasswordHash = passwordHash,
            IsAdmin = false, // New users are not admins by default
            CreatedAt = DateTime.UtcNow
        };

        // 4. Add user to database
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // 5. Log successful registration
        _logger.LogInformation(
            "User registered successfully: {Email}",
            request.Email);

        // 6. Generating JWT token
        var token = _jwtService.GenerateToken(
            user.Id,
            user.Email,
            user.IsAdmin,
            out var expiresAt
        );

        // 7. Return success response
        return new Contracts.Response(
            Token: token,
            UserId: user.Id,
            Email: user.Email,
            IsAdmin: user.IsAdmin,
            ExpiresAt: expiresAt
        );
    }
}
