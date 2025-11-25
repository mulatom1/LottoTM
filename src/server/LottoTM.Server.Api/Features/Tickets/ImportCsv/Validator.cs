using FluentValidation;

namespace LottoTM.Server.Api.Features.Tickets.ImportCsv;

/// <summary>
/// Validator for ImportCsvRequest using FluentValidation
/// Validates that:
/// - File is not null
/// - File has CSV content type
/// - File size does not exceed 1MB
/// </summary>
public class ImportCsvValidator : AbstractValidator<Contracts.Request>
{
    private const int MaxFileSizeBytes = 1048576; // 1MB

    public ImportCsvValidator()
    {
        RuleFor(x => x.File)
            .NotNull()
            .WithMessage("Plik CSV jest wymagany");

        RuleFor(x => x.File)
            .Must(file => file != null && (file.ContentType == "text/csv" || file.ContentType == "application/vnd.ms-excel"))
            .WithMessage("Dozwolony tylko format CSV");

        RuleFor(x => x.File)
            .Must(file => file != null && file.Length > 0 && file.Length <= MaxFileSizeBytes)
            .WithMessage($"Rozmiar pliku musi być w zakresie 1B - {MaxFileSizeBytes / 1024 / 1024}MB");
    }
}
