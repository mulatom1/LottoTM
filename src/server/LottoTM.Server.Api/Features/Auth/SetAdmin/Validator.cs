using FluentValidation;

namespace LottoTM.Server.Api.Features.Auth.SetAdmin;

/// <summary>
/// Validator for SetAdmin requests
/// Validates email format
/// TEMPORARY: This endpoint is only for MVP. Will be replaced with proper admin management in production.
/// </summary>
public class Validator : AbstractValidator<Contracts.Request>
{
    public Validator()
    {
        // Email: required
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email jest wymagany");

        // Email: format validation
        RuleFor(x => x.Email)
            .EmailAddress()
            .WithMessage("Nieprawidłowy format email");
    }
}
