using MediatR;

namespace LottoTM.Server.Api.Features.ApiVersion;

public class Contracts
{
    public record Request : IRequest<Response>;
    public record Response(string Version);
}
