using FluentValidation;

namespace LottoTM.Server.Api.Features.Verification.Check;

/// <summary>
/// Validator for verification check request
/// Ensures date range is valid and not exceeding limits
/// </summary>
public class CheckValidator : AbstractValidator<Contracts.Request>
{
    public CheckValidator()
    {
        RuleFor(x => x.DateFrom)
            .NotEmpty()
            .WithMessage("Data początkowa jest wymagana");

        RuleFor(x => x.DateTo)
            .NotEmpty()
            .WithMessage("Data końcowa jest wymagana");

        RuleFor(x => x.DateTo)
            .GreaterThanOrEqualTo(x => x.DateFrom)
            .WithMessage("'Date To' must be greater than or equal to 'Date From'.");

        RuleFor(x => x)
            .Must(x => (x.DateTo.ToDateTime(TimeOnly.MinValue) - x.DateFrom.ToDateTime(TimeOnly.MinValue)).TotalDays <= 1095)
            .WithMessage("Zakres dat nie może przekraczać 3 lat.")
            .WithName("DateRange");
    }
}
