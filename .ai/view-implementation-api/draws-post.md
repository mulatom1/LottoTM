# API Endpoint Implementation Plan: POST /api/draws

## 1. Przegląd punktu końcowego

**Cel:** Dodanie nowego wyniku losowania LOTTO do globalnego rejestru wyników dostępnego dla wszystkich użytkowników.

**Funkcjonalność:**
- Wprowadzenie wyniku losowania (data + 6 liczb) przez zalogowanego użytkownika
- Walidacja daty losowania (nie może być w przyszłości, unikalna globalnie)
- Walidacja liczb (6 liczb w zakresie 1-49, wszystkie unikalne)
- Tracking autora losowania przez pole `CreatedByUserId`
- Opcjonalne nadpisanie istniejącego losowania na daną datę (decyzja biznesowa)
- Tylko użytkownicy z uprawnieniami administratora mogą dodawać losowania (IsAdmin)
- Jeśli losowanie o podanej dacie i typie już istnieje zwraca błąd (unikalny klucz)

**Architektura:** Vertical Slice Architecture z MediatR

---

## 2. Szczegóły żądania

### HTTP Method & URL
- **Metoda:** POST
- **URL:** `/api/draws`
- **Autoryzacja:** Wymagany JWT token w header `Authorization: Bearer <token>`

### Request Headers
```
Content-Type: application/json
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### Parametry zapytania
Brak

### Request Body (JSON)
```json
{
  "drawDate": "2025-10-30",
  "numbers": [3, 12, 25, 31, 42, 48],
  "lottoType": "LOTTO"
}
```

### Walidacja parametrów

| Pole | Typ | Wymagane | Walidacja | Komunikaty błędów |
|------|-----|----------|-----------|-------------------|
| `drawDate` | DATE (YYYY-MM-DD) | Tak | - Format DATE<br>- Nie w przyszłości<br>- Unikalny globalnie (DrawDate + LottoType) | - "Data losowania jest wymagana"<br>- "Nieprawidłowy format daty"<br>- "Data losowania nie może być w przyszłości"<br>- "Losowanie z tą datą i typem gry już istnieje" |
| `numbers` | Array<int> | Tak | - Dokładnie 6 elementów<br>- Każda liczba 1-49<br>- Wszystkie unikalne | - "Wymagane dokładnie 6 liczb"<br>- "Liczby muszą być w zakresie 1-49"<br>- "Liczby muszą być unikalne" |
| `lottoType` | STRING | Tak | - Wymagane<br>- Tylko "LOTTO" lub "LOTTO PLUS" | - "Typ gry jest wymagany"<br>- "Dozwolone wartości: LOTTO, LOTTO PLUS" |

---

## 3. Wykorzystywane typy

### Contracts (DTOs)

```csharp
// Contracts.cs
using MediatR;

namespace LottoTM.Server.Api.Features.Draws;

public class Contracts
{
    public record CreateDrawRequest(
        DateOnly DrawDate,
        int[] Numbers,
        string LottoType
    ) : IRequest<CreateDrawResponse>;

    public record CreateDrawResponse(
        string Message
    );
}
```

### Command Model (Entity Models)

```csharp
// Models/Draw.cs
public class Draw
{
    public int Id { get; set; }
    public DateTime DrawDate { get; set; }
    public string LottoType { get; set; } = string.Empty; // "LOTTO" lub "LOTTO PLUS"
    public DateTime CreatedAt { get; set; }
    public int CreatedByUserId { get; set; }

    // Navigation properties
    public User CreatedByUser { get; set; } = null!;
    public ICollection<DrawNumber> Numbers { get; set; } = new List<DrawNumber>();
}

// Models/DrawNumber.cs
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

### Response Success (201 Created)
```json
{
  "message": "Losowanie utworzone pomyślnie"
}
```

**HTTP Status:** 201 Created

**Headers:**
```
Content-Type: application/json
```

### Response Errors

#### 400 Bad Request - Nieprawidłowe dane wejściowe
```json
{
  "errors": {
    "drawDate": [
      "Data losowania nie może być w przyszłości",
      "Losowanie z tą datą już istnieje"
    ],
    "numbers": [
      "Wymagane dokładnie 6 liczb",
      "Liczby muszą być unikalne",
      "Liczby muszą być w zakresie 1-49"
    ]
  }
}
```

#### 401 Unauthorized - Brak tokenu lub token nieprawidłowy
```json
{
  "error": "Brak autoryzacji. Wymagany token JWT."
}
```

#### 500 Internal Server Error - Błąd serwera
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.6.1",
  "title": "An error occurred while processing your request.",
  "status": 500,
  "traceId": "00-abc123..."
}
```

---

## 5. Przepływ danych

### Sekwencja operacji

```
1. Client → API: POST /api/draws
   ↓
2. API Endpoint (Endpoint.cs)
   - Odbiera request
   - Deleguje do MediatR
   ↓
3. FluentValidation (Validator.cs)
   - Walidacja struktury danych
   - Walidacja zakresu liczb
   - Walidacja unikalności liczb w tablicy
   ↓
4. Handler (Handler.cs)
   - Pobiera UserId z JWT claims
   - Sprawdza czy losowanie na daną datę i typ już istnieje
   - Jeśli tak, rzucić błąd
   ↓
5. Database Transaction
   - BEGIN TRANSACTION
   - INSERT do Draws (DrawDate, LottoType, CreatedAt, CreatedByUserId)
   - Otrzymanie Draw.Id
   - INSERT 6 rekordów do DrawNumbers (DrawId, Number, Position)
   - COMMIT TRANSACTION
   ↓
6. Response → Client: 201 Created
   - { "message": "Losowanie utworzone pomyślnie" }
```

### Diagram przepływu danych

```
┌─────────────┐
│   Client    │
└──────┬──────┘
       │ POST /api/draws
       │ { drawDate, numbers[] }
       ↓
┌─────────────────────┐
│  Endpoint.cs        │
│  - MapPost          │
│  - MediatR.Send()   │
└──────┬──────────────┘
       ↓
┌─────────────────────┐
│  Validator.cs       │
│  - FluentValidation │
│  - RuleFor(...)     │
└──────┬──────────────┘
       ↓
┌─────────────────────────────────┐
│  Handler.cs                     │
│  - Get UserId from JWT          │
│  - Check drawDate uniqueness    │
│  - BEGIN TRANSACTION            │
│  - INSERT Draws                 │
│  - INSERT DrawNumbers (6x)      │
│  - COMMIT                       │
└──────┬──────────────────────────┘
       ↓
┌─────────────────────┐
│  Database           │
│  - Draws table      │
│  - DrawNumbers      │
└──────┬──────────────┘
       ↓
┌─────────────────────┐
│  Response           │
│  201 Created        │
└─────────────────────┘
```

### Interakcje z bazą danych

**Tabele zaangażowane:**
1. `Draws` - zapis metadanych losowania
2. `DrawNumbers` - zapis 6 liczb z pozycjami

**Indeksy wykorzystane:**
- `IX_Draws_DrawDate` - sprawdzenie unikalności daty
- `IX_DrawNumbers_DrawId` - relacja CASCADE dla DrawNumbers

**Transakcja:**
```sql
BEGIN TRANSACTION;

-- Krok 1: Wstawienie losowania
INSERT INTO Draws (DrawDate, CreatedAt, CreatedByUserId)
VALUES ('2025-10-30', GETUTCDATE(), @userId);

-- Otrzymanie nowego Id
DECLARE @drawId INT = SCOPE_IDENTITY();

-- Krok 2: Wstawienie 6 liczb
INSERT INTO DrawNumbers (DrawId, Number, Position)
VALUES
    (@drawId, 3, 1),
    (@drawId, 12, 2),
    (@drawId, 25, 3),
    (@drawId, 31, 4),
    (@drawId, 42, 5),
    (@drawId, 48, 6);

COMMIT TRANSACTION;
```

---

## 6. Względy bezpieczeństwa

### 6.1 Uwierzytelnianie (Authentication)

**Mechanizm:** JWT (JSON Web Tokens)

**Implementacja:**
- Middleware JWT Authentication w ASP.NET Core
- Algorytm: HMAC SHA-256 (HS256)
- Ważność tokenu: 24 godziny

**Konfiguracja w Endpoint.cs:**
```csharp
app.MapPost("api/draws", async (IMediator mediator, CreateDrawRequest request) =>
{
    var result = await mediator.Send(request);
    return Results.Created($"/api/draws", result);
})
.RequireAuthorization() // ← JWT required
.WithName("CreateDraw")
.Produces<CreateDrawResponse>(StatusCodes.Status201Created)
.Produces(StatusCodes.Status401Unauthorized)
.ProducesValidationProblem(StatusCodes.Status400BadRequest)
.WithOpenApi();
```

### 6.2 Autoryzacja (Authorization)

**Pobieranie UserId z JWT Claims:**
```csharp
// W Handler.cs
var currentUserId = int.Parse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier));
```

**Tracking autora:**
- Pole `CreatedByUserId` zapisuje kto wprowadził losowanie
- Losowania są globalne (dostępne dla wszystkich użytkowników)
- W przyszłości można dodać autoryzację admin-only dla tego endpointu

### 6.3 Walidacja danych wejściowych

**Warstwa 1: FluentValidation (Validator.cs)**
```csharp
public class CreateDrawValidator : AbstractValidator<CreateDrawRequest>
{
    public CreateDrawValidator()
    {
        RuleFor(x => x.DrawDate)
            .NotEmpty().WithMessage("Data losowania jest wymagana")
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.Today))
                .WithMessage("Data losowania nie może być w przyszłości");

        RuleFor(x => x.Numbers)
            .NotEmpty().WithMessage("Liczby są wymagane")
            .Must(n => n.Length == 6).WithMessage("Wymagane dokładnie 6 liczb")
            .Must(n => n.All(num => num >= 1 && num <= 49))
                .WithMessage("Liczby muszą być w zakresie 1-49")
            .Must(n => n.Distinct().Count() == 6)
                .WithMessage("Liczby muszą być unikalne");

        RuleFor(x => x.LottoType)
            .NotEmpty().WithMessage("Typ gry jest wymagany")
            .Must(lt => lt == "LOTTO" || lt == "LOTTO PLUS")
                .WithMessage("Dozwolone wartości: LOTTO, LOTTO PLUS");
    }
}
```

**Warstwa 2: Business Logic (Handler.cs)**
```csharp
// Sprawdzenie unikalności kombinacji DrawDate + LottoType
var existingDraw = await _dbContext.Draws
    .Where(d => d.DrawDate == request.DrawDate && d.LottoType == request.LottoType)
    .AnyAsync(cancellationToken);

if (existingDraw)
{
    throw new ValidationException("Losowanie z tą datą i typem gry już istnieje");
}
```

### 6.4 Zabezpieczenia przed atakami

**SQL Injection:**
- ✅ Entity Framework Core automatycznie parametryzuje zapytania
- ✅ NIGDY nie używać `FromSqlRaw()` z konkatenacją stringów

**Mass Assignment:**
- ✅ Wykorzystanie DTO (Contracts.Request) zamiast bezpośrednio modelu Entity
- ✅ Mapowanie ręczne pól w Handler.cs

**CHECK Constraints w bazie:**
```sql
-- Automatycznie wymuszane przez EF Core migrations
CHECK (Number BETWEEN 1 AND 49)
CHECK (Position BETWEEN 1 AND 6)
```

**Rate Limiting (opcjonalnie dla MVP):**
- AspNetCoreRateLimit middleware
- Limit: 10 żądań/minutę/użytkownik dla POST /api/draws

---

## 7. Obsługa błędów

### 7.1 Scenariusze błędów

| Scenariusz | Status Code | Response Body | Handling |
|------------|-------------|---------------|----------|
| Brak tokenu JWT | 401 | `{ "error": "Unauthorized" }` | JWT Middleware |
| Token wygasły | 401 | `{ "error": "Token expired" }` | JWT Middleware |
| Nieprawidłowy format JSON | 400 | Validation errors | Model Binding |
| Nieprawidłowa data (przyszłość) | 400 | `{ "errors": { "drawDate": [...] } }` | FluentValidation |
| Nieprawidłowe liczby (< 6 lub > 6) | 400 | `{ "errors": { "numbers": [...] } }` | FluentValidation |
| Liczby poza zakresem 1-49 | 400 | `{ "errors": { "numbers": [...] } }` | FluentValidation |
| Liczby nieunikalnego | 400 | `{ "errors": { "numbers": [...] } }` | FluentValidation |
| Data losowania już istnieje | 400 | `{ "errors": { "drawDate": ["Losowanie z tą datą już istnieje"] } }` | Handler logic |
| Błąd bazy danych (constraint) | 500 | ProblemDetails | ExceptionHandlingMiddleware |
| Utrata połączenia z bazą | 500 | ProblemDetails | ExceptionHandlingMiddleware |

### 7.2 Implementacja obsługi błędów

**GlobalExceptionHandlingMiddleware (istniejący w projekcie):**
```csharp
// Middleware automatycznie łapie wszystkie wyjątki
// i zwraca standardowy format ProblemDetails
app.UseMiddleware<ExceptionHandlingMiddleware>();
```

**ValidationException Handling:**
```csharp
// W Handler.cs
var validationResult = await _validator.ValidateAsync(request, cancellationToken);
if (!validationResult.IsValid)
{
    throw new ValidationException(validationResult.Errors);
}
```

**Business Logic Exceptions:**
```csharp
// Rzucanie wyjątku z komunikatem błędu
if (existingDraw)
{
    var errors = new List<ValidationFailure>
    {
        new ValidationFailure("drawDate", "Losowanie z tą datą już istnieje")
    };
    throw new ValidationException(errors);
}
```

### 7.3 Logging błędów

**Serilog - strukturalne logowanie:**
```csharp
// W Handler.cs
public class CreateDrawHandler : IRequestHandler<CreateDrawRequest, CreateDrawResponse>
{
    private readonly ILogger<CreateDrawHandler> _logger;

    public async Task<CreateDrawResponse> Handle(...)
    {
        try
        {
            _logger.LogDebug(
                "Creating draw for date {DrawDate} by user {UserId}",
                request.DrawDate, currentUserId
            );

            // ... business logic

            _logger.LogDebug(
                "Draw {DrawId} created successfully for date {DrawDate}",
                draw.Id, draw.DrawDate
            );

            return new CreateDrawResponse("Losowanie utworzone pomyślnie");
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex,
                "Validation failed for draw creation: {Errors}",
                string.Join(", ", ex.Errors.Select(e => e.ErrorMessage))
            );
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error while creating draw for date {DrawDate}",
                request.DrawDate
            );
            throw;
        }
    }
}
```

**Log output (przykład):**
```
[10:30:15 INF] Creating draw for date 2025-10-30 by user 123
[10:30:15 INF] Draw 456 created successfully for date 2025-10-30
```

---

## 8. Wydajność

### 8.1 Potencjalne wąskie gardła

| Wąskie gardło | Opis | Mitygacja |
|---------------|------|-----------|
| **Brak indeksu na (DrawDate, LottoType)** | Sprawdzanie unikalności kombinacji: O(n) table scan | ✅ Indeks `IX_Draws_DrawDate_LottoType` (UNIQUE) |
| **N+1 problem przy INSERT** | 6 osobnych INSERT do DrawNumbers | ✅ Bulk insert lub transakcja |
| **Serializacja JSON** | Parsing dużych request body | ⚠️ Akceptowalne dla MVP (tylko 6 liczb) |
| **Lock contention** | Wiele użytkowników dodaje losowania jednocześnie | ⚠️ Bardzo niskie ryzyko (1 losowanie/dzień/typ) |

### 8.2 Strategie optymalizacji

**1. Indeksy bazy danych:**
```sql
CREATE UNIQUE INDEX IX_Draws_DrawDate_LottoType ON Draws(DrawDate, LottoType);
CREATE INDEX IX_DrawNumbers_DrawId ON DrawNumbers(DrawId);
```

**2. Transakcja z bulk insert:**
```csharp
using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

try
{
    // 1. Dodaj Draw
    var draw = new Draw
    {
        DrawDate = request.DrawDate.ToDateTime(TimeOnly.MinValue),
        LottoType = request.LottoType,
        CreatedAt = DateTime.UtcNow,
        CreatedByUserId = currentUserId
    };
    _dbContext.Draws.Add(draw);
    await _dbContext.SaveChangesAsync(cancellationToken);

    // 2. Bulk insert DrawNumbers
    var drawNumbers = request.Numbers
        .Select((number, index) => new DrawNumber
        {
            DrawId = draw.Id,
            Number = number,
            Position = (byte)(index + 1)
        })
        .ToList();

    _dbContext.DrawNumbers.AddRange(drawNumbers);
    await _dbContext.SaveChangesAsync(cancellationToken);

    await transaction.CommitAsync(cancellationToken);
}
catch
{
    await transaction.RollbackAsync(cancellationToken);
    throw;
}
```

**3. Eager loading NIE jest potrzebny (tylko INSERT):**
```csharp
// Nie używamy .Include() bo nie pobieramy danych, tylko zapisujemy
```

**4. Cache dla sprawdzenia unikalności daty (post-MVP):**
```csharp
// Redis cache z kluczem: "draw:dates"
// TTL: 24 godziny
// Invalidacja: przy dodaniu nowego losowania
```

### 8.3 Metryki wydajności (cel)

| Metryka | Cel MVP | Pomiar |
|---------|---------|--------|
| Response Time (p95) | < 500 ms | Application Insights |
| Database Query Time | < 100 ms | SQL Server Profiler |
| Transaction Time | < 200 ms | EF Core logging |
| Throughput | 100 req/s | Load testing (k6) |

---

## 9. Etapy wdrożenia

### Krok 1: Utworzenie struktury folderów

```bash
mkdir -p src/server/LottoTM.Server.Api/Features/Draws
```

**Pliki do utworzenia:**
- `Endpoint.cs` - definicja endpointu Minimal API
- `Contracts.cs` - Request/Response DTOs
- `Handler.cs` - MediatR handler z logiką biznesową
- `Validator.cs` - FluentValidation validator

### Krok 2: Implementacja Contracts.cs

```csharp
using MediatR;

namespace LottoTM.Server.Api.Features.Draws;

public class Contracts
{
    public record CreateDrawRequest(
        DateOnly DrawDate,
        int[] Numbers,
        string LottoType
    ) : IRequest<CreateDrawResponse>;

    public record CreateDrawResponse(
        string Message
    );
}
```

### Krok 3: Implementacja Validator.cs

```csharp
using FluentValidation;

namespace LottoTM.Server.Api.Features.Draws;

public class CreateDrawValidator : AbstractValidator<Contracts.CreateDrawRequest>
{
    public CreateDrawValidator()
    {
        RuleFor(x => x.DrawDate)
            .NotEmpty().WithMessage("Data losowania jest wymagana")
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.Today))
                .WithMessage("Data losowania nie może być w przyszłości");

        RuleFor(x => x.Numbers)
            .NotEmpty().WithMessage("Liczby są wymagane")
            .Must(n => n != null && n.Length == 6)
                .WithMessage("Wymagane dokładnie 6 liczb")
            .Must(n => n.All(num => num >= 1 && num <= 49))
                .WithMessage("Liczby muszą być w zakresie 1-49")
            .Must(n => n.Distinct().Count() == 6)
                .WithMessage("Liczby muszą być unikalne");
    }
}
```

### Krok 4: Implementacja Handler.cs

```csharp
using System.Security.Claims;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using LottoTM.Server.Api.Data;
using LottoTM.Server.Api.Models;

namespace LottoTM.Server.Api.Features.Draws;

public class CreateDrawHandler : IRequestHandler<Contracts.CreateDrawRequest, Contracts.CreateDrawResponse>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IValidator<Contracts.CreateDrawRequest> _validator;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<CreateDrawHandler> _logger;

    public CreateDrawHandler(
        ApplicationDbContext dbContext,
        IValidator<Contracts.CreateDrawRequest> validator,
        IHttpContextAccessor httpContextAccessor,
        ILogger<CreateDrawHandler> logger)
    {
        _dbContext = dbContext;
        _validator = validator;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<Contracts.CreateDrawResponse> Handle(
        Contracts.CreateDrawRequest request,
        CancellationToken cancellationToken)
    {
        // 1. Walidacja FluentValidation
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // 2. Pobierz UserId z JWT claims
        var currentUserId = int.Parse(
            _httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier)!
        );

        _logger.LogDebug(
            "Creating draw for date {DrawDate} by user {UserId}",
            request.DrawDate, currentUserId
        );

        // 3. Sprawdź czy losowanie na daną datę już istnieje
        var drawDate = request.DrawDate.ToDateTime(TimeOnly.MinValue);
        var existingDraw = await _dbContext.Draws
            .AnyAsync(d => d.DrawDate == drawDate, cancellationToken);

        if (existingDraw)
        {
            _logger.LogWarning(
                "Draw for date {DrawDate} already exists",
                request.DrawDate
            );

            var errors = new List<FluentValidation.Results.ValidationFailure>
            {
                new("drawDate", "Losowanie z tą datą już istnieje")
            };
            throw new ValidationException(errors);
        }

        // 4. Transakcja: INSERT Draws + DrawNumbers
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // 4a. Dodaj Draw
            var draw = new Draw
            {
                DrawDate = drawDate,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = currentUserId
            };
            _dbContext.Draws.Add(draw);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // 4b. Bulk insert DrawNumbers (6 rekordów)
            var drawNumbers = request.Numbers
                .Select((number, index) => new DrawNumber
                {
                    DrawId = draw.Id,
                    Number = number,
                    Position = (byte)(index + 1)
                })
                .ToList();

            _dbContext.DrawNumbers.AddRange(drawNumbers);
            await _dbContext.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            _logger.LogDebug(
                "Draw {DrawId} created successfully for date {DrawDate}",
                draw.Id, draw.DrawDate
            );

            return new Contracts.CreateDrawResponse("Losowanie utworzone pomyślnie");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);

            _logger.LogError(ex,
                "Error creating draw for date {DrawDate}",
                request.DrawDate
            );

            throw;
        }
    }
}
```

### Krok 5: Implementacja Endpoint.cs

```csharp
using MediatR;

namespace LottoTM.Server.Api.Features.Draws;

public static class Endpoint
{
    public static void AddEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("api/draws", async (
            IMediator mediator,
            Contracts.CreateDrawRequest request) =>
        {
            var result = await mediator.Send(request);
            return Results.Created($"/api/draws", result);
        })
        .RequireAuthorization()
        .WithName("CreateDraw")
        .Produces<Contracts.CreateDrawResponse>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status401Unauthorized)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .WithOpenApi();
    }
}
```

### Krok 6: Rejestracja endpointu w Program.cs

```csharp
// W pliku src/server/LottoTM.Server.Api/Program.cs
// Dodaj po linii z ApiVersion endpoint:

LottoTM.Server.Api.Features.ApiVersion.Endpoint.AddEndpoint(app);
LottoTM.Server.Api.Features.Draws.Endpoint.AddEndpoint(app);  // ← NOWA LINIA
```

### Krok 7: Rejestracja Validator w DI Container

```csharp
// W pliku Program.cs, w sekcji services.AddValidatorsFromAssemblyContaining:
// Validator powinien być automatycznie zarejestrowany przez:
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
```

### Krok 8: Utworzenie testów jednostkowych

**Lokalizacja:** `tests/server/LottoTM.Server.Api.Tests/Features/Draws/EndpointTests.cs`

```csharp
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace LottoTM.Server.Api.Tests.Features.Draws;

public class CreateDrawEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public CreateDrawEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateDraw_WithValidData_Returns201Created()
    {
        // Arrange
        var request = new
        {
            drawDate = "2025-10-30",
            numbers = new[] { 3, 12, 25, 31, 42, 48 }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/draws", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateDraw_WithFutureDate_Returns400BadRequest()
    {
        // Arrange
        var request = new
        {
            drawDate = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd"),
            numbers = new[] { 3, 12, 25, 31, 42, 48 }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/draws", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateDraw_WithInvalidNumberCount_Returns400BadRequest()
    {
        // Arrange
        var request = new
        {
            drawDate = "2025-10-30",
            numbers = new[] { 3, 12, 25, 31, 42 } // tylko 5 liczb
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/draws", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateDraw_WithNumbersOutOfRange_Returns400BadRequest()
    {
        // Arrange
        var request = new
        {
            drawDate = "2025-10-30",
            numbers = new[] { 0, 12, 25, 31, 42, 48 } // 0 poza zakresem
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/draws", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateDraw_WithDuplicateNumbers_Returns400BadRequest()
    {
        // Arrange
        var request = new
        {
            drawDate = "2025-10-30",
            numbers = new[] { 3, 3, 25, 31, 42, 48 } // duplikat 3
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/draws", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
```

### Krok 9: Uruchomienie testów

```bash
# Uruchom wszystkie testy
dotnet test

# Uruchom tylko testy dla Draws
dotnet test --filter "FullyQualifiedName~Draws"
```

### Krok 10: Testowanie manualne (Postman/cURL)

**cURL example:**
```bash
curl -X POST http://localhost:5000/api/draws \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{
    "drawDate": "2025-10-30",
    "numbers": [3, 12, 25, 31, 42, 48]
  }'
```

**Postman collection:**
- Zapisz token JWT w Environment Variables: `{{jwt_token}}`
- Utwórz request: POST `{{base_url}}/api/draws`
- Headers: `Authorization: Bearer {{jwt_token}}`
- Body (raw JSON): `{ "drawDate": "2025-10-30", "numbers": [3, 12, 25, 31, 42, 48] }`

### Krok 11: Weryfikacja w bazie danych

```sql
-- Sprawdź czy losowanie zostało utworzone
SELECT * FROM Draws WHERE DrawDate = '2025-10-30';

-- Sprawdź czy 6 liczb zostało zapisanych
SELECT * FROM DrawNumbers WHERE DrawId = (
    SELECT Id FROM Draws WHERE DrawDate = '2025-10-30'
);
```

---

## 10. Checklist implementacji

### Backend Implementation
- [ ] Utworzenie folderu `Features/Draws/`
- [ ] Implementacja `Contracts.cs` (Request/Response)
- [ ] Implementacja `Validator.cs` (FluentValidation rules)
- [ ] Implementacja `Handler.cs` (business logic + transaction)
- [ ] Implementacja `Endpoint.cs` (Minimal API registration)
- [ ] Rejestracja endpointu w `Program.cs`
- [ ] Weryfikacja automatycznej rejestracji Validator w DI

### Database Verification
- [ ] Sprawdzenie istnienia tabeli `Draws`
- [ ] Sprawdzenie istnienia tabeli `DrawNumbers`
- [ ] Weryfikacja indeksu `IX_Draws_DrawDate` (UNIQUE)
- [ ] Weryfikacja indeksu `IX_DrawNumbers_DrawId`
- [ ] Weryfikacja CHECK constraints (Number BETWEEN 1 AND 49)

### Testing
- [ ] Utworzenie `EndpointTests.cs` w projekcie testowym
- [ ] Test: Pomyślne utworzenie losowania (201 Created)
- [ ] Test: Data w przyszłości (400 Bad Request)
- [ ] Test: Nieprawidłowa liczba liczb != 6 (400 Bad Request)
- [ ] Test: Liczby poza zakresem 1-49 (400 Bad Request)
- [ ] Test: Duplikaty liczb (400 Bad Request)
- [ ] Test: Duplikat daty losowania (400 Bad Request)
- [ ] Test: Brak autoryzacji (401 Unauthorized)
- [ ] Uruchomienie testów: `dotnet test`

### Manual Testing
- [ ] Testowanie przez Postman/Swagger
- [ ] Sprawdzenie odpowiedzi 201 Created
- [ ] Sprawdzenie komunikatu "Losowanie utworzone pomyślnie"
- [ ] Weryfikacja w bazie danych (Draws + DrawNumbers)
- [ ] Sprawdzenie tracking `CreatedByUserId`

### Security Verification
- [ ] Weryfikacja wymagania JWT tokenu (.RequireAuthorization())
- [ ] Test dostępu bez tokenu (401 Unauthorized)
- [ ] Test z wygasłym tokenem (401 Unauthorized)
- [ ] Weryfikacja poprawnego zapisania `CreatedByUserId`

### Performance Testing
- [ ] Pomiar czasu odpowiedzi (< 500ms dla p95)
- [ ] Sprawdzenie wykorzystania indeksów (SQL Execution Plan)
- [ ] Test transakcji (ROLLBACK przy błędzie)

### Documentation
- [ ] Aktualizacja dokumentacji API (Swagger)
- [ ] Dodanie przykładów request/response
- [ ] Dokumentacja błędów walidacji

---

## Podsumowanie

Ten endpoint implementuje funkcjonalność dodawania wyników losowań LOTTO zgodnie z:
- **Architekturą:** Vertical Slice Architecture z MediatR
- **Walidacją:** 2-warstwowa (FluentValidation + business logic)
- **Bezpieczeństwem:** JWT authentication, parametryzowane zapytania
- **Wydajnością:** Indeksy bazy danych, bulk insert, transakcje
- **Obsługą błędów:** Globalna obsługa wyjątków, strukturalne logowanie

Endpoint jest gotowy do wdrożenia w środowisku MVP i spełnia wszystkie wymagania funkcjonalne i niefunkcjonalne określone w dokumentacji projektu.
