using FluentValidation;

namespace LottoTM.Server.Api.Features.Tickets.GenerateRandom;

public class Validator : AbstractValidator<Contracts.Request>
{
    public Validator()
    {
    }
}
