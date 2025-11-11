# API Endpoint Implementation Plan: PUT /api/tickets/{id}

**Data:** 2025-11-05  
**Endpoint:** PUT /api/tickets/{id}  
**Moduł:** Tickets (Zestawy liczb)  
**Architektura:** Vertical Slice Architecture

---

## 1. Przegląd punktu końcowego

### 1.1 Cel
Endpoint umożliwia użytkownikowi edycję istniejącego zestawu liczb LOTTO. Pozwala na zmianę 6 liczb w zakresie 1-49, z zachowaniem walidacji unikalności zestawu oraz sprawdzeniem właściciela zasobu.

### 1.2 Kontekst biznesowy
- Użytkownik może modyfikować swoje zestawy liczb po ich utworzeniu
- System musi zapewnić, że edytowany zestaw pozostanie unikalny (pomijając sam siebie podczas sprawdzania)
- Tylko właściciel zestawu może go edytować
- Data utworzenia zestawu pozostaje niezmieniona (brak kolumny UpdatedAt w MVP)

### 1.3 Kluczowe wymagania
- **Autoryzacja:** Wymagany JWT token w header `Authorization: Bearer <token>`
- **Własność:** Użytkownik może edytować tylko swoje zestawy
- **Walidacja:** 6 unikalnych liczb w zakresie 1-49
- **Unikalność:** Zestaw musi być unikalny dla użytkownika (porównanie niezależne od kolejności liczb)
- **Transakcyjność:** DELETE starych liczb + INSERT nowych liczb w jednej transakcji

---

## 2. Szczegóły żądania

### 2.1 Metoda HTTP
`PUT`

### 2.2 Struktura URL
```
PUT /api/tickets/{id}
```

### 2.3 Parametry

#### Parametry URL (Route Parameters)
| Parametr | Typ | Wymagany | Opis |
|----------|-----|----------|------|
| `id` | int | Tak | Identyfikator zestawu do edycji |

**Przykład:** `123`

#### Headers
| Header | Wartość | Wymagany | Opis |
|--------|---------|----------|------|
| `Authorization` | `Bearer <JWT_TOKEN>` | Tak | Token JWT użytkownika |
| `Content-Type` | `application/json` | Tak | Format danych |

### 2.4 Request Body

```json
{
  "groupName": "Rodzina",
  "numbers": [7, 15, 22, 33, 38, 45]
}
```

#### Struktura Request DTO
```csharp
public record UpdateTicketRequest(string? GroupName, int[] Numbers);
```

#### Walidacja Request Body
- `groupName`:
  - **Wymagane:** Nie (opcjonalne)
  - **Max długość:** 100 znaków
  - **Domyślnie:** Pusty string jeśli nie podano
- `numbers`: 
  - **Wymagane:** Nie może być null
  - **Długość:** Dokładnie 6 elementów
  - **Zakres:** Każda liczba w przedziale 1-49
  - **Unikalność:** Wszystkie liczby muszą być unikalne w tablicy

---

## 3. Wykorzystywane typy

### 3.1 Contracts (DTOs)

#### Request DTO
```csharp
namespace LottoTM.Server.Api.Features.Tickets;

public class UpdateTicketContracts
{
    // Request zawiera ID z route i body z liczbami
    public record Request(int TicketId, string? GroupName, int[] Numbers) : IRequest<Response>;
    
    // Response dla sukcesu
    public record Response(string Message);
    
    // Response dla błędu walidacji
    public record ValidationErrorResponse(Dictionary<string, string[]> Errors);
}
```

### 3.2 Command Model
```csharp
// Command model używany wewnętrznie przez Handler
public record UpdateTicketCommand
{
    public int TicketId { get; init; }
    public int UserId { get; init; } // Z JWT
    public int[] Numbers { get; init; }
}
```

### 3.3 Wykorzystywane Entities
- `Ticket` (Entities/Ticket.cs) - istniejąca encja
- `TicketNumber` (Entities/TicketNumber.cs) - istniejąca encja
- `User` - do weryfikacji JWT

---

## 4. Szczegóły odpowiedzi

### 4.1 Kody statusu HTTP

#### Sukces
| Kod | Nazwa | Warunek | Response Body |
|-----|-------|---------|---------------|
| 200 | OK | Zestaw został pomyślnie zaktualizowany | `{ "message": "Zestaw zaktualizowany pomyślnie" }` |

#### Błędy klienta (4xx)
| Kod | Nazwa | Warunek | Response Body |
|-----|-------|---------|---------------|
| 400 | Bad Request | Nieprawidłowe dane wejściowe (walidacja) | `{ "errors": { "numbers": ["Wymagane dokładnie 6 liczb"], ... } }` |
| 401 | Unauthorized | Brak tokenu JWT lub token nieprawidłowy | `{ "error": "Unauthorized" }` |
| 403 | Forbidden | Zestaw należy do innego użytkownika | `{ "error": "Brak dostępu do tego zasobu" }` |
| 404 | Not Found | Zestaw o podanym ID nie istnieje | `{ "error": "Zestaw nie został znaleziony" }` |

#### Błędy serwera (5xx)
| Kod | Nazwa | Warunek | Response Body |
|-----|-------|---------|---------------|
| 500 | Internal Server Error | Nieoczekiwany błąd serwera | `{ "error": "Wystąpił błąd serwera" }` |

### 4.2 Przykłady odpowiedzi

#### Sukces (200 OK)
```json
{
  "message": "Zestaw zaktualizowany pomyślnie"
}
```

#### Błąd walidacji (400 Bad Request)
```json
{
  "errors": {
    "numbers": [
      "Wymagane dokładnie 6 liczb"
    ],
    "numbers[0]": [
      "Liczba musi być w zakresie 1-49"
    ],
    "duplicate": [
      "Taki zestaw już istnieje w Twoich zapisanych zestawach"
    ]
  }
}
```

#### Błąd autoryzacji (403 Forbidden)
```json
{
  "error": "Brak dostępu do tego zasobu"
}
```

#### Błąd nie znaleziono (404 Not Found)
```json
{
  "error": "Zestaw nie został znaleziony"
}
```

---

## 5. Przepływ danych

### 5.1 Diagram przepływu

```
┌─────────────┐
│   Client    │
└──────┬──────┘
       │ PUT /api/tickets/{id}
       │ Authorization: Bearer <JWT>
       │ Body: { "numbers": [7, 15, 22, 33, 38, 45] }
       ▼
┌──────────────────────┐
│   ASP.NET Core       │
│   Minimal API        │
└──────┬───────────────┘
       │ 1. Parse route param (id) as int
       │ 2. Extract JWT claims (UserId)
       │ 3. Create Request object
       ▼
┌──────────────────────┐
│   Validator          │
│   (FluentValidation) │
└──────┬───────────────┘
       │ 4. Validate:
       │    - numbers not null
       │    - numbers.Length == 6
       │    - all numbers in 1-49
       │    - all numbers unique
       ▼
┌──────────────────────┐
│   Handler            │
│   (MediatR)          │
└──────┬───────────────┘
       │ 5. Check ticket exists
       │ 6. Check ownership (UserId)
       │ 7. Check uniqueness (excluding self)
       │ 8. Update ticket (transaction)
       ▼
┌──────────────────────┐
│   AppDbContext       │
│   (EF Core)          │
└──────┬───────────────┘
       │ 9. BEGIN TRANSACTION
       │ 10. DELETE FROM TicketNumbers WHERE TicketId = @id
       │ 11. INSERT 6x INTO TicketNumbers
       │ 12. COMMIT TRANSACTION
       ▼
┌──────────────────────┐
│   SQL Server 2022    │
└──────┬───────────────┘
       │ 13. Return success
       ▼
┌──────────────────────┐
│   Response           │
│   200 OK             │
└──────────────────────┘
```

### 5.2 Szczegółowy opis kroków

#### Krok 1-3: Endpoint & Request Mapping
- ASP.NET Core Minimal API odbiera żądanie
- Route parameter `{id}` jest parsowany do int
- JWT token jest walidowany przez middleware autentykacji
- UserId jest wyodrębniany z JWT claims
- Request object jest tworzony: `new Request(ticketId, body.Numbers)`

#### Krok 4: Walidacja (Validator)
```csharp
public class UpdateTicketValidator : AbstractValidator<UpdateTicketContracts.Request>
{
    public UpdateTicketValidator()
    {
        RuleFor(x => x.Numbers)
            .NotNull().WithMessage("Liczby są wymagane")
            .Must(n => n.Length == 6).WithMessage("Wymagane dokładnie 6 liczb")
            .Must(n => n.All(num => num >= 1 && num <= 49))
                .WithMessage("Wszystkie liczby muszą być w zakresie 1-49")
            .Must(n => n.Distinct().Count() == 6)
                .WithMessage("Liczby muszą być unikalne");
    }
}
```

#### Krok 5-6: Sprawdzenie istnienia i własności
```csharp
var ticket = await _context.Tickets
    .Include(t => t.Numbers)
    .FirstOrDefaultAsync(t => t.Id == request.TicketId, cancellationToken);

if (ticket == null)
    return Results.NotFound(new { error = "Zestaw nie został znaleziony" });

if (ticket.UserId != currentUserId)
    return Results.Forbid(); // 403 Forbidden
```

#### Krok 7: Sprawdzenie unikalności (wykluczając edytowany zestaw)
```csharp
var sortedNewNumbers = request.Numbers.OrderBy(n => n).ToArray();

var existingTickets = await _context.Tickets
    .Where(t => t.UserId == currentUserId && t.Id != request.TicketId)
    .Include(t => t.Numbers)
    .ToListAsync(cancellationToken);

foreach (var existingTicket in existingTickets)
{
    var sortedExisting = existingTicket.Numbers
        .OrderBy(n => n.Number)
        .Select(n => n.Number)
        .ToArray();
    
    if (sortedNewNumbers.SequenceEqual(sortedExisting))
    {
        return Results.BadRequest(new ValidationErrorResponse(
            new Dictionary<string, string[]>
            {
                { "duplicate", new[] { "Taki zestaw już istnieje w Twoich zapisanych zestawach" } }
            }
        ));
    }
}
```

#### Krok 8-12: Aktualizacja w transakcji
```csharp
using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
try
{
    // Usuń stare liczby
    _context.TicketNumbers.RemoveRange(ticket.Numbers);
    
    // Dodaj nowe liczby
    for (byte i = 0; i < request.Numbers.Length; i++)
    {
        ticket.Numbers.Add(new TicketNumber
        {
            TicketId = ticket.Id,
            Number = request.Numbers[i],
            Position = (byte)(i + 1)
        });
    }
    
    await _context.SaveChangesAsync(cancellationToken);
    await transaction.CommitAsync(cancellationToken);
    
    return Results.Ok(new Response("Zestaw zaktualizowany pomyślnie"));
}
catch (Exception ex)
{
    await transaction.RollbackAsync(cancellationToken);
    _logger.LogError(ex, "Błąd podczas aktualizacji zestawu {TicketId}", request.TicketId);
    throw;
}
```

### 5.3 Interakcje z bazą danych

#### Zapytania SELECT
```sql
-- 1. Pobranie zestawu z liczbami (z eager loading)
SELECT t.Id, t.UserId, t.CreatedAt, 
       tn.Id, tn.TicketId, tn.Number, tn.Position
FROM Tickets t
INNER JOIN TicketNumbers tn ON t.Id = tn.TicketId
WHERE t.Id = @ticketId;

-- 2. Pobranie pozostałych zestawów użytkownika (dla sprawdzenia unikalności)
SELECT t.Id, tn.Number, tn.Position
FROM Tickets t
INNER JOIN TicketNumbers tn ON t.Id = tn.TicketId
WHERE t.UserId = @userId AND t.Id != @ticketId
ORDER BY t.Id, tn.Position;
```

#### Operacje modyfikacji (w transakcji)
```sql
-- 3. Usunięcie starych liczb
DELETE FROM TicketNumbers WHERE TicketId = @ticketId;

-- 4. Wstawienie nowych liczb (6x)
INSERT INTO TicketNumbers (TicketId, Number, Position)
VALUES 
    (@ticketId, @number1, 1),
    (@ticketId, @number2, 2),
    (@ticketId, @number3, 3),
    (@ticketId, @number4, 4),
    (@ticketId, @number5, 5),
    (@ticketId, @number6, 6);
```

#### Wykorzystywane indeksy
- `IX_Tickets_UserId` - dla filtrowania po UserId
- `IX_TicketNumbers_TicketId` - dla JOIN z TicketNumbers
- `UQ_TicketNumbers_TicketPosition` - zapewnienie unikalności (TicketId, Position)

---

## 6. Względy bezpieczeństwa

### 6.1 Uwierzytelnianie (Authentication)

#### JWT Token Validation
- **Middleware:** ASP.NET Core JWT Bearer Authentication
- **Header:** `Authorization: Bearer <token>`
- **Claims wymagane:**
  - `sub` lub `userId` - identyfikator użytkownika
  - `exp` - czas wygaśnięcia tokenu
- **Weryfikacja:**
  - Podpis tokenu (HMAC SHA-256)
  - Czas wygaśnięcia (token ważny 24h)
  - Issuer i Audience

**Implementacja w Endpoint:**
```csharp
.RequireAuthorization() // Wymaga JWT
```

### 6.2 Autoryzacja (Authorization)

#### Weryfikacja właściciela zasobu
```csharp
// 1. Pobranie UserId z JWT claims
var currentUserId = int.Parse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier));

// 2. Sprawdzenie czy użytkownik jest właścicielem
if (ticket.UserId != currentUserId)
{
    _logger.LogWarning(
        "Użytkownik {UserId} próbował edytować cudzy zestaw {TicketId}",
        currentUserId, 
        request.TicketId
    );
    return Results.Forbid(); // 403 Forbidden - nie ujawniamy czy zasób istnieje
}
```

**Zasada bezpieczeństwa:** Zwracamy 403 Forbidden (nie 404 Not Found) gdy zestaw istnieje ale należy do innego użytkownika - **security by obscurity**.

### 6.3 Walidacja danych wejściowych

#### Walidacja na poziomie FluentValidation
- Liczby w zakresie 1-49 (zapobiega injection, overflow)
- Dokładnie 6 liczb (zapobiega DoS przez duże tablice)
- Liczby unikalne (zapobiega nielogicznym danym)

#### Walidacja biznesowa w Handler
- Unikalność zestawu dla użytkownika
- Istnienie zestawu w bazie
- Własność zasobu

### 6.4 Zapobieganie atakom

#### SQL Injection
- **Ochrona:** Entity Framework Core używa parametryzowanych zapytań
- Wszystkie dane wejściowe są przekazywane jako parametry, nie konkatenowane

#### Mass Assignment
- **Ochrona:** Używamy dedykowanych DTOs (Request/Response)
- Użytkownik nie może modyfikować `UserId`, `CreatedAt`, `Id`

#### ID Enumeration Protection
- **Ochrona:** Security by obscurity - zwracanie 403 zamiast 404 dla nieistniejących zasobów
- Weryfikacja własności zasobu przed ujawnieniem informacji o istnieniu

#### CSRF (Cross-Site Request Forgery)
- **Ochrona:** JWT w header (nie w cookies)
- CORS policy ogranicza pochodzenie requestów

### 6.5 Logowanie bezpieczeństwa (Audit Trail)

```csharp
_logger.LogInformation(
    "Użytkownik {UserId} zaktualizował zestaw {TicketId} na liczby [{Numbers}]",
    currentUserId,
    request.TicketId,
    string.Join(", ", request.Numbers)
);
```

**Logowane zdarzenia:**
- Udana aktualizacja zestawu
- Próba edycji cudzego zestawu (403)
- Błędy walidacji (400)
- Błędy transakcji (500)

---

## 7. Obsługa błędów

### 7.1 Hierarchia wyjątków

```
Exception
├── ValidationException (FluentValidation)
│   └── Obsługiwany przez Handler → 400 Bad Request
├── UnauthorizedAccessException
│   └── Obsługiwany przez middleware → 401 Unauthorized
├── DbUpdateException (EF Core)
│   └── Obsługiwany przez middleware → 500 Internal Server Error
└── OperationCanceledException
    └── Obsługiwany przez middleware → 499 Client Closed Request
```

### 7.2 Scenariusze błędów i odpowiedzi

#### 7.2.1 Błędy walidacji (400 Bad Request)

##### Brak liczb (null)
**Request:**
```json
{
  "numbers": null
}
```
**Response:**
```json
{
  "errors": {
    "numbers": ["Liczby są wymagane"]
  }
}
```

##### Nieprawidłowa liczba elementów
**Request:**
```json
{
  "numbers": [1, 2, 3, 4, 5]
}
```
**Response:**
```json
{
  "errors": {
    "numbers": ["Wymagane dokładnie 6 liczb"]
  }
}
```

##### Liczby poza zakresem
**Request:**
```json
{
  "numbers": [1, 2, 3, 50, 100, 6]
}
```
**Response:**
```json
{
  "errors": {
    "numbers": ["Wszystkie liczby muszą być w zakresie 1-49"]
  }
}
```

##### Nieunikalny zestaw
**Request:**
```json
{
  "numbers": [1, 2, 3, 3, 5, 6]
}
```
**Response:**
```json
{
  "errors": {
    "numbers": ["Liczby muszą być unikalne"]
  }
}
```

##### Duplikat istniejącego zestawu
**Kontekst:** Użytkownik ma już zestaw [5, 14, 23, 29, 37, 41]

**Request:**
```json
{
  "numbers": [5, 14, 23, 29, 37, 41]
}
```
**Response:**
```json
{
  "errors": {
    "duplicate": ["Taki zestaw już istnieje w Twoich zapisanych zestawach"]
  }
}
```

#### 7.2.2 Błędy autoryzacji (401 Unauthorized)

##### Brak tokenu JWT
**Request:** Brak header `Authorization`

**Response:** 
```
HTTP/1.1 401 Unauthorized
WWW-Authenticate: Bearer
```

##### Token wygasły
**Request:** Token JWT po upływie 24h

**Response:**
```
HTTP/1.1 401 Unauthorized
WWW-Authenticate: Bearer error="invalid_token", error_description="The token expired at '10/30/2025 10:00:00'"
```

#### 7.2.3 Błędy dostępu (403 Forbidden)

##### Cudzy zestaw
**Kontekst:** Użytkownik (UserId=123) próbuje edytować zestaw należący do UserId=456

**Response:**
```json
{
  "error": "Brak dostępu do tego zasobu"
}
```

**Logi:**
```
[Warning] Użytkownik 123 próbował edytować cudzy zestaw 1
```

#### 7.2.4 Błędy zasobu (404 Not Found)

##### Nieistniejący zestaw
**Request:** `PUT /api/tickets/123`

**Response:**
```json
{
  "error": "Zestaw nie został znaleziony"
}
```

#### 7.2.5 Błędy serwera (500 Internal Server Error)

##### Błąd bazy danych podczas transakcji
**Kontekst:** Utrata połączenia z SQL Server podczas DELETE/INSERT

**Response:**
```json
{
  "error": "Wystąpił błąd serwera",
  "traceId": "00-abc123..."
}
```

**Logi:**
```
[Error] Błąd podczas aktualizacji zestawu 123
Microsoft.Data.SqlClient.SqlException: A network-related or instance-specific error...
```

**Działanie:** Transakcja zostaje wycofana (rollback), dane pozostają nienaruszone

### 7.3 Middleware obsługi błędów

**ExceptionHandlingMiddleware** (istniejący) obsługuje:
- `ValidationException` → 400 Bad Request
- `UnauthorizedAccessException` → 401 Unauthorized
- `DbUpdateException` → 500 Internal Server Error
- `Exception` (catch-all) → 500 Internal Server Error

**Przykład implementacji:**
```csharp
catch (ValidationException ex)
{
    context.Response.StatusCode = StatusCodes.Status400BadRequest;
    await context.Response.WriteAsJsonAsync(new
    {
        errors = ex.Errors.GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray()
            )
    });
}
```

---

## 8. Rozważania dotyczące wydajności

### 8.1 Potencjalne wąskie gardła

#### 8.1.1 N+1 Query Problem
**Problem:** Pobieranie zestawów użytkownika w pętli dla sprawdzenia unikalności

**Rozwiązanie:** Eager loading z `.Include()`
```csharp
var existingTickets = await _context.Tickets
    .Where(t => t.UserId == currentUserId && t.Id != request.TicketId)
    .Include(t => t.Numbers) // Eager loading - 1 zapytanie zamiast N+1
    .ToListAsync(cancellationToken);
```

**Wynik:** 1 zapytanie SELECT z JOIN zamiast 1 + N zapytań

#### 8.1.2 Duża liczba zestawów użytkownika
**Problem:** Użytkownik ma 99 zestawów, sprawdzanie unikalności wymaga porównania z każdym

**Analiza:**
- Złożoność: O(n) gdzie n ≤ 100 (limit zestawów)
- Operacja w pamięci: sortowanie + porównanie tablic
- Czas: ~1-2ms dla 100 zestawów

**Optymalizacja (post-MVP):**
- Cache zestawów użytkownika w Redis (klucz: `user:{userId}:tickets`)
- Invalidacja cache przy CREATE/UPDATE/DELETE
- TTL: 1 godzina

#### 8.1.3 Transakcja DELETE + INSERT
**Problem:** DELETE wszystkich 6 liczb + INSERT 6 nowych liczb

**Analiza:**
- 2 operacje SQL w transakcji
- Wykorzystanie indeksu `IX_TicketNumbers_TicketId` dla DELETE
- Bulk INSERT 6 rekordów (1 zapytanie)

**Alternatywa (bardziej złożona):**
- UPDATE istniejących rekordów zamiast DELETE/INSERT
- Wymaga mapowania Position → Number
- Nie daje znaczącej poprawy wydajności dla 6 rekordów

### 8.2 Strategie optymalizacji

#### 8.2.1 Indeksy bazy danych (już zaimplementowane)
```sql
-- Optymalizacja filtrowania po UserId
CREATE INDEX IX_Tickets_UserId ON Tickets(UserId);

-- Optymalizacja JOIN i DELETE
CREATE INDEX IX_TicketNumbers_TicketId ON TicketNumbers(TicketId);

-- Zapewnienie unikalności (TicketId, Position)
CREATE UNIQUE INDEX UQ_TicketNumbers_TicketPosition 
ON TicketNumbers(TicketId, Position);
```

#### 8.2.2 Entity Framework Core - śledzenie zmian
```csharp
// Dla read-only query (sprawdzanie unikalności)
var existingTickets = await _context.Tickets
    .Where(t => t.UserId == currentUserId && t.Id != request.TicketId)
    .Include(t => t.Numbers)
    .AsNoTracking() // Wyłącz change tracking - lepsza wydajność
    .ToListAsync(cancellationToken);
```

**Korzyść:** ~20-30% szybsze zapytania read-only

#### 8.2.3 Compiled Queries (dla często używanych zapytań)
```csharp
private static readonly Func<AppDbContext, int, Task<Ticket?>> GetTicketById =
    EF.CompileAsyncQuery((AppDbContext context, int id) =>
        context.Tickets
            .Include(t => t.Numbers)
            .FirstOrDefault(t => t.Id == id)
    );
```

**Korzyść:** Zapytanie kompilowane raz, używane wielokrotnie

#### 8.2.4 Response Caching (post-MVP)
```csharp
// Cache GET /api/tickets (lista zestawów)
// Invalidacja przy PUT/POST/DELETE
app.UseResponseCaching();
```

### 8.3 Oczekiwana wydajność

#### Metryki docelowe (NFR-002)
- **CRUD ≤ 500ms** (95 percentyl)

#### Szacowany czas wykonania (PUT /api/tickets/{id})
| Operacja | Czas | Uwagi |
|----------|------|-------|
| JWT validation | ~5ms | Middleware ASP.NET Core |
| FluentValidation | ~2ms | Walidacja 6 liczb |
| SELECT ticket by ID | ~10ms | 1 zapytanie z JOIN, indeks PK |
| SELECT user's tickets | ~20ms | 1 zapytanie z JOIN, indeks IX_Tickets_UserId, ~100 zestawów |
| Sprawdzanie unikalności | ~2ms | Operacja w pamięci, ~100 porównań |
| BEGIN TRANSACTION | ~2ms | SQL Server |
| DELETE 6 numbers | ~5ms | Indeks IX_TicketNumbers_TicketId |
| INSERT 6 numbers | ~5ms | Bulk insert |
| COMMIT TRANSACTION | ~5ms | SQL Server |
| Serializacja JSON | ~2ms | System.Text.Json |
| **TOTAL** | **~58ms** | **Znacznie poniżej limitu 500ms** |

#### Worst-case scenario
- Użytkownik z 100 zestawami
- Baza danych pod obciążeniem
- Szacowany czas: ~150-200ms
- **Nadal poniżej wymagania 500ms**

### 8.4 Monitoring wydajności

#### Logi wydajnościowe (Serilog)
```csharp
using var activity = _logger.BeginScope(new Dictionary<string, object>
{
    ["TicketId"] = request.TicketId,
    ["UserId"] = currentUserId,
    ["Operation"] = "UpdateTicket"
});

var stopwatch = Stopwatch.StartNew();

// ... operacje ...

stopwatch.Stop();
_logger.LogInformation(
    "Zestaw {TicketId} zaktualizowany w {ElapsedMs}ms",
    request.TicketId,
    stopwatch.ElapsedMilliseconds
);
```

#### Metryki do monitorowania
- Średni czas odpowiedzi (p50, p95, p99)
- Liczba błędów 400/403/404/500
- Liczba prób edycji cudzych zestawów (security metric)
- Czas wykonania zapytań SQL (EF Core logging)

---

## 9. Etapy wdrożenia

### 9.1 Przygotowanie struktury plików

#### Krok 1: Utworzenie katalogów
```
src/server/LottoTM.Server.Api/Features/Tickets/
├── UpdateTicket/
│   ├── Contracts.cs       # DTOs (Request, Response, ValidationErrorResponse)
│   ├── Endpoint.cs        # Minimal API endpoint definition
│   ├── Handler.cs         # MediatR request handler (business logic)
│   └── Validator.cs       # FluentValidation rules
```

**Powód:** Vertical Slice Architecture - wszystkie pliki związane z jednym feature w jednym miejscu

### 9.2 Implementacja Contracts (DTOs)

#### Krok 2: Utworzenie `Features/Tickets/UpdateTicket/Contracts.cs`

```csharp
using MediatR;

namespace LottoTM.Server.Api.Features.Tickets.UpdateTicket;

public class Contracts
{
    /// <summary>
    /// Request do aktualizacji zestawu liczb.
    /// Zawiera ID zestawu (z route) i nowe liczby (z body).
    /// </summary>
    public record Request(int TicketId, int[] Numbers) : IRequest<Response>;
    
    /// <summary>
    /// Response dla udanej aktualizacji.
    /// </summary>
    public record Response(string Message);
    
    /// <summary>
    /// Response dla błędów walidacji.
    /// </summary>
    public record ValidationErrorResponse(Dictionary<string, string[]> Errors);
}
```

**Wyjaśnienie:**
- `Request` implementuje `IRequest<Response>` dla MediatR
- `TicketId` pochodzi z route parameter `{id}`
- `Numbers` pochodzi z request body
- `ValidationErrorResponse` używany dla 400 Bad Request

### 9.3 Implementacja Validator

#### Krok 3: Utworzenie `Features/Tickets/UpdateTicket/Validator.cs`

```csharp
using FluentValidation;

namespace LottoTM.Server.Api.Features.Tickets.UpdateTicket;

public class Validator : AbstractValidator<Contracts.Request>
{
    public Validator()
    {
        // Walidacja pola Numbers
        RuleFor(x => x.Numbers)
            .NotNull()
            .WithMessage("Liczby są wymagane");
        
        // Walidacja długości tablicy
        RuleFor(x => x.Numbers)
            .Must(numbers => numbers != null && numbers.Length == 6)
            .WithMessage("Wymagane dokładnie 6 liczb")
            .When(x => x.Numbers != null);
        
        // Walidacja zakresu liczb (1-49)
        RuleFor(x => x.Numbers)
            .Must(numbers => numbers.All(n => n >= 1 && n <= 49))
            .WithMessage("Wszystkie liczby muszą być w zakresie 1-49")
            .When(x => x.Numbers != null && x.Numbers.Length == 6);
        
        // Walidacja unikalności liczb w tablicy
        RuleFor(x => x.Numbers)
            .Must(numbers => numbers.Distinct().Count() == numbers.Length)
            .WithMessage("Liczby muszą być unikalne")
            .When(x => x.Numbers != null && x.Numbers.Length == 6);
        
        // Walidacja ID zestawu
        RuleFor(x => x.TicketId)
            .GreaterThan(0)
            .WithMessage("ID zestawu musi być liczbą większą od 0");
    }
}
```

**Wyjaśnienie:**
- Walidacja wielopoziomowa z `.When()` - unikamy NullReferenceException
- Kolejność walidacji: null → długość → zakres → unikalność
- FluentValidation automatycznie agreguje błędy do słownika

### 9.4 Implementacja Handler (Business Logic)

#### Krok 4: Utworzenie `Features/Tickets/UpdateTicket/Handler.cs`

```csharp
using FluentValidation;
using LottoTM.Server.Api.Entities;
using LottoTM.Server.Api.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LottoTM.Server.Api.Features.Tickets.UpdateTicket;

public class UpdateTicketHandler : IRequestHandler<Contracts.Request, IResult>
{
    private readonly AppDbContext _context;
    private readonly IValidator<Contracts.Request> _validator;
    private readonly ILogger<UpdateTicketHandler> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UpdateTicketHandler(
        AppDbContext context,
        IValidator<Contracts.Request> validator,
        ILogger<UpdateTicketHandler> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _validator = validator;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IResult> Handle(Contracts.Request request, CancellationToken cancellationToken)
    {
        // 1. Walidacja FluentValidation
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray()
                );
            
            _logger.LogWarning(
                "Walidacja nie powiodła się dla aktualizacji zestawu {TicketId}: {Errors}",
                request.TicketId,
                string.Join(", ", errors.Keys)
            );
            
            return Results.BadRequest(new Contracts.ValidationErrorResponse(errors));
        }

        // 2. Pobranie UserId z JWT
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated != true)
        {
            return Results.Unauthorized();
        }

        var userIdClaim = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out int currentUserId))
        {
            _logger.LogError("Nie można sparsować UserId z JWT: {UserIdClaim}", userIdClaim);
            return Results.Unauthorized();
        }

        // 3. Sprawdzenie czy zestaw istnieje + eager loading liczb
        var ticket = await _context.Tickets
            .Include(t => t.Numbers)
            .FirstOrDefaultAsync(t => t.Id == request.TicketId, cancellationToken);

        if (ticket == null)
        {
            _logger.LogWarning(
                "Zestaw {TicketId} nie został znaleziony",
                request.TicketId
            );
            return Results.NotFound(new { error = "Zestaw nie został znaleziony" });
        }

        // 4. Sprawdzenie własności zasobu
        if (ticket.UserId != currentUserId)
        {
            _logger.LogWarning(
                "Użytkownik {UserId} próbował edytować cudzy zestaw {TicketId} (właściciel: {OwnerId})",
                currentUserId,
                request.TicketId,
                ticket.UserId
            );
            return Results.Forbid(); // 403 Forbidden
        }

        // 5. Sprawdzenie unikalności zestawu (wykluczając edytowany zestaw)
        var sortedNewNumbers = request.Numbers.OrderBy(n => n).ToArray();

        var existingTickets = await _context.Tickets
            .Where(t => t.UserId == currentUserId && t.Id != request.TicketId)
            .Include(t => t.Numbers)
            .AsNoTracking() // Read-only query
            .ToListAsync(cancellationToken);

        foreach (var existingTicket in existingTickets)
        {
            var sortedExisting = existingTicket.Numbers
                .OrderBy(n => n.Number)
                .Select(n => n.Number)
                .ToArray();

            if (sortedNewNumbers.SequenceEqual(sortedExisting))
            {
                _logger.LogWarning(
                    "Użytkownik {UserId} próbował zapisać duplikat zestawu (zestaw {ExistingTicketId} już zawiera te liczby)",
                    currentUserId,
                    existingTicket.Id
                );
                
                return Results.BadRequest(new Contracts.ValidationErrorResponse(
                    new Dictionary<string, string[]>
                    {
                        { "duplicate", new[] { "Taki zestaw już istnieje w Twoich zapisanych zestawach" } }
                    }
                ));
            }
        }

        // 6. Aktualizacja zestawu w transakcji
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // Usuń stare liczby
            _context.TicketNumbers.RemoveRange(ticket.Numbers);

            // Dodaj nowe liczby
            for (byte i = 0; i < request.Numbers.Length; i++)
            {
                var ticketNumber = new TicketNumber
                {
                    TicketId = ticket.Id,
                    Number = request.Numbers[i],
                    Position = (byte)(i + 1)
                };
                _context.TicketNumbers.Add(ticketNumber);
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Użytkownik {UserId} zaktualizował zestaw {TicketId} na liczby [{Numbers}]",
                currentUserId,
                request.TicketId,
                string.Join(", ", request.Numbers.OrderBy(n => n))
            );

            return Results.Ok(new Contracts.Response("Zestaw zaktualizowany pomyślnie"));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            
            _logger.LogError(
                ex,
                "Błąd podczas aktualizacji zestawu {TicketId} dla użytkownika {UserId}",
                request.TicketId,
                currentUserId
            );
            
            throw; // ExceptionHandlingMiddleware obsłuży 500
        }
    }
}
```

**Wyjaśnienie:**
- Handler zwraca `IResult` (Minimal API Results) zamiast `Response`
- Wszystkie scenariusze błędów obsłużone (401, 403, 404, 400, 500)
- Transakcja zapewnia atomowość operacji DELETE + INSERT
- Szczegółowe logowanie dla auditingu i debugowania
- `.AsNoTracking()` dla query read-only (lepsza wydajność)

### 9.5 Implementacja Endpoint (Minimal API)

#### Krok 5: Utworzenie `Features/Tickets/UpdateTicket/Endpoint.cs`

```csharp
using MediatR;

namespace LottoTM.Server.Api.Features.Tickets.UpdateTicket;

public static class Endpoint
{
    /// <summary>
    /// Rejestracja endpointu PUT /api/tickets/{id}
    /// </summary>
    public static void AddEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPut("api/tickets/{id:int}", async (
            int id,
            UpdateTicketRequest body,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var request = new Contracts.Request(id, body.Numbers);
            var result = await mediator.Send(request, cancellationToken);
            return result;
        })
        .WithName("UpdateTicket")
        .RequireAuthorization() // Wymaga JWT
        .Produces<Contracts.Response>(StatusCodes.Status200OK)
        .Produces<Contracts.ValidationErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status500InternalServerError)
        .WithOpenApi(operation =>
        {
            operation.Summary = "Aktualizacja zestawu liczb";
            operation.Description = "Edycja istniejącego zestawu 6 liczb LOTTO (1-49). Wymaga autoryzacji JWT. Użytkownik może edytować tylko własne zestawy.";
            return operation;
        });
    }

    /// <summary>
    /// DTO dla request body (deserializacja JSON)
    /// </summary>
    public record UpdateTicketRequest(int[] Numbers);
}
```

**Wyjaśnienie:**
- `{id:int}` - route constraint dla INT
- `RequireAuthorization()` - wymusza JWT authentication
- `UpdateTicketRequest` - pomocniczy DTO dla deserializacji body
- `Produces<T>()` - dokumentacja OpenAPI (Swagger)
- Handler zwraca `IResult`, więc endpoint bezpośrednio zwraca wynik

### 9.6 Rejestracja w Program.cs

#### Krok 6: Dodanie endpointu do `Program.cs`

```csharp
// ... istniejący kod ...

// Rejestracja IHttpContextAccessor (potrzebny dla Handler)
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// ... middleware configuration ...

// Rejestracja endpointów
LottoTM.Server.Api.Features.ApiVersion.Endpoint.AddEndpoint(app);
LottoTM.Server.Api.Features.Tickets.UpdateTicket.Endpoint.AddEndpoint(app); // NOWY

await app.RunAsync();
```

**Lokalizacja:** Dodaj przed `await app.RunAsync();` (linia ~105)

### 9.7 Testowanie

#### Krok 7: Testy jednostkowe (opcjonalne dla MVP)

##### Utworzenie `tests/server/LottoTM.Server.Api.Tests/Features/Tickets/UpdateTicketTests.cs`

```csharp
using FluentAssertions;
using LottoTM.Server.Api.Entities;
using LottoTM.Server.Api.Features.Tickets.UpdateTicket;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LottoTM.Server.Api.Tests.Features.Tickets;

public class UpdateTicketTests : IDisposable
{
    private readonly AppDbContext _context;

    public UpdateTicketTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
    }

    [Fact]
    public async Task Validator_ShouldFail_WhenNumbersIsNull()
    {
        // Arrange
        var validator = new Validator();
        var request = new Contracts.Request(1, null!);

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Numbers");
    }

    [Fact]
    public async Task Validator_ShouldFail_WhenNumbersCountIsNot6()
    {
        // Arrange
        var validator = new Validator();
        var request = new Contracts.Request(1, new[] { 1, 2, 3, 4, 5 });

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("dokładnie 6 liczb"));
    }

    [Fact]
    public async Task Validator_ShouldFail_WhenNumbersOutOfRange()
    {
        // Arrange
        var validator = new Validator();
        var request = new Contracts.Request(1, new[] { 0, 2, 3, 4, 5, 50 });

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("1-49"));
    }

    [Fact]
    public async Task Validator_ShouldFail_WhenNumbersNotUnique()
    {
        // Arrange
        var validator = new Validator();
        var request = new Contracts.Request(1, new[] { 1, 2, 3, 3, 5, 6 });

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("unikalne"));
    }

    [Fact]
    public async Task Validator_ShouldPass_WhenRequestIsValid()
    {
        // Arrange
        var validator = new Validator();
        var request = new Contracts.Request(1, new[] { 7, 15, 22, 33, 38, 45 });

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
```

#### Krok 8: Testy integracyjne z WebApplicationFactory

```csharp
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace LottoTM.Server.Api.Tests.Features.Tickets;

public class UpdateTicketIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public UpdateTicketIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task UpdateTicket_ShouldReturn401_WhenNoJwtToken()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var request = new { numbers = new[] { 7, 15, 22, 33, 38, 45 } };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/tickets/{ticketId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ... więcej testów dla 200, 400, 403, 404 ...
}
```

#### Krok 9: Testy manualne (Swagger / Postman)

##### Swagger UI
1. Uruchomić aplikację: `dotnet run --project src/server/LottoTM.Server.Api`
2. Otworzyć: `https://localhost:7000/swagger`
3. Użyć przycisku "Authorize" → wkleić JWT token
4. Przetestować endpoint `PUT /api/tickets/{id}`

##### Postman Collection
```json
{
  "name": "PUT Update Ticket",
  "request": {
    "method": "PUT",
    "header": [
      {
        "key": "Authorization",
        "value": "Bearer {{jwt_token}}"
      }
    ],
    "body": {
      "mode": "raw",
      "raw": "{\n  \"numbers\": [7, 15, 22, 33, 38, 45]\n}",
      "options": {
        "raw": {
          "language": "json"
        }
      }
    },
    "url": {
      "raw": "https://localhost:7000/api/tickets/123",
      "protocol": "https",
      "host": ["localhost"],
      "port": "7000",
      "path": ["api", "tickets", "123"]
    }
  }
}
```

### 9.8 Checklist wdrożenia

#### Pre-deployment
- [ ] Kod przeszedł code review
- [ ] Testy jednostkowe przechodzą (jeśli zaimplementowane)
- [ ] Testy integracyjne przechodzą
- [ ] Walidacja działa poprawnie (400 dla nieprawidłowych danych)
- [ ] Autoryzacja działa poprawnie (401, 403)
- [ ] Transakcja działa atomowo (rollback w przypadku błędu)
- [ ] Logowanie jest kompletne (info, warning, error)
- [ ] Dokumentacja OpenAPI/Swagger jest aktualna

#### Deployment
- [ ] Migracje bazy danych (jeśli potrzebne - nie dotyczy MVP)
- [ ] Aplikacja uruchomiona na środowisku testowym
- [ ] Testy manualne przeprowadzone (Postman/Swagger)
- [ ] Monitoring wydajności aktywny (Serilog)

#### Post-deployment
- [ ] Sprawdzenie logów pod kątem błędów
- [ ] Weryfikacja metryk wydajności (czas odpowiedzi < 500ms)
- [ ] Testy end-to-end przez QA/użytkowników
- [ ] Dokumentacja API zaktualizowana

---

## 10. Podsumowanie

### 10.1 Kluczowe punkty implementacji

1. **Vertical Slice Architecture** - wszystkie pliki w `Features/Tickets/UpdateTicket/`
2. **Walidacja wielopoziomowa:**
   - FluentValidation (format danych)
   - Business logic validation (unikalność, własność)
3. **Bezpieczeństwo:**
   - JWT authentication (middleware)
   - Ownership verification (handler)
   - Parametryzowane zapytania (EF Core)
4. **Transakcyjność:** DELETE + INSERT w jednej transakcji
5. **Wydajność:** Eager loading, AsNoTracking, indeksy
6. **Observability:** Szczegółowe logowanie (Serilog)

### 10.2 Zgodność z wymaganiami

| Wymaganie | Status | Implementacja |
|-----------|--------|---------------|
| F-TICKET-003 | ✅ | Edycja zestawu z walidacją |
| NFR-002 (≤500ms) | ✅ | ~58ms szacowany czas |
| NFR-005 (Security) | ✅ | JWT + ownership check |
| NFR-008 (SQL Injection) | ✅ | EF Core parametrized queries |
| F-AUTH-004 (Isolation) | ✅ | Filtrowanie po UserId |

### 10.3 Następne kroki

Po wdrożeniu PUT /api/tickets/{id}:
1. **DELETE /api/tickets/{id}** - usuwanie zestawu
2. **POST /api/tickets** - dodawanie zestawu ręcznie
3. **POST /api/tickets/generate-random** - generator losowy
4. **POST /api/tickets/generate-system** - generator systemowy (9 zestawów)
5. **POST /api/verification/check** - weryfikacja wygranych

---

**Koniec dokumentu**
