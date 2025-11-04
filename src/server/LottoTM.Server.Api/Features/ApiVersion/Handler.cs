using FluentValidation;
using MediatR;

namespace LottoTM.Server.Api.Features.ApiVersion;

public class ApiVersionHandler : IRequestHandler<Contracts.Request, Contracts.Response>
{
    private readonly IConfiguration _configuration;
    private readonly IValidator<Contracts.Request> _validator;

    public ApiVersionHandler(IConfiguration configuration, IValidator<Contracts.Request> validator)
    {
        _configuration = configuration;
        _validator = validator; 
    }

    public async Task<Contracts.Response> Handle(Contracts.Request request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            // Jeśli są błędy, rzucamy wyjątek ValidationException,
            // który zawiera wszystkie błędy.
            // Można go później obsłużyć globalnie w middleware.
            throw new ValidationException(validationResult.Errors);
        }

        throw new Exception("Test exception from ApiVersionHandler");

        var apiVersion = _configuration.GetValue<string>("ApiVersion");
        return await Task.FromResult(new Contracts.Response(apiVersion ?? "Version not found"));
    }
}
