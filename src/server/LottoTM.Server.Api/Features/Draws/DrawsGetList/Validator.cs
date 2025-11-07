using FluentValidation;

namespace LottoTM.Server.Api.Features.Draws.DrawsGetList;

/// <summary>
/// Validator for GetDrawsRequest ensuring valid pagination and sorting parameters
/// </summary>
public class GetDrawsValidator : AbstractValidator<Contracts.GetDrawsRequest>
{
    public GetDrawsValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0)
            .WithMessage("Wartość page musi być większa od 0");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("PageSize musi być w zakresie 1-100");

        RuleFor(x => x.SortBy)
            .Must(value => value.ToLower() == "drawdate" || value.ToLower() == "createdat")
            .WithMessage("Dozwolone wartości dla sortBy: 'drawDate', 'createdAt'");

        RuleFor(x => x.SortOrder)
            .Must(value => value.ToLower() == "asc" || value.ToLower() == "desc")
            .WithMessage("Dozwolone wartości dla sortOrder: 'asc', 'desc'");
    }
}
