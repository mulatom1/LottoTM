using FluentValidation;

namespace LottoTM.Server.Api.Features.Auth.Login;

/// <summary>
/// Validator for login request input data
/// </summary>
public class Validator : AbstractValidator<Contracts.Request>
{
    public Validator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email jest wymagany")
            .EmailAddress().WithMessage("Nieprawidłowy format email");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Hasło jest wymagane");
    }
}
