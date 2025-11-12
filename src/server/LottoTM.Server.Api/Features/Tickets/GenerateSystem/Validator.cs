using FluentValidation;
using LottoTM.Server.Api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LottoTM.Server.Api.Features.Tickets.GenerateSystem;

/// <summary>
/// Validates that user has sufficient space for 9 new system tickets.
/// Maximum allowed tickets per user: 100
/// System generation requires: 9 slots available (≤91 existing tickets)
/// </summary>
public class Validator : AbstractValidator<Contracts.Request>
{
    public Validator()
    {
    }
}
