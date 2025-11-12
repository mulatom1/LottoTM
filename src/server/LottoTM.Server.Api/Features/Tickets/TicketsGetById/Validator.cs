using FluentValidation;

namespace LottoTM.Server.Api.Features.Tickets.TicketsGetById;

/// <summary>
/// Validator for GetByIdRequest - validates ticket ID
/// </summary>
public class GetByIdValidator : AbstractValidator<Contracts.Request>
{
    public GetByIdValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("ID musi być liczbą całkowitą dodatnią");
    }
}
