# API Endpoint Implementation Plan: GET /api/draws

## 1. Przegląd punktu końcowego

**Cel:** Pobieranie listy wszystkich wyników losowań Lotto z paginacją i sortowaniem.

**Funkcjonalność:**
- Endpoint zwraca globalny rejestr wyników losowań dostępny dla wszystkich zalogowanych użytkowników
- Losowania NIE są filtrowane po UserId - każdy użytkownik widzi wszystkie losowania
- Losowania są zwracane wraz z liczbami (6 liczb na losowanie) w posortowanej kolejności
- Wsparcie dla paginacji z kontrolowanym rozmiarem strony
- Elastyczne sortowanie po dacie losowania lub dacie utworzenia

**Architektura:** Vertical Slice Architecture z MediatR, Minimal APIs, FluentValidation

---

## 2. Szczegóły żądania

### 2.1 Metoda i URL
- **Metoda HTTP:** GET
- **URL:** `/api/draws`
- **Przykład pełnego wywołania:** `GET /api/draws?page=1&pageSize=20&sortBy=drawDate&sortOrder=desc`

### 2.2 Parametry zapytania (query parameters)

| Parametr | Typ | Wymagany | Domyślna wartość | Opis | Walidacja |
|----------|-----|----------|------------------|------|-----------|
| `dateFrom` | date | Nie | null | Zakres daty losowania od (YYYY-MM-DD) | Format DATE |
| `dateTo` | date | Nie | null | Zakres daty losowania do (YYYY-MM-DD) | Format DATE |
| `page` | int | Nie | 1 | Numer strony (od 1) | >= 1 |
| `pageSize` | int | Nie | 100 | Liczba elementów na stronie | 1-100 |
| `sortBy` | string | Nie | "drawDate" | Pole sortowania | "drawDate" lub "createdAt" |
| `sortOrder` | string | Nie | "desc" | Kierunek sortowania | "asc" lub "desc" |

### 2.3 Nagłówki (Headers)
```
Authorization: Bearer <JWT_TOKEN>
Content-Type: application/json
```

### 2.4 Request Body
Brak (metoda GET nie przyjmuje body)

---

## 3. Wykorzystywane typy

### 3.1 Request (MediatR IRequest)
```csharp
namespace LottoTM.Server.Api.Features.Draws;

public class Contracts
{
    public record GetDrawsRequest(
        DateOnly? DateFrom = null,
        DateOnly? DateTo = null,
        int Page = 1,
        int PageSize = 20,
        string SortBy = "drawDate",
        string SortOrder = "desc"
    ) : IRequest<GetDrawsResponse>;
}
```

### 3.2 Response DTO
```csharp
public record GetDrawsResponse(
    List<DrawDto> Draws,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);

public record DrawDto(
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
```

### 3.3 Entity Models (z db-plan.md)
```csharp
// Draw.cs
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

// DrawNumber.cs
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

### 4.1 Sukces - 200 OK
```json
{
  "draws": [
    {
      "id": 1,
      "drawDate": "2025-10-30",
      "lottoType": "Lotto",
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
    },
    {
      "id": 2,
      "drawDate": "2025-10-28",
      "lottoType": "LottoPlus",
      "numbers": [5, 14, 23, 29, 37, 41],
      "drawSystemId": null,
      "ticketPrice": null,
      "winPoolCount1": null,
      "winPoolAmount1": null,
      "winPoolCount2": null,
      "winPoolAmount2": null,
      "winPoolCount3": null,
      "winPoolAmount3": null,
      "winPoolCount4": null,
      "winPoolAmount4": null,
      "createdAt": "2025-10-28T19:00:00Z"
    }
  ],
  "totalCount": 42,
  "page": 1,
  "pageSize": 20,
  "totalPages": 3
}
```

### 4.2 Błąd - 401 Unauthorized
```json
{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
  "title": "Unauthorized",
  "status": 401,
  "detail": "Brak tokenu lub token nieprawidłowy"
}
```

### 4.3 Błąd - 400 Bad Request
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Validation Error",
  "status": 400,
  "errors": {
    "pageSize": ["Maksymalna wartość to 100"],
    "page": ["Wartość musi być większa od 0"],
    "sortBy": ["Dozwolone wartości: 'drawDate', 'createdAt'"],
    "sortOrder": ["Dozwolone wartości: 'asc', 'desc'"]
  }
}
```

---

## 5. Przepływ danych

### 5.1 Diagram przepływu

```
[Client]
   | GET /api/draws?page=1&pageSize=20&sortBy=drawDate&sortOrder=desc&dateFrom=2025-01-01&dateTo=2025-12-31
   | Authorization: Bearer <token>
   v
[Minimal API Endpoint]
   | MapGet("api/draws")
   | Autoryzacja: RequireAuthorization()
   v
[MediatR] - Send(GetDrawsRequest)
   v
[FluentValidation]
   | Walidacja parametrów (page >= 1, pageSize <= 100, etc.)
   | Jeśli błąd -> throw ValidationException -> 400 Bad Request
   v
[Handler: GetDrawsHandler]
   | 1. Pobranie UserId z JWT (dla logu, nie filtracji)
   | 2. Query do bazy: COUNT dla totalCount
   | 3. Query do bazy: SELECT z OFFSET/FETCH + INNER JOIN DrawNumbers
   |    - Include(d => d.Numbers) dla eager loading
   |    - OrderBy/OrderByDescending według sortBy
   | 4. Mapowanie Draw -> DrawDto (groupowanie DrawNumbers)
   | 5. Obliczenie totalPages = (totalCount + pageSize - 1) / pageSize
   v
[Response: GetDrawsResponse]
   | draws[], totalCount, page, pageSize, totalPages
   v
[Client] - Wyświetlenie listy + kontrolki paginacji
```

### 5.2 Interakcje z bazą danych

**Query 1: Pobranie totalCount**
```sql
SELECT COUNT(*)
FROM Draws;
```
- **Optymalizacja:** Szybkie query bez JOIN
- **Indeks:** Clustered Index na Draws.Id

**Query 2: Pobranie losowań z liczbami (z paginacją)**
```sql
SELECT d.Id, d.DrawDate, d.CreatedAt, d.CreatedByUserId,
       dn.Id, dn.DrawId, dn.Number, dn.Position
FROM Draws d
INNER JOIN DrawNumbers dn ON d.Id = dn.DrawId
WHERE 1=1  -- Brak filtrowania po UserId (Draws jest globalny)
ORDER BY d.DrawDate DESC  -- lub CreatedAt, ASC/DESC
OFFSET @offset ROWS
FETCH NEXT @pageSize ROWS ONLY;
```
- **Optymalizacja:** Indeksy IX_Draws_DrawDate, IX_DrawNumbers_DrawId
- **Eager loading:** EF Core `.Include(d => d.Numbers)` generuje efektywny JOIN
- **Parametry:**
  - `@offset = (page - 1) * pageSize`
  - `@pageSize = pageSize`

### 5.3 Entity Framework Core Query (w Handler)

```csharp
// 2. Build query with eager loading of DrawNumbers
var drawsQuery = _dbContext.Draws
    .AsNoTracking() // Read-only query for better performance
    .Include(d => d.Numbers) // Eager loading to prevent N+1 problem
    .AsQueryable();

if (request.DateFrom.HasValue)
    drawsQuery = drawsQuery.Where(d => d.DrawDate >= request.DateFrom.Value);
if (request.DateTo.HasValue)
    drawsQuery = drawsQuery.Where(d => d.DrawDate <= request.DateTo.Value);

// 3. Get total count for pagination metadata
var totalCount = await drawsQuery.CountAsync(cancellationToken);

// 4. Calculate offset for pagination
var offset = (request.Page - 1) * request.PageSize;

// 5. Apply dynamic sorting based on request parameters
drawsQuery = request.SortBy.ToLower() switch
{
    "drawdate" => request.SortOrder.ToLower() == "asc"
        ? drawsQuery.OrderBy(d => d.DrawDate)
        : drawsQuery.OrderByDescending(d => d.DrawDate),
    "createdat" => request.SortOrder.ToLower() == "asc"
        ? drawsQuery.OrderBy(d => d.CreatedAt)
        : drawsQuery.OrderByDescending(d => d.CreatedAt),
    _ => drawsQuery.OrderByDescending(d => d.DrawDate) // Fallback to default
};

// 6. Apply pagination
var draws = await drawsQuery
    .Skip(offset)
    .Take(request.PageSize)
    .ToListAsync(cancellationToken);

// 7. Map entities to DTOs
var drawDtos = draws.Select(d => new Contracts.DrawDto(
    d.Id,
    d.DrawDate.ToDateTime(TimeOnly.MinValue), // Convert DateOnly to DateTime
    d.LottoType,
    d.Numbers.OrderBy(dn => dn.Position).Select(dn => dn.Number).ToArray(),
    d.DrawSystemId,
    d.TicketPrice,
    d.WinPoolCount1,
    d.WinPoolAmount1,
    d.WinPoolCount2,
    d.WinPoolAmount2,
    d.WinPoolCount3,
    d.WinPoolAmount3,
    d.WinPoolCount4,
    d.WinPoolAmount4,
    d.CreatedAt
)).ToList();

// 8. Calculate total pages
var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

// 9. Log success
_logger.LogDebug(
    "Pobrano {Count} losowań (strona {Page}/{TotalPages})",
    drawDtos.Count, request.Page, totalPages);

return new Contracts.Response(
    drawDtos,
    totalCount,
    request.Page,
    request.PageSize,
    totalPages
);
```

---

## 6. Względy bezpieczeństwa

### 6.1 Autoryzacja JWT

**Implementacja w Endpoint.cs:**
```csharp
app.MapGet("api/draws", async (
    [AsParameters] GetDrawsRequest request,
    IMediator mediator) =>
{
    var result = await mediator.Send(request);
    return Results.Ok(result);
})
.RequireAuthorization()  // <-- Wymaga JWT
.WithName("GetDraws")
.Produces<GetDrawsResponse>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status401Unauthorized)
.Produces(StatusCodes.Status400BadRequest)
.WithOpenApi();
```

**Middleware weryfikacji JWT (w Program.cs):**
- Sprawdzenie obecności tokenu w header `Authorization: Bearer <token>`
- Weryfikacja podpisu HMAC SHA-256
- Sprawdzenie czasu wygaśnięcia (exp > current time)
- Dodanie claims do `HttpContext.User`

### 6.2 Brak filtracji po UserId (WAŻNE!)

**Zgodnie z api-plan.md (linia 251):**
> "Brak filtrowania po UserId - Draws jest globalną tabelą dostępną dla wszystkich użytkowników"

**Draws jest zasobem globalnym:**
- Każdy zalogowany użytkownik widzi WSZYSTKIE losowania
- NIE stosujemy filtra `WHERE UserId = @currentUserId`
- Losowania są wprowadzane przez administratorów (`CreatedByUserId`)
- Użytkownicy mogą weryfikować swoje zestawy względem wszystkich losowań

### 6.3 Zabezpieczenie przed atakami

| Zagrożenie | Mechanizm obronny |
|-----------|-------------------|
| **SQL Injection** | EF Core automatyczna parametryzacja zapytań |
| **Excessive data exposure** | Limit pageSize = 100, paginacja obowiązkowa |
| **DoS przez duże pageSize** | Walidacja: pageSize <= 100 |
| **Unauthorized access** | JWT middleware: 401 jeśli brak tokenu |
| **Parameter tampering** | FluentValidation: strict validation przed wykonaniem query |
| **IDOR** | N/A - Draws jest globalny, brak izolacji per user |

### 6.4 Rate Limiting (opcjonalne, post-MVP)

```csharp
// AspNetCoreRateLimit configuration
services.AddMemoryCache();
services.Configure<IpRateLimitOptions>(options =>
{
    options.GeneralRules = new List<RateLimitRule>
    {
        new RateLimitRule
        {
            Endpoint = "GET:/api/draws",
            Limit = 100,
            Period = "1m" // 100 requests per minute
        }
    };
});
```

---

## 7. Obsługa błędów

### 7.1 Tabela kodów błędów

| Kod | Scenariusz | Komunikat | Obsługa |
|-----|-----------|-----------|---------|
| **200 OK** | Sukces | Lista zwrócona + metadane | Standardowy flow |
| **400 Bad Request** | Nieprawidłowe parametry | "Maksymalna wartość pageSize to 100" | FluentValidation -> ValidationException |
| **400 Bad Request** | page < 1 | "Wartość page musi być większa od 0" | FluentValidation |
| **400 Bad Request** | Nieprawidłowy sortBy | "Dozwolone wartości: 'drawDate', 'createdAt'" | FluentValidation |
| **400 Bad Request** | Nieprawidłowy sortOrder | "Dozwolone wartości: 'asc', 'desc'" | FluentValidation |
| **401 Unauthorized** | Brak tokenu | "Brak tokenu autoryzacyjnego" | JWT Middleware |
| **401 Unauthorized** | Token wygasły | "Token wygasł" | JWT Middleware |
| **401 Unauthorized** | Token nieprawidłowy | "Token nieprawidłowy" | JWT Middleware |
| **500 Internal Server Error** | Błąd bazy danych | "Wewnętrzny błąd serwera" | ExceptionHandlingMiddleware |

### 7.2 Implementacja walidacji (FluentValidation)

**Validator.cs:**
```csharp
using FluentValidation;

namespace LottoTM.Server.Api.Features.Draws;

public class GetDrawsValidator : AbstractValidator<Contracts.GetDrawsRequest>
{
    public GetDrawsValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0)
            .WithMessage("Wartość page musi być większa od 0");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("PageSize musi być w zakresie 1-100");

        RuleFor(x => x.SortBy)
            .Must(value => value.ToLower() == "drawdate" || value.ToLower() == "createdat")
            .WithMessage("Dozwolone wartości dla sortBy: 'drawDate', 'createdAt'");

        RuleFor(x => x.SortOrder)
            .Must(value => value.ToLower() == "asc" || value.ToLower() == "desc")
            .WithMessage("Dozwolone wartości dla sortOrder: 'asc', 'desc'");
    }
}
```

### 7.3 Obsługa ValidationException w Handler

```csharp
public async Task<GetDrawsResponse> Handle(GetDrawsRequest request, CancellationToken cancellationToken)
{
    // FluentValidation automatycznie wykonana przez MediatR pipeline behavior
    // Jeśli walidacja failed -> ValidationException -> 400 Bad Request

    try
    {
        // ... logika pobierania danych
        return response;
    }
    catch (Exception ex)
    {
        // Logowanie przez Serilog
        _logger.LogError(ex, "Błąd podczas pobierania listy losowań");
        throw; // Przekazanie do ExceptionHandlingMiddleware
    }
}
```

### 7.4 Globalna obsługa błędów (ExceptionHandlingMiddleware)

Zgodnie z CLAUDE.md:
- Wszystkie nieobsłużone wyjątki przechwytywane przez `ExceptionHandlingMiddleware`
- Zwrot standardowego `ProblemDetails` JSON
- Logowanie przez Serilog z analizą stack trace

---

## 8. Rozważania dotyczące wydajności

### 8.1 Potencjalne wąskie gardła

| Wąskie gardło | Opis | Ryzyko | Mitygacja |
|--------------|------|--------|-----------|
| **N+1 Query Problem** | Pobieranie DrawNumbers dla każdego Draw osobno | WYSOKIE | `.Include(d => d.Numbers)` - eager loading |
| **Brak paginacji** | Zwrot wszystkich 1000+ losowań naraz | WYSOKIE | OFFSET/FETCH NEXT (paginacja SQL) |
| **Brak indeksów** | Full table scan przy sortowaniu | ŚREDNIE | IX_Draws_DrawDate dla sortowania |
| **Duży pageSize** | Zwrot 10000 elementów w jednym zapytaniu | ŚREDNIE | Limit pageSize = 100 (walidacja) |
| **JOIN bez indeksów** | Powolny JOIN Draws ⋈ DrawNumbers | ŚREDNIE | IX_DrawNumbers_DrawId |

### 8.2 Strategie optymalizacji

**1. Indeksy (z db-plan.md):**
```sql
-- Sortowanie po DrawDate (domyślne)
CREATE UNIQUE INDEX IX_Draws_DrawDate ON Draws(DrawDate);

-- JOIN z DrawNumbers
CREATE INDEX IX_DrawNumbers_DrawId ON DrawNumbers(DrawId);
```

**2. Eager Loading (EF Core):**
```csharp
// ✅ DOBRZE - 1 zapytanie z JOIN
var draws = await _dbContext.Draws
    .Include(d => d.Numbers)
    .ToListAsync();

// ❌ ŹLE - N+1 queries (1 dla Draws + N dla DrawNumbers)
var draws = await _dbContext.Draws.ToListAsync();
foreach (var draw in draws)
{
    var numbers = draw.Numbers.ToList(); // Lazy loading -> osobne query!
}
```

**3. Paginacja SQL (OFFSET/FETCH):**
```csharp
// EF Core generuje efektywny SQL z OFFSET/FETCH
var draws = await query
    .Skip((page - 1) * pageSize)  // OFFSET
    .Take(pageSize)                // FETCH NEXT
    .ToListAsync();
```

**4. AsNoTracking dla read-only queries:**
```csharp
// GET endpoints nie modyfikują danych -> AsNoTracking dla wydajności
var draws = await _dbContext.Draws
    .AsNoTracking()
    .Include(d => d.Numbers)
    .ToListAsync();
```

### 8.3 Benchmarking

**Wymaganie wydajnościowe (z api-plan.md, NFR-002):**
> "CRUD operations ≤ 500ms (95 percentyl)"

**Estymacja dla GET /api/draws:**
- 100 losowań × 6 liczb = 600 wierszy
- Query z JOIN + eager loading: ~50-150ms
- Mapowanie do DTO: ~10ms
- Całkowity czas: **~60-160ms** ✅ (poniżej 500ms)

**Testowanie wydajności:**
```csharp
// xUnit + BenchmarkDotNet
[Benchmark]
public async Task GetDraws_Page1_PageSize20()
{
    var request = new GetDrawsRequest(Page: 1, PageSize: 20);
    var result = await _handler.Handle(request, CancellationToken.None);
}
```

### 8.4 Cache (post-MVP, jeśli potrzebny)

**Redis cache dla popularnych stron:**
```csharp
// Cache key
var cacheKey = $"draws:page:{page}:size:{pageSize}:sort:{sortBy}:{sortOrder}";

// Próba pobrania z cache
var cachedResult = await _cache.GetStringAsync(cacheKey);
if (cachedResult != null)
{
    return JsonSerializer.Deserialize<GetDrawsResponse>(cachedResult);
}

// Query do bazy + zapis do cache
var result = await QueryDatabase();
await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(result),
    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) });
return result;
```

**Invalidation:**
- Przy dodaniu nowego losowania (POST /api/draws): `_cache.RemoveAsync("draws:*")`
- Przy edycji losowania (PUT /api/draws/{id}): `_cache.RemoveAsync("draws:*")`

---

## 9. Kroki implementacji

### Krok 1: Utworzenie struktury Vertical Slice
```bash
cd src/server/LottoTM.Server.Api/Features/
mkdir Draws
cd Draws
```

**Pliki do utworzenia:**
- `Endpoint.cs` - rejestracja Minimal API
- `Contracts.cs` - Request/Response DTOs
- `Handler.cs` - GetDrawsHandler (MediatR)
- `Validator.cs` - FluentValidation

### Krok 2: Implementacja Contracts.cs

```csharp
using MediatR;

namespace LottoTM.Server.Api.Features.Draws;

public class Contracts
{
    public record GetDrawsRequest(
        int Page = 1,
        int PageSize = 20,
        string SortBy = "drawDate",
        string SortOrder = "desc"
    ) : IRequest<GetDrawsResponse>;

    public record GetDrawsResponse(
        List<DrawDto> Draws,
        int TotalCount,
        int Page,
        int PageSize,
        int TotalPages
    );

    public record DrawDto(
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

### Krok 3: Implementacja Validator.cs

```csharp
using FluentValidation;

namespace LottoTM.Server.Api.Features.Draws;

public class GetDrawsValidator : AbstractValidator<Contracts.GetDrawsRequest>
{
    public GetDrawsValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0)
            .WithMessage("Wartość page musi być większa od 0");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("PageSize musi być w zakresie 1-100");

        RuleFor(x => x.SortBy)
            .Must(value => value.ToLower() == "drawdate" || value.ToLower() == "createdat")
            .WithMessage("Dozwolone wartości dla sortBy: 'drawDate', 'createdAt'");

        RuleFor(x => x.SortOrder)
            .Must(value => value.ToLower() == "asc" || value.ToLower() == "desc")
            .WithMessage("Dozwolone wartości dla sortOrder: 'asc', 'desc'");
    }
}
```

### Krok 4: Implementacja Handler.cs

```csharp
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using LottoTM.Server.Api.Data; // DbContext

namespace LottoTM.Server.Api.Features.Draws;

public class GetDrawsHandler : IRequestHandler<Contracts.GetDrawsRequest, Contracts.GetDrawsResponse>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IValidator<Contracts.GetDrawsRequest> _validator;
    private readonly ILogger<GetDrawsHandler> _logger;

    public GetDrawsHandler(
        ApplicationDbContext dbContext,
        IValidator<Contracts.GetDrawsRequest> validator,
        ILogger<GetDrawsHandler> logger)
    {
        _dbContext = dbContext;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Contracts.GetDrawsResponse> Handle(
        Contracts.GetDrawsRequest request,
        CancellationToken cancellationToken)
    {
        // 1. Walidacja (FluentValidation)
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        try
        {
            // 2. TotalCount (dla metadanych paginacji)
            var totalCount = await _dbContext.Draws.CountAsync(cancellationToken);

            // 3. Obliczenie offset
            var offset = (request.Page - 1) * request.PageSize;

            // 4. Query z eager loading
            var drawsQuery = _dbContext.Draws
                .AsNoTracking() // Read-only query
                .Include(d => d.Numbers.OrderBy(dn => dn.Position)) // Eager loading
                .AsQueryable();

            // 5. Sortowanie dynamiczne
            drawsQuery = request.SortBy.ToLower() switch
            {
                "drawdate" => request.SortOrder.ToLower() == "asc"
                    ? drawsQuery.OrderBy(d => d.DrawDate)
                    : drawsQuery.OrderByDescending(d => d.DrawDate),
                "createdat" => request.SortOrder.ToLower() == "asc"
                    ? drawsQuery.OrderBy(d => d.CreatedAt)
                    : drawsQuery.OrderByDescending(d => d.CreatedAt),
                _ => drawsQuery.OrderByDescending(d => d.DrawDate) // Fallback
            };

            // 6. Paginacja
            var draws = await drawsQuery
                .Skip(offset)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            // 7. Mapowanie do DTO
            var drawDtos = draws.Select(d => new Contracts.DrawDto(
                d.Id,
                d.DrawDate,
                d.LottoType,
                d.Numbers.OrderBy(dn => dn.Position).Select(dn => dn.Number).ToArray(),
                d.DrawSystemId,
                d.TicketPrice,
                d.WinPoolCount1,
                d.WinPoolAmount1,
                d.WinPoolCount2,
                d.WinPoolAmount2,
                d.WinPoolCount3,
                d.WinPoolAmount3,
                d.WinPoolCount4,
                d.WinPoolAmount4,
                d.CreatedAt
            )).ToList();

            // 8. Obliczenie totalPages
            var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

            // 9. Logowanie sukcesu
            _logger.LogDebug(
                "Pobrano {Count} losowań (strona {Page}/{TotalPages})",
                drawDtos.Count, request.Page, totalPages);

            return new Contracts.GetDrawsResponse(
                drawDtos,
                totalCount,
                request.Page,
                request.PageSize,
                totalPages
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas pobierania listy losowań");
            throw;
        }
    }
}
```

### Krok 5: Implementacja Endpoint.cs

```csharp
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LottoTM.Server.Api.Features.Draws;

public static class Endpoint
{
    public static void AddEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("api/draws", async (
            [FromQuery] int page,
            [FromQuery] int pageSize,
            [FromQuery] string sortBy,
            [FromQuery] string sortOrder,
            IMediator mediator) =>
        {
            var request = new Contracts.GetDrawsRequest(
                page == 0 ? 1 : page,
                pageSize == 0 ? 20 : pageSize,
                string.IsNullOrWhiteSpace(sortBy) ? "drawDate" : sortBy,
                string.IsNullOrWhiteSpace(sortOrder) ? "desc" : sortOrder
            );

            var result = await mediator.Send(request);
            return Results.Ok(result);
        })
        .RequireAuthorization() // Wymagany JWT
        .WithName("GetDraws")
        .Produces<Contracts.GetDrawsResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
        .WithOpenApi();
    }
}
```

### Krok 6: Rejestracja endpointa w Program.cs

```csharp
// W pliku Program.cs, po konfiguracji middleware:
using LottoTM.Server.Api.Features.Draws;

// ...

app.UseAuthentication();
app.UseAuthorization();

// Rejestracja wszystkich endpoints
LottoTM.Server.Api.Features.ApiVersion.Endpoint.AddEndpoint(app);
LottoTM.Server.Api.Features.Draws.Endpoint.AddEndpoint(app); // <-- NOWE

app.Run();
```

### Krok 7: Testy jednostkowe (xUnit)

**Utworzenie pliku:** `tests/server/LottoTM.Server.Api.Tests/Features/Draws/GetDrawsEndpointTests.cs`

```csharp
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LottoTM.Server.Api.Features.Draws;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace LottoTM.Server.Api.Tests.Features.Draws;

public class GetDrawsEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public GetDrawsEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetDraws_WithValidParameters_Returns200WithPaginatedData()
    {
        // Arrange
        var url = "/api/draws?page=1&pageSize=10&sortBy=drawDate&sortOrder=desc";

        // Act
        var response = await _client.GetAsync(url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Contracts.GetDrawsResponse>();
        result.Should().NotBeNull();
        result!.Draws.Should().NotBeNull();
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.TotalPages.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GetDraws_WithPageSizeGreaterThan100_Returns400()
    {
        // Arrange
        var url = "/api/draws?pageSize=101";

        // Act
        var response = await _client.GetAsync(url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetDraws_WithInvalidSortBy_Returns400()
    {
        // Arrange
        var url = "/api/draws?sortBy=invalidField";

        // Act
        var response = await _client.GetAsync(url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetDraws_WithoutAuthorization_Returns401()
    {
        // Arrange
        var clientWithoutAuth = _factory.CreateClient();
        clientWithoutAuth.DefaultRequestHeaders.Remove("Authorization");

        // Act
        var response = await clientWithoutAuth.GetAsync("/api/draws");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
```

### Krok 8: Manualne testowanie (Postman/curl)

**Request 1: Podstawowe zapytanie**
```bash
curl -X GET "https://localhost:7000/api/draws?page=1&pageSize=20" \
  -H "Authorization: Bearer <YOUR_JWT_TOKEN>" \
  -H "Content-Type: application/json"
```

**Request 2: Sortowanie rosnące po CreatedAt**
```bash
curl -X GET "https://localhost:7000/api/draws?page=1&pageSize=10&sortBy=createdAt&sortOrder=asc" \
  -H "Authorization: Bearer <YOUR_JWT_TOKEN>"
```

**Request 3: Testowanie walidacji (pageSize > 100)**
```bash
curl -X GET "https://localhost:7000/api/draws?pageSize=150" \
  -H "Authorization: Bearer <YOUR_JWT_TOKEN>"
# Oczekiwany wynik: 400 Bad Request
```

### Krok 9: Dokumentacja OpenAPI/Swagger

**Weryfikacja w Swagger UI:**
1. Uruchom aplikację: `dotnet run --project LottoTM.Server.Api`
2. Otwórz: `https://localhost:7000/swagger`
3. Sprawdź endpoint `GET /api/draws`:
   - Parametry query (page, pageSize, sortBy, sortOrder)
   - Response schema (GetDrawsResponse)
   - Authorization: Bearer token
   - Przykładowe wartości

### Krok 10: Deployment checklist

- [ ] Testy jednostkowe przechodzą (xUnit)
- [ ] Testy integracyjne przechodzą (WebApplicationFactory)
- [ ] Endpoint widoczny w Swagger
- [ ] JWT authorization działa (401 bez tokenu)
- [ ] Walidacja parametrów działa (400 dla błędnych wartości)
- [ ] Paginacja działa poprawnie (totalPages obliczone)
- [ ] Sortowanie działa dla obu pól i kierunków
- [ ] Eager loading DrawNumbers działa (brak N+1)
- [ ] Wydajność < 500ms dla 100 losowań
- [ ] Logowanie przez Serilog działa
- [ ] Dokumentacja OpenAPI jest aktualna

---

## 10. Podsumowanie kluczowych decyzji

| Decyzja | Uzasadnienie |
|---------|--------------|
| **Draws jest globalny (NIE filtrujemy po UserId)** | Zgodnie z api-plan.md - wszystkie losowania dostępne dla wszystkich użytkowników |
| **Eager loading DrawNumbers** | Zapobiega N+1 query problem, wydajność |
| **Limit pageSize = 100** | Ochrona przed DoS, excessive data exposure |
| **AsNoTracking()** | Read-only query, lepsza wydajność |
| **Dynamiczne sortowanie** | Flexibility dla UI (sortowanie po drawDate lub createdAt) |
| **OFFSET/FETCH paginacja** | Standard SQL Server, efektywna wydajność |
| **FluentValidation** | Spójna walidacja, jasne komunikaty błędów |
| **MediatR pattern** | Vertical Slice Architecture, separation of concerns |
| **Minimal APIs** | .NET 8 best practice, mniej boilerplate |

---

**Koniec planu implementacji**
