using MediatR;
using Microsoft.Extensions.Configuration;

namespace LottoTM.Server.Api.Features.Config;

/// <summary>
/// Handler for retrieving application configuration
/// </summary>
public class ConfigHandler : IRequestHandler<Contracts.Request, Contracts.Response>
{
    private readonly IConfiguration _configuration;

    public ConfigHandler(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task<Contracts.Response> Handle(Contracts.Request request, CancellationToken cancellationToken)
    {
        // Read verification max days from configuration (default: 31 days)
        var verificationMaxDays = _configuration.GetValue<int>("Features:Verification:Days", 31);

        var response = new Contracts.Response(verificationMaxDays);

        return Task.FromResult(response);
    }
}
