using MediatR;
using System.Text;

namespace LottoTM.Server.Api.Features.Tickets.ExportCsv;

/// <summary>
/// Endpoint for exporting lottery tickets to CSV file
/// GET /api/tickets/export-csv
/// </summary>
public static class Endpoint
{
    public static void AddEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/tickets/export-csv", async (
            IMediator mediator,
            IConfiguration configuration,
            CancellationToken cancellationToken) =>
        {
            // Check Feature Flag
            var featureEnabled = configuration.GetValue<bool>("Features:TicketImportExport:Enable");
            if (!featureEnabled)
            {
                return Results.NotFound(new { message = "Feature not available" });
            }

            var request = new Contracts.Request();
            var response = await mediator.Send(request, cancellationToken);

            // Return CSV file as downloadable blob
            var csvBytes = Encoding.UTF8.GetBytes(response.CsvContent);
            return Results.File(
                csvBytes,
                contentType: "text/csv",
                fileDownloadName: response.FileName
            );
        })
        .RequireAuthorization()
        .WithName("ExportCsv")
        .WithTags("Tickets")
        .Produces(StatusCodes.Status200OK, contentType: "text/csv")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status404NotFound);
    }
}
