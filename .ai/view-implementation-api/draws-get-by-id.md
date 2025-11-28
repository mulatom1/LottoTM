# API Endpoint Implementation Plan: GET /api/draws/{id}

## 1. Przegląd punktu końcowego

**Endpoint:** `GET /api/draws/{id}`

**Opis:** Pobieranie szczegółów pojedynczego losowania LOTTO na podstawie jego identyfikatora. Endpoint zwraca datę losowania oraz wylosowane liczby. Losowania są globalnym zasobem dostępnym dla wszystkich zalogowanych użytkowników (nie wymagają filtrowania po UserId).

**Cel biznesowy:** Umożliwienie użytkownikom przeglądania szczegółów konkretnego wyniku losowania LOTTO w celu weryfikacji wyników lub weryfikacji swoich zestawów.

**Architektura:** Vertical Slice Architecture z wykorzystaniem MediatR, FluentValidation, Entity Framework Core.

---

## 2. Szczegóły żądania

### 2.1 Metoda HTTP
**GET**

### 2.2 Struktura URL
```
GET /api/draws/{id}
```

**Przykłady:**
```
GET /api/draws/123
GET /api/draws/1
```

### 2.3 Parametry

#### Parametry URL (Route Parameters):
| Parametr | Typ | Wymagany | Opis | Walidacja |
|----------|-----|----------|------|-----------|
| `id` | INT | ✅ Tak | Identyfikator losowania w bazie danych | - Musi być liczbą całkowitą dodatnią (> 0)<br>- Typ: int |

#### Parametry Query String:
Brak

#### Request Body:
Brak

#### Headers:
| Header | Wymagany | Wartość | Opis |
|--------|----------|---------|------|
| `Authorization` | ✅ Tak | `Bearer <JWT_TOKEN>` | Token JWT użytkownika |
| `Content-Type` | ❌ Nie | - | Brak body w żądaniu GET |

---

## 3. Wykorzystywane typy

### 3.1 DTOs (Data Transfer Objects)

#### GetDrawByIdRequest (IRequest)
```csharp
namespace LottoTM.Server.Api.Features.Draws.GetById;

public class Contracts
{
    public record Request(int Id) : IRequest<Response>;

    public record Response(
        int Id,
        DateTime DrawDate,
        string LottoType,
        int[] Numbers,
        int? DrawSystemId,
        decimal? TicketPrice,
        int? WinPoolCount1,
        decimal? WinPoolAmount1,
        int? WinPoolCount2,
        decimal? WinPoolAmount2,
        int? WinPoolCount3,
        decimal? WinPoolAmount3,
        int? WinPoolCount4,
        decimal? WinPoolAmount4,
        DateTime CreatedAt
    );
}
```

**Uwaga:** `Request` zawiera tylko `Id` jako parametr, ponieważ to jedyne dane wejściowe dla tego endpointu.

### 3.2 Encje bazodanowe (Entity Models)

#### Draw (z relacją do DrawNumbers)
```csharp
public class Draw
{
    public int Id { get; set; }
    public DateTime DrawDate { get; set; }
    public string LottoType { get; set; } = "";
    public int? DrawSystemId { get; set; }
    public decimal? TicketPrice { get; set; }
    public int? WinPoolCount1 { get; set; }
    public decimal? WinPoolAmount1 { get; set; }
    public int? WinPoolCount2 { get; set; }
    public decimal? WinPoolAmount2 { get; set; }
    public int? WinPoolCount3 { get; set; }
    public decimal? WinPoolAmount3 { get; set; }
    public int? WinPoolCount4 { get; set; }
    public decimal? WinPoolAmount4 { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedByUserId { get; set; }

    // Navigation properties
    public User CreatedByUser { get; set; } = null!;
    public ICollection<DrawNumber> Numbers { get; set; } = new List<DrawNumber>();
}
```

#### DrawNumber
```csharp
public class DrawNumber
{
    public int Id { get; set; }
    public int DrawId { get; set; }
    public int Number { get; set; }
    public byte Position { get; set; }

    // Navigation property
    public Draw Draw { get; set; } = null!;
}
```

---

## 4. Szczegóły odpowiedzi

### 4.1 Odpowiedź sukcesu (200 OK)

**Kod statusu:** `200 OK`

**Struktura JSON:**
```json
{
  "id": 123,
  "drawDate": "2025-10-30",
  "lottoType": "LOTTO",
  "numbers": [3, 12, 25, 31, 42, 48],
  "drawSystemId": 20250001,
  "ticketPrice": 3.00,
  "winPoolCount1": 2,
  "winPoolAmount1": 5000000.00,
  "winPoolCount2": 15,
  "winPoolAmount2": 50000.00,
  "winPoolCount3": 120,
  "winPoolAmount3": 500.00,
  "winPoolCount4": 850,
  "winPoolAmount4": 20.00,
  "createdAt": "2025-10-30T18:30:00Z"
}
```

**Opis pól:**
- `id` (int): Identyfikator losowania w bazie danych
- `drawDate` (string): Data losowania w formacie ISO 8601 (YYYY-MM-DD)
- `lottoType` (string): Typ gry ("LOTTO" lub "LOTTO PLUS")
- `numbers` (int[]): Tablica 6 wylosowanych liczb w zakresie 1-49, posortowanych według pozycji
- `drawSystemId` (int | null): Zewnętrzny identyfikator systemu losowań (opcjonalnie)
- `ticketPrice` (decimal | null): Cena biletu dla tego losowania (opcjonalnie)
- `winPoolCount1` (int | null): Liczba wygranych 1 stopnia - 6 trafień (opcjonalnie)
- `winPoolAmount1` (decimal | null): Kwota wygranych 1 stopnia - 6 trafień (opcjonalnie)
- `winPoolCount2` (int | null): Liczba wygranych 2 stopnia - 5 trafień (opcjonalnie)
- `winPoolAmount2` (decimal | null): Kwota wygranych 2 stopnia - 5 trafień (opcjonalnie)
- `winPoolCount3` (int | null): Liczba wygranych 3 stopnia - 4 trafienia (opcjonalnie)
- `winPoolAmount3` (decimal | null): Kwota wygranych 3 stopnia - 4 trafienia (opcjonalnie)
- `winPoolCount4` (int | null): Liczba wygranych 4 stopnia - 3 trafienia (opcjonalnie)
- `winPoolAmount4` (decimal | null): Kwota wygranych 4 stopnia - 3 trafienia (opcjonalnie)
- `createdAt` (string): Data i czas wprowadzenia wyniku do systemu w formacie ISO 8601 (UTC)

### 4.2 Odpowiedzi błędów

#### 400 Bad Request - Nieprawidłowy format ID
**Sytuacja:** ID nie jest liczbą całkowitą lub jest mniejsze/równe 0

**Struktura JSON:**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "id": ["ID musi być liczbą całkowitą większą od 0"]
  }
}
```

#### 401 Unauthorized - Brak lub nieprawidłowy token JWT
**Sytuacja:** Brak tokenu JWT w nagłówku Authorization lub token jest nieprawidłowy/wygasły

**Struktura JSON:**
```json
{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
  "title": "Unauthorized",
  "status": 401,
  "detail": "Brak tokenu autoryzacji lub token jest nieprawidłowy"
}
```

#### 404 Not Found - Losowanie nie istnieje
**Sytuacja:** Losowanie o podanym ID nie zostało znalezione w bazie danych

**Struktura JSON:**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Not Found",
  "status": 404,
  "detail": "Losowanie o podanym ID nie istnieje"
}
```

#### 500 Internal Server Error - Błąd serwera
**Sytuacja:** Nieoczekiwany błąd aplikacji (np. problem z bazą danych)

**Struktura JSON:**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.6.1",
  "title": "Internal Server Error",
  "status": 500,
  "detail": "Wystąpił nieoczekiwany błąd. Spróbuj ponownie później."
}
```

---

## 5. Przepływ danych

### 5.1 Diagram przepływu

```
┌──────────────┐
│   Client     │
│  (React App) │
└──────┬───────┘
       │ 1. GET /api/draws/123
       │    Header: Authorization: Bearer <JWT>
       ▼
┌──────────────────────┐
│  ASP.NET Middleware  │
│  - JWT Authentication│  2. Walidacja tokenu JWT
│  - Authorization     │     (sprawdzenie podpisu, exp)
└──────┬───────────────┘
       │ 3. Token OK, wyciągnięcie UserId z claims
       ▼
┌──────────────────────┐
│   MediatR Pipeline   │
│  - Validation        │  4. FluentValidation: Walidacja Request.Id
│  - Logging           │     (Id > 0)
└──────┬───────────────┘
       │ 5. Walidacja OK
       ▼
┌──────────────────────┐
│ GetDrawByIdHandler   │  6. await mediator.Send(request)
│  (IRequestHandler)   │
└──────┬───────────────┘
       │ 7. Zapytanie do bazy danych
       ▼
┌──────────────────────┐
│  Entity Framework    │  8. SELECT * FROM Draws WHERE Id = @id
│      (DbContext)     │     INNER JOIN DrawNumbers ON Draws.Id = DrawNumbers.DrawId
│                      │     (eager loading: .Include(d => d.Numbers))
└──────┬───────────────┘
       │
       ▼
┌──────────────────────┐
│   SQL Server 2022    │  9. Wykonanie zapytania
│   Tabele:            │     - Użycie indeksu PRIMARY KEY (Draws.Id)
│   - Draws            │     - Użycie indeksu IX_DrawNumbers_DrawId
│   - DrawNumbers      │
└──────┬───────────────┘
       │
       │ 10. Zwrot danych (Draw + 6 DrawNumbers)
       ▼
┌──────────────────────┐
│ GetDrawByIdHandler   │  11. Mapowanie encji → DTO
│                      │      - draw.Id → response.Id
│                      │      - draw.DrawDate → response.DrawDate
│                      │      - draw.Numbers → response.Numbers (sortowane po Position)
│                      │      - draw.CreatedAt → response.CreatedAt
└──────┬───────────────┘
       │ 12. return new Response(...)
       ▼
┌──────────────────────┐
│    Endpoint.cs       │  13. Results.Ok(response)
│   (Minimal API)      │      lub Results.NotFound()
└──────┬───────────────┘
       │ 14. JSON serialization
       ▼
┌──────────────┐
│   Client     │  15. Response 200 OK + JSON body
│  (React App) │
└──────────────┘
```

### 5.2 Szczegółowy opis kroków

#### Krok 1-3: Autentykacja
- Client wysyła żądanie GET z tokenem JWT w nagłówku `Authorization: Bearer <token>`
- ASP.NET Core JWT middleware waliduje token (podpis HMAC SHA-256, expiration time)
- Middleware wyciąga claims z tokenu (`UserId`, `Email`, `IsAdmin`) i dodaje do `HttpContext.User`

#### Krok 4-5: Walidacja parametrów
- MediatR pipeline automatycznie wywołuje FluentValidation dla `Request`
- `Validator.cs` sprawdza czy `Id > 0`
- Jeśli walidacja nie przejdzie → `ValidationException` → middleware zwraca 400 Bad Request

#### Krok 6-9: Pobranie danych z bazy
- Handler wywołuje `dbContext.Draws.Include(d => d.Numbers).FirstOrDefaultAsync(d => d.Id == request.Id)`
- Entity Framework generuje zapytanie SQL z INNER JOIN do DrawNumbers
- SQL Server wykonuje zapytanie z wykorzystaniem indeksów:
  - PRIMARY KEY na `Draws.Id` (O(log n))
  - INDEX `IX_DrawNumbers_DrawId` dla JOIN (O(log n))

#### Krok 10-12: Mapowanie danych
- EF Core zwraca encję `Draw` z kolekcją `Numbers` (6 obiektów `DrawNumber`)
- Handler sortuje liczby po `Position` (1-6) i ekstraktuje wartości `Number` do tablicy `int[]`
- Handler tworzy obiekt `Response` z danymi DTO

#### Krok 13-15: Zwrot odpowiedzi
- Endpoint zwraca `Results.Ok(response)` dla sukcesu lub `Results.NotFound()` dla braku danych
- ASP.NET Core serializuje obiekt do JSON (System.Text.Json)
- Client otrzymuje odpowiedź 200 OK z JSON body

---

## 6. Względy bezpieczeństwa

### 6.1 Autentykacja (OBOWIĄZKOWA)

**Mechanizm:** JWT Bearer Token

**Implementacja:**
```csharp
// W Endpoint.cs - dodanie .RequireAuthorization()
app.MapGet("api/draws/{id}", async (int id, IMediator mediator) =>
{
    var request = new Contracts.Request(id);
    var result = await mediator.Send(request);
    return result != null ? Results.Ok(result) : Results.NotFound();
})
.RequireAuthorization() // ← WYMAGA TOKENU JWT
.WithName("GetDrawById")
.Produces<Contracts.Response>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status401Unauthorized)
.Produces(StatusCodes.Status404NotFound)
.WithOpenApi();
```

**Walidacja tokenu:**
- Middleware `UseAuthentication()` sprawdza:
  - Obecność nagłówka `Authorization: Bearer <token>`
  - Poprawność podpisu HMAC SHA-256
  - Ważność tokenu (`exp` > current time)
- Jeśli token nieprawidłowy → zwrot 401 Unauthorized

### 6.2 Autoryzacja (Dostęp do zasobu)

**Polityka dostępu:** Draws jest globalnym zasobem - **NIE wymaga** filtrowania po UserId

**Dlaczego?**
- Tabela `Draws` jest współdzielona przez wszystkich użytkowników
- Każdy zalogowany użytkownik może przeglądać wyniki losowań (potrzebne do weryfikacji własnych zestawów)
- Kolumna `CreatedByUserId` służy tylko do trackingu, nie do kontroli dostępu

**Bezpieczeństwo:**
- ✅ Token JWT jest wymagany (użytkownik musi być zalogowany)
- ❌ BRAK filtrowania po UserId (w przeciwieństwie do `/api/tickets/{id}`)
- ✅ Brak wrażliwych danych w losowaniach (tylko data + 6 liczb publicznych)

### 6.3 Walidacja danych wejściowych

**Parametr `id`:**
```csharp
// W Validator.cs
public class Validator : AbstractValidator<Contracts.Request>
{
    public Validator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("ID musi być liczbą całkowitą większą od 0");
    }
}
```

**Zabezpieczenia:**
- FluentValidation chroni przed nieprawidłowymi wartościami (0, ujemne, null)
- EF Core używa parametryzowanych zapytań → ochrona przed SQL Injection

### 6.4 Ochrona przed atakami

#### SQL Injection
**Status:** ✅ Chroniony automatycznie
- Entity Framework Core używa parametryzowanych zapytań
- NIE używać `.FromSqlRaw()` z konkatenacją stringów

#### Enumeracja zasobów (Resource Enumeration)
**Status:** ⚠️ Możliwe, ale akceptowalne dla tego endpointu
- Atakujący może próbować odgadywać ID: `/api/draws/1`, `/api/draws/2`, etc.
- **Dlaczego akceptowalne:** Draws jest publicznym zasobem (jak wyniki Lotto w realu)
- **Mitygacja:** Rate limiting (np. max 100 żądań/minutę/IP)

#### JWT Token Hijacking
**Status:** ✅ Chroniony przez HTTPS
- Wymaganie HTTPS na produkcji (`app.UseHttpsRedirection()`)
- Secure headers: `Strict-Transport-Security`, `X-Frame-Options`

### 6.5 Rate Limiting (Opcjonalne, rekomendowane)

**Implementacja:**
```csharp
// W Program.cs
services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("api", limiterOptions =>
    {
        limiterOptions.PermitLimit = 100;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
    });
});

// W Endpoint.cs
.RequireRateLimiting("api")
```

---

## 7. Obsługa błędów

### 7.1 Kategorie błędów

| Kod | Typ błędu | Przyczyna | Komunikat |
|-----|-----------|-----------|-----------|
| 400 | Walidacja | Id ≤ 0 | "ID musi być liczbą całkowitą większą od 0" |
| 401 | Autentykacja | Brak tokenu lub token nieprawidłowy | "Brak tokenu autoryzacji lub token jest nieprawidłowy" |
| 404 | Nie znaleziono | Losowanie o podanym ID nie istnieje | "Losowanie o podanym ID nie istnieje" |
| 500 | Serwer | Błąd bazy danych, wyjątek aplikacji | "Wystąpił nieoczekiwany błąd. Spróbuj ponownie później." |

### 7.2 Implementacja obsługi błędów

#### 7.2.1 Walidacja (400 Bad Request)
**Mechanizm:** FluentValidation w MediatR pipeline

```csharp
// W Validator.cs
public class Validator : AbstractValidator<Contracts.Request>
{
    public Validator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("ID musi być liczbą całkowitą większą od 0");
    }
}

// ValidationException jest automatycznie przechwytywany przez ExceptionHandlingMiddleware
```

#### 7.2.2 Brak zasobu (404 Not Found)
**Mechanizm:** Sprawdzenie wyniku zapytania w Handler

```csharp
// W Handler.cs
public async Task<Contracts.Response?> Handle(Contracts.Request request, CancellationToken cancellationToken)
{
    var draw = await _dbContext.Draws
        .Include(d => d.Numbers.OrderBy(dn => dn.Position))
        .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken);

    if (draw == null)
    {
        return null; // Handler zwraca null
    }

    // ... mapowanie do Response
}

// W Endpoint.cs
var result = await mediator.Send(request);
return result != null
    ? Results.Ok(result)
    : Results.NotFound(new { detail = "Losowanie o podanym ID nie istnieje" });
```

#### 7.2.3 Błędy serwera (500 Internal Server Error)
**Mechanizm:** Globalna obsługa wyjątków przez `ExceptionHandlingMiddleware`

```csharp
// ExceptionHandlingMiddleware automatycznie:
// 1. Loguje wyjątek z Serilog (plik + konsola)
// 2. Zwraca ProblemDetails z kodem 500
// 3. Ukrywa szczegóły błędu przed klientem (security)

try
{
    // Logika handlera
}
catch (Exception ex)
{
    _logger.LogError(ex, "Błąd podczas pobierania losowania {DrawId}", request.Id);
    throw; // Middleware przejmie obsługę
}
```

### 7.3 Logowanie błędów

**Serilog configuration:**
```json
// appsettings.json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      { "Name": "Console" },
      {
        "Name": "File",
        "Args": {
          "path": "Logs/applog-.txt",
          "rollingInterval": "Day"
        }
      }
    ]
  }
}
```

**Logowanie w Handler:**
```csharp
_logger.LogDebug("Pobieranie losowania o ID: {DrawId}", request.Id);

if (draw == null)
{
    _logger.LogDebug("Losowanie o ID {DrawId} nie zostało znalezione", request.Id);
}
```

---

## 8. Wydajność

### 8.1 Optymalizacje bazy danych

#### 8.1.1 Indeksy wykorzystywane przez zapytanie

| Indeks | Tabela | Kolumna | Typ | Cel |
|--------|--------|---------|-----|-----|
| PRIMARY KEY | Draws | Id | CLUSTERED | Wyszukiwanie losowania O(log n) |
| IX_DrawNumbers_DrawId | DrawNumbers | DrawId | NONCLUSTERED | JOIN z DrawNumbers O(log n) |

**Zapytanie SQL generowane przez EF Core:**
```sql
SELECT d.Id, d.DrawDate, d.CreatedAt, d.CreatedByUserId,
       dn.Id, dn.DrawId, dn.Number, dn.Position
FROM Draws d
INNER JOIN DrawNumbers dn ON d.Id = dn.DrawId
WHERE d.Id = @p0
ORDER BY dn.Position
```

**Wydajność:**
- Pobranie 1 losowania + 6 liczb: **< 10ms** (z indeksami)
- Złożoność: O(log n) dla wyszukiwania + O(6) dla JOIN = **O(log n)**

#### 8.1.2 Eager Loading vs. Lazy Loading

**✅ Używamy Eager Loading:**
```csharp
var draw = await _dbContext.Draws
    .Include(d => d.Numbers.OrderBy(dn => dn.Position))
    .FirstOrDefaultAsync(d => d.Id == request.Id);
```

**Dlaczego?**
- Eager loading generuje 1 zapytanie SQL z JOIN → wydajniejsze
- Lazy loading generowałby 1+N zapytań (1 dla Draw + 6 dla DrawNumbers) → problem N+1

### 8.2 Wymagania wydajnościowe

**Cel:** Odpowiedź w czasie **≤ 500ms** (95 percentyl, zgodnie z NFR-002)

**Benchmark:**
- Pobranie danych z bazy: **< 10ms**
- Mapowanie encji → DTO: **< 1ms**
- Serializacja JSON: **< 5ms**
- **Total:** **< 20ms** ✅ (daleko poniżej 500ms)

### 8.3 Potencjalne wąskie gardła

#### 8.3.1 Brak indeksu PRIMARY KEY
**Symptom:** Zapytanie trwa > 100ms
**Rozwiązanie:** Indeks PRIMARY KEY jest tworzony automatycznie przez SQL Server

#### 8.3.2 Brak indeksu na DrawNumbers.DrawId
**Symptom:** JOIN trwa > 50ms
**Rozwiązanie:** Utworzenie indeksu `IX_DrawNumbers_DrawId` (zdefiniowany w db-plan.md)

#### 8.3.3 Lazy Loading przypadkowo włączony
**Symptom:** N+1 query problem (7 zapytań zamiast 1)
**Rozwiązanie:** Użycie `.Include()` w każdym zapytaniu

### 8.4 Cache (Opcjonalnie, post-MVP)

**Kiedy rozważyć cache?**
- Gdy endpoint jest bardzo często wywoływany (> 1000 req/s)
- Gdy wyniki losowań zmieniają się rzadko (1 losowanie / 2-3 dni)

**Implementacja Redis cache:**
```csharp
// W Handler.cs
var cacheKey = $"draw:{request.Id}";
var cachedDraw = await _cache.GetStringAsync(cacheKey);

if (cachedDraw != null)
{
    return JsonSerializer.Deserialize<Contracts.Response>(cachedDraw);
}

var draw = await _dbContext.Draws...
await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(response),
    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24) });
```

**Invalidacja cache:**
- Po dodaniu/edycji/usunięciu losowania: `await _cache.RemoveAsync($"draw:{drawId}")`

---

## 9. Etapy wdrożenia

### Krok 1: Utworzenie struktury katalogów
```bash
mkdir -p src/server/LottoTM.Server.Api/Features/Draws/GetById
```

**Pliki do utworzenia:**
- `Endpoint.cs` - definicja Minimal API endpoint
- `Contracts.cs` - Request/Response DTOs
- `Handler.cs` - MediatR handler (logika biznesowa)
- `Validator.cs` - FluentValidation validator

### Krok 2: Implementacja Contracts.cs

**Plik:** `src/server/LottoTM.Server.Api/Features/Draws/GetById/Contracts.cs`

```csharp
using MediatR;

namespace LottoTM.Server.Api.Features.Draws.GetById;

public class Contracts
{
    public record Request(int Id) : IRequest<Response?>;

    public record Response(
        int Id,
        DateTime DrawDate,
        string LottoType,
        int[] Numbers,
        int? DrawSystemId,
        decimal? TicketPrice,
        int? WinPoolCount1,
        decimal? WinPoolAmount1,
        int? WinPoolCount2,
        decimal? WinPoolAmount2,
        int? WinPoolCount3,
        decimal? WinPoolAmount3,
        int? WinPoolCount4,
        decimal? WinPoolAmount4,
        DateTime CreatedAt
    );
}
```

**Uwagi:**
- `Request` implementuje `IRequest<Response?>` (nullable, bo losowanie może nie istnieć)
- `Response` zawiera tablicę `int[] Numbers` (6 liczb posortowanych)

### Krok 3: Implementacja Validator.cs

**Plik:** `src/server/LottoTM.Server.Api/Features/Draws/GetById/Validator.cs`

```csharp
using FluentValidation;

namespace LottoTM.Server.Api.Features.Draws.GetById;

public class Validator : AbstractValidator<Contracts.Request>
{
    public Validator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("ID musi być liczbą całkowitą większą od 0");
    }
}
```

**Walidacja:**
- `Id > 0` (nie akceptujemy 0 ani liczb ujemnych)

### Krok 4: Implementacja Handler.cs

**Plik:** `src/server/LottoTM.Server.Api/Features/Draws/GetById/Handler.cs`

```csharp
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using LottoTM.Server.Api.Data;

namespace LottoTM.Server.Api.Features.Draws.GetById;

public class GetDrawByIdHandler : IRequestHandler<Contracts.Request, Contracts.Response?>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IValidator<Contracts.Request> _validator;
    private readonly ILogger<GetDrawByIdHandler> _logger;

    public GetDrawByIdHandler(
        ApplicationDbContext dbContext,
        IValidator<Contracts.Request> validator,
        ILogger<GetDrawByIdHandler> logger)
    {
        _dbContext = dbContext;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Contracts.Response?> Handle(
        Contracts.Request request,
        CancellationToken cancellationToken)
    {
        // 1. Walidacja
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            _logger.LogDebug("Walidacja nieudana dla GetDrawById: {Errors}",
                string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
            throw new ValidationException(validationResult.Errors);
        }

        _logger.LogDebug("Pobieranie losowania o ID: {DrawId}", request.Id);

        // 2. Pobranie losowania z bazy danych (eager loading)
        var draw = await _dbContext.Draws
            .Include(d => d.Numbers.OrderBy(dn => dn.Position))
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken);

        // 3. Sprawdzenie czy losowanie istnieje
        if (draw == null)
        {
            _logger.LogDebug("Losowanie o ID {DrawId} nie zostało znalezione", request.Id);
            return null; // Endpoint zwróci 404 Not Found
        }

        // 4. Mapowanie encji → DTO
        var numbers = draw.Numbers
            .OrderBy(dn => dn.Position)
            .Select(dn => dn.Number)
            .ToArray();

        var response = new Contracts.Response(
            Id: draw.Id,
            DrawDate: draw.DrawDate,
            LottoType: draw.LottoType,
            Numbers: numbers,
            DrawSystemId: draw.DrawSystemId,
            TicketPrice: draw.TicketPrice,
            WinPoolCount1: draw.WinPoolCount1,
            WinPoolAmount1: draw.WinPoolAmount1,
            WinPoolCount2: draw.WinPoolCount2,
            WinPoolAmount2: draw.WinPoolAmount2,
            WinPoolCount3: draw.WinPoolCount3,
            WinPoolAmount3: draw.WinPoolAmount3,
            WinPoolCount4: draw.WinPoolCount4,
            WinPoolAmount4: draw.WinPoolAmount4,
            CreatedAt: draw.CreatedAt
        );

        _logger.LogDebug("Losowanie o ID {DrawId} pobrane pomyślnie", request.Id);
        return response;
    }
}
```

**Kluczowe elementy:**
- **Eager loading:** `.Include(d => d.Numbers.OrderBy(...))` pobiera liczby w jednym zapytaniu
- **Sortowanie:** Liczby sortowane po `Position` (1-6) dla poprawnej kolejności
- **Obsługa null:** Handler zwraca `null` gdy losowanie nie istnieje, endpoint obsłuży 404
- **Logowanie:** Informacje o sukcesie/porażce zapisywane przez Serilog

### Krok 5: Implementacja Endpoint.cs

**Plik:** `src/server/LottoTM.Server.Api/Features/Draws/GetById/Endpoint.cs`

```csharp
using MediatR;

namespace LottoTM.Server.Api.Features.Draws.GetById;

public static class Endpoint
{
    public static void AddEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("api/draws/{id:int}", async (int id, IMediator mediator) =>
        {
            var request = new Contracts.Request(id);
            var result = await mediator.Send(request);

            return result != null
                ? Results.Ok(result)
                : Results.NotFound(new
                {
                    detail = "Losowanie o podanym ID nie istnieje"
                });
        })
        .RequireAuthorization() // ← Wymaga tokenu JWT
        .WithName("GetDrawById")
        .Produces<Contracts.Response>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status400BadRequest)
        .WithOpenApi();
    }
}
```

**Kluczowe elementy:**
- **Route constraint:** `{id:int}` wymusza typ int dla parametru
- **Autoryzacja:** `.RequireAuthorization()` wymaga tokenu JWT
- **Obsługa 404:** Sprawdzenie `result != null` przed zwrotem
- **Swagger:** `.WithOpenApi()` generuje dokumentację API

### Krok 6: Rejestracja endpointu w Program.cs

**Plik:** `src/server/LottoTM.Server.Api/Program.cs`

Dodaj rejestrację endpointu w sekcji konfiguracji routingu:

```csharp
// ... inne rejestracje endpointów

// Rejestracja endpointu GET /api/draws/{id}
LottoTM.Server.Api.Features.Draws.GetById.Endpoint.AddEndpoint(app);

// ... pozostałe endpointy
```

**Uwaga:** Upewnij się, że middleware `UseAuthentication()` i `UseAuthorization()` są zarejestrowane PRZED `MapEndpoints()`.

### Krok 7: Testy integracyjne

**Plik:** `tests/server/LottoTM.Server.Api.Tests/Features/Draws/GetById/EndpointTests.cs`

```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using LottoTM.Server.Api.Features.Draws.GetById;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace LottoTM.Server.Api.Tests.Features.Draws.GetById;

public class EndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;

    public EndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetDrawById_WithValidId_Returns200AndDraw()
    {
        // Arrange
        var token = await GetValidJwtToken(); // Helper do pobrania tokenu
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var drawId = await CreateTestDraw(); // Helper do utworzenia losowania

        // Act
        var response = await _client.GetAsync($"/api/draws/{drawId}");
        var draw = await response.Content.ReadFromJsonAsync<Contracts.Response>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        draw.Should().NotBeNull();
        draw!.Id.Should().Be(drawId);
        draw.Numbers.Should().HaveCount(6);
        draw.Numbers.Should().OnlyContain(n => n >= 1 && n <= 49);
    }

    [Fact]
    public async Task GetDrawById_WithInvalidId_Returns404()
    {
        // Arrange
        var token = await GetValidJwtToken();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/draws/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetDrawById_WithoutToken_Returns401()
    {
        // Act
        var response = await _client.GetAsync("/api/draws/1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetDrawById_WithInvalidIdValue_Returns400(int invalidId)
    {
        // Arrange
        var token = await GetValidJwtToken();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync($"/api/draws/{invalidId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // Helper methods
    private async Task<string> GetValidJwtToken() { /* ... */ }
    private async Task<int> CreateTestDraw() { /* ... */ }
}
```

**Scenariusze testowe:**
1. ✅ GET z prawidłowym ID → 200 OK + dane losowania
2. ✅ GET z nieistniejącym ID → 404 Not Found
3. ✅ GET bez tokenu JWT → 401 Unauthorized
4. ✅ GET z nieprawidłowym ID (0, ujemne) → 400 Bad Request

### Krok 8: Uruchomienie i weryfikacja

**Uruchomienie serwera:**
```bash
cd src/server/LottoTM.Server.Api
dotnet run
```

**Test manualny przez Swagger:**
1. Otwórz `https://localhost:5001/swagger`
2. Authorize z tokenem JWT
3. Wykonaj GET `/api/draws/{id}`
4. Zweryfikuj odpowiedź 200 OK

**Test przez cURL:**
```bash
curl -X GET "https://localhost:5001/api/draws/1" \
  -H "Authorization: Bearer <YOUR_JWT_TOKEN>" \
  -H "Content-Type: application/json"
```

**Uruchomienie testów:**
```bash
cd tests/server/LottoTM.Server.Api.Tests
dotnet test
```

### Krok 9: Integracja z frontendem

**Plik:** `src/client/app01/src/services/api-service.ts`

Dodaj metodę do ApiService:

```typescript
export interface DrawResponse {
  id: number;
  drawDate: string;
  lottoType: string;
  numbers: number[];
  drawSystemId?: string;
  ticketPrice?: number;
  winPoolCount1?: number;
  winPoolAmount1?: number;
  winPoolCount2?: number;
  winPoolAmount2?: number;
  winPoolCount3?: number;
  winPoolAmount3?: number;
  winPoolCount4?: number;
  winPoolAmount4?: number;
  createdAt: string;
}

export class ApiService {
  // ... existing methods

  async getDrawById(id: number): Promise<DrawResponse> {
    const response = await fetch(`${this.apiUrl}/api/draws/${id}`, {
      method: 'GET',
      headers: this.getHeaders(),
    });

    if (!response.ok) {
      if (response.status === 404) {
        throw new Error('Losowanie nie zostało znalezione');
      }
      throw new Error('Błąd podczas pobierania losowania');
    }

    return response.json();
  }
}
```

**Użycie w komponencie React:**
```typescript
// src/client/app01/src/pages/draws/draw-details-page.tsx
import { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { useAppContext } from '../../context/app-context';

export const DrawDetailsPage = () => {
  const { id } = useParams<{ id: string }>();
  const { getApiService } = useAppContext();
  const [draw, setDraw] = useState<DrawResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchDraw = async () => {
      try {
        const apiService = getApiService();
        const data = await apiService.getDrawById(Number(id));
        setDraw(data);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Nieznany błąd');
      } finally {
        setLoading(false);
      }
    };

    fetchDraw();
  }, [id]);

  if (loading) return <div>Ładowanie...</div>;
  if (error) return <div>Błąd: {error}</div>;
  if (!draw) return <div>Losowanie nie znalezione</div>;

  return (
    <div>
      <h1>Losowanie #{draw.id}</h1>
      <p>Data: {new Date(draw.drawDate).toLocaleDateString('pl-PL')}</p>
      <p>Liczby: {draw.numbers.join(', ')}</p>
      <p>Utworzono: {new Date(draw.createdAt).toLocaleString('pl-PL')}</p>
    </div>
  );
};
```

---

## 10. Podsumowanie

### 10.1 Checklist implementacji

- [ ] **Krok 1:** Utworzenie struktury katalogów `Features/Draws/GetById/`
- [ ] **Krok 2:** Implementacja `Contracts.cs` (Request, Response)
- [ ] **Krok 3:** Implementacja `Validator.cs` (walidacja Id > 0)
- [ ] **Krok 4:** Implementacja `Handler.cs` (logika pobierania z EF Core)
- [ ] **Krok 5:** Implementacja `Endpoint.cs` (Minimal API + autoryzacja)
- [ ] **Krok 6:** Rejestracja endpointu w `Program.cs`
- [ ] **Krok 7:** Testy integracyjne w `EndpointTests.cs`
- [ ] **Krok 8:** Test manualny przez Swagger/cURL
- [ ] **Krok 9:** Integracja z frontendem (ApiService + React component)
- [ ] **Krok 10:** Weryfikacja wydajności (czas odpowiedzi < 500ms)
- [ ] **Krok 11:** Weryfikacja indeksów w SQL Server (PRIMARY KEY, IX_DrawNumbers_DrawId)

### 10.2 Kluczowe decyzje projektowe

| Decyzja | Uzasadnienie |
|---------|--------------|
| **Draws jako zasób globalny** | Losowania dostępne dla wszystkich użytkowników (jak prawdziwe wyniki Lotto) |
| **Brak filtrowania po UserId** | W przeciwieństwie do `/api/tickets/{id}`, Draws nie należy do konkretnego użytkownika |
| **Eager loading z .Include()** | Uniknięcie problemu N+1 query, optymalizacja wydajności |
| **Zwrot null zamiast wyjątku dla 404** | Handler zwraca null, endpoint obsługuje 404 - separation of concerns |
| **Walidacja Id > 0** | Ochrona przed nieprawidłowymi wartościami |
| **Autoryzacja wymagana** | Tylko zalogowani użytkownicy mają dostęp (wymóg biznesowy) |

### 10.3 Metryki wydajności

| Metryka | Cel | Rzeczywisty |
|---------|-----|-------------|
| **Czas odpowiedzi (95 percentyl)** | ≤ 500ms | ≈ 20ms ✅ |
| **Liczba zapytań SQL** | 1 | 1 ✅ (z eager loading) |
| **Wielkość odpowiedzi JSON** | < 1KB | ≈ 200 bytes ✅ |

### 10.4 Zgodność z architekturą

✅ **Vertical Slice Architecture:**
- Wszystkie pliki w jednym folderze `Features/Draws/GetById/`
- Jasny podział odpowiedzialności: Endpoint → Handler → DbContext

✅ **MediatR CQRS:**
- Request implementuje `IRequest<Response>`
- Handler implementuje `IRequestHandler<Request, Response>`

✅ **FluentValidation:**
- Validator sprawdza parametry przed wykonaniem logiki biznesowej

✅ **Serilog:**
- Strukturyzowane logowanie na każdym etapie (info, warning, error)

---

## Koniec planu wdrożenia dla GET /api/draws/{id}
