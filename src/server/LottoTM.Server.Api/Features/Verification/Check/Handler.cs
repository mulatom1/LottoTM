using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using LottoTM.Server.Api.Repositories;
using System.Diagnostics;
using System.Security.Claims;

namespace LottoTM.Server.Api.Features.Verification.Check;

/// <summary>
/// Handler for verification check request - compares user tickets against lottery draws
/// </summary>
public class CheckVerificationHandler : IRequestHandler<Contracts.Request, Contracts.Response>
{
    private readonly AppDbContext _dbContext;
    private readonly IValidator<Contracts.Request> _validator;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<CheckVerificationHandler> _logger;

    public CheckVerificationHandler(
        AppDbContext dbContext,
        IValidator<Contracts.Request> validator,
        IHttpContextAccessor httpContextAccessor,
        ILogger<CheckVerificationHandler> logger)
    {
        _dbContext = dbContext;
        _validator = validator;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<Contracts.Response> Handle(
        Contracts.Request request,
        CancellationToken cancellationToken)
    {
        // 1. Validate request
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // 2. Get UserId from JWT token
        var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException("Nie można zidentyfikować użytkownika");
        }

        // 3. Start performance measurement
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // 4. Fetch user tickets and draws in parallel
            var ticketsTask = _dbContext.Tickets
                .AsNoTracking()
                .Where(t => t.UserId == userId)
                .Include(t => t.Numbers)
                .ToListAsync(cancellationToken);

            var drawsTask = _dbContext.Draws
                .AsNoTracking()
                .Where(d => d.DrawDate >= request.DateFrom && d.DrawDate <= request.DateTo)
                .Include(d => d.Numbers)
                .ToListAsync(cancellationToken);

            await Task.WhenAll(ticketsTask, drawsTask);

            var tickets = await ticketsTask;
            var draws = await drawsTask;

            _logger.LogInformation(
                "Weryfikacja: pobrano {TicketCount} kuponów i {DrawCount} losowań dla użytkownika {UserId}",
                tickets.Count, draws.Count, userId);

            // 5. Perform verification in memory
            var results = new List<Contracts.TicketVerificationResult>();

            foreach (var ticket in tickets)
            {
                var ticketNumbers = ticket.Numbers
                    .OrderBy(n => n.Position)
                    .Select(n => n.Number)
                    .ToList();

                var drawResults = new List<Contracts.DrawVerificationResult>();

                foreach (var draw in draws)
                {
                    var drawNumbers = draw.Numbers
                        .OrderBy(n => n.Position)
                        .Select(n => n.Number)
                        .ToList();

                    // Find matching numbers using Intersect
                    var winningNumbers = ticketNumbers.Intersect(drawNumbers).OrderBy(n => n).ToList();
                    var hits = winningNumbers.Count;

                    // Only include draws with 3 or more hits
                    if (hits >= 3)
                    {
                        drawResults.Add(new Contracts.DrawVerificationResult(
                            draw.Id,
                            draw.DrawDate,
                            drawNumbers,
                            hits,
                            winningNumbers
                        ));
                    }
                }

                // Only include tickets that have at least one hit
                if (drawResults.Count > 0)
                {
                    results.Add(new Contracts.TicketVerificationResult(
                        ticket.Id,
                        ticketNumbers,
                        drawResults
                    ));
                }
            }

            // 6. Stop measurement and build response
            stopwatch.Stop();

            _logger.LogInformation(
                "Weryfikacja zakończona: znaleziono {ResultCount} kuponów z trafieniami, czas wykonania: {ExecutionTime}ms",
                results.Count, stopwatch.ElapsedMilliseconds);

            return new Contracts.Response(
                results,
                tickets.Count,
                draws.Count,
                stopwatch.ElapsedMilliseconds
            );
        }
        catch (ValidationException)
        {
            // Re-throw validation exceptions to be handled by middleware
            throw;
        }
        catch (UnauthorizedAccessException)
        {
            // Re-throw authorization exceptions
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas weryfikacji kuponów");
            throw;
        }
    }
}
