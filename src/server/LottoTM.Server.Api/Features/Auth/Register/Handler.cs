using FluentValidation;
using FluentValidation.Results;
using LottoTM.Server.Api.Entities;
using LottoTM.Server.Api.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LottoTM.Server.Api.Features.Auth.Register;

/// <summary>
/// Handler for user registration requests
/// Creates new user account with BCrypt password hashing
/// </summary>
public class RegisterHandler : IRequestHandler<Contracts.Request, Contracts.Response>
{
    private readonly AppDbContext _dbContext;
    private readonly IValidator<Contracts.Request> _validator;
    private readonly ILogger<RegisterHandler> _logger;

    public RegisterHandler(
        AppDbContext dbContext,
        IValidator<Contracts.Request> validator,
        ILogger<RegisterHandler> logger)
    {
        _dbContext = dbContext;
        _validator = validator;
        _logger = logger;
    }

    /// <summary>
    /// Handles user registration by:
    /// 1. Validating the request
    /// 2. Hashing the password with BCrypt
    /// 3. Creating a new User entity
    /// 4. Saving to database
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

        // 2. Check for existing user with same email
        var existingUser = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);
        if (existingUser != null)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure("Email", "Użytkownik z podanym adresem email już istnieje.")
            });
        }

        // 3. Hash the password using BCrypt (10 rounds)
        // BCrypt automatically generates a random salt
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 10);

        // 4. Create new user entity
        var user = new User
        {
            Email = request.Email,
            PasswordHash = passwordHash,
            IsAdmin = false, // New users are not admins by default
            CreatedAt = DateTime.UtcNow
        };

        // 5. Add user to database
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // 6. Log successful registration
        _logger.LogInformation(
            "User registered successfully: {Email}",
            request.Email);

        // 7. Return success response
        return new Contracts.Response("Rejestracja zakończona sukcesem");
    }
}
