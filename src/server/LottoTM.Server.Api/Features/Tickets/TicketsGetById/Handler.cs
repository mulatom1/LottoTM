using FluentValidation;
using LottoTM.Server.Api.Entities;
using LottoTM.Server.Api.Exceptions;
using LottoTM.Server.Api.Repositories;
using LottoTM.Server.Api.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LottoTM.Server.Api.Features.Tickets.TicketsGetById;

/// <summary>
/// Handler for GetByIdRequest - implements business logic for retrieving a ticket by ID
/// Validates ownership (security by obscurity) and returns ticket details with numbers
/// </summary>
public class GetByIdHandler : IRequestHandler<Contracts.Request, Contracts.Response>
{
    private readonly ILogger<GetByIdHandler> _logger;
    private readonly IValidator<Contracts.Request> _validator;
    private readonly AppDbContext _dbContext;
    private readonly IJwtService _jwtService;

    public GetByIdHandler(
        ILogger<GetByIdHandler> logger,
        IValidator<Contracts.Request> validator,
        AppDbContext dbContext,
        IJwtService jwtService)
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

        // 2. Extract UserId from JWT token
        var currentUserId = await _jwtService.GetUserIdFromJwt();

        _logger.LogInformation(
            "Pobieranie zestawu {TicketId} dla użytkownika {UserId}",
            request.Id, currentUserId
        );

        // 3. Query with ownership verification (eager loading for performance)
        var ticket = await _dbContext.Tickets
            .Include(t => t.Numbers)
            .FirstOrDefaultAsync(
                t => t.Id == request.Id && t.UserId == currentUserId,
                cancellationToken
            );

        // 4. Handle not found scenario with security by obscurity
        if (ticket == null)
        {
            // Check if ticket exists at all (regardless of ownership)
            var exists = await _dbContext.Tickets.AnyAsync(
                t => t.Id == request.Id,
                cancellationToken
            );

            if (exists)
            {
                // Ticket exists but belongs to another user - return 403 Forbidden
                // Security by obscurity: don't reveal that the resource exists
                _logger.LogWarning(
                    "Użytkownik {UserId} próbował uzyskać dostęp do zestawu {TicketId} należącego do innego użytkownika",
                    currentUserId, request.Id
                );
                throw new ForbiddenException("Nie masz uprawnień do tego zasobu");
            }
            else
            {
                // Ticket doesn't exist in the system at all - return 404 Not Found
                _logger.LogInformation(
                    "Zestaw {TicketId} nie istnieje w systemie",
                    request.Id
                );
                throw new NotFoundException("Zestaw o podanym ID nie istnieje");
            }
        }

        // 5. Map to DTO - order numbers by position
        var numbers = ticket.Numbers
            .OrderBy(n => n.Position)
            .Select(n => n.Number)
            .ToArray();

        return new Contracts.Response(
            ticket.Id,
            ticket.UserId,
            ticket.GroupName,
            numbers,
            ticket.CreatedAt
        );
    }
}
