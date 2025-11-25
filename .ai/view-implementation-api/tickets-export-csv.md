# API Endpoint Implementation Plan: GET /api/tickets/export-csv

## 1. Przegląd punktu końcowego

Endpoint **GET /api/tickets/export-csv** umożliwia użytkownikom eksport wszystkich swoich zestawów liczb LOTTO do pliku CSV. Funkcjonalność kontrolowana jest przez Feature Flag `Features:TicketImportExport:Enable`. Endpoint generuje plik CSV zawierający wszystkie zestawy użytkownika (każdy wiersz = 1 zestaw 6 liczb + nazwa grupy) i zwraca go jako plik do pobrania.

System automatycznie:
- Pobiera wszystkie zestawy użytkownika z bazy danych
- Generuje plik CSV z nagłówkiem i wierszami danych
- Sortuje liczby w każdym zestawie rosnąco
- Zwraca plik z odpowiednimi headerami HTTP (Content-Disposition: attachment)

Endpoint zwraca plik CSV gotowy do pobrania przez przeglądarkę.

---

## 2. Szczegóły żądania

- **Metoda HTTP:** GET
- **Struktura URL:** `/api/tickets/export-csv`
- **Autoryzacja:** Wymagany JWT token w header `Authorization: Bearer <token>`
- **Feature Flag:** `Features:TicketImportExport:Enable` (true/false)

### Parametry:
- **Wymagane:**
  - Header: `Authorization: Bearer <token>` (JWT)

- **Opcjonalne:** Brak

### Request Body:
Brak (metoda GET)

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
    /// Request do eksportu zestawów do pliku CSV
    /// </summary>
    public record ExportTicketsToCsvRequest() : IRequest<ExportTicketsToCsvResponse>;

    /// <summary>
    /// Response z plikiem CSV
    /// </summary>
    /// <param name="CsvContent">Zawartość pliku CSV</param>
    /// <param name="FileName">Nazwa pliku do pobrania</param>
    public record ExportTicketsToCsvResponse(string CsvContent, string FileName);
}
```

### 3.2 Encje bazodanowe (już istniejące)

- `Ticket` - metadane zestawu (Id, UserId, GroupName, CreatedAt)
- `TicketNumber` - pojedyncza liczba w zestawie (Id, TicketId, Number, Position)

### 3.3 Modele pomocnicze

```csharp
// Model wiersza CSV dla eksportu
private record CsvTicketRow(
    int Number1,
    int Number2,
    int Number3,
    int Number4,
    int Number5,
    int Number6,
    string? GroupName);
```

---

## 4. Szczegóły odpowiedzi

### Sukces (200 OK):
**Headers:**
```
Content-Type: text/csv; charset=utf-8
Content-Disposition: attachment; filename="lotto-tickets-123-2025-11-25.csv"
```

**Body (plik CSV):**
```csv
Number1,Number2,Number3,Number4,Number5,Number6,GroupName
1,10,20,30,40,49,Ulubione
3,12,18,25,31,44,Rodzina
5,14,23,29,37,41,
```

### Brak zestawów (200 OK):
**Headers:** Jak wyżej

**Body (plik CSV tylko z nagłówkiem):**
```csv
Number1,Number2,Number3,Number4,Number5,Number6,GroupName
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
   ↓ GET /api/tickets/export-csv + JWT
[Endpoint.cs]
   ↓ Sprawdzenie Feature Flag (Features:TicketImportExport:Enable)
   ↓ Jeśli false → 404 Not Found
   ↓ Wyodrębnienie UserId z JWT Claims
   ↓ Utworzenie ExportTicketsToCsvRequest
[MediatR]
   ↓ Send(request)
[Handler.cs]
   ↓ 1. Pobranie wszystkich zestawów użytkownika (eager loading Numbers)
   ↓ 2. Konwersja do CsvTicketRow (sortowanie liczb)
   ↓ 3. Generowanie zawartości CSV:
   ↓    - Nagłówek: Number1,Number2,...,GroupName
   ↓    - Wiersze danych (każdy zestaw = 1 wiersz)
   ↓ 4. Generowanie nazwy pliku: lotto-tickets-{userId}-{YYYY-MM-DD}.csv
   ↓ ExportTicketsToCsvResponse(CsvContent, FileName)
[Endpoint.cs]
   ↓ Results.File(csvBytes, "text/csv", fileName)
[Klient]
   ↓ 200 OK + Plik CSV do pobrania
```

### 5.2 Interakcje z bazą danych

**Tabele:**
- `Tickets` - SELECT wszystkie zestawy użytkownika
- `TicketNumbers` - SELECT wszystkie liczby dla zestawów (eager loading)

**Zapytania:**

1. **Pobranie wszystkich zestawów użytkownika:**
```sql
SELECT t.Id, t.UserId, t.GroupName, t.CreatedAt,
       tn.Id, tn.TicketId, tn.Number, tn.Position
FROM Tickets t
LEFT JOIN TicketNumbers tn ON t.Id = tn.TicketId
WHERE t.UserId = @userId
ORDER BY t.CreatedAt ASC, tn.Position ASC
```

Wykorzystane indeksy:
- `IX_Tickets_UserId` - szybkie filtrowanie po UserId
- `IX_TicketNumbers_TicketId` - szybki JOIN z TicketNumbers

---

### 5.3 Algorytm generowania CSV

```csharp
// 1. Pobranie zestawów z bazy (eager loading)
var tickets = await dbContext.Tickets
    .Where(t => t.UserId == userId)
    .Include(t => t.Numbers)
    .OrderBy(t => t.CreatedAt)
    .ToListAsync(cancellationToken);

// 2. Konwersja do wierszy CSV
var csvRows = tickets.Select(ticket =>
{
    var numbers = ticket.Numbers
        .OrderBy(n => n.Position)
        .Select(n => n.Number)
        .ToArray();

    return new CsvTicketRow(
        Number1: numbers[0],
        Number2: numbers[1],
        Number3: numbers[2],
        Number4: numbers[3],
        Number5: numbers[4],
        Number6: numbers[5],
        GroupName: ticket.GroupName ?? ""
    );
}).ToList();

// 3. Generowanie zawartości CSV
var csv = new StringBuilder();
csv.AppendLine("Number1,Number2,Number3,Number4,Number5,Number6,GroupName");

foreach (var row in csvRows)
{
    csv.AppendLine($"{row.Number1},{row.Number2},{row.Number3},{row.Number4},{row.Number5},{row.Number6},{row.GroupName}");
}

// 4. Generowanie nazwy pliku
var fileName = $"lotto-tickets-{userId}-{DateTime.UtcNow:yyyy-MM-dd}.csv";

return new ExportTicketsToCsvResponse(csv.ToString(), fileName);
```

---

## 6. Względy bezpieczeństwa

### 6.1 Autoryzacja i uwierzytelnianie

- **JWT Token:** Wymagany w header `Authorization: Bearer <token>`
- **Walidacja tokenu:** Automatyczna przez middleware ASP.NET Core
- **Ekstrakcja UserId:** Z JWT Claims (`ClaimTypes.NameIdentifier`)
- **Izolacja danych:** UserId z tokenu zapewnia, że użytkownik eksportuje tylko swoje dane

### 6.2 Feature Flag Security

- **Sprawdzenie flagi:** `IConfiguration["Features:TicketImportExport:Enable"]`
- **Jeśli wyłączona:** Zwróć 404 Not Found (ukrycie funkcjonalności)
- **Konfiguracja:** `appsettings.json` → `Features:TicketImportExport:Enable: false/true`

### 6.3 Ochrona danych

- **Brak wrażliwych danych:** CSV zawiera tylko numery LOTTO (nie zawiera hasła, email, itp.)
- **Izolacja użytkownika:** Każdy użytkownik eksportuje tylko swoje zestawy
- **Nazwa pliku:** Zawiera UserId dla unikalności, ale nie ujawnia wrażliwych informacji

### 6.4 Ochrona przed atakami

| Atak | Ochrona |
|------|---------|
| **SQL Injection** | Entity Framework Core (parametryzowane zapytania) |
| **CSV Injection** | Tylko dane numeryczne (1-49) i text (GroupName) - brak formuł |
| **Data Leak** | Filtrowanie WHERE UserId = @userId z JWT |
| **IDOR (Insecure Direct Object Reference)** | UserId z JWT, nie z parametru URL |
| **Path Traversal** | Plik generowany w pamięci, zwracany przez stream |

---

## 7. Obsługa błędów

### 7.1 Rodzaje błędów

| Typ błędu | Kod HTTP | Obsługa | Przykład |
|-----------|----------|---------|----------|
| **Feature Flag wyłączony** | 404 | Sprawdzenie w Endpoint | `Enable: false` w config |
| **Brak autoryzacji** | 401 | ASP.NET Middleware | Brak/nieprawidłowy token JWT |
| **Błąd bazy danych** | 500 | ExceptionHandlingMiddleware | Connection timeout |
| **Nieoczekiwany błąd** | 500 | ExceptionHandlingMiddleware + Serilog | Null reference, etc. |

### 7.2 Szczegółowe scenariusze

**1. Feature Flag wyłączony (Endpoint.cs):**
```csharp
var isEnabled = configuration.GetValue<bool>("Features:TicketImportExport:Enable");
if (!isEnabled)
{
    return Results.NotFound(new { errors = new[] { "Funkcjonalność importu/eksportu CSV jest wyłączona" } });
}
```

**2. Brak zestawów użytkownika:**
Nie jest błędem - zwracany plik CSV tylko z nagłówkiem (puste dane)

**3. Błąd bazy danych:**
```csharp
catch (DbException ex)
{
    logger.LogError(ex, "Database error while exporting tickets for user {UserId}", userId);
    throw; // Middleware obsłuży
}
```

---

## 8. Rozważania dotyczące wydajności

### 8.1 Potencjalne wąskie gardła

| Problem | Impact | Rozwiązanie |
|---------|--------|-------------|
| **SELECT 100 zestawów + 600 liczb** | O(n) gdzie n=100 | Eager loading, max 100 zestawów |
| **N+1 query problem** | 100 dodatkowych SELECT dla Numbers | ✅ `.Include(t => t.Numbers)` |
| **Generowanie CSV w pamięci** | O(n) string concat | StringBuilder (efektywne) |
| **Transfer dużego pliku** | Max ~10KB (100 zestawów) | Akceptowalne |

### 8.2 Optymalizacje

**1. Eager Loading (zapobiega N+1 problem):**
```csharp
var tickets = await dbContext.Tickets
    .Where(t => t.UserId == userId)
    .Include(t => t.Numbers) // Zapobiega N+1 query
    .OrderBy(t => t.CreatedAt)
    .ToListAsync(cancellationToken);
```

**2. StringBuilder dla wydajności:**
```csharp
var csv = new StringBuilder();
csv.AppendLine("Number1,Number2,Number3,Number4,Number5,Number6,GroupName,CreatedAt");

foreach (var row in csvRows)
{
    csv.AppendLine($"{row.Number1},{row.Number2},...");
}
```

**3. Asynchroniczne operacje:**
```csharp
var tickets = await dbContext.Tickets
    .Where(t => t.UserId == userId)
    .Include(t => t.Numbers)
    .ToListAsync(cancellationToken); // Async DB operation
```

### 8.3 Metryki wydajności (szacowane)

| Operacja | Czas | Uzasadnienie |
|----------|------|--------------|
| SELECT zestawów + Numbers | 10-30ms | Eager loading, max 100+600 rekordów, indeksy |
| Konwersja do CsvTicketRow | 1-5ms | W pamięci, proste LINQ |
| Generowanie CSV (StringBuilder) | 5-10ms | 100 wierszy, string concat |
| Zwrot pliku przez stream | 1-5ms | ~10KB danych |
| **TOTAL** | **~20-50ms** | Bardzo szybkie dla MVP |

### 8.4 Rozmiar pliku

**Szacowanie:**
- Nagłówek: ~70 bajtów
- 1 wiersz danych: ~50 bajtów (6 liczb + GroupName + data)
- 100 zestawów: ~5KB + nagłówek ≈ **5-10KB**

Bardzo mały plik - brak problemów z transferem.

---

## 9. Etapy wdrożenia

### 9.1 Aktualizacja Contracts.cs

```csharp
public record ExportTicketsToCsvRequest() : IRequest<ExportTicketsToCsvResponse>;

public record ExportTicketsToCsvResponse(string CsvContent, string FileName);
```

### 9.2 Implementacja Handler.cs

```csharp
public class ExportTicketsToCsvHandler : IRequestHandler<Contracts.ExportTicketsToCsvRequest, Contracts.ExportTicketsToCsvResponse>
{
    private readonly AppDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ExportTicketsToCsvHandler> _logger;

    public ExportTicketsToCsvHandler(
        AppDbContext dbContext,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ExportTicketsToCsvHandler> logger)
    {
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<Contracts.ExportTicketsToCsvResponse> Handle(
        Contracts.ExportTicketsToCsvRequest request,
        CancellationToken cancellationToken)
    {
        // 1. Pobranie UserId z JWT
        var userId = GetUserIdFromJwt();

        // 2. Pobranie wszystkich zestawów użytkownika
        var tickets = await _dbContext.Tickets
            .Where(t => t.UserId == userId)
            .Include(t => t.Numbers)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync(cancellationToken);

        _logger.LogDebug("Exporting {Count} tickets for user {UserId}", tickets.Count, userId);

        // 3. Generowanie CSV
        var csvContent = GenerateCsvContent(tickets);

        // 4. Generowanie nazwy pliku
        var fileName = $"lotto-tickets-{userId}-{DateTime.UtcNow:yyyy-MM-dd}.csv";

        return new Contracts.ExportTicketsToCsvResponse(csvContent, fileName);
    }

    private string GenerateCsvContent(List<Ticket> tickets)
    {
        var csv = new StringBuilder();
        csv.AppendLine("Number1,Number2,Number3,Number4,Number5,Number6,GroupName");

        foreach (var ticket in tickets)
        {
            var numbers = ticket.Numbers
                .OrderBy(n => n.Position)
                .Select(n => n.Number)
                .ToArray();

            var groupName = string.IsNullOrEmpty(ticket.GroupName) ? "" : ticket.GroupName;

            csv.AppendLine($"{numbers[0]},{numbers[1]},{numbers[2]},{numbers[3]},{numbers[4]},{numbers[5]},{groupName}");
        }

        return csv.ToString();
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
```

### 9.3 Implementacja Endpoint.cs

```csharp
app.MapGet("api/tickets/export-csv", [Authorize] async (
    IMediator mediator,
    IConfiguration configuration) =>
{
    // Sprawdzenie Feature Flag
    var isEnabled = configuration.GetValue<bool>("Features:TicketImportExport:Enable");
    if (!isEnabled)
    {
        return Results.NotFound(new { errors = new[] { "Funkcjonalność importu/eksportu CSV jest wyłączona" } });
    }

    var request = new Contracts.ExportTicketsToCsvRequest();
    var result = await mediator.Send(request);

    var csvBytes = Encoding.UTF8.GetBytes(result.CsvContent);

    return Results.File(
        csvBytes,
        contentType: "text/csv; charset=utf-8",
        fileDownloadName: result.FileName
    );
})
.WithName("ExportTicketsToCsv")
.Produces(StatusCodes.Status200OK, contentType: "text/csv")
.Produces(StatusCodes.Status401Unauthorized)
.Produces(StatusCodes.Status404NotFound)
.WithOpenApi();
```

### 9.4 Konfiguracja Feature Flag

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

### 9.5 Testowanie

**Test 1: Sukces (200 OK z plikiem CSV)**
```http
GET /api/tickets/export-csv
Authorization: Bearer eyJhbGc...

Expected: 200 OK
Content-Type: text/csv; charset=utf-8
Content-Disposition: attachment; filename="lotto-tickets-123-2025-11-25.csv"

Body:
Number1,Number2,Number3,Number4,Number5,Number6,GroupName
1,10,20,30,40,49,Ulubione
3,12,18,25,31,44,Rodzina
```

**Test 2: Brak zestawów (200 OK z pustym CSV)**
```http
GET /api/tickets/export-csv
Authorization: Bearer eyJhbGc...

Expected: 200 OK
Body:
Number1,Number2,Number3,Number4,Number5,Number6,GroupName
```

**Test 3: Brak autoryzacji (401)**
```http
GET /api/tickets/export-csv

Expected: 401 Unauthorized
```

**Test 4: Feature Flag wyłączony (404)**
```http
GET /api/tickets/export-csv
Authorization: Bearer eyJhbGc...

# Z appsettings: Features:TicketImportExport:Enable = false

Expected: 404 Not Found
{
  "errors": ["Funkcjonalność importu/eksportu CSV jest wyłączona"]
}
```

### 9.6 Checklist implementacji

- [ ] **Zaktualizowano** `Contracts.cs` z typami DTO
- [ ] **Utworzono plik** `Handler.cs` z logiką eksportu
- [ ] **Zaktualizowano** `Endpoint.cs` z definicją GET endpoint
- [ ] **Skonfigurowano** Feature Flag w `appsettings.json`
- [ ] **Przetestowano** scenariusz sukcesu (200 OK + plik CSV)
- [ ] **Przetestowano** brak autoryzacji (401)
- [ ] **Przetestowano** Feature Flag wyłączony (404)
- [ ] **Przetestowano** brak zestawów (plik CSV tylko z nagłówkiem)
- [ ] **Zweryfikowano** poprawność danych w pliku CSV
- [ ] **Sprawdzono** logi w Serilog (brak błędów)

---

## 10. Podsumowanie

Endpoint **GET /api/tickets/export-csv** został zaprojektowany zgodnie z:
- ✅ Architekturą Vertical Slice Architecture (VSA)
- ✅ Wzorcem MediatR
- ✅ Specyfikacją z `api-plan.md` i `prd.md`
- ✅ Feature Flag pattern dla kontroli dostępu
- ✅ Najlepszymi praktykami bezpieczeństwa i wydajności

**Kluczowe cechy implementacji:**
- Eksport wszystkich zestawów użytkownika do CSV
- Format CSV zgodny z szablonem importu (interoperability)
- Eager loading dla wydajności (zapobiega N+1 problem)
- Sortowanie liczb rosnąco w każdym zestawie
- Nazwa pliku: `lotto-tickets-{userId}-{YYYY-MM-DD}.csv`
- Feature Flag dla kontroli dostępu
- Izolacja danych użytkownika (UserId z JWT)
- Bardzo wysoka wydajność (~20-50ms dla 100 zestawów)
- Mały rozmiar pliku (~5-10KB)

Endpoint jest gotowy do implementacji i testów!
