using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LottoTM.Server.Api.Features.Tickets.ImportCsv;

/// <summary>
/// Endpoint for importing lottery tickets from CSV file
/// POST /api/tickets/import-csv
/// </summary>
public static class Endpoint
{
    public static void AddEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/tickets/import-csv", async (
            HttpContext httpContext,
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

            // Get file from form
            var form = await httpContext.Request.ReadFormAsync(cancellationToken);
            var file = form.Files.GetFile("file");

            if (file == null)
            {
                return Results.BadRequest(new { message = "Plik CSV jest wymagany" });
            }

            var request = new Contracts.Request(file);
            var response = await mediator.Send(request, cancellationToken);

            return Results.Ok(response);
        })
        .RequireAuthorization()
        .DisableAntiforgery() // Required for file uploads
        .Accepts<IFormFile>("multipart/form-data") // Tell Swagger about content type
        .WithName("ImportCsv")
        .WithTags("Tickets")
        .Produces<Contracts.Response>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status404NotFound)
        .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest);
    }
}
