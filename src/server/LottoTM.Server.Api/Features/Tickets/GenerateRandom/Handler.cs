using FluentValidation;
using LottoTM.Server.Api.Services;
using MediatR;

namespace LottoTM.Server.Api.Features.Tickets.GenerateRandom;

public class Handler : IRequestHandler<Contracts.Request, Contracts.Response>
{
    private readonly ITicketService _ticketService;
    private readonly IValidator<Contracts.Request> _validator;
    private readonly ILogger<Handler> _logger;

    public Handler(
        ITicketService ticketService,
        IValidator<Contracts.Request> validator,
        ILogger<Handler> logger)
    {
        _ticketService = ticketService;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Contracts.Response> Handle(
        Contracts.Request request,
        CancellationToken cancellationToken)
    {
        // 1. Walidacja żądania (FluentValidation)
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // 2. Sprawdzenie limitu 100 zestawów
        var ticketCount = await _ticketService.GetUserTicketCountAsync(
            request.UserId,
            cancellationToken);

        if (ticketCount >= 100)
        {
            _logger.LogWarning(
                "User {UserId} attempted to generate ticket but reached limit of 100 (current: {Count})",
                request.UserId, ticketCount);

            throw new ValidationException(
                new[] {
                    new FluentValidation.Results.ValidationFailure("limit",
                        "Osiągnięto limit 100 zestawów. Usuń istniejące zestawy, aby wygenerować nowe.")
                });
        }

        // 3. Generowanie losowych liczb
        var numbers = _ticketService.GenerateRandomNumbers();

        _logger.LogInformation(
            "Generated random numbers for user {UserId}: {Numbers}",
            request.UserId, string.Join(", ", numbers));

        // 4. Zapis do bazy danych (transakcja)
        try
        {
            var ticketId = await _ticketService.CreateTicketWithNumbersAsync(
                request.UserId,
                numbers,
                cancellationToken);

            _logger.LogInformation(
                "Successfully created random ticket {TicketId} for user {UserId}",
                ticketId, request.UserId);

            return new Contracts.Response("Zestaw wygenerowany pomyślnie", ticketId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to create random ticket for user {UserId}",
                request.UserId);
            throw; // ExceptionHandlingMiddleware obsłuży
        }
    }
}
