using FluentValidation;

namespace LottoTM.Server.Api.Features.Draws.DrawsUpdate;

/// <summary>
/// Validator for UpdateDraw request
/// </summary>
public class Validator : AbstractValidator<Contracts.Request>
{
    public Validator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("ID losowania musi być większe od 0");

        RuleFor(x => x.DrawDate)
            .NotEmpty()
            .WithMessage("Data losowania jest wymagana")
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.Today))
            .WithMessage("Data losowania nie może być w przyszłości");

        RuleFor(x => x.Numbers)
            .NotEmpty()
            .WithMessage("Liczby są wymagane")
            .Must(n => n.Length == 6)
            .WithMessage("Wymagane dokładnie 6 liczb")
            .Must(n => n.All(num => num >= 1 && num <= 49))
            .WithMessage("Liczby muszą być w zakresie 1-49")
            .Must(n => n.Distinct().Count() == 6)
            .WithMessage("Liczby muszą być unikalne");
    }
}
