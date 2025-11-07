using FluentValidation;

namespace LottoTM.Server.Api.Features.Draws.DrawsDelete;

/// <summary>
/// Validator for DELETE draw request
/// </summary>
public class Validator : AbstractValidator<Contracts.Request>
{
    public Validator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("ID musi być większe od 0");
    }
}
