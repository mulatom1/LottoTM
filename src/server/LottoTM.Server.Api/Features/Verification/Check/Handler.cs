using FluentValidation;
using LottoTM.Server.Api.Repositories;
using LottoTM.Server.Api.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;
using static LottoTM.Server.Api.Features.Verification.Check.Contracts;

namespace LottoTM.Server.Api.Features.Verification.Check;

/// <summary>
/// Handler for verification check request - compares user tickets against lottery draws
/// </summary>
public class CheckVerificationHandler : IRequestHandler<Contracts.Request, Contracts.Response>
{
    private readonly AppDbContext _dbContext;
    private readonly IValidator<Contracts.Request> _validator;
    private readonly IJwtService _jwtService;
    private readonly ILogger<CheckVerificationHandler> _logger;

    public CheckVerificationHandler(
        AppDbContext dbContext,
        IValidator<Contracts.Request> validator,
        IJwtService jwtService,
        ILogger<CheckVerificationHandler> logger)
    {
        _dbContext = dbContext;
        _validator = validator;
        _jwtService = jwtService;
        _logger = logger;
    }

    public async Task<Contracts.Response> Handle(
        Contracts.Request request,
        CancellationToken cancellationToken)
    {
        // 1. Start performance measurement
        var stopwatch = Stopwatch.StartNew();

        // 2. Validate request
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // 3. Get UserId from JWT token
        var userId = await _jwtService.GetUserIdFromJwt();

        try
        {
            // 4. Fetch user tickets and draws sequentially (DbContext is not thread-safe)
            var ticketsQuery = _dbContext.Tickets
                .AsNoTracking()
                .Where(t => t.UserId == userId);

            // Filter by GroupName if provided (partial match, case-insensitive)
            if (!string.IsNullOrWhiteSpace(request.GroupName))
            {
                var groupNameLower = request.GroupName.ToLower();
                ticketsQuery = ticketsQuery.Where(t => t.GroupName != null &&
                    t.GroupName.ToLower().Contains(groupNameLower));
            }

            var tickets = await ticketsQuery
                .Include(t => t.Numbers)
                .ToListAsync(cancellationToken);

            var ticketResults = new List<Contracts.TicketsResult>();
            tickets.ForEach(t => {
                var ticketNumbers = t.Numbers
                     .OrderBy(n => n.Number)
                     .Select(n => n.Number)
                     .ToList();
                var tr = new Contracts.TicketsResult(t.Id, t.GroupName ?? string.Empty, ticketNumbers);
               ticketResults.Add(tr);
            });
            

            var draws = await _dbContext.Draws
                .AsNoTracking()
                .Where(d => d.DrawDate >= request.DateFrom && d.DrawDate <= request.DateTo)
                .Include(d => d.Numbers)
                .ToListAsync(cancellationToken);

            var drawResults = new List<Contracts.DrawsResult>();
            draws.ForEach(d => {
                
                var drawNumbers = d.Numbers
                     .OrderBy(n => n.Number)
                     .Select(n => n.Number)
                     .ToList();

                
                var winningTickets = new List<WinningTicketResult>();
                // TU dopisz logike dotyczaca WinningTicket jesli bedzie potrzebna
                foreach (var ticket in ticketResults)
                {
                    var ticketNumbers = ticket.TicketNumbers.ToList();                    
                    // Lista pasujących numerów
                    var matchingNumbers = drawNumbers.Intersect(ticketNumbers).ToList();
                    if (matchingNumbers.Count > 2) winningTickets.Add(new WinningTicketResult(ticket.TicketId, matchingNumbers));
                }

                var dr = new Contracts.DrawsResult(d.Id, d.DrawDate, d.DrawSystemId, d.LottoType, drawNumbers,
                                                   d.TicketPrice,
                                                   d.WinPoolCount1, d.WinPoolAmount1,
                                                   d.WinPoolCount2, d.WinPoolAmount2,
                                                   d.WinPoolCount3, d.WinPoolAmount3,
                                                   d.WinPoolCount4, d.WinPoolAmount4,
                                                   winningTickets);
                drawResults.Add(dr);
            });


            _logger.LogDebug(
                "Weryfikacja: pobrano {TicketCount} kuponów i {DrawCount} losowań dla użytkownika {UserId}",
                tickets.Count, draws.Count, userId);


            // 6. Stop measurement and build response
            stopwatch.Stop();

            _logger.LogDebug("Weryfikacja zakończona: czas wykonania: {ExecutionTime}ms", stopwatch.ElapsedMilliseconds);

            return new Contracts.Response(
                stopwatch.ElapsedMilliseconds,
                drawResults,
                ticketResults
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
