using MediatR;
using Microsoft.AspNetCore.Http;

namespace LottoTM.Server.Api.Features.Tickets.ImportCsv;

/// <summary>
/// Data contracts for CSV Import feature - mass import of lottery tickets from CSV file
/// </summary>
public class Contracts
{
    /// <summary>
    /// Request to import lottery tickets from CSV file
    /// </summary>
    /// <param name="File">CSV file with tickets (max 1MB)</param>
    public record Request(IFormFile File) : IRequest<Response>;

    /// <summary>
    /// Response after CSV import with import report
    /// </summary>
    /// <param name="Imported">Number of successfully imported tickets</param>
    /// <param name="Rejected">Number of rejected tickets</param>
    /// <param name="Errors">List of errors encountered during import</param>
    public record Response(int Imported, int Rejected, List<ImportError> Errors);

    /// <summary>
    /// Details of a single import error
    /// </summary>
    /// <param name="Row">Row number in CSV file (1-based, excluding header)</param>
    /// <param name="Reason">Reason for rejection</param>
    public record ImportError(int Row, string Reason);
}
