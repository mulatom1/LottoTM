using FluentValidation;
using LottoTM.Server.Api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LottoTM.Server.Api.Features.Auth.Register;

/// <summary>
/// Validator for user registration requests
/// Validates email format, uniqueness, and password complexity
/// </summary>
public class Validator : AbstractValidator<Contracts.Request>
{
    public Validator(AppDbContext dbContext)
    {
        // Email: required
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email jest wymagany");

        // Email: format validation
        RuleFor(x => x.Email)
            .EmailAddress()
            .WithMessage("Nieprawidłowy format email");

        // Email: uniqueness check (async validator)
        RuleFor(x => x.Email)
            .MustAsync(async (email, cancellation) =>
            {
                var exists = await dbContext.Users
                    .AnyAsync(u => u.Email == email, cancellation);
                return !exists;
            })
            .WithMessage("Email jest już zajęty");

        // Password: required
        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Hasło jest wymagane");

        // Password: minimum length
        RuleFor(x => x.Password)
            .MinimumLength(8)
            .WithMessage("Hasło musi mieć min. 8 znaków");

        // Password: must contain uppercase letter
        RuleFor(x => x.Password)
            .Matches(@"[A-Z]")
            .WithMessage("Hasło musi zawierać wielką literę");

        // Password: must contain digit
        RuleFor(x => x.Password)
            .Matches(@"[0-9]")
            .WithMessage("Hasło musi zawierać cyfrę");

        // Password: must contain special character
        RuleFor(x => x.Password)
            .Matches(@"[\W_]")
            .WithMessage("Hasło musi zawierać znak specjalny");

        // ConfirmPassword: required
        RuleFor(x => x.ConfirmPassword)
            .NotEmpty()
            .WithMessage("Potwierdzenie hasła jest wymagane");

        // ConfirmPassword: must match Password
        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password)
            .WithMessage("Hasła nie są identyczne");
    }
}
