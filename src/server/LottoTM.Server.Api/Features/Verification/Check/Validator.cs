using FluentValidation;
using Microsoft.Extensions.Configuration;

namespace LottoTM.Server.Api.Features.Verification.Check;

/// <summary>
/// Validator for verification check request
/// Ensures date range is valid and not exceeding limits
/// </summary>
public class CheckValidator : AbstractValidator<Contracts.Request>
{
    private readonly int _maxVerificationDays;

    public CheckValidator(IConfiguration configuration)
    {
        // Read the maximum verification days from configuration (default: 31 days)
        _maxVerificationDays = configuration.GetValue<int>("Features:Verification:Days", 31);

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
            .Must(x => (x.DateTo.ToDateTime(TimeOnly.MinValue) - x.DateFrom.ToDateTime(TimeOnly.MinValue)).TotalDays <= _maxVerificationDays)
            .WithMessage(x => $"Zakres dat nie może przekraczać {_maxVerificationDays} dni.")
            .WithName("DateRange");
    }
}
