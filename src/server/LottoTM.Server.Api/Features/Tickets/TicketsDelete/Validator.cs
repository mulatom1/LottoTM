using FluentValidation;

namespace LottoTM.Server.Api.Features.Tickets.TicketsDelete;

public class Validator : AbstractValidator<Contracts.Request>
{
    public Validator()
    {
        // Walidacja formatu int jest już zapewniona przez routing ASP.NET Core
        // Dodatkowa walidacja: Id musi być większe od 0
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("ID zestawu musi być większe od 0");
    }
}
