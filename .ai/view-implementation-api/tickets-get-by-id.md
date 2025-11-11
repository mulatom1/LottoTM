# API Endpoint Implementation Plan: GET /api/tickets/{id}

## 1. Przegląd punktu końcowego

Endpoint służy do pobierania szczegółów pojedynczego zestawu liczb LOTTO należącego do zalogowanego użytkownika. Zestaw jest identyfikowany przez ID (int). Endpoint zwraca szczegóły zestawu wraz z 6 liczbami, datą utworzenia oraz ID użytkownika.

**Kluczowe aspekty:**
- Wymaga autoryzacji JWT
- Weryfikacja własności zestawu (czy zestaw należy do zalogowanego użytkownika)
- Zwraca 403 Forbidden jeśli zestaw należy do innego użytkownika (security by obscurity)
- Zwraca 404 Not Found jeśli zestaw nie istnieje
- Eager loading liczb zestawu dla wydajności

## 2. Szczegóły żądania

**Metoda HTTP:** GET

**Struktura URL:** `/api/tickets/{id}`

**Parametry:**
- **Wymagane:**
  - `id` (route parameter): ID zestawu (int)
    - Przykład: `123`
    - Format: liczba całkowita dodatnia
    - Walidacja: > 0

- **Opcjonalne:** Brak

**Headers:**
- `Authorization: Bearer <JWT_TOKEN>` (wymagany)
- `Content-Type: application/json`

**Request Body:** Brak (metoda GET)

## 3. Wykorzystywane typy

### DTOs (Data Transfer Objects)

```csharp
namespace LottoTM.Server.Api.Features.Tickets;

public class Contracts
{
    // Request bez ciała - ID w parametrze route
    public record GetByIdRequest(int Id) : IRequest<GetByIdResponse>;

    // Response z detalami zestawu
    public record GetByIdResponse(
        int Id,
        int UserId,
        string GroupName,
        int[] Numbers,
        DateTime CreatedAt
    );
}
```

### Entity Models (z db-plan.md)

```csharp
// Ticket.cs - główna encja zestawu
public class Ticket
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string GroupName { get; set; } = "";
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public ICollection<TicketNumber> Numbers { get; set; } = new List<TicketNumber>();
}

// TicketNumber.cs - pojedyncza liczba w zestawie
public class TicketNumber
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public int Number { get; set; }
    public byte Position { get; set; }

    // Navigation property
    public Ticket Ticket { get; set; } = null!;
}
```

## 4. Szczegóły odpowiedzi

### Sukces (200 OK)

```json
{
  "id": 123,
  "userId": 123,
  "groupName": "Ulubione",
  "numbers": [5, 14, 23, 29, 37, 41],
  "createdAt": "2025-10-25T10:00:00Z"
}
```

**Content-Type:** `application/json`

### Błędy

#### 400 Bad Request - Nieprawidłowy format ID

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "id": ["ID musi być liczbą całkowitą dodatnią"]
  }
}
```

#### 401 Unauthorized - Brak tokenu lub token nieprawidłowy

```json
{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
  "title": "Unauthorized",
  "status": 401
}
```

#### 403 Forbidden - Zestaw należy do innego użytkownika

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.3",
  "title": "Forbidden",
  "status": 403,
  "detail": "Nie masz uprawnień do tego zasobu"
}
```

**Uwaga:** Zwracamy 403 zamiast 404 nawet jeśli zestaw istnieje ale należy do innego użytkownika (security by obscurity - nie ujawniamy czy zasób istnieje).

#### 404 Not Found - Zestaw nie istnieje w systemie

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Not Found",
  "status": 404,
  "detail": "Zestaw o podanym ID nie istnieje"
}
```

## 5. Przepływ danych

### Diagram sekwencji

```
Client → [JWT Token] → API Endpoint
                           ↓
                      JWT Middleware
                      (walidacja tokenu,
                       ekstrakcja UserId)
                           ↓
                      MediatR Handler
                           ↓
                    FluentValidation
                    (walidacja ID > 0)
                           ↓
                      DbContext Query
                      (Tickets + TicketNumbers)
                           ↓
                   Sprawdzenie własności
                   (ticket.UserId == currentUserId)
                           ↓
                      Mapowanie do DTO
                           ↓
                      Response 200 OK
                           ↓
                        Client
```

### Szczegółowy przepływ

1. **Request przychodzi do endpointu** z parametrem `{id}` w URL
2. **JWT Middleware** waliduje token i ekstraktuje `UserId` do `HttpContext.User`
3. **Endpoint** wywołuje MediatR z `GetByIdRequest(id)`
4. **FluentValidation** waliduje czy ID > 0
5. **Handler** pobiera `currentUserId` z JWT claims
6. **Query do bazy danych**:
   ```csharp
   var ticket = await db.Tickets
       .Include(t => t.Numbers.OrderBy(n => n.Position))
       .FirstOrDefaultAsync(t => t.Id == id && t.UserId == currentUserId);
   ```
7. **Weryfikacja wyniku**:
   - Jeśli `ticket == null`: sprawdź czy zestaw istnieje w ogóle
     - Jeśli istnieje (ale należy do innego użytkownika): zwróć 403 Forbidden
     - Jeśli nie istnieje: zwróć 404 Not Found
   - Jeśli `ticket != null`: kontynuuj
8. **Mapowanie do Response**:
   ```csharp
   var numbers = ticket.Numbers
       .OrderBy(n => n.Position)
       .Select(n => n.Number)
       .ToArray();

   return new GetByIdResponse(ticket.Id, ticket.UserId, numbers, ticket.CreatedAt);
   ```
9. **Zwrot odpowiedzi** 200 OK z JSON

### Wykorzystywane indeksy bazy danych

- **Primary Key** na `Tickets.Id` - szybkie wyszukiwanie po ID
- **Index** `IX_Tickets_UserId` - optymalizacja filtrowania po właścicielu
- **Index** `IX_TicketNumbers_TicketId` - szybki JOIN z liczbami

## 6. Względy bezpieczeństwa

### 6.1 Uwierzytelnianie i Autoryzacja

**JWT Token (wymagany):**
- Endpoint wymaga nagłówka `Authorization: Bearer <token>`
- JWT Middleware waliduje:
  - Podpis tokenu (HMAC SHA-256)
  - Data wygaśnięcia (`exp` claim)
  - Poprawność struktury
- Z tokenu ekstraktowany jest `UserId` (claim `sub` lub `NameIdentifier`)

**Weryfikacja własności zasobu:**
```csharp
// KRYTYCZNE: ZAWSZE filtruj po UserId
var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

var ticket = await db.Tickets
    .Where(t => t.Id == id && t.UserId == currentUserId)
    .FirstOrDefaultAsync();
```

**Security by obscurity:**
- Jeśli zestaw istnieje ale należy do innego użytkownika: zwróć 403 Forbidden
- Nigdy nie zwracaj 404 dla istniejącego zestawu należącego do innego użytkownika
- Zapobiega to atakom typu "enumeration" (zgadywanie ID zestawów)

### 6.2 Walidacja danych wejściowych

**Walidacja formatu ID:**
```csharp
public class GetByIdRequestValidator : AbstractValidator<GetByIdRequest>
{
    public GetByIdRequestValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("ID musi być liczbą całkowitą dodatnią");
    }
}
```

### 6.3 Ochrona przed atakami

**SQL Injection:**
- Entity Framework Core automatycznie parametryzuje zapytania
- NIGDY nie używaj `FromSqlRaw()` z konkatenacją stringów

**Brute force / Enumeration:**
- Security by obscurity (403 zamiast 404)
- Rate limiting na poziomie middleware (opcjonalne, post-MVP)

**CORS:**
- Konfiguracja w `Program.cs` pozwala tylko na znane origins frontendu
- Preflight requests obsługiwane automatycznie przez ASP.NET Core

## 7. Obsługa błędów

### 7.1 Scenariusze błędów

| Kod | Scenariusz | Przyczyna | Response |
|-----|------------|-----------|----------|
| 400 | Bad Request | Nieprawidłowy format ID (np. ujemna liczba, 0) | ProblemDetails z błędami walidacji |
| 401 | Unauthorized | Brak tokenu JWT lub token wygasły/nieprawidłowy | ProblemDetails z komunikatem "Unauthorized" |
| 403 | Forbidden | Zestaw istnieje ale należy do innego użytkownika | ProblemDetails z komunikatem "Nie masz uprawnień" |
| 404 | Not Found | Zestaw o podanym ID nie istnieje w systemie | ProblemDetails z komunikatem "Nie znaleziono" |
| 500 | Internal Server Error | Błąd bazy danych, nieoczekiwany wyjątek | ProblemDetails z ogólnym komunikatem |

### 7.2 Implementacja obsługi błędów

**W Handlerze:**
```csharp
// Sprawdzenie własności z security by obscurity
var ticket = await db.Tickets
    .Include(t => t.Numbers.OrderBy(n => n.Position))
    .FirstOrDefaultAsync(t => t.Id == request.Id && t.UserId == currentUserId);

if (ticket == null)
{
    // Sprawdź czy zestaw w ogóle istnieje
    var exists = await db.Tickets.AnyAsync(t => t.Id == request.Id);

    if (exists)
    {
        // Zestaw istnieje, ale należy do innego użytkownika
        throw new ForbiddenException("Nie masz uprawnień do tego zasobu");
    }
    else
    {
        // Zestaw nie istnieje w systemie
        throw new NotFoundException("Zestaw o podanym ID nie istnieje");
    }
}
```

**Custom Exceptions (do utworzenia):**
```csharp
public class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message) { }
}

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}
```

**Global Exception Handler Middleware:**
- Już istnieje w projekcie (`ExceptionHandlingMiddleware`)
- Obsługuje `ValidationException` → 400
- Obsługuje `ForbiddenException` → 403
- Obsługuje `NotFoundException` → 404
- Obsługuje pozostałe wyjątki → 500
- Loguje błędy przez Serilog

### 7.3 Logging

**Poziomy logowania:**
```csharp
// Information - normalny flow
_logger.LogInformation("Pobieranie zestawu {TicketId} dla użytkownika {UserId}", request.Id, currentUserId);

// Warning - próba dostępu do cudzego zasobu
_logger.LogWarning("Użytkownik {UserId} próbował uzyskać dostęp do zestawu {TicketId} należącego do innego użytkownika", currentUserId, request.Id);

// Error - błędy bazy danych
_logger.LogError(ex, "Błąd podczas pobierania zestawu {TicketId}", request.Id);
```

## 8. Wydajność

### 8.1 Optymalizacje

**Eager Loading:**
```csharp
// ✅ DOBRZE - 1 zapytanie SQL z JOIN
var ticket = await db.Tickets
    .Include(t => t.Numbers.OrderBy(n => n.Position))
    .FirstOrDefaultAsync(t => t.Id == id && t.UserId == currentUserId);

// ❌ ŹLE - N+1 problem (7 zapytań SQL)
var ticket = await db.Tickets.FirstOrDefaultAsync(t => t.Id == id);
var numbers = await db.TicketNumbers.Where(n => n.TicketId == id).ToListAsync();
```

**Wykorzystanie indeksów:**
- Query automatycznie wykorzysta:
  - Primary Key index na `Tickets.Id` (INT)
  - Non-clustered index `IX_Tickets_UserId`
  - Foreign Key index `IX_TicketNumbers_TicketId`

**Projection (opcjonalna optymalizacja):**
```csharp
// Jeśli nie potrzebujemy wszystkich pól
var ticketDto = await db.Tickets
    .Where(t => t.Id == id && t.UserId == currentUserId)
    .Select(t => new GetByIdResponse(
        t.Id,
        t.UserId,
        t.Numbers.OrderBy(n => n.Position).Select(n => n.Number).ToArray(),
        t.CreatedAt
    ))
    .FirstOrDefaultAsync();
```

### 8.2 Wymagania wydajnościowe

**NFR-002:** Operacje CRUD ≤ 500ms (95 percentile)

**Oczekiwany czas odpowiedzi:**
- Średnio: 50-100ms (z indeksami)
- 95 percentile: <300ms
- Maksymalnie: <500ms

**Testowanie wydajności:**
- Unit testy z in-memory database
- Integration testy z rzeczywistą bazą SQL Server
- Load testing z narzędziem k6 lub JMeter (100 concurrent requests)

### 8.3 Caching (post-MVP)

Dla tego endpointu caching nie jest krytyczny (zestawy rzadko się zmieniają), ale można rozważyć:

**Redis Cache (opcjonalnie):**
```csharp
var cacheKey = $"ticket:{id}";
var cached = await _cache.GetAsync<GetByIdResponse>(cacheKey);

if (cached != null)
    return cached;

// ... query do bazy
var result = // ...

await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(15));
return result;
```

**Invalidation:**
- Przy edycji zestawu: `await _cache.RemoveAsync($"ticket:{id}")`
- Przy usunięciu: `await _cache.RemoveAsync($"ticket:{id}")`

## 9. Kroki implementacji

### Krok 1: Utworzenie struktury plików

Utworzyć folder `Features/Tickets/` z plikami:
```
Features/
  Tickets/
    Endpoint.cs          - definicja endpointu GET /api/tickets/{id}
    Contracts.cs         - GetByIdRequest, GetByIdResponse
    GetByIdHandler.cs    - handler dla MediatR
    GetByIdValidator.cs  - walidacja FluentValidation
```

### Krok 2: Definicja kontraktów (Contracts.cs)

```csharp
using MediatR;

namespace LottoTM.Server.Api.Features.Tickets;

public class Contracts
{
    public record GetByIdRequest(int Id) : IRequest<GetByIdResponse>;

    public record GetByIdResponse(
        int Id,
        int UserId,
        int[] Numbers,
        DateTime CreatedAt
    );
}
```

### Krok 3: Walidacja (GetByIdValidator.cs)

```csharp
using FluentValidation;

namespace LottoTM.Server.Api.Features.Tickets;

public class GetByIdValidator : AbstractValidator<Contracts.GetByIdRequest>
{
    public GetByIdValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("ID musi być liczbą całkowitą dodatnią");
    }
}
```

### Krok 4: Handler (GetByIdHandler.cs)

```csharp
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using LottoTM.Server.Api.Data;

namespace LottoTM.Server.Api.Features.Tickets;

public class GetByIdHandler : IRequestHandler<Contracts.GetByIdRequest, Contracts.GetByIdResponse>
{
    private readonly ApplicationDbContext _db;
    private readonly IValidator<Contracts.GetByIdRequest> _validator;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<GetByIdHandler> _logger;

    public GetByIdHandler(
        ApplicationDbContext db,
        IValidator<Contracts.GetByIdRequest> validator,
        IHttpContextAccessor httpContextAccessor,
        ILogger<GetByIdHandler> logger)
    {
        _db = db;
        _validator = validator;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<Contracts.GetByIdResponse> Handle(
        Contracts.GetByIdRequest request,
        CancellationToken cancellationToken)
    {
        // 1. Walidacja
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // 2. Pobranie UserId z JWT
        var currentUserId = int.Parse(
            _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("Brak autoryzacji")
        );

        _logger.LogInformation(
            "Pobieranie zestawu {TicketId} dla użytkownika {UserId}",
            request.Id, currentUserId
        );

        // 3. Query z weryfikacją własności
        var ticket = await _db.Tickets
            .Include(t => t.Numbers.OrderBy(n => n.Position))
            .FirstOrDefaultAsync(
                t => t.Id == request.Id && t.UserId == currentUserId,
                cancellationToken
            );

        // 4. Obsługa braku zasobu (security by obscurity)
        if (ticket == null)
        {
            var exists = await _db.Tickets.AnyAsync(
                t => t.Id == request.Id,
                cancellationToken
            );

            if (exists)
            {
                _logger.LogWarning(
                    "Użytkownik {UserId} próbował uzyskać dostęp do zestawu {TicketId} należącego do innego użytkownika",
                    currentUserId, request.Id
                );
                throw new ForbiddenException("Nie masz uprawnień do tego zasobu");
            }
            else
            {
                _logger.LogInformation(
                    "Zestaw {TicketId} nie istnieje w systemie",
                    request.Id
                );
                throw new NotFoundException("Zestaw o podanym ID nie istnieje");
            }
        }

        // 5. Mapowanie do DTO
        var numbers = ticket.Numbers
            .OrderBy(n => n.Position)
            .Select(n => n.Number)
            .ToArray();

        return new Contracts.GetByIdResponse(
            ticket.Id,
            ticket.UserId,
            numbers,
            ticket.CreatedAt
        );
    }
}
```

### Krok 5: Endpoint (Endpoint.cs)

```csharp
using MediatR;
using Microsoft.AspNetCore.Authorization;

namespace LottoTM.Server.Api.Features.Tickets;

public static class Endpoint
{
    public static void AddEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("api/tickets/{id:int}",
            [Authorize] async (int id, IMediator mediator) =>
            {
                var request = new Contracts.GetByIdRequest(id);
                var result = await mediator.Send(request);
                return Results.Ok(result);
            })
            .WithName("GetTicketById")
            .Produces<Contracts.GetByIdResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .WithOpenApi();
    }
}
```

### Krok 6: Utworzenie Custom Exceptions

W folderze `Common/Exceptions/`:

```csharp
// ForbiddenException.cs
namespace LottoTM.Server.Api.Common.Exceptions;

public class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message) { }
}

// NotFoundException.cs
namespace LottoTM.Server.Api.Common.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}
```

### Krok 7: Aktualizacja Global Exception Handler

W `Middleware/ExceptionHandlingMiddleware.cs` dodać obsługę nowych wyjątków:

```csharp
// Obsługa ForbiddenException
catch (ForbiddenException ex)
{
    context.Response.StatusCode = StatusCodes.Status403Forbidden;
    await context.Response.WriteAsJsonAsync(new ProblemDetails
    {
        Status = StatusCodes.Status403Forbidden,
        Title = "Forbidden",
        Detail = ex.Message
    });
}

// Obsługa NotFoundException
catch (NotFoundException ex)
{
    context.Response.StatusCode = StatusCodes.Status404NotFound;
    await context.Response.WriteAsJsonAsync(new ProblemDetails
    {
        Status = StatusCodes.Status404NotFound,
        Title = "Not Found",
        Detail = ex.Message
    });
}
```

### Krok 8: Rejestracja endpointu w Program.cs

```csharp
// W pliku Program.cs, po konfiguracji middleware
using LottoTM.Server.Api.Features.Tickets;

// ...

app.UseAuthorization();

// Rejestracja endpointów
Tickets.Endpoint.AddEndpoint(app);

app.Run();
```

### Krok 9: Testy jednostkowe

Utworzyć `tests/server/LottoTM.Server.Api.Tests/Features/Tickets/GetByIdEndpointTests.cs`:

```csharp
using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;

namespace LottoTM.Server.Api.Tests.Features.Tickets;

public class GetByIdEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public GetByIdEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetTicketById_WithValidId_ReturnsOk()
    {
        // Arrange
        var ticketId = 123; // W rzeczywistości: zestaw utworzony w setup
        _client.DefaultRequestHeaders.Add("Authorization", "Bearer valid-jwt-token");

        // Act
        var response = await _client.GetAsync($"/api/tickets/{ticketId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var ticket = await response.Content.ReadFromJsonAsync<GetByIdResponse>();
        ticket.Should().NotBeNull();
        ticket!.Id.Should().Be(ticketId);
        ticket.Numbers.Should().HaveCount(6);
    }

    [Fact]
    public async Task GetTicketById_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var ticketId = 123;

        // Act
        var response = await _client.GetAsync($"/api/tickets/{ticketId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetTicketById_WithInvalidId_ReturnsBadRequest()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("Authorization", "Bearer valid-jwt-token");

        // Act
        var response = await _client.GetAsync("/api/tickets/0");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetTicketById_WhenNotOwner_ReturnsForbidden()
    {
        // Arrange
        var ticketId = 456; // Zestaw należący do innego użytkownika
        _client.DefaultRequestHeaders.Add("Authorization", "Bearer other-user-jwt-token");

        // Act
        var response = await _client.GetAsync($"/api/tickets/{ticketId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetTicketById_WhenNotExists_ReturnsNotFound()
    {
        // Arrange
        var ticketId = 99999; // Nieistniejący zestaw
        _client.DefaultRequestHeaders.Add("Authorization", "Bearer valid-jwt-token");

        // Act
        var response = await _client.GetAsync($"/api/tickets/{ticketId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
```

### Krok 10: Weryfikacja i deployment

1. **Uruchomienie testów:**
   ```bash
   dotnet test tests/server/LottoTM.Server.Api.Tests/
   ```

2. **Uruchomienie API:**
   ```bash
   dotnet run --project src/server/LottoTM.Server.Api/
   ```

3. **Testowanie ręczne (Postman/curl):**
   ```bash
   # Pobranie zestawu
   curl -X GET "https://localhost:5001/api/tickets/123" \
     -H "Authorization: Bearer <jwt-token>" \
     -H "Content-Type: application/json"
   ```

4. **Weryfikacja w Swagger UI:**
   - Przejdź do `https://localhost:5001/swagger`
   - Znajdź endpoint `GET /api/tickets/{id}`
   - Kliknij "Try it out"
   - Wprowadź ID zestawu
   - Dodaj JWT token w "Authorize"
   - Kliknij "Execute"

5. **Monitoring i logging:**
   - Sprawdź logi w `Logs/applog-{Date}.txt`
   - Zweryfikuj poprawne logowanie zapytań i błędów
   - Sprawdź metryki wydajności (czas odpowiedzi <500ms)

---

## Podsumowanie

Endpoint `GET /api/tickets/{id}` zapewnia bezpieczny dostęp do szczegółów pojedynczego zestawu liczb LOTTO. Kluczowe aspekty implementacji to:

1. **Bezpieczeństwo:** Weryfikacja JWT + własności zasobu + security by obscurity
2. **Wydajność:** Eager loading + indeksy bazy danych
3. **Walidacja:** FluentValidation dla formatu ID (int > 0)
4. **Obsługa błędów:** Global exception handler + szczegółowe komunikaty
5. **Architektura:** Vertical Slice + MediatR + CQRS pattern

Implementacja zgodna z wzorcem `ApiVersion` i wytycznymi projektu LottoTM.
