using FluentValidation;

namespace LottoTM.Server.Api.Features.Draws.DrawsCreate;

/// <summary>
/// Validator for CreateDrawRequest
/// Validates draw date and lottery numbers according to business rules
/// </summary>
public class CreateDrawValidator : AbstractValidator<Contracts.CreateDrawRequest>
{
    public CreateDrawValidator()
    {
        RuleFor(x => x.DrawDate)
            .NotEmpty().WithMessage("Data losowania jest wymagana")
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.Today))
                .WithMessage("Data losowania nie może być w przyszłości");

        RuleFor(x => x.Numbers)
            .NotEmpty().WithMessage("Liczby są wymagane")
            .Must(n => n != null && n.Length == 6)
                .WithMessage("Wymagane dokładnie 6 liczb")
            .Must(n => n != null && n.All(num => num >= 1 && num <= 49))
                .WithMessage("Liczby muszą być w zakresie 1-49")
            .Must(n => n != null && n.Distinct().Count() == n.Length)
                .WithMessage("Liczby muszą być unikalne");
    }
}
