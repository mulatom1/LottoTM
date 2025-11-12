using FluentValidation;

namespace LottoTM.Server.Api.Features.Tickets.TicketsCreate;

/// <summary>
/// Validator for CreateTicketRequest using FluentValidation
/// Validates that:
/// - GroupName (if provided) is max 100 characters
/// - Numbers field is not null
/// - Exactly 6 numbers are provided
/// - All numbers are in range 1-49
/// - All numbers are unique within the set
/// </summary>
public class CreateTicketValidator : AbstractValidator<Contracts.Request>
{
    public CreateTicketValidator()
    {
        RuleFor(x => x.GroupName)
            .MaximumLength(100)
            .WithMessage("Nazwa grupy nie może przekraczać 100 znaków");

        RuleFor(x => x.Numbers)
            .NotNull()
            .WithMessage("Pole 'numbers' jest wymagane");

        RuleFor(x => x.Numbers)
            .Must(numbers => numbers != null && numbers.Length == 6)
            .WithMessage("Zestaw musi zawierać dokładnie 6 liczb");

        RuleFor(x => x.Numbers)
            .Must(numbers => numbers != null && numbers.All(n => n >= 1 && n <= 49))
            .WithMessage("Wszystkie liczby muszą być w zakresie 1-49");

        RuleFor(x => x.Numbers)
            .Must(numbers => numbers != null && numbers.Distinct().Count() == numbers.Length)
            .WithMessage("Liczby w zestawie muszą być unikalne");
    }
}
