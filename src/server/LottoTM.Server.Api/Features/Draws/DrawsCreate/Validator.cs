using FluentValidation;

namespace LottoTM.Server.Api.Features.Draws.DrawsCreate;

/// <summary>
/// Validator for CreateDrawRequest
/// Validates draw date and lottery numbers according to business rules
/// </summary>
public class CreateDrawValidator : AbstractValidator<Contracts.Request>
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

        RuleFor(x => x.LottoType)
           .NotEmpty()
           .WithMessage("Typ loterii jest wymagany")
           .Must(lt => lt == "Lotto" || lt == "LottoPlus")
           .WithMessage("Dozwolone wartości: Lotto, LottoPlus");

        RuleFor(x => x.DrawSystemId)
            .NotEmpty()
            .WithMessage("DrawSystemId jest wymagany")
            .GreaterThan(0)
            .WithMessage("DrawSystemId musi być liczbą całkowitą większą od 0");

        RuleFor(x => x.TicketPrice)
            .GreaterThan(0)
            .When(x => x.TicketPrice.HasValue)
            .WithMessage("Cena biletu musi być wartością dodatnią");

        RuleFor(x => x.WinPoolCount1)
            .GreaterThanOrEqualTo(0)
            .When(x => x.WinPoolCount1.HasValue)
            .WithMessage("Ilość wygranych musi być wartością nieujemną");

        RuleFor(x => x.WinPoolAmount1)
            .GreaterThan(0)
            .When(x => x.WinPoolAmount1.HasValue)
            .WithMessage("Kwota wygranych musi być wartością dodatnią");

        RuleFor(x => x.WinPoolCount2)
            .GreaterThanOrEqualTo(0)
            .When(x => x.WinPoolCount2.HasValue)
            .WithMessage("Ilość wygranych musi być wartością nieujemną");

        RuleFor(x => x.WinPoolAmount2)
            .GreaterThan(0)
            .When(x => x.WinPoolAmount2.HasValue)
            .WithMessage("Kwota wygranych musi być wartością dodatnią");

        RuleFor(x => x.WinPoolCount3)
            .GreaterThanOrEqualTo(0)
            .When(x => x.WinPoolCount3.HasValue)
            .WithMessage("Ilość wygranych musi być wartością nieujemną");

        RuleFor(x => x.WinPoolAmount3)
            .GreaterThan(0)
            .When(x => x.WinPoolAmount3.HasValue)
            .WithMessage("Kwota wygranych musi być wartością dodatnią");

        RuleFor(x => x.WinPoolCount4)
            .GreaterThanOrEqualTo(0)
            .When(x => x.WinPoolCount4.HasValue)
            .WithMessage("Ilość wygranych musi być wartością nieujemną");

        RuleFor(x => x.WinPoolAmount4)
            .GreaterThan(0)
            .When(x => x.WinPoolAmount4.HasValue)
            .WithMessage("Kwota wygranych musi być wartością dodatnią");
    }
}
