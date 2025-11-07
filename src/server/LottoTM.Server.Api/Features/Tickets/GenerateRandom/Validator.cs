using FluentValidation;

namespace LottoTM.Server.Api.Features.Tickets.GenerateRandom;

public class Validator : AbstractValidator<Contracts.Request>
{
    public Validator()
    {
        // Walidacja UserId (powinna być zawsze > 0 z JWT)
        RuleFor(x => x.UserId)
            .GreaterThan(0)
            .WithMessage("UserId musi być większy od 0");
    }
}
