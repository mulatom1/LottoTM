using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LottoTM.Server.Api.Features.Draws.DrawsImportCsv;

/// <summary>
/// Endpoint for importing lottery draw results from CSV file
/// POST /api/draws/import-csv
/// </summary>
public static class Endpoint
{
    public static void AddEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/draws/import-csv", async (
            HttpContext httpContext,
            IMediator mediator,
            IConfiguration configuration,
            CancellationToken cancellationToken) =>
        {
            // Check Feature Flag
            var featureEnabled = configuration.GetValue<bool>("Features:DrawImportExport:Enable");
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
        .WithName("DrawsImportCsv")
        .WithTags("Draws")
        .Produces<Contracts.Response>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status404NotFound)
        .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest);
    }
}
