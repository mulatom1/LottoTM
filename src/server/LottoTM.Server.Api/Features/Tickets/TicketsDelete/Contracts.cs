using MediatR;

namespace LottoTM.Server.Api.Features.Tickets.TicketsDelete;

public class Contracts
{
    /// <summary>
    /// Żądanie usunięcia zestawu liczb LOTTO
    /// </summary>
    /// <param name="Id">ID zestawu do usunięcia</param>
    public record Request(int Id) : IRequest<Response>;

    /// <summary>
    /// Odpowiedź po pomyślnym usunięciu zestawu
    /// </summary>
    /// <param name="Message">Komunikat potwierdzający operację</param>
    public record Response(string Message);
}
