using MediatR;

namespace LottoTM.Server.Api.Features.Draws.DrawsExportCsv;

/// <summary>
/// Data contracts for Draws CSV Export feature - exports all draw results to CSV file
/// </summary>
public class Contracts
{
    /// <summary>
    /// Request to export all draw results to CSV file
    /// </summary>
    public record Request() : IRequest<Response>;

    /// <summary>
    /// Response containing CSV file content
    /// </summary>
    /// <param name="CsvContent">CSV file content as string</param>
    /// <param name="FileName">Suggested file name for download</param>
    public record Response(string CsvContent, string FileName);
}
