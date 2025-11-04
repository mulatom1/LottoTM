using FluentValidation;

namespace LottoTM.Server.Api.Features.ApiVersion;

public class Validator : AbstractValidator<Contracts.Request>
{
    public Validator()
    {
        // W tym przypadku nie ma pól do walidacji,
        // ale można tu dodać reguły, gdyby zapytanie miało parametry.
    }
}
