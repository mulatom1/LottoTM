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

        RuleFor(x => x.LottoType)
              .NotEmpty()
              .WithMessage("Typ loterii jest wymagany")
              .Must(lt => lt == "LOTTO" || lt == "LOTTO PLUS")
              .WithMessage("Dozwolone wartości: LOTTO, LOTTO PLUS");

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
