using FluentValidation;
using LottoTM.Server.Api.Entities;
using LottoTM.Server.Api.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LottoTM.Server.Api.Features.Tickets.GenerateSystem;

/// <summary>
/// Handles generation of 9 system tickets covering all lottery numbers 1-49.
/// Each ticket contains 6 unique numbers. Algorithm ensures all 49 numbers
/// appear at least once across the 9 tickets.
/// </summary>
public class Handler : IRequestHandler<Contracts.Request, Contracts.Response>
{
    private readonly ILogger<Handler> _logger;
    private readonly IValidator<Contracts.Request> _validator;
    private readonly IJwtService _jwtService;

    public Handler(
        ILogger<Handler> logger,
        IValidator<Contracts.Request> validator,
        IJwtService jwtService
    )
    {
        _logger = logger;
        _validator = validator;
        _jwtService = jwtService;
    }

    public async Task<Contracts.Response> Handle(Contracts.Request request, CancellationToken cancellationToken)
    {
        // Validate request
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // Extract UserId from JWT
        var userId = await _jwtService.GetUserIdFromJwt();

        // Generate 9 system tickets
        var generatedNumbers = GenerateSystemTickets();

        // Validate coverage (all numbers 1-49 must appear)
        ValidateCoverage(generatedNumbers);

        // Create Ticket entities
        var tickets = new List<Ticket>();
        var createdAt = DateTime.UtcNow;

        foreach (var numbers in generatedNumbers)
        {
            var ticket = new Ticket
            {
                Numbers = numbers.Select((num, index) => new TicketNumber
                {
                    Number = num,
                    Position = (byte)(index + 1) // Positions 1-6
                }).ToList()
            };

            tickets.Add(ticket);
        }

        // Build response
        var response = new Contracts.Response(
            tickets.Select(t => new Contracts.TicketDto(
                t.Numbers.OrderBy(n => n.Position).Select(n => n.Number).ToArray()
            )).ToList()
        );

        return response;
    }

    /// <summary>
    /// Generates 9 tickets covering all numbers 1-49 using system algorithm.
    /// Algorithm ensures each number 1-49 appears at least once.
    /// Total: 9 tickets × 6 numbers = 54 slots for 49 unique numbers (5 numbers will repeat)
    /// </summary>
    private List<int[]> GenerateSystemTickets()
    {
        var tickets = new List<int[]>();

        // Initialize 9 empty tickets (6 numbers each)
        for (int i = 0; i < 9; i++)
        {
            tickets.Add(new int[6]);
        }

        // Create a pool with all numbers 1-49
        var allNumbers = Enumerable.Range(1, 49).ToList();

        // Shuffle the pool
        allNumbers = allNumbers.OrderBy(x => Random.Shared.Next()).ToList();

        // Strategy: Fill position by position, distributing numbers across tickets
        int numberIndex = 0;

        // Fill first 5 positions (5 × 9 = 45 numbers)
        for (int position = 0; position < 5; position++)
        {
            for (int ticketIndex = 0; ticketIndex < 9; ticketIndex++)
            {
                tickets[ticketIndex][position] = allNumbers[numberIndex];
                numberIndex++;
            }
        }

        // After 5 positions, we've used 45 numbers, 4 remaining (indices 45-48)
        // Assign remaining 4 numbers to first 4 tickets at position 6
        for (int i = 0; i < 4; i++)
        {
            tickets[i][5] = allNumbers[45 + i];
        }

        // For tickets 5-9 at position 6, randomly pick from numbers NOT already in those tickets
        for (int i = 4; i < 9; i++)
        {
            // Get numbers already in this ticket (positions 0-4)
            var numbersInTicket = tickets[i].Take(5).ToHashSet();

            // Find candidates from all 49 numbers that are not already in this ticket
            var candidates = allNumbers.Where(n => !numbersInTicket.Contains(n)).ToList();

            // Pick a random one
            tickets[i][5] = candidates[Random.Shared.Next(candidates.Count)];
        }

        return tickets;
    }

    /// <summary>
    /// Validates that generated tickets cover all numbers 1-49.
    /// Throws exception if validation fails.
    /// </summary>
    private void ValidateCoverage(List<int[]> tickets)
    {
        var allNumbers = tickets
            .SelectMany(t => t)
            .Distinct()
            .OrderBy(n => n)
            .ToList();

        if (allNumbers.Count < 49 || allNumbers.First() != 1 || allNumbers.Last() != 49)
        {
            var missing = Enumerable.Range(1, 49).Except(allNumbers).ToList();
            _logger.LogCritical(
                "System ticket algorithm failed. Coverage: {Count}/49. Missing: {Missing}",
                allNumbers.Count,
                string.Join(", ", missing)
            );
            throw new InvalidOperationException(
                "Błąd algorytmu generowania systemowego - spróbuj ponownie"
            );
        }

        // Validate each ticket has 6 unique numbers in range 1-49
        foreach (var ticket in tickets)
        {
            if (ticket.Length != 6 ||
                ticket.Distinct().Count() != 6 ||
                ticket.Any(n => n < 1 || n > 49))
            {
                _logger.LogCritical(
                    "Invalid ticket generated: {Ticket}",
                    string.Join(", ", ticket)
                );
                throw new InvalidOperationException("Błąd walidacji wygenerowanego zestawu");
            }
        }
    }
}
