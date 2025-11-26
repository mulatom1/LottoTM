using FluentValidation;

namespace LottoTM.Server.Api.Features.Tickets.TicketsGetList;

/// <summary>
/// Validator for GetListRequest
/// Validates optional GroupName parameter
/// </summary>
public class GetListValidator : AbstractValidator<Contracts.Request>
{
    public GetListValidator()
    {
        // Validate GroupName if provided
        RuleFor(x => x.GroupName)
            .MaximumLength(100)
            .When(x => !string.IsNullOrEmpty(x.GroupName))
            .WithMessage("Nazwa grupy nie może być dłuższa niż 100 znaków");
    }
}
