using FluentValidation;

namespace LottoTM.Server.Api.Features.Draws.DrawsGetById;

/// <summary>
/// Validator for GetDrawById request
/// </summary>
public class Validator : AbstractValidator<Contracts.Request>
{
    public Validator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("ID musi być liczbą całkowitą większą od 0");       
    }
}
