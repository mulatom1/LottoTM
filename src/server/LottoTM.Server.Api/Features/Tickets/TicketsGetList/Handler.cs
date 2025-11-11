using FluentValidation;
using LottoTM.Server.Api.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LottoTM.Server.Api.Features.Tickets.TicketsGetList;

/// <summary>
/// Handler for GetListRequest - implements business logic for retrieving all tickets for authenticated user
/// Returns list of tickets with 6 numbers each, ordered by creation date (newest first)
/// </summary>
public class GetListHandler : IRequestHandler<Contracts.GetListRequest, Contracts.GetListResponse>
{
    private readonly AppDbContext _dbContext;
    private readonly IValidator<Contracts.GetListRequest> _validator;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<GetListHandler> _logger;

    public GetListHandler(
        AppDbContext dbContext,
        IValidator<Contracts.GetListRequest> validator,
        IHttpContextAccessor httpContextAccessor,
        ILogger<GetListHandler> logger)
    {
        _dbContext = dbContext;
        _validator = validator;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<Contracts.GetListResponse> Handle(
        Contracts.GetListRequest request,
        CancellationToken cancellationToken)
    {
        // 1. Validate request using FluentValidation
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // 2. Extract UserId from JWT token (CRITICAL for security - data isolation)
        var currentUserId = GetUserIdFromJwt();

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
            var page = 1; // No pagination in MVP
            var pageSize = totalCount; // All tickets on one page
            var totalPages = totalCount > 0 ? 1 : 0;
            var limit = 100; // Max tickets per user (enforced in Create endpoint)

            // 6. Return response
            return new Contracts.GetListResponse(
                Tickets: ticketDtos,
                TotalCount: totalCount,
                Page: page,
                PageSize: pageSize,
                TotalPages: totalPages,
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

    /// <summary>
    /// Extracts the UserId from JWT token claims
    /// </summary>
    /// <exception cref="UnauthorizedAccessException">Thrown when user cannot be identified</exception>
    private int GetUserIdFromJwt()
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Brak autoryzacji");
        }
        return userId;
    }
}
