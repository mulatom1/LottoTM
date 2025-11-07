using FluentValidation;
using LottoTM.Server.Api.Entities;
using LottoTM.Server.Api.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LottoTM.Server.Api.Features.Tickets.TicketsCreate;

/// <summary>
/// Handler for CreateTicketRequest - implements business logic for ticket creation
/// Validates ticket limit (100 per user), checks for duplicate sets, and saves to database
/// </summary>
public class CreateTicketHandler : IRequestHandler<Contracts.CreateTicketRequest, Contracts.CreateTicketResponse>
{
    private readonly AppDbContext _dbContext;
    private readonly IValidator<Contracts.CreateTicketRequest> _validator;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<CreateTicketHandler> _logger;

    public CreateTicketHandler(
        AppDbContext dbContext,
        IValidator<Contracts.CreateTicketRequest> validator,
        IHttpContextAccessor httpContextAccessor,
        ILogger<CreateTicketHandler> logger)
    {
        _dbContext = dbContext;
        _validator = validator;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<Contracts.CreateTicketResponse> Handle(
        Contracts.CreateTicketRequest request,
        CancellationToken cancellationToken)
    {
        // 1. Validate request using FluentValidation
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // 2. Extract UserId from JWT token
        var userId = GetUserIdFromJwt();

        // 3. Check if user has reached the limit of 100 tickets
        await ValidateTicketLimitAsync(userId, cancellationToken);

        // 4. Check if ticket set already exists (regardless of number order)
        await ValidateTicketUniquenessAsync(userId, request.Numbers, cancellationToken);

        // 5. Create ticket in database transaction
        var ticketId = await CreateTicketAsync(userId, request.Numbers, cancellationToken);

        _logger.LogInformation("Ticket {TicketId} created successfully for user {UserId}", ticketId, userId);

        return new Contracts.CreateTicketResponse(ticketId, "Zestaw utworzony pomyślnie");
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
            throw new UnauthorizedAccessException("Nie można zidentyfikować użytkownika");
        }
        return userId;
    }

    /// <summary>
    /// Validates that user has not reached the limit of 100 tickets
    /// </summary>
    /// <exception cref="ValidationException">Thrown when limit is reached</exception>
    private async Task ValidateTicketLimitAsync(int userId, CancellationToken cancellationToken)
    {
        var ticketCount = await _dbContext.Tickets
            .CountAsync(t => t.UserId == userId, cancellationToken);

        if (ticketCount >= 100)
        {
            throw new ValidationException("Osiągnięto limit 100 zestawów. Usuń istniejące zestawy, aby dodać nowe.");
        }
    }

    /// <summary>
    /// Validates that the ticket set doesn't already exist for the user
    /// Comparison is order-independent (sorted arrays are compared)
    /// </summary>
    /// <exception cref="ValidationException">Thrown when duplicate set is found</exception>
    private async Task ValidateTicketUniquenessAsync(int userId, int[] newNumbers, CancellationToken cancellationToken)
    {
        // Sort new numbers for comparison
        var newNumbersSorted = newNumbers.OrderBy(n => n).ToArray();

        // Fetch all existing tickets for the user with their numbers
        var existingTickets = await _dbContext.Tickets
            .Where(t => t.UserId == userId)
            .Include(t => t.Numbers)
            .ToListAsync(cancellationToken);

        // Check if any existing ticket has the same set of numbers
        foreach (var ticket in existingTickets)
        {
            var existingNumbersSorted = ticket.Numbers
                .OrderBy(n => n.Number)
                .Select(n => n.Number)
                .ToArray();

            if (newNumbersSorted.SequenceEqual(existingNumbersSorted))
            {
                throw new ValidationException("Taki zestaw już istnieje w Twoim koncie");
            }
        }
    }

    /// <summary>
    /// Creates a new ticket with associated numbers in a database transaction
    /// </summary>
    /// <returns>The ID of the created ticket</returns>
    private async Task<int> CreateTicketAsync(int userId, int[] numbers, CancellationToken cancellationToken)
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // 1. Create Ticket entity
            var ticket = new Ticket
            {
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Tickets.Add(ticket);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // 2. Create TicketNumber entities (6 numbers with positions 1-6)
            var ticketNumbers = numbers.Select((number, index) => new TicketNumber
            {
                TicketId = ticket.Id,
                Number = number,
                Position = (byte)(index + 1)
            }).ToList();

            _dbContext.TicketNumbers.AddRange(ticketNumbers);
            await _dbContext.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return ticket.Id;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
