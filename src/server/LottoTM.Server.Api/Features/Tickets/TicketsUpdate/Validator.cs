using FluentValidation;

namespace LottoTM.Server.Api.Features.Tickets.TicketsUpdate;

/// <summary>
/// Validator for ticket update requests
/// </summary>
public class Validator : AbstractValidator<Contracts.Request>
{
    public Validator()
    {
        // Validate GroupName (optional, but max 100 characters if provided)
        RuleFor(x => x.GroupName)
            .MaximumLength(100)
            .WithMessage("Nazwa grupy nie może przekraczać 100 znaków");

        // Validate Numbers field is not null
        RuleFor(x => x.Numbers)
            .NotNull()
            .WithMessage("Liczby są wymagane");

        // Validate array length is exactly 6
        RuleFor(x => x.Numbers)
            .Must(numbers => numbers != null && numbers.Length == 6)
            .WithMessage("Wymagane dokładnie 6 liczb")
            .When(x => x.Numbers != null);

        // Validate all numbers are in range 1-49
        RuleFor(x => x.Numbers)
            .Must(numbers => numbers.All(n => n >= 1 && n <= 49))
            .WithMessage("Wszystkie liczby muszą być w zakresie 1-49")
            .When(x => x.Numbers != null && x.Numbers.Length == 6);

        // Validate all numbers are unique
        RuleFor(x => x.Numbers)
            .Must(numbers => numbers.Distinct().Count() == numbers.Length)
            .WithMessage("Liczby muszą być unikalne")
            .When(x => x.Numbers != null && x.Numbers.Length == 6);

        // Validate TicketId is not 0
        RuleFor(x => x.TicketId)
            .GreaterThan(0)
            .WithMessage("ID zestawu jest wymagane");
    }
}
