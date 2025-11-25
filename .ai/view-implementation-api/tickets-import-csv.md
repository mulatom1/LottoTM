# API Endpoint Implementation Plan: POST /api/tickets/import-csv

## 1. Przegląd punktu końcowego

Endpoint **POST /api/tickets/import-csv** umożliwia użytkownikom masowy import zestawów liczb LOTTO z pliku CSV. Funkcjonalność kontrolowana jest przez Feature Flag `Features:TicketImportExport:Enable`. Użytkownik przesyła plik CSV zawierający wiele zestawów (każdy wiersz = 1 zestaw 6 liczb + opcjonalna nazwa grupy), które są walidowane i zapisywane w bazie danych jako zestawy powiązane z kontem użytkownika.

System automatycznie:
- Waliduje format pliku CSV (nagłówek, struktura kolumn)
- Waliduje każdy wiersz (6 liczb z zakresu 1-49, unikalność w zestawie)
- Sprawdza limit 100 zestawów na użytkownika
- Sprawdza unikalność importowanych zestawów (pomija duplikaty)
- Generuje raport z liczbą zaimportowanych/odrzuconych zestawów + lista błędów

Endpoint zwraca raport importu z podsumowaniem sukcesu i błędów.

---

## 2. Szczegóły żądania

- **Metoda HTTP:** POST
- **Struktura URL:** `/api/tickets/import-csv`
- **Autoryzacja:** Wymagany JWT token w header `Authorization: Bearer <token>`
- **Feature Flag:** `Features:TicketImportExport:Enable` (true/false)

### Parametry:
- **Wymagane:**
  - Header: `Authorization: Bearer <token>` (JWT)
  - Header: `Content-Type: multipart/form-data`
  - Form Data: `file` (plik CSV)

- **Opcjonalne:** Brak

### Request Body (multipart/form-data):
```
Content-Type: multipart/form-data; boundary=----WebKitFormBoundary7MA4YWxkTrZu0gW

------WebKitFormBoundary7MA4YWxkTrZu0gW
Content-Disposition: form-data; name="file"; filename="lotto-tickets.csv"
Content-Type: text/csv

Number1,Number2,Number3,Number4,Number5,Number6,GroupName
5,14,23,29,37,41,Ulubione
7,15,22,33,38,45,Rodzina
1,10,20,30,40,49,
------WebKitFormBoundary7MA4YWxkTrZu0gW--
```

**Format pliku CSV:**
- **Nagłówek (wymagany):** `Number1,Number2,Number3,Number4,Number5,Number6,GroupName`
- **Wiersze danych:** 6 liczb (1-49) + opcjonalny GroupName (max 100 znaków)
- **Kodowanie:** UTF-8
- **Separator:** przecinek (,)
- **Maksymalna liczba wierszy:** ograniczona limitem dostępnych miejsc (100 - istniejące zestawy)

---

## 3. Wykorzystywane typy

### 3.1 Contracts (DTO)

**Plik:** `Features/Tickets/Contracts.cs`

```csharp
using MediatR;

namespace LottoTM.Server.Api.Features.Tickets;

public class Contracts
{
    /// <summary>
    /// Request do importu zestawów z pliku CSV
    /// </summary>
    /// <param name="File">Plik CSV z zestawami</param>
    public record ImportTicketsFromCsvRequest(IFormFile File) : IRequest<ImportTicketsFromCsvResponse>;

    /// <summary>
    /// Response po imporcie zestawów
    /// </summary>
    /// <param name="Imported">Liczba pomyślnie zaimportowanych zestawów</param>
    /// <param name="Rejected">Liczba odrzuconych zestawów</param>
    /// <param name="Errors">Lista błędów (numer wiersza + przyczyna)</param>
    public record ImportTicketsFromCsvResponse(
        int Imported,
        int Rejected,
        List<ImportError> Errors);

    /// <summary>
    /// Szczegóły błędu importu
    /// </summary>
    /// <param name="Row">Numer wiersza (1-based, bez nagłówka)</param>
    /// <param name="Reason">Przyczyna odrzucenia</param>
    public record ImportError(int Row, string Reason);
}
```

### 3.2 Encje bazodanowe (już istniejące)

- `Ticket` - metadane zestawu (Id, UserId, GroupName, CreatedAt)
- `TicketNumber` - pojedyncza liczba w zestawie (Id, TicketId, Number, Position)

### 3.3 Modele pomocnicze

```csharp
// Model wiersza CSV
private record CsvTicketRow(int RowNumber, int[] Numbers, string? GroupName);

// Model zestawu do walidacji
private record TicketToImport(int[] Numbers, string? GroupName, int SourceRowNumber);
```

---

## 4. Szczegóły odpowiedzi

### Sukces (200 OK):
```json
{
  "imported": 15,
  "rejected": 2,
  "errors": [
    { "row": 3, "reason": "Duplicate ticket" },
    { "row": 7, "reason": "Invalid number range: 52" }
  ]
}
```

### Błąd Feature Flag wyłączony (404 Not Found):
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Not Found",
  "status": 404,
  "errors": [
    "Funkcjonalność importu/eksportu CSV jest wyłączona"
  ]
}
```

### Błąd walidacji pliku (400 Bad Request):
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Błąd walidacji",
  "status": 400,
  "errors": [
    "Plik CSV jest wymagany",
    "Nieprawidłowy format nagłówka. Oczekiwano: Number1,Number2,Number3,Number4,Number5,Number6,GroupName"
  ]
}
```

### Błąd limitu (400 Bad Request):
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Błąd walidacji",
  "status": 400,
  "errors": [
    "Osiągnięto limit 100 zestawów. Dostępne: 10 zestawów. Plik zawiera 20 wierszy."
  ]
}
```

### Brak autoryzacji (401 Unauthorized):
```json
{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
  "title": "Unauthorized",
  "status": 401
}
```

---

## 5. Przepływ danych

### 5.1 Diagram przepływu

```
[Klient]
   ↓ POST /api/tickets/import-csv + JWT + multipart/form-data (file)
[Endpoint.cs]
   ↓ Sprawdzenie Feature Flag (Features:TicketImportExport:Enable)
   ↓ Jeśli false → 404 Not Found
   ↓ Wyodrębnienie UserId z JWT Claims
   ↓ Utworzenie ImportTicketsFromCsvRequest
[MediatR]
   ↓ Send(request)
[Validator.cs]
   ↓ Walidacja FluentValidation (plik wymagany, format CSV)
[Handler.cs]
   ↓ 1. Sprawdzenie limitu dostępnych miejsc
   ↓ 2. Parsowanie pliku CSV (odczyt wierszy)
   ↓ 3. Walidacja nagłówka CSV
   ↓ 4. Dla każdego wiersza:
   ↓    - Parsowanie 6 liczb + GroupName
   ↓    - Walidacja (zakres 1-49, unikalność w zestawie)
   ↓    - Sprawdzenie unikalności zestawu (duplikaty istniejących)
   ↓    - Jeśli OK → dodaj do listy ticketsToImport
   ↓    - Jeśli błąd → dodaj do listy errors
   ↓ 5. Batch insert pomyślnie zwalidowanych zestawów (transakcja)
   ↓    - INSERT Ticket (ID auto-generated)
   ↓    - INSERT 6x TicketNumber (Position 1-6)
   ↓ 6. Generowanie raportu (imported, rejected, errors)
   ↓ ImportTicketsFromCsvResponse
[Endpoint.cs]
   ↓ Results.Ok(response)
[Klient]
   ↓ 200 OK + Response Body (raport importu)
```

### 5.2 Interakcje z bazą danych

**Tabele:**
- `Tickets` - INSERT N rekordów (N = liczba pomyślnie zwalidowanych zestawów)
- `TicketNumbers` - INSERT N×6 rekordów (każdy zestaw = 6 liczb)

**Zapytania:**

1. **Sprawdzenie limitu dostępnych miejsc:**
```sql
SELECT COUNT(*) FROM Tickets WHERE UserId = @userId
-- Obliczenie: available = 100 - count
```

2. **Pobranie istniejących zestawów dla walidacji unikalności:**
```sql
SELECT t.Id, tn.Number
FROM Tickets t
INNER JOIN TicketNumbers tn ON t.Id = tn.TicketId
WHERE t.UserId = @userId
ORDER BY t.Id, tn.Position
```

3. **Batch insert zestawów (transakcja):**
```sql
BEGIN TRANSACTION;

-- Dla każdego zestawu:
INSERT INTO Tickets (UserId, GroupName, CreatedAt)
VALUES (@userId, @groupName, GETUTCDATE());

DECLARE @ticketId INT = SCOPE_IDENTITY();

-- 6x INSERT dla liczb
INSERT INTO TicketNumbers (TicketId, Number, Position)
VALUES (@ticketId, @number1, 1),
       (@ticketId, @number2, 2),
       (@ticketId, @number3, 3),
       (@ticketId, @number4, 4),
       (@ticketId, @number5, 5),
       (@ticketId, @number6, 6);

COMMIT;
```

### 5.3 Algorytm parsowania i walidacji CSV

```csharp
// 1. Odczyt pliku CSV
using var reader = new StreamReader(request.File.OpenReadStream(), Encoding.UTF8);
var csvContent = await reader.ReadToEndAsync();
var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);

// 2. Walidacja nagłówka
var header = lines[0].Trim();
if (header != "Number1,Number2,Number3,Number4,Number5,Number6,GroupName")
{
    throw new ValidationException("Nieprawidłowy format nagłówka");
}

// 3. Parsowanie wierszy danych
var ticketsToImport = new List<TicketToImport>();
var errors = new List<ImportError>();

for (int i = 1; i < lines.Length; i++)
{
    var rowNumber = i; // Numer wiersza (1-based, bez nagłówka)
    var line = lines[i].Trim();
    var columns = line.Split(',');

    try
    {
        // Parsowanie 6 liczb
        var numbers = columns.Take(6).Select(int.Parse).ToArray();

        // Walidacja: 6 liczb, zakres 1-49, unikalność
        if (numbers.Length != 6)
            throw new Exception("Wymagane 6 liczb");

        if (numbers.Any(n => n < 1 || n > 49))
            throw new Exception($"Invalid number range: {numbers.First(n => n < 1 || n > 49)}");

        if (numbers.Distinct().Count() != 6)
            throw new Exception("Numbers must be unique in set");

        // GroupName (opcjonalny)
        var groupName = columns.Length > 6 ? columns[6].Trim() : null;

        // Sprawdzenie unikalności zestawu
        if (IsDuplicateTicket(numbers, existingTickets))
        {
            errors.Add(new ImportError(rowNumber, "Duplicate ticket"));
            continue;
        }

        ticketsToImport.Add(new TicketToImport(numbers, groupName, rowNumber));
    }
    catch (Exception ex)
    {
        errors.Add(new ImportError(rowNumber, ex.Message));
    }
}
```

---

## 6. Względy bezpieczeństwa

### 6.1 Autoryzacja i uwierzytelnianie

- **JWT Token:** Wymagany w header `Authorization: Bearer <token>`
- **Walidacja tokenu:** Automatyczna przez middleware ASP.NET Core
- **Ekstrakcja UserId:** Z JWT Claims (`ClaimTypes.NameIdentifier`)
- **Izolacja danych:** UserId z tokenu zapewnia, że użytkownik importuje tylko do swojego konta

### 6.2 Feature Flag Security

- **Sprawdzenie flagi:** `IConfiguration["Features:TicketImportExport:Enable"]`
- **Jeśli wyłączona:** Zwróć 404 Not Found (ukrycie funkcjonalności)
- **Konfiguracja:** `appsettings.json` → `Features:TicketImportExport:Enable: false/true`

### 6.3 Walidacja pliku CSV

- **Rozmiar pliku:** Limit max 1MB (konfigurowalny)
- **Format:** Tylko text/csv
- **Kodowanie:** UTF-8
- **Walidacja nagłówka:** Dokładne dopasowanie wymagane
- **Sanitization:** Trimowanie whitespace z wartości

### 6.4 Ochrona przed atakami

| Atak | Ochrona |
|------|---------|
| **SQL Injection** | Entity Framework Core (parametryzowane zapytania) |
| **CSV Injection** | Walidacja formatu, brak wykonywania formuł |
| **File Upload Attack** | Limit rozmiaru, walidacja typu MIME |
| **DoS (duże pliki)** | Limit rozmiaru pliku (1MB), limit wierszy (100) |
| **Path Traversal** | Plik przetwarzany w pamięci (stream), nie zapisywany na dysku |
| **Mass Assignment** | Explicite zdefiniowane pola w DTO |

### 6.5 Bezpieczeństwo transakcji

- **Atomowość:** Wszystkie inserty w jednej transakcji (albo wszystkie, albo żaden)
- **Rollback:** Automatyczny w przypadku błędu
- **Izolacja:** Default isolation level SQL Server (READ COMMITTED)

---

## 7. Obsługa błędów

### 7.1 Rodzaje błędów

| Typ błędu | Kod HTTP | Obsługa | Przykład |
|-----------|----------|---------|----------|
| **Feature Flag wyłączony** | 404 | Sprawdzenie w Endpoint | `Enable: false` w config |
| **Brak autoryzacji** | 401 | ASP.NET Middleware | Brak/nieprawidłowy token JWT |
| **Brak pliku** | 400 | FluentValidation | `file` nie przesłany |
| **Nieprawidłowy nagłówek CSV** | 400 | Custom Exception w Handler | Błędny format nagłówka |
| **Limit osiągnięty** | 400 | Custom ValidationException | Brak miejsca na import |
| **Błędy wierszy CSV** | 200 | Raport w Response | Nieprawidłowe liczby, duplikaty |
| **Błąd bazy danych** | 500 | ExceptionHandlingMiddleware | Connection timeout |

### 7.2 Szczegółowe scenariusze

**1. Feature Flag wyłączony (Endpoint.cs):**
```csharp
var isEnabled = configuration.GetValue<bool>("Features:TicketImportExport:Enable");
if (!isEnabled)
{
    return Results.NotFound(new { errors = new[] { "Funkcjonalność importu/eksportu CSV jest wyłączona" } });
}
```

**2. Walidacja pliku (Validator.cs):**
```csharp
RuleFor(x => x.File)
    .NotNull()
    .WithMessage("Plik CSV jest wymagany");

RuleFor(x => x.File)
    .Must(file => file.ContentType == "text/csv")
    .WithMessage("Dozwolony tylko format CSV");

RuleFor(x => x.File)
    .Must(file => file.Length <= 1048576) // 1MB
    .WithMessage("Maksymalny rozmiar pliku to 1MB");
```

**3. Limit dostępnych miejsc (Handler.cs):**
```csharp
var ticketCount = await dbContext.Tickets.CountAsync(t => t.UserId == userId);
var available = 100 - ticketCount;
var rowCount = csvLines.Length - 1; // Bez nagłówka

if (rowCount > available)
{
    throw new ValidationException($"Osiągnięto limit 100 zestawów. Dostępne: {available} zestawów. Plik zawiera {rowCount} wierszy.");
}
```

**4. Błędy wierszy CSV:**
Nie przerywają procesu - są zbierane w listę `errors` i zwracane w Response

---

## 8. Rozważania dotyczące wydajności

### 8.1 Potencjalne wąskie gardła

| Problem | Impact | Rozwiązanie |
|---------|--------|-------------|
| **Parsowanie dużego pliku CSV** | O(n) gdzie n=liczba wierszy | Limit 100 wierszy (max dla użytkownika) |
| **Walidacja unikalności każdego wiersza** | O(m × k) gdzie m=nowe zestawy, k=istniejące | Maksymalnie 100 istniejących → akceptowalne |
| **Multiple INSERT** | N × 7 operacji (1 Ticket + 6 Numbers) | Batch insert w transakcji |
| **Odczyt pliku** | Blokujący I/O | Async stream reading |

### 8.2 Optymalizacje

**1. Stream reading (async):**
```csharp
using var reader = new StreamReader(request.File.OpenReadStream(), Encoding.UTF8);
var csvContent = await reader.ReadToEndAsync(cancellationToken);
```

**2. Batch insert (EF Core):**
```csharp
// Zbieranie wszystkich encji przed SaveChanges
var allTickets = new List<Ticket>();
var allTicketNumbers = new List<TicketNumber>();

foreach (var ticketData in ticketsToImport)
{
    var ticket = new Ticket { ... };
    allTickets.Add(ticket);

    // Po SaveChanges ticket będzie miał Id
}

dbContext.Tickets.AddRange(allTickets);
await dbContext.SaveChangesAsync(); // Batch 1: wszystkie Tickets

foreach (var ticket in allTickets)
{
    // Teraz mamy ticket.Id
    var numbers = CreateTicketNumbers(ticket.Id, ...);
    allTicketNumbers.AddRange(numbers);
}

dbContext.TicketNumbers.AddRange(allTicketNumbers);
await dbContext.SaveChangesAsync(); // Batch 2: wszystkie TicketNumbers
```

**3. Eager loading dla walidacji:**
```csharp
var existingTickets = await dbContext.Tickets
    .Where(t => t.UserId == userId)
    .Include(t => t.Numbers)
    .ToListAsync();
```

### 8.3 Metryki wydajności (szacowane)

| Operacja | Czas | Uzasadnienie |
|----------|------|--------------|
| Walidacja FluentValidation | <1ms | Proste sprawdzenia |
| Odczyt pliku CSV (1MB) | 10-50ms | Async stream |
| Parsowanie CSV (100 wierszy) | 5-10ms | String split, int parsing |
| SELECT istniejących zestawów | 10-30ms | Eager loading, max 100 |
| Walidacja unikalności (100×100) | 10-20ms | SequenceEqual, posortowane tablice |
| Batch INSERT (100 zestawów) | 100-200ms | Transakcja, 100+600 rekordów |
| **TOTAL** | **~150-350ms** | Akceptowalne dla MVP |

---

## 9. Etapy wdrożenia

### 9.1 Aktualizacja Contracts.cs

```csharp
public record ImportTicketsFromCsvRequest(IFormFile File) : IRequest<ImportTicketsFromCsvResponse>;

public record ImportTicketsFromCsvResponse(int Imported, int Rejected, List<ImportError> Errors);

public record ImportError(int Row, string Reason);
```

### 9.2 Implementacja Validator.cs

```csharp
public class ImportTicketsFromCsvValidator : AbstractValidator<Contracts.ImportTicketsFromCsvRequest>
{
    public ImportTicketsFromCsvValidator()
    {
        RuleFor(x => x.File)
            .NotNull()
            .WithMessage("Plik CSV jest wymagany");

        RuleFor(x => x.File)
            .Must(file => file != null && file.ContentType == "text/csv")
            .WithMessage("Dozwolony tylko format CSV");

        RuleFor(x => x.File)
            .Must(file => file != null && file.Length > 0 && file.Length <= 1048576)
            .WithMessage("Rozmiar pliku musi być w zakresie 1B - 1MB");
    }
}
```

### 9.3 Implementacja Handler.cs

```csharp
public class ImportTicketsFromCsvHandler : IRequestHandler<Contracts.ImportTicketsFromCsvRequest, Contracts.ImportTicketsFromCsvResponse>
{
    private readonly AppDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ImportTicketsFromCsvHandler> _logger;

    public async Task<Contracts.ImportTicketsFromCsvResponse> Handle(
        Contracts.ImportTicketsFromCsvRequest request,
        CancellationToken cancellationToken)
    {
        // 1. Pobranie UserId z JWT
        var userId = GetUserIdFromJwt();

        // 2. Sprawdzenie limitu dostępnych miejsc
        var available = await CheckAvailableSlotsAsync(userId, cancellationToken);

        // 3. Parsowanie pliku CSV
        var (ticketsToImport, errors) = await ParseCsvFileAsync(request.File, userId, cancellationToken);

        // 4. Sprawdzenie czy liczba wierszy nie przekracza dostępnych miejsc
        if (ticketsToImport.Count > available)
        {
            throw new ValidationException($"Osiągnięto limit 100 zestawów. Dostępne: {available} zestawów. Plik zawiera {ticketsToImport.Count + errors.Count} wierszy.");
        }

        // 5. Batch insert zwalidowanych zestawów
        var imported = await ImportTicketsAsync(userId, ticketsToImport, cancellationToken);

        return new Contracts.ImportTicketsFromCsvResponse(
            Imported: imported,
            Rejected: errors.Count,
            Errors: errors
        );
    }

    private async Task<int> CheckAvailableSlotsAsync(int userId, CancellationToken cancellationToken)
    {
        var count = await _dbContext.Tickets.CountAsync(t => t.UserId == userId, cancellationToken);
        return 100 - count;
    }

    private async Task<(List<TicketToImport>, List<Contracts.ImportError>)> ParseCsvFileAsync(
        IFormFile file,
        int userId,
        CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8);
        var csvContent = await reader.ReadToEndAsync();
        var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // Walidacja nagłówka
        if (lines.Length == 0 || lines[0].Trim() != "Number1,Number2,Number3,Number4,Number5,Number6,GroupName")
        {
            throw new ValidationException("Nieprawidłowy format nagłówka. Oczekiwano: Number1,Number2,Number3,Number4,Number5,Number6,GroupName");
        }

        // Pobranie istniejących zestawów dla walidacji unikalności
        var existingTickets = await _dbContext.Tickets
            .Where(t => t.UserId == userId)
            .Include(t => t.Numbers)
            .ToListAsync(cancellationToken);

        var ticketsToImport = new List<TicketToImport>();
        var errors = new List<Contracts.ImportError>();

        for (int i = 1; i < lines.Length; i++)
        {
            var rowNumber = i;
            var line = lines[i].Trim();

            try
            {
                var columns = line.Split(',');
                var numbers = columns.Take(6).Select(int.Parse).ToArray();

                // Walidacja
                if (numbers.Length != 6)
                    throw new Exception("Wymagane 6 liczb");

                if (numbers.Any(n => n < 1 || n > 49))
                    throw new Exception($"Invalid number range: {numbers.First(n => n < 1 || n > 49)}");

                if (numbers.Distinct().Count() != 6)
                    throw new Exception("Numbers must be unique in set");

                // Sprawdzenie unikalności
                var numbersSorted = numbers.OrderBy(n => n).ToArray();
                var isDuplicate = existingTickets.Any(ticket =>
                {
                    var existingNumbers = ticket.Numbers.OrderBy(n => n.Number).Select(n => n.Number).ToArray();
                    return numbersSorted.SequenceEqual(existingNumbers);
                });

                if (isDuplicate)
                {
                    errors.Add(new Contracts.ImportError(rowNumber, "Duplicate ticket"));
                    continue;
                }

                var groupName = columns.Length > 6 ? columns[6].Trim() : null;
                ticketsToImport.Add(new TicketToImport(numbers, groupName, rowNumber));
            }
            catch (Exception ex)
            {
                errors.Add(new Contracts.ImportError(rowNumber, ex.Message));
            }
        }

        return (ticketsToImport, errors);
    }

    private async Task<int> ImportTicketsAsync(
        int userId,
        List<TicketToImport> ticketsToImport,
        CancellationToken cancellationToken)
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var allTickets = new List<Ticket>();

            foreach (var ticketData in ticketsToImport)
            {
                var ticket = new Ticket
                {
                    UserId = userId,
                    GroupName = ticketData.GroupName,
                    CreatedAt = DateTime.UtcNow
                };
                allTickets.Add(ticket);
            }

            _dbContext.Tickets.AddRange(allTickets);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // Teraz mamy ticket.Id dla każdego zestawu
            var allTicketNumbers = new List<TicketNumber>();

            for (int i = 0; i < allTickets.Count; i++)
            {
                var ticket = allTickets[i];
                var ticketData = ticketsToImport[i];

                var ticketNumbers = ticketData.Numbers.Select((number, index) => new TicketNumber
                {
                    TicketId = ticket.Id,
                    Number = number,
                    Position = (byte)(index + 1)
                }).ToList();

                allTicketNumbers.AddRange(ticketNumbers);
            }

            _dbContext.TicketNumbers.AddRange(allTicketNumbers);
            await _dbContext.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return allTickets.Count;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private int GetUserIdFromJwt()
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Nie można zidentyfikować użytkownika");
        }
        return userId;
    }
}

private record TicketToImport(int[] Numbers, string? GroupName, int SourceRowNumber);
```

### 9.4 Implementacja Endpoint.cs

```csharp
app.MapPost("api/tickets/import-csv", [Authorize] async (
    [FromForm] IFormFile file,
    IMediator mediator,
    IConfiguration configuration) =>
{
    // Sprawdzenie Feature Flag
    var isEnabled = configuration.GetValue<bool>("Features:TicketImportExport:Enable");
    if (!isEnabled)
    {
        return Results.NotFound(new { errors = new[] { "Funkcjonalność importu/eksportu CSV jest wyłączona" } });
    }

    var request = new Contracts.ImportTicketsFromCsvRequest(file);
    var result = await mediator.Send(request);
    return Results.Ok(result);
})
.WithName("ImportTicketsFromCsv")
.Produces<Contracts.ImportTicketsFromCsvResponse>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status401Unauthorized)
.Produces(StatusCodes.Status404NotFound)
.DisableAntiforgery() // Required for file uploads
.WithOpenApi();
```

### 9.5 Konfiguracja Feature Flag

**appsettings.json:**
```json
{
  "Features": {
    "TicketImportExport": {
      "Enable": false
    }
  }
}
```

**appsettings.Development.json:**
```json
{
  "Features": {
    "TicketImportExport": {
      "Enable": true
    }
  }
}
```

---

## 10. Podsumowanie

Endpoint **POST /api/tickets/import-csv** został zaprojektowany zgodnie z:
- ✅ Architekturą Vertical Slice Architecture (VSA)
- ✅ Wzorcem MediatR + FluentValidation
- ✅ Specyfikacją z `api-plan.md` i `prd.md`
- ✅ Feature Flag pattern dla kontroli dostępu
- ✅ Najlepszymi praktykami bezpieczeństwa i wydajności

**Kluczowe cechy implementacji:**
- Masowy import zestawów z pliku CSV
- Walidacja formatu pliku i danych
- Sprawdzanie limitu 100 zestawów/użytkownik
- Sprawdzanie unikalności importowanych zestawów
- Raport z importu (imported/rejected/errors)
- Batch insert dla wydajności
- Transakcje dla atomowości
- Feature Flag dla kontroli dostępu
- Obsługa błędów z szczegółowym raportem

Endpoint jest gotowy do implementacji i testów!
