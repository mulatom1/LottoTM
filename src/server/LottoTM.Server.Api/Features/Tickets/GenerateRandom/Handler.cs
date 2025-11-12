using FluentValidation;
using LottoTM.Server.Api.Services;
using MediatR;

namespace LottoTM.Server.Api.Features.Tickets.GenerateRandom;

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
        _validator = validator;
        _logger = logger;
        _jwtService = jwtService;
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

        // 2. Get UserId from JWT claims
        var userId = await _jwtService.GetUserIdFromJwt();

        // 3. Generowanie losowych liczb
        var numbers = GenerateRandomNumbers();

        _logger.LogInformation(
            "Generated random numbers for user {UserId}: {Numbers}",
            userId, string.Join(", ", numbers));

        return new Contracts.Response(numbers);
    }

    private int[] GenerateRandomNumbers()
    {
        // Bardziej wydajne - nie sortuje wszystkich 49 liczb
        var numbers = new List<int>(49);
        for (int i = 1; i <= 49; i++) numbers.Add(i);

        // Fisher-Yates shuffle 
        for (int i = 0; i < 6; i++)
        {
            int j = Random.Shared.Next(i, 49);
            (numbers[i], numbers[j]) = (numbers[j], numbers[i]);
        }

        //wystarczy tylko pierwsze 6 pozycji
        return numbers.Take(6).OrderBy(x => x).ToArray();
    }
}
