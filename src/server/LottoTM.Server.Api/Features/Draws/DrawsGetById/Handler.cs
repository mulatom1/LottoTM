using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using LottoTM.Server.Api.Repositories;

namespace LottoTM.Server.Api.Features.Draws.DrawsGetById;

/// <summary>
/// Handler for retrieving a draw by ID
/// </summary>
public class Handler : IRequestHandler<Contracts.Request, Contracts.Response?>
{
    private readonly ILogger<Handler> _logger;
    private readonly IValidator<Contracts.Request> _validator;
    private readonly AppDbContext _dbContext;

    public Handler(
        AppDbContext dbContext,
        IValidator<Contracts.Request> validator,
        ILogger<Handler> logger)
    {
        _logger = logger;
        _validator = validator;
        _dbContext = dbContext;
    }

    public async Task<Contracts.Response?> Handle(
        Contracts.Request request,
        CancellationToken cancellationToken)
    {
        // 1. Validate the request
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            _logger.LogDebug("Walidacja nieudana dla GetDrawById: {Errors}",
                string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
            throw new ValidationException(validationResult.Errors);
        }

        _logger.LogDebug("Pobieranie losowania o ID: {DrawId}", request.Id);

        // 2. Fetch the draw from database with eager loading of numbers
        var draw = await _dbContext.Draws
            .Include(d => d.Numbers)
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken);

        // 3. Check if draw exists
        if (draw == null)
        {
            _logger.LogDebug("Losowanie o ID {DrawId} nie zostało znalezione", request.Id);
            return null; // Endpoint will return 404 Not Found
        }

        // 4. Map entity to DTO
        var numbers = draw.Numbers
            .OrderBy(dn => dn.Position)
            .Select(dn => dn.Number)
            .ToArray();

        var response = new Contracts.Response(
            Id: draw.Id,
            DrawDate: draw.DrawDate.ToDateTime(TimeOnly.MinValue),
            LottoType: draw.LottoType,
            Numbers: numbers,
            DrawSystemId: draw.DrawSystemId,
            TicketPrice: draw.TicketPrice,
            WinPoolCount1: draw.WinPoolCount1,
            WinPoolAmount1: draw.WinPoolAmount1,
            WinPoolCount2: draw.WinPoolCount2,
            WinPoolAmount2: draw.WinPoolAmount2,
            WinPoolCount3: draw.WinPoolCount3,
            WinPoolAmount3: draw.WinPoolAmount3,
            WinPoolCount4: draw.WinPoolCount4,
            WinPoolAmount4: draw.WinPoolAmount4,
            CreatedAt: draw.CreatedAt
        );

        _logger.LogDebug("Losowanie o ID {DrawId} pobrane pomyślnie", request.Id);
        return response;
    }
}
