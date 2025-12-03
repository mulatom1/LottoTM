using FluentValidation;

namespace LottoTM.Server.Api.Features.Draws.DrawsImportCsv;

/// <summary>
/// Validator for DrawsImportCsv request - validates CSV file properties
/// </summary>
public class ImportCsvValidator : AbstractValidator<Contracts.Request>
{
    private const long MaxFileSizeBytes = 1 * 1024 * 1024; // 1 MB
    private static readonly string[] AllowedContentTypes = { "text/csv", "application/vnd.ms-excel", "text/plain" };

    public ImportCsvValidator()
    {
        RuleFor(x => x.File)
            .NotNull()
            .WithMessage("Plik CSV jest wymagany");

        RuleFor(x => x.File.Length)
            .LessThanOrEqualTo(MaxFileSizeBytes)
            .WithMessage($"Rozmiar pliku nie może przekraczać {MaxFileSizeBytes / 1024 / 1024} MB");

        RuleFor(x => x.File.ContentType)
            .Must(ct => AllowedContentTypes.Contains(ct))
            .WithMessage($"Nieprawidłowy typ pliku. Dozwolone: {string.Join(", ", AllowedContentTypes)}");
    }
}
