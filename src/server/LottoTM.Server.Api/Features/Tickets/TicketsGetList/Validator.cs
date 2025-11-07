using FluentValidation;

namespace LottoTM.Server.Api.Features.Tickets.TicketsGetList;

/// <summary>
/// Validator for GetListRequest
/// Since the request has no parameters, this validator is empty but kept for architecture consistency
/// </summary>
public class GetListValidator : AbstractValidator<Contracts.GetListRequest>
{
    public GetListValidator()
    {
        // Request has no parameters to validate
        // Validator is kept for consistency with Vertical Slice Architecture pattern
    }
}
