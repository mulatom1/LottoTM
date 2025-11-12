using FluentValidation;
using LottoTM.Server.Api.Repositories;
using LottoTM.Server.Api.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LottoTM.Server.Api.Features.Auth.Login;

/// <summary>
/// Handler for processing login requests
/// Validates credentials and generates JWT token
/// </summary>
public class LoginHandler : IRequestHandler<Contracts.Request, Contracts.Response>
{
    private readonly AppDbContext _dbContext;
    private readonly IJwtService _jwtService;
    private readonly IValidator<Contracts.Request> _validator;
    private readonly ILogger<LoginHandler> _logger;

    public LoginHandler(
        AppDbContext dbContext,
        IJwtService jwtService,
        IValidator<Contracts.Request> validator,
        ILogger<LoginHandler> logger)
    {
        _dbContext = dbContext;
        _jwtService = jwtService;
        _validator = validator;
        _logger = logger;
    }

    /// <summary>
    /// Handles the login request by:
    /// 1. Validating input data
    /// 2. Finding user by email
    /// 3. Verifying password with BCrypt
    /// 4. Generating JWT token
    /// 5. Utworzenie odpowiedzi
    /// </summary>
    public async Task<Contracts.Response> Handle(
        Contracts.Request request,
        CancellationToken cancellationToken)
    {
        // 1. Walidacja danych wejściowych
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // 2. Wyszukanie użytkownika po email (wykorzystanie indeksu IX_Users_Email)
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        // 3. Weryfikacja credentials (stały czas odpowiedzi - ochrona przed timing attacks)
        // Zawsze weryfikujemy hash, nawet jeśli użytkownik nie istnieje
        var dummyHash = "$2a$10$dummyhashtopreventtimingattacksxxxxxxxxxxxxxxxxxxxxxxxxxx";
        var isValidPassword = BCrypt.Net.BCrypt.Verify(
            request.Password,
            user?.PasswordHash ?? dummyHash
        );

        if (user == null || !isValidPassword)
        {
            _logger.LogWarning(
                "Nieudana próba logowania dla email: {Email}",
                request.Email
            );
            throw new UnauthorizedAccessException("Nieprawidłowy email lub hasło");
        }

        // 4. Generowanie tokenu JWT
        var token = _jwtService.GenerateToken(
            user.Id,
            user.Email,
            user.IsAdmin,
            out var expiresAt
        );

        _logger.LogInformation(
            "Użytkownik {UserId} ({Email}) zalogowany pomyślnie",
            user.Id,
            user.Email
        );

        // 5. Utworzenie odpowiedzi
        return new Contracts.Response(
            Token: token,
            UserId: user.Id,
            Email: user.Email,
            IsAdmin: user.IsAdmin,
            ExpiresAt: expiresAt
        );
    }
}
