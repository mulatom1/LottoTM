# API Endpoint Implementation Plan: PUT /api/draws/{id}

## 1. Przegląd punktu końcowego

**Endpoint:** `PUT /api/draws/{id}`

**Opis:** Edycja istniejącego wyniku losowania LOTTO. Pozwala administratorowi na aktualizację daty losowania oraz wylosowanych liczb. Operacja wymaga uprawnień administratora i wykonuje transakcyjną aktualizację zarówno metadanych losowania (Draw), jak i wszystkich 6 liczb (DrawNumbers).

**Architektura:** Vertical Slice Architecture z MediatR
- Feature: `Features/Draws/UpdateDraw/`
- Komponenty: Endpoint.cs, Contracts.cs, Handler.cs, Validator.cs

**Model biznesowy:**
- Globalna tabela losowań dostępna dla wszystkich użytkowników do weryfikacji
- Znormalizowana struktura: 1 Draw + 6 DrawNumbers (pozycje 1-6)
- Edycja dostępna tylko dla administratorów
- Data losowania musi pozostać unikalna w systemie

---

## 2. Szczegóły żądania

### Metoda HTTP
`PUT`

### Struktura URL
```
PUT /api/draws/{id}
```

### Parametry

#### Parametry URL (wymagane)
| Parametr | Typ | Opis | Walidacja |
|----------|-----|------|-----------|
| `id` | int | ID losowania do edycji | Musi być > 0 |

#### Request Body (wymagany, Content-Type: application/json)
```json
{
  "drawDate": "2025-10-30",
  "numbers": [3, 12, 25, 31, 42, 48]
}
```

#### Nagłówki HTTP (wymagane)
| Nagłówek | Wartość | Opis |
|----------|---------|------|
| `Authorization` | `Bearer <jwt_token>` | Token JWT z claim IsAdmin=true |
| `Content-Type` | `application/json` | Format danych żądania |

---

## 3. Wykorzystywane typy

### DTOs (Data Transfer Objects)

**Namespace:** `LottoTM.Server.Api.Features.Draws.UpdateDraw`

#### Request DTO
```csharp
public class Contracts
{
    /// <summary>
    /// Request for updating a lottery draw result
    /// </summary>
    public record Request : IRequest<Response>
    {
        /// <summary>
        /// ID of the draw to update (from route parameter)
        /// </summary>
        public int Id { get; init; }

        /// <summary>
        /// New draw date (cannot be in the future)
        /// </summary>
        public DateOnly DrawDate { get; init; }

        /// <summary>
        /// New array of 6 drawn numbers (1-49, unique)
        /// </summary>
        public int[] Numbers { get; init; } = Array.Empty<int>();
    }

    /// <summary>
    /// Response after successful draw update
    /// </summary>
    public record Response(string Message);
}
```

### Encje domenowe (wykorzystywane)
- `Draw` - encja główna losowania (z Entities/Draw.cs)
- `DrawNumber` - encja pojedynczej liczby (z Entities/DrawNumber.cs)
- `User` - encja użytkownika (dla weryfikacji IsAdmin)

---

## 4. Szczegóły odpowiedzi

### Sukces (200 OK)
```json
{
  "message": "Losowanie zaktualizowane pomyślnie"
}
```

**Headers:**
```
Content-Type: application/json
```

### Błąd walidacji (400 Bad Request)
```json
{
  "errors": {
    "drawDate": [
      "Data losowania nie może być w przyszłości"
    ],
    "numbers": [
      "Wymagane dokładnie 6 liczb",
      "Liczby muszą być unikalne",
      "Liczby muszą być w zakresie 1-49"
    ]
  }
}
```

### Nieautoryzowany (401 Unauthorized)
```json
{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
  "title": "Unauthorized",
  "status": 401,
  "detail": "Brak tokenu JWT lub token nieprawidłowy"
}
```

### Brak uprawnień (403 Forbidden)
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.3",
  "title": "Forbidden",
  "status": 403,
  "detail": "Brak uprawnień administratora. Tylko administratorzy mogą edytować losowania."
}
```

### Nie znaleziono (404 Not Found)
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Not Found",
  "status": 404,
  "detail": "Losowanie o podanym ID nie istnieje"
}
```

---

## 5. Przepływ danych

### Diagram sekwencji

```
Client -> API Endpoint: PUT /api/draws/123 + JWT Bearer Token
API Endpoint -> JWT Middleware: Validate token
JWT Middleware --> API Endpoint: User claims (UserId, IsAdmin)
API Endpoint -> MediatR: Send(UpdateDrawRequest)
MediatR -> Validator: Validate(request)
Validator --> MediatR: ValidationResult

[Jeśli walidacja niepomyślna]
MediatR -> Client: 400 Bad Request + błędy

[Jeśli walidacja pomyślna]
MediatR -> Handler: Handle(request)
Handler -> AppDbContext: FindAsync<Draw>(id) + Include(Numbers)
AppDbContext --> Handler: Draw entity lub null

[Jeśli draw == null]
Handler -> Client: 404 Not Found

[Jeśli draw != null]
Handler: Check IsAdmin from JWT claims
[Jeśli IsAdmin == false]
Handler -> Client: 403 Forbidden

[Jeśli IsAdmin == true]
Handler: Begin Transaction
Handler -> AppDbContext: Check DrawDate uniqueness (exclude current draw)
[Jeśli konflikt daty]
Handler -> Client: 400 Bad Request (data już istnieje)

[Jeśli brak konfliktu]
Handler: Update Draw.DrawDate
Handler -> AppDbContext: RemoveRange(draw.Numbers)
Handler: Create 6 new DrawNumber entities
Handler -> AppDbContext: AddRange(newNumbers)
Handler -> AppDbContext: SaveChangesAsync()
Handler: Commit Transaction
Handler --> MediatR: Response("Losowanie zaktualizowane pomyślnie")
MediatR --> API Endpoint: Response
API Endpoint --> Client: 200 OK + Response
```

### Interakcje z bazą danych

**Zapytania:**

1. **Pobranie losowania z liczbami (eager loading):**
```csharp
var draw = await _dbContext.Draws
    .Include(d => d.Numbers.OrderBy(n => n.Position))
    .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken);
```

2. **Sprawdzenie unikalności daty (jeśli zmieniona):**
```csharp
if (draw.DrawDate != request.DrawDate)
{
    var dateExists = await _dbContext.Draws
        .AnyAsync(d => d.DrawDate == request.DrawDate && d.Id != request.Id, cancellationToken);
}
```

3. **Aktualizacja transakcyjna:**
```csharp
using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
try
{
    // Update Draw metadata
    draw.DrawDate = request.DrawDate;

    // Delete old numbers (CASCADE handled by EF Core)
    _dbContext.DrawNumbers.RemoveRange(draw.Numbers);

    // Add new numbers
    for (int i = 0; i < request.Numbers.Length; i++)
    {
        _dbContext.DrawNumbers.Add(new DrawNumber
        {
            DrawId = draw.Id,
            Number = request.Numbers[i],
            Position = (byte)(i + 1)
        });
    }

    await _dbContext.SaveChangesAsync(cancellationToken);
    await transaction.CommitAsync(cancellationToken);
}
catch
{
    await transaction.RollbackAsync(cancellationToken);
    throw;
}
```

**Indeksy wykorzystywane:**
- `IX_Draws_DrawDate` (UNIQUE) - dla sprawdzenia unikalności daty
- `IX_DrawNumbers_DrawId` - dla eager loading liczb

---

## 6. Względy bezpieczeństwa

### Uwierzytelnianie i autoryzacja

**1. JWT Token Validation (middleware):**
- Sprawdzenie obecności header `Authorization: Bearer <token>`
- Weryfikacja podpisu HMAC SHA-256
- Sprawdzenie czasu wygaśnięcia (exp claim)
- Dodanie claims do `HttpContext.User`

**2. Administrator Authorization (handler):**
```csharp
var isAdminClaim = User.FindFirst("IsAdmin")?.Value;
var isAdmin = bool.TryParse(isAdminClaim, out var result) && result;

if (!isAdmin)
{
    return Results.Problem(
        statusCode: StatusCodes.Status403Forbidden,
        title: "Forbidden",
        detail: "Brak uprawnień administratora. Tylko administratorzy mogą edytować losowania."
    );
}
```

**Struktura JWT payload:**
```json
{
  "sub": "123",          // UserId
  "email": "admin@lottotm.com",
  "IsAdmin": "true",     // KRYTYCZNY dla tego endpointa
  "exp": 1730638800,
  "iat": 1730552400
}
```

### Walidacja danych wejściowych

**FluentValidation (Validator.cs):**
```csharp
public class Validator : AbstractValidator<Contracts.Request>
{
    public Validator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("ID losowania musi być większe od 0");

        RuleFor(x => x.DrawDate)
            .NotEmpty()
            .WithMessage("Data losowania jest wymagana")
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.Today))
            .WithMessage("Data losowania nie może być w przyszłości");

        RuleFor(x => x.Numbers)
            .NotEmpty()
            .WithMessage("Liczby są wymagane")
            .Must(n => n.Length == 6)
            .WithMessage("Wymagane dokładnie 6 liczb")
            .Must(n => n.All(num => num >= 1 && num <= 49))
            .WithMessage("Liczby muszą być w zakresie 1-49")
            .Must(n => n.Distinct().Count() == 6)
            .WithMessage("Liczby muszą być unikalne");
    }
}
```

**CHECK constraints w bazie danych (double-check):**
- `CHK_DrawNumbers_Number: [Number] >= 1 AND [Number] <= 49`
- `CHK_DrawNumbers_Position: [Position] >= 1 AND [Position] <= 6`

### Ochrona przed atakami

| Typ ataku | Ochrona | Implementacja |
|-----------|---------|---------------|
| **SQL Injection** | Entity Framework Core automatyczne parametryzowanie | ✅ Automatyczne |
| **Unauthorized Access** | JWT + IsAdmin claim check | ✅ W middleware + handler |
| **Mass Assignment** | DTO kontroluje dozwolone pola | ✅ Request DTO |
| **Data Tampering** | Transakcje + CHECK constraints | ✅ Transaction scope |
| **CSRF** | Stateless API (JWT w header, nie cookie) | ✅ Architektura |
| **XSS** | API nie renderuje HTML | ✅ N/A dla API |

---

## 7. Obsługa błędów

### Hierarchia obsługi błędów

1. **FluentValidation (przed handlerem):**
   - Walidacja struktury i formatów danych
   - Rzuca `ValidationException` z listą błędów
   - Przechwytywane przez `ExceptionHandlingMiddleware`

2. **Business Logic Errors (w handlerze):**
   - 404 Not Found - brak losowania
   - 403 Forbidden - brak uprawnień
   - 400 Bad Request - konflikt daty

3. **Database Errors (EF Core):**
   - `DbUpdateException` - naruszenie constraints
   - `DbUpdateConcurrencyException` - równoczesna edycja
   - Logowane przez Serilog + zwracane jako 500

4. **Global Exception Handler (middleware):**
   - Catch-all dla nieobsłużonych wyjątków
   - Generuje `ProblemDetails` response
   - Loguje stack trace przez Serilog

### Tabela kodów błędów

| Kod | Scenariusz | Response Body | Logowanie |
|-----|-----------|---------------|-----------|
| **200** | Sukces aktualizacji | `{ "message": "..." }` | Info |
| **400** | Nieprawidłowe dane wejściowe | `{ "errors": {...} }` | Warning |
| **400** | Konflikt daty | `{ "errors": { "drawDate": ["..."] } }` | Warning |
| **401** | Brak/nieprawidłowy token JWT | ProblemDetails | Info |
| **403** | Użytkownik nie jest adminem | ProblemDetails | Warning |
| **404** | Losowanie nie istnieje | ProblemDetails | Warning |
| **500** | Błąd bazy/serwera | ProblemDetails + trace | Error |

### Przykładowa obsługa w Handler:

```csharp
// 404 Not Found
if (draw == null)
{
    return Results.Problem(
        statusCode: StatusCodes.Status404NotFound,
        title: "Not Found",
        detail: "Losowanie o podanym ID nie istnieje"
    );
}

// 403 Forbidden
if (!isAdmin)
{
    return Results.Problem(
        statusCode: StatusCodes.Status403Forbidden,
        title: "Forbidden",
        detail: "Brak uprawnień administratora"
    );
}

// 400 Bad Request - konflikt daty
if (dateExists)
{
    return Results.Problem(
        statusCode: StatusCodes.Status400BadRequest,
        title: "Bad Request",
        detail: "Losowanie z podaną datą już istnieje"
    );
}
```

---

## 8. Wydajność

### Kluczowe metryki wydajności

| Metryka | Cel | Sposób osiągnięcia |
|---------|-----|-------------------|
| Response Time (p95) | < 500ms | Indeksy, eager loading, transakcje |
| Concurrent Updates | Obsługa 10 req/s | Optimistic concurrency (EF Core) |
| Database Round Trips | ≤ 3 | Eager loading, batch operations |

### Optymalizacje

**1. Eager Loading (zapobiega N+1 queries):**
```csharp
.Include(d => d.Numbers.OrderBy(n => n.Position))
```
- 1 zapytanie zamiast 1 + 6 (dla każdej liczby osobno)

**2. Indeksy wykorzystywane:**
- `IX_Draws_DrawDate` (UNIQUE) - O(log n) lookup dla walidacji unikalności
- `IX_DrawNumbers_DrawId` - O(log n) JOIN dla eager loading

**3. Transakcja (zapewnia ACID):**
- `BeginTransactionAsync()` - minimalizuje czas blokady
- `SaveChangesAsync()` + `CommitAsync()` - batch commit
- Rollback automatyczny przy wyjątku

**4. Asynchroniczne operacje:**
- `FirstOrDefaultAsync()` - nie blokuje wątku podczas I/O
- `SaveChangesAsync()` - non-blocking database writes

### Potencjalne wąskie gardła

| Wąskie gardło | Prawdopodobieństwo | Mitigation |
|---------------|-------------------|------------|
| **Blokada tabeli Draws** | Średnie (równoczesne edycje) | Optimistic concurrency, row-level locking |
| **Długie transakcje** | Niskie (proste operacje) | Minimalizacja logiki w transakcji |
| **N+1 queries** | Niskie (eager loading) | `.Include()` dla DrawNumbers |
| **Walidacja unikalności daty** | Niskie (indeks UNIQUE) | `IX_Draws_DrawDate` index seek |

### Monitoring i logowanie

**Serilog - kluczowe punkty logowania:**
```csharp
_logger.LogInformation("Updating draw {DrawId} by admin {UserId}", request.Id, userId);
_logger.LogWarning("Draw {DrawId} not found", request.Id);
_logger.LogError(ex, "Failed to update draw {DrawId}", request.Id);
_logger.LogInformation("Draw {DrawId} updated successfully in {ElapsedMs}ms", request.Id, elapsed);
```

---

## 9. Etapy wdrożenia

### Faza 1: Struktura projektu (15 min)

1. **Utworzenie folderu feature:**
   ```
   src/server/LottoTM.Server.Api/Features/Draws/UpdateDraw/
   ```

2. **Utworzenie plików:**
   - `Endpoint.cs` - rejestracja endpointa PUT
   - `Contracts.cs` - Request i Response DTOs
   - `Handler.cs` - logika biznesowa
   - `Validator.cs` - FluentValidation rules

### Faza 2: Implementacja Contracts.cs (5 min)

```csharp
using MediatR;

namespace LottoTM.Server.Api.Features.Draws.UpdateDraw;

public class Contracts
{
    public record Request : IRequest<Response>
    {
        public int Id { get; init; }
        public DateOnly DrawDate { get; init; }
        public int[] Numbers { get; init; } = Array.Empty<int>();
    }

    public record Response(string Message);
}
```

### Faza 3: Implementacja Validator.cs (10 min)

```csharp
using FluentValidation;

namespace LottoTM.Server.Api.Features.Draws.UpdateDraw;

public class Validator : AbstractValidator<Contracts.Request>
{
    public Validator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("ID losowania musi być większe od 0");

        RuleFor(x => x.DrawDate)
            .NotEmpty()
            .WithMessage("Data losowania jest wymagana")
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.Today))
            .WithMessage("Data losowania nie może być w przyszłości");

        RuleFor(x => x.Numbers)
            .NotEmpty()
            .WithMessage("Liczby są wymagane")
            .Must(n => n.Length == 6)
            .WithMessage("Wymagane dokładnie 6 liczb")
            .Must(n => n.All(num => num >= 1 && num <= 49))
            .WithMessage("Liczby muszą być w zakresie 1-49")
            .Must(n => n.Distinct().Count() == 6)
            .WithMessage("Liczby muszą być unikalne");
    }
}
```

### Faza 4: Implementacja Handler.cs (30 min)

```csharp
using FluentValidation;
using LottoTM.Server.Api.Entities;
using LottoTM.Server.Api.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LottoTM.Server.Api.Features.Draws.UpdateDraw;

public class UpdateDrawHandler : IRequestHandler<Contracts.Request, IResult>
{
    private readonly AppDbContext _dbContext;
    private readonly IValidator<Contracts.Request> _validator;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<UpdateDrawHandler> _logger;

    public UpdateDrawHandler(
        AppDbContext dbContext,
        IValidator<Contracts.Request> validator,
        IHttpContextAccessor httpContextAccessor,
        ILogger<UpdateDrawHandler> logger)
    {
        _dbContext = dbContext;
        _validator = validator;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<IResult> Handle(Contracts.Request request, CancellationToken cancellationToken)
    {
        // 1. Walidacja FluentValidation
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // 2. Pobranie użytkownika z JWT claims
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User == null)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized",
                detail: "Brak tokenu JWT"
            );
        }

        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var isAdminClaim = httpContext.User.FindFirst("IsAdmin")?.Value;

        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized",
                detail: "Nieprawidłowy token JWT"
            );
        }

        var isAdmin = bool.TryParse(isAdminClaim, out var adminResult) && adminResult;

        // 3. Sprawdzenie uprawnień administratora
        if (!isAdmin)
        {
            _logger.LogWarning("User {UserId} attempted to update draw {DrawId} without admin privileges",
                userId, request.Id);

            return Results.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Forbidden",
                detail: "Brak uprawnień administratora. Tylko administratorzy mogą edytować losowania."
            );
        }

        // 4. Pobranie losowania z bazy danych (z eager loading liczb)
        var draw = await _dbContext.Draws
            .Include(d => d.Numbers.OrderBy(n => n.Position))
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken);

        if (draw == null)
        {
            _logger.LogWarning("Draw {DrawId} not found for update", request.Id);

            return Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found",
                detail: "Losowanie o podanym ID nie istnieje"
            );
        }

        // 5. Sprawdzenie unikalności daty (jeśli zmieniona)
        if (draw.DrawDate != request.DrawDate)
        {
            var dateExists = await _dbContext.Draws
                .AnyAsync(d => d.DrawDate == request.DrawDate && d.Id != request.Id, cancellationToken);

            if (dateExists)
            {
                _logger.LogWarning("Draw date {DrawDate} already exists for another draw", request.DrawDate);

                return Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Bad Request",
                    detail: "Losowanie z podaną datą już istnieje w systemie"
                );
            }
        }

        // 6. Aktualizacja transakcyjna
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            _logger.LogInformation("Updating draw {DrawId} by admin {UserId}", request.Id, userId);

            // Update Draw metadata
            draw.DrawDate = request.DrawDate;

            // Delete old numbers (EF Core tracking handles CASCADE)
            _dbContext.DrawNumbers.RemoveRange(draw.Numbers);

            // Add new numbers with positions 1-6
            for (int i = 0; i < request.Numbers.Length; i++)
            {
                _dbContext.DrawNumbers.Add(new DrawNumber
                {
                    DrawId = draw.Id,
                    Number = request.Numbers[i],
                    Position = (byte)(i + 1)
                });
            }

            // Save changes and commit transaction
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Draw {DrawId} updated successfully", request.Id);

            return Results.Ok(new Contracts.Response("Losowanie zaktualizowane pomyślnie"));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Failed to update draw {DrawId}", request.Id);
            throw;
        }
    }
}
```

**Uwaga:** Handler zwraca `IResult` zamiast `Response` dla lepszej kontroli kodów HTTP.

### Faza 5: Implementacja Endpoint.cs (10 min)

```csharp
using MediatR;

namespace LottoTM.Server.Api.Features.Draws.UpdateDraw;

public static class Endpoint
{
    public static void AddEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPut("api/draws/{id:int}", async (int id, UpdateDrawDto dto, IMediator mediator, HttpContext httpContext) =>
        {
            var request = new Contracts.Request
            {
                Id = id,
                DrawDate = dto.DrawDate,
                Numbers = dto.Numbers
            };

            return await mediator.Send(request);
        })
        .RequireAuthorization() // Wymaga JWT
        .WithName("UpdateDraw")
        .Produces<Contracts.Response>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .WithOpenApi();
    }

    /// <summary>
    /// DTO for binding request body (without Id, which comes from route)
    /// </summary>
    public record UpdateDrawDto(DateOnly DrawDate, int[] Numbers);
}
```

**Uwaga:** Potrzebny osobny DTO `UpdateDrawDto` dla body, ponieważ `id` pochodzi z route parametru.

### Faza 6: Rejestracja w Program.cs (2 min)

Dodać linię po istniejącej rejestracji ApiVersion:

```csharp
// Istniejące
LottoTM.Server.Api.Features.ApiVersion.Endpoint.AddEndpoint(app);

// DODAĆ:
LottoTM.Server.Api.Features.Draws.UpdateDraw.Endpoint.AddEndpoint(app);
```

### Faza 7: Rejestracja IHttpContextAccessor (jeśli nie istnieje) (2 min)

W `Program.cs` przed `builder.Build()`:

```csharp
// Dodać jeśli nie istnieje
builder.Services.AddHttpContextAccessor();
```

### Faza 8: Testy jednostkowe (30 min)

**Utworzyć plik:** `tests/server/LottoTM.Server.Api.Tests/Features/Draws/UpdateDraw/EndpointTests.cs`

```csharp
using LottoTM.Server.Api.Features.Draws.UpdateDraw;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace LottoTM.Server.Api.Tests.Features.Draws.UpdateDraw;

public class EndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public EndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task UpdateDraw_WithoutToken_Returns401()
    {
        // Arrange
        var dto = new { drawDate = "2025-10-30", numbers = new[] { 1, 2, 3, 4, 5, 6 } };

        // Act
        var response = await _client.PutAsJsonAsync("/api/draws/1", dto);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateDraw_WithNonAdminToken_Returns403()
    {
        // Arrange - TODO: Generate JWT with IsAdmin=false
        // Act
        // Assert
    }

    [Fact]
    public async Task UpdateDraw_WithInvalidData_Returns400()
    {
        // Arrange - TODO: Generate admin JWT
        var dto = new { drawDate = "2099-10-30", numbers = new[] { 1, 2, 3 } }; // Future date + 3 numbers

        // Act
        // var response = await _client.PutAsJsonAsync("/api/draws/1", dto);

        // Assert
        // Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateDraw_WithNonExistentId_Returns404()
    {
        // Arrange - TODO: Generate admin JWT
        // Act
        // Assert
    }

    [Fact]
    public async Task UpdateDraw_WithValidData_Returns200()
    {
        // Arrange - TODO: Seed database + generate admin JWT
        // Act
        // Assert
    }
}
```

### Faza 9: Testowanie manualne (15 min)

**1. Uruchomić backend:**
```bash
dotnet run --project src/server/LottoTM.Server.Api/LottoTM.Server.Api.csproj
```

**2. Testować przez curl/Postman:**

**A) Test bez tokenu (401):**
```bash
curl -X PUT http://localhost:5000/api/draws/1 \
  -H "Content-Type: application/json" \
  -d '{"drawDate":"2025-10-30","numbers":[1,2,3,4,5,6]}'
```

**B) Test z tokenem admin (200):**
```bash
# Najpierw zalogować się jako admin i skopiować token
# Potem:
curl -X PUT http://localhost:5000/api/draws/1 \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <TOKEN>" \
  -d '{"drawDate":"2025-10-30","numbers":[1,2,3,4,5,6]}'
```

**C) Test z nieprawidłowymi danymi (400):**
```bash
curl -X PUT http://localhost:5000/api/draws/1 \
  -H "Authorization: Bearer <ADMIN_TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{"drawDate":"2099-10-30","numbers":[1,2,3]}'
```

### Faza 10: Code review i dokumentacja (10 min)

**Checklist:**
- [ ] Wszystkie pliki utworzone (Endpoint, Contracts, Handler, Validator)
- [ ] Handler zwraca poprawne kody HTTP (200, 400, 401, 403, 404)
- [ ] Walidacja FluentValidation pokrywa wszystkie przypadki
- [ ] Transakcja używa `using` statement dla auto-rollback
- [ ] Eager loading używa `.Include()` dla DrawNumbers
- [ ] Logowanie Serilog na poziomach Info/Warning/Error
- [ ] Endpoint zarejestrowany w Program.cs
- [ ] IHttpContextAccessor zarejestrowany w DI
- [ ] Testy jednostkowe utworzone (nawet jako TODO)
- [ ] Endpoint testowany manualnie

---

## 10. Podsumowanie

### Kluczowe decyzje implementacyjne

| Decyzja | Uzasadnienie |
|---------|--------------|
| **IResult zamiast Response w Handler** | Lepsze wsparcie dla kodów HTTP (Results.Problem, Results.Ok) |
| **Osobny UpdateDrawDto w Endpoint** | `id` z route, body bez `id` - separacja concerns |
| **IHttpContextAccessor dla JWT claims** | Dostęp do User.Claims w handlerze |
| **Transakcja dla Delete+Insert** | ACID - wszystko lub nic |
| **Eager loading DrawNumbers** | Zapobiega N+1 queries |
| **Walidacja unikalności daty w handlerze** | FluentValidation nie ma dostępu do DB context |
| **Logowanie Serilog** | Audyt działań administratora |

### Potencjalne rozszerzenia (post-MVP)

- [ ] Optimistic concurrency (RowVersion w Draw entity)
- [ ] Soft delete zamiast hard delete (IsDeleted flag)
- [ ] Event sourcing dla historii zmian
- [ ] Walidacja duplikatów liczb na poziomie bazy (computed column hash)
- [ ] Caching częstych zapytań (Redis)
- [ ] Rate limiting dla endpointa (max 10 req/min per admin)

### Zgodność z wymaganiami

| Wymaganie | Status | Implementacja |
|-----------|--------|---------------|
| NFR-002: CRUD ≤500ms | ✅ | Indeksy + eager loading + transakcje |
| NFR-005: Haszowanie haseł | ✅ | BCrypt (nie dotyczy tego endpointa) |
| NFR-006: JWT 24h | ✅ | Middleware JWT (istniejące) |
| NFR-007: HTTPS | ✅ | Program.cs (produkcja) |
| NFR-008: Parametryzowane zapytania | ✅ | EF Core automatyczne |
| F-AUTH-004: Izolacja danych | ✅ | IsAdmin check |
| F-DRAW-003: Edycja losowań | ✅ | Cały endpoint |

---

**Czas implementacji:** ~2 godziny (z testami)

**Autor planu:** AI Assistant (Claude)
**Data:** 2025-11-05
**Wersja:** 1.0
