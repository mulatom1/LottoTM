using FluentValidation;
using LottoTM.Server.Api.Repositories;
using LottoTM.Server.Api.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LottoTM.Server.Api.Features.Tickets.TicketsGetList;

/// <summary>
/// Handler for GetListRequest - implements business logic for retrieving all tickets for authenticated user
/// Returns list of tickets with 6 numbers each, ordered by creation date (newest first)
/// </summary>
public class GetListHandler : IRequestHandler<Contracts.Request, Contracts.Response>
{
    private readonly ILogger<GetListHandler> _logger;
    private readonly IValidator<Contracts.Request> _validator;
    private readonly AppDbContext _dbContext;
    private readonly IJwtService _jwtService;

    public GetListHandler(
        ILogger<GetListHandler> logger,
        IValidator<Contracts.Request> validator,
        AppDbContext dbContext,
        IJwtService jwtService
        )
    {
        _logger = logger;
        _validator = validator;
        _dbContext = dbContext;
        _jwtService = jwtService;
    }

    public async Task<Contracts.Response> Handle(
        Contracts.Request request,
        CancellationToken cancellationToken)
    {
        // 1. Validate request using FluentValidation
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // 2. Extract UserId from JWT token (CRITICAL for security - data isolation)
        var currentUserId = await _jwtService.GetUserIdFromJwt();

        _logger.LogDebug(
            "Pobieranie zestawów dla użytkownika {UserId}" +
            (request.GroupName != null ? " w grupie {GroupName}" : ""),
            currentUserId,
            request.GroupName
        );

        try
        {
            // 3. Build query with user isolation (CRITICAL for security)
            var query = _dbContext.Tickets
                .AsNoTracking() // Read-only optimization
                .Where(t => t.UserId == currentUserId); // CRITICAL: User data isolation

            // 4. Apply optional GroupName filter (partial match - case-sensitive)
            if (!string.IsNullOrEmpty(request.GroupName))
            {
                query = query.Where(t => t.GroupName.Contains(request.GroupName));
            }

            // 5. Execute query with eager loading and ordering
            var tickets = await query
                .Include(t => t.Numbers) // Eager loading to prevent N+1
                .OrderByDescending(t => t.CreatedAt) // Newest first
                .ToListAsync(cancellationToken);

            _logger.LogDebug(
                "Znaleziono {Count} zestawów dla użytkownika {UserId}" +
                (request.GroupName != null ? " w grupie {GroupName}" : ""),
                tickets.Count,
                currentUserId,
                request.GroupName
            );

            // 6. Map entities to DTOs
            var ticketDtos = tickets.Select(ticket => new Contracts.TicketDto(
                Id: ticket.Id,
                UserId: ticket.UserId,
                GroupName: ticket.GroupName,
                Numbers: ticket.Numbers
                    .OrderBy(n => n.Position)
                    .Select(n => n.Number)
                    .ToArray(), // Array of 6 numbers ordered by position
                CreatedAt: ticket.CreatedAt
            )).ToList();

            // 7. Calculate metadata
            var totalCount = ticketDtos.Count;
            var limit = 100; // Max tickets per user (enforced in Create endpoint)

            // 8. Return response
            return new Contracts.Response(
                Tickets: ticketDtos,
                TotalCount: totalCount,
                Limit: limit
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Błąd podczas pobierania zestawów dla użytkownika {UserId}",
                currentUserId
            );
            throw; // Re-throw, ExceptionHandlingMiddleware will handle it
        }
    }
}
