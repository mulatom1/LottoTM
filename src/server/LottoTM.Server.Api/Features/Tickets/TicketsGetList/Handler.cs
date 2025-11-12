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

        _logger.LogInformation(
            "Pobieranie zestawów dla użytkownika {UserId}",
            currentUserId
        );

        try
        {
            // 3. Query database with eager loading (prevents N+1 queries)
            // CRITICAL: Filter by UserId for data isolation - user can only see their own tickets
            var tickets = await _dbContext.Tickets
                .AsNoTracking() // Read-only optimization
                .Where(t => t.UserId == currentUserId) // CRITICAL: User data isolation
                .Include(t => t.Numbers) // Eager loading to prevent N+1
                .OrderByDescending(t => t.CreatedAt) // Newest first
                .ToListAsync(cancellationToken);

            _logger.LogInformation(
                "Znaleziono {Count} zestawów dla użytkownika {UserId}",
                tickets.Count,
                currentUserId
            );

            // 4. Map entities to DTOs
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

            // 5. Calculate metadata
            var totalCount = ticketDtos.Count;
            var limit = 100; // Max tickets per user (enforced in Create endpoint)

            // 6. Return response
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
