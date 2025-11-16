# API Endpoint Implementation Plan: DELETE /api/tickets/{id}

**Wersja:** 1.0  
**Data:** 2025-11-05  
**Endpoint:** `DELETE /api/tickets/{id}`  
**Moduł:** Tickets (Zestawy)

---

## 1. Przegląd punktu końcowego

Endpoint służy do usuwania zestawu liczb LOTTO należącego do zalogowanego użytkownika. Operacja wymaga autoryzacji JWT i weryfikacji własności zasobu. Usunięcie zestawu skutkuje automatycznym usunięciem powiązanych liczb z tabeli `TicketNumbers` dzięki kaskadowemu usuwaniu (CASCADE DELETE).

**Główne funkcje:**
- Usunięcie zestawu liczb użytkownika
- Weryfikacja własności zasobu (IDOR protection)
- Kaskadowe usunięcie powiązanych danych (TicketNumbers)
- Ochrona przed enumeration attacks (zwracanie 403 zamiast 404)

---

## 2. Szczegóły żądania

### Metoda HTTP
`DELETE`

### Struktura URL
```
DELETE /api/tickets/{id}
```

### Parametry

#### Parametry URL (Route Parameters)
- **`id`** (wymagany)
  - Typ: `int` 
  - Opis: Unikalny identyfikator zestawu do usunięcia
  - Przykład: `1`
  - Walidacja: Automatyczna przez routing ASP.NET Core (format int)

#### Parametry Query
Brak

#### Request Body
Brak (DELETE nie wymaga body)

#### Headers
- **`Authorization`** (wymagany)
  - Format: `Bearer <JWT_TOKEN>`
  - Opis: Token JWT zawierający UserId zalogowanego użytkownika
  - Walidacja: Middleware uwierzytelniania ASP.NET Core

### Przykład żądania
```http
DELETE /api/tickets/1 HTTP/1.1
Host: localhost:5000
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

---

## 3. Wykorzystywane typy

### Namespace
```csharp
LottoTM.Server.Api.Features.Tickets.Delete
```

### Contracts.cs
```csharp
using MediatR;

namespace LottoTM.Server.Api.Features.Tickets.Delete;

public class Contracts
{
    // Request zawiera tylko Id z route parameter
    public record Request(int Id) : IRequest<Response>;
    
    // Response zawiera komunikat o sukcesie
    public record Response(string Message);
}
```

### Encje bazy danych (istniejące)
- **`Ticket`** (Entities/Ticket.cs)
  - `Id` - int (PK)
  - `UserId` - int (FK → Users)
  - `CreatedAt` - DateTime
  - Navigation property: `ICollection<TicketNumber> Numbers`

- **`TicketNumber`** (Entities/TicketNumber.cs)
  - `Id` - int (PK)
  - `TicketId` - int (FK → Tickets, CASCADE DELETE)
  - `Number` - int (1-49)
  - `Position` - byte (1-6)

---

## 4. Szczegóły odpowiedzi

### Odpowiedź sukcesu (200 OK)

**Status Code:** `200 OK`

**Body:**
```json
{
  "message": "Zestaw usunięty pomyślnie"
}
```

**Content-Type:** `application/json`

### Odpowiedzi błędów

#### 400 Bad Request
**Scenariusz:** Nieprawidłowy format int w parametrze `id`

**Body:**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "The value 'invalid-id' is not valid for parameter id.",
  "traceId": "00-abc123..."
}
```

**Uwaga:** Ten błąd jest obsługiwany automatycznie przez routing ASP.NET Core.

#### 401 Unauthorized
**Scenariusz:** Brak tokenu JWT lub token nieprawidłowy/wygasły

**Body:**
```json
{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
  "title": "Unauthorized",
  "status": 401,
  "detail": "Brak tokenu autoryzacji lub token nieprawidłowy"
}
```

#### 403 Forbidden
**Scenariusze:**
1. Zestaw należy do innego użytkownika
2. Zestaw nie istnieje (ochrona przed enumeration)

**Body:**
```json
{
  "message": "Brak dostępu do zasobu"
}
```

**Uwaga:** Nie ujawniamy czy zasób istnieje, aby uniknąć enumeration attacks.

#### 500 Internal Server Error
**Scenariusz:** Nieoczekiwany błąd aplikacji (błąd bazy danych, exception)

**Body:**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.6.1",
  "title": "Internal Server Error",
  "status": 500,
  "detail": "Wystąpił nieoczekiwany błąd. Prosimy spróbować ponownie później.",
  "traceId": "00-xyz789..."
}
```

---

## 5. Przepływ danych

### Diagram sekwencji

```
Client                 Endpoint              Handler              AppDbContext            Database
  |                       |                      |                       |                      |
  |--DELETE /api/tickets/{id}------------------>|                       |                      |
  |  + Authorization: Bearer JWT                 |                       |                      |
  |                       |                      |                       |                      |
  |                       |---Request----------->|                       |                      |
  |                       |   (Id: int)          |                       |                      |
  |                       |                      |                       |                      |
  |                       |                      |--Extract UserId------>|                      |
  |                       |                      |  from HttpContext     |                      |
  |                       |                      |                       |                      |
  |                       |                      |--Validate Request---->|                      |
  |                       |                      |  (Validator)          |                      |
  |                       |                      |                       |                      |
  |                       |                      |--Find Ticket--------->|                      |
  |                       |                      |  WHERE Id = @id       |                      |
  |                       |                      |  AND UserId = @userId |--SELECT------------>|
  |                       |                      |                       |<--Ticket or null----|
  |                       |                      |<--Ticket/null---------|                      |
  |                       |                      |                       |                      |
  |                       |                      |--[if null]----------->[throw 403]            |
  |                       |                      |                       |                      |
  |                       |                      |--Remove Ticket------->|                      |
  |                       |                      |                       |                      |
  |                       |                      |--SaveChangesAsync---->|                      |
  |                       |                      |                       |--DELETE Tickets---->|
  |                       |                      |                       |--DELETE Numbers---->|
  |                       |                      |                       |  (CASCADE)          |
  |                       |                      |                       |<--Success-----------|
  |                       |                      |<--Success-------------|                      |
  |                       |                      |                       |                      |
  |                       |<--Response-----------|                       |                      |
  |                       |   (Message)          |                       |                      |
  |<--200 OK--------------|                      |                       |                      |
  |  {message: "..."}     |                      |                       |                      |
```

### Szczegółowy przepływ

1. **Routing & Authentication Middleware**
   - ASP.NET Core routing parsuje parametr `{id}` jako `int`
   - Middleware JWT weryfikuje token z headera `Authorization`
   - Jeśli token brak/nieprawidłowy → 401 Unauthorized (automatycznie)
   - Jeśli token OK → ustawia `HttpContext.User` z claims (UserId)

2. **Endpoint**
   - Odbiera żądanie DELETE z parametrem `id` (int)
   - Pobiera `UserId` z `HttpContext.User` (claim z JWT)
   - Tworzy `Request` z `Id` i `UserId`
   - Wywołuje `mediator.Send(request)`

3. **Validator** (opcjonalnie)
   - Walidacja formatu int (już zrobione przez routing)
   - Brak dodatkowych reguł walidacji

4. **Handler**
   - **Krok 1:** Pobranie `UserId` z `HttpContext.User`
   - **Krok 2:** Zapytanie do bazy:
     ```csharp
     var ticket = await _dbContext.Tickets
         .FirstOrDefaultAsync(t => t.Id == request.Id && t.UserId == userId, cancellationToken);
     ```
   - **Krok 3:** Weryfikacja istnienia i własności:
     ```csharp
     if (ticket == null)
         throw new UnauthorizedAccessException("Brak dostępu do zasobu");
     ```
     **Uwaga:** Rzucamy 403, nie 404, aby nie ujawniać czy zasób istnieje
   
   - **Krok 4:** Usunięcie zestawu:
     ```csharp
     _dbContext.Tickets.Remove(ticket);
     await _dbContext.SaveChangesAsync(cancellationToken);
     ```
     **Uwaga:** CASCADE DELETE automatycznie usunie rekordy z `TicketNumbers`
   
   - **Krok 5:** Zwrot odpowiedzi:
     ```csharp
     return new Response("Zestaw usunięty pomyślnie");
     ```

5. **Exception Handling Middleware**
   - `UnauthorizedAccessException` → 403 Forbidden
   - `ValidationException` → 400 Bad Request
   - Inne wyjątki → 500 Internal Server Error
   - Logowanie wszystkich błędów przez Serilog

---

## 6. Względy bezpieczeństwa

### 6.1 Uwierzytelnianie (Authentication)

**Mechanizm:** JWT Bearer Token

**Implementacja:**
- Token przekazywany w headerze: `Authorization: Bearer <token>`
- Middleware ASP.NET Core automatycznie weryfikuje:
  - Sygnaturę tokenu (HMAC SHA256)
  - Ważność tokenu (exp claim)
  - Issuer i Audience (jeśli skonfigurowane)
- Endpoint wymaga atrybutu `[Authorize]` lub `.RequireAuthorization()`

**Konfiguracja w Endpoint.cs:**
```csharp
app.MapDelete("api/tickets/{id}", async (int id, IMediator mediator, HttpContext httpContext) =>
{
    // ... handler logic
})
.RequireAuthorization() // ← Wymusza JWT
.WithName("DeleteTicket")
.Produces<Contracts.Response>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status401Unauthorized)
.Produces(StatusCodes.Status403Forbidden)
.WithOpenApi();
```

### 6.2 Autoryzacja (Authorization)

**Weryfikacja własności zasobu:**

```csharp
// Handler.cs
var userId = int.Parse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)!);

var ticket = await _dbContext.Tickets
    .FirstOrDefaultAsync(t => t.Id == request.Id && t.UserId == userId, cancellationToken);

if (ticket == null)
{
    // Ochrona przed IDOR i enumeration attacks
    // Nie ujawniamy czy zasób istnieje czy należy do innego użytkownika
    throw new UnauthorizedAccessException("Brak dostępu do zasobu");
}
```

**Ochrona przed IDOR (Insecure Direct Object Reference):**
- ZAWSZE filtrujemy zapytanie po `UserId` z JWT
- NIGDY nie ufamy parametrowi `id` bez weryfikacji własności
- Zapytanie: `WHERE Id = @id AND UserId = @currentUserId`

### 6.3 Ochrona przed Enumeration Attacks

**Problem:** Atakujący może próbować zgadywać istniejące ID zestawów, analizując różnice w odpowiedziach:
- 404 Not Found → zasób nie istnieje
- 403 Forbidden → zasób istnieje, ale należy do innego użytkownika

**Rozwiązanie:** ZAWSZE zwracamy `403 Forbidden` zarówno gdy:
1. Zasób nie istnieje
2. Zasób należy do innego użytkownika

```csharp
// ZŁA PRAKTYKA (ujawnia informację o istnieniu zasobu)
var ticket = await _dbContext.Tickets.FindAsync(request.Id);
if (ticket == null)
    throw new NotFoundException(); // 404

if (ticket.UserId != userId)
    throw new ForbiddenException(); // 403

// DOBRA PRAKTYKA (nie ujawnia informacji)
var ticket = await _dbContext.Tickets
    .FirstOrDefaultAsync(t => t.Id == request.Id && t.UserId == userId);

if (ticket == null)
    throw new UnauthorizedAccessException(); // 403 w obu przypadkach
```

### 6.4 Ochrona przed SQL Injection

**Mechanizm:** Entity Framework Core automatycznie parametryzuje zapytania

**Przykład bezpiecznego kodu:**
```csharp
// EF Core automatycznie tworzy parametryzowane zapytanie:
// SELECT * FROM Tickets WHERE Id = @p0 AND UserId = @p1
var ticket = await _dbContext.Tickets
    .FirstOrDefaultAsync(t => t.Id == request.Id && t.UserId == userId);
```

**Uwaga:** Unikać LINQ `.FromSqlRaw()` z konkatenacją stringów. Używać `.FromSqlInterpolated()`.

### 6.5 Kaskadowe usuwanie (CASCADE DELETE)

**Konfiguracja w AppDbContext:**
```csharp
// AppDbContext.cs (OnModelCreating)
modelBuilder.Entity<TicketNumber>()
    .HasOne<Ticket>()
    .WithMany(t => t.Numbers)
    .HasForeignKey(tn => tn.TicketId)
    .OnDelete(DeleteBehavior.Cascade); // ← Kluczowe!
```

**Efekt:** Usunięcie `Ticket` automatycznie usuwa wszystkie powiązane `TicketNumber` w jednej transakcji.

### 6.6 Logowanie bezpieczeństwa

**Co logować:**
- Każdą próbę usunięcia zestawu (sukces i porażka)
- Próby dostępu do cudzych zasobów (403)
- Próby z nieprawidłowymi tokenami (401)

**Przykład (Serilog):**
```csharp
_logger.LogDebug("User {UserId} successfully deleted ticket {TicketId}", userId, request.Id);

_logger.LogDebug("User {UserId} attempted to delete ticket {TicketId} without permission", userId, request.Id);
```

---

## 7. Obsługa błędów

### 7.1 Tabela scenariuszy błędów

| Kod | Scenariusz | Przyczyna | Obsługa | Komunikat |
|-----|-----------|-----------|---------|-----------|
| **400** | Bad Request | Nieprawidłowy format int w URL | Automatycznie przez routing ASP.NET Core | "The value 'xyz' is not valid for parameter id." |
| **401** | Unauthorized | Brak tokenu JWT | Middleware uwierzytelniania | "Brak tokenu autoryzacji lub token nieprawidłowy" |
| **401** | Unauthorized | Token wygasły | Middleware uwierzytelniania | "Token wygasł" |
| **401** | Unauthorized | Token nieprawidłowy (sygnatura) | Middleware uwierzytelniania | "Token nieprawidłowy" |
| **403** | Forbidden | Zestaw należy do innego użytkownika | Handler → UnauthorizedAccessException | "Brak dostępu do zasobu" |
| **403** | Forbidden | Zestaw nie istnieje (ochrona przed enumeration) | Handler → UnauthorizedAccessException | "Brak dostępu do zasobu" |
| **500** | Internal Server Error | Błąd połączenia z bazą danych | ExceptionHandlingMiddleware | "Wystąpił nieoczekiwany błąd" |
| **500** | Internal Server Error | Nieoczekiwany wyjątek | ExceptionHandlingMiddleware | "Wystąpił nieoczekiwany błąd" |

### 7.2 Implementacja obsługi błędów

#### W Handler.cs

```csharp
public async Task<Contracts.Response> Handle(Contracts.Request request, CancellationToken cancellationToken)
{
    // 1. Pobranie UserId z JWT
    var userId = int.Parse(_httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // 2. Walidacja (jeśli potrzebna - tutaj brak dodatkowych reguł)
    var validationResult = await _validator.ValidateAsync(request, cancellationToken);
    if (!validationResult.IsValid)
    {
        throw new ValidationException(validationResult.Errors); // → 400
    }

    // 3. Znalezienie zestawu z weryfikacją własności
    var ticket = await _dbContext.Tickets
        .FirstOrDefaultAsync(t => t.Id == request.Id && t.UserId == userId, cancellationToken);

    if (ticket == null)
    {
        _logger.LogDebug("User {UserId} attempted to delete ticket {TicketId} - access denied", 
            userId, request.Id);
        throw new UnauthorizedAccessException("Brak dostępu do zasobu"); // → 403
    }

    // 4. Usunięcie zestawu (CASCADE DELETE usuwa TicketNumbers)
    _dbContext.Tickets.Remove(ticket);
    await _dbContext.SaveChangesAsync(cancellationToken);

    _logger.LogDebug("User {UserId} successfully deleted ticket {TicketId}", 
        userId, request.Id);

    return new Contracts.Response("Zestaw usunięty pomyślnie");
}
```

#### W ExceptionHandlingMiddleware.cs (istniejący)

```csharp
public async Task InvokeAsync(HttpContext context)
{
    try
    {
        await _next(context);
    }
    catch (ValidationException ex)
    {
        // 400 Bad Request
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsJsonAsync(new
        {
            message = "Błędy walidacji",
            errors = ex.Errors.Select(e => new { e.PropertyName, e.ErrorMessage })
        });
    }
    catch (UnauthorizedAccessException ex)
    {
        // 403 Forbidden
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(new { message = ex.Message });
        
        _logger.LogDebug(ex, "Forbidden access attempt");
    }
    catch (Exception ex)
    {
        // 500 Internal Server Error
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(new
        {
            message = "Wystąpił nieoczekiwany błąd. Prosimy spróbować ponownie później.",
            traceId = Activity.Current?.Id ?? context.TraceIdentifier
        });

        _logger.LogError(ex, "Unhandled exception occurred");
    }
}
```

### 7.3 Strategie retry (opcjonalnie)

Dla błędów przejściowych bazy danych (timeouts, deadlocks):

```csharp
// Konfiguracja w Program.cs
services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null);
    });
});
```

---

## 8. Rozważania dotyczące wydajności

### 8.1 Optymalizacja zapytań

#### Indeksy bazy danych

**Wymagane indeksy (zdefiniowane w db-plan.md):**

```sql
-- Indeks na UserId (już istniejący)
CREATE INDEX IX_Tickets_UserId ON Tickets(UserId);
```

**Efekt:** Zapytanie `WHERE Id = @id AND UserId = @userId` wykorzystuje:
- Primary Key Index na `Id` (INT)
- Index IX_Tickets_UserId na `UserId`

**Szacowany czas wykonania:**
- Znalezienie zestawu: < 5ms (dzięki indeksom)
- Usunięcie zestawu + TicketNumbers: < 10ms (CASCADE DELETE w jednej transakcji)

#### Optymalizacja zapytania EF Core

```csharp
// DOBRA PRAKTYKA - jedno zapytanie SQL
var ticket = await _dbContext.Tickets
    .FirstOrDefaultAsync(t => t.Id == request.Id && t.UserId == userId, cancellationToken);

// ZŁA PRAKTYKA - dwa zapytania SQL
var ticket = await _dbContext.Tickets.FindAsync(request.Id); // Zapytanie 1
if (ticket?.UserId != userId) throw new ForbiddenException();
```

**Wygenerowany SQL (dobra praktyka):**
```sql
SELECT TOP(1) [t].[Id], [t].[UserId], [t].[CreatedAt]
FROM [Tickets] AS [t]
WHERE [t].[Id] = @__id_0 AND [t].[UserId] = @__userId_1
```

### 8.2 Transakcje i izolacja

**EF Core automatycznie:**
- Każde `SaveChangesAsync()` jest transakcją atomową
- CASCADE DELETE wykonywane w tej samej transakcji
- Izolacja: Read Committed (domyślnie w SQL Server)

**Nie ma potrzeby** ręcznego zarządzania transakcjami dla tego prostego endpointa.

### 8.3 Potencjalne wąskie gardła

| Obszar | Ryzyko | Mitygacja |
|--------|--------|-----------|
| **Połączenia z bazą** | Wyczerpanie connection pool przy dużej liczbie równoczesnych żądań | Connection pooling (domyślnie w EF Core), monitoring |
| **Długie transakcje** | Blokowanie wierszy podczas CASCADE DELETE | CASCADE DELETE jest szybkie (< 10ms), brak problemu |
| **N+1 queries** | Nie dotyczy (jedno zapytanie SELECT + jeden DELETE) | - |

### 8.4 Monitoring i metryki

**Co monitorować:**
1. **Czas wykonania zapytania:** SELECT + DELETE (cel: < 50ms na poziomie 95th percentile)
2. **Liczba żądań DELETE/s:** Wykrywanie anomalii (np. ataki DoS)
3. **Współczynnik błędów 403:** Monitoring prób dostępu do cudzych zasobów
4. **Czas odpowiedzi endpointa:** End-to-end (cel: < 100ms dla 95% żądań)

**Implementacja (Application Insights):**
```csharp
_telemetry.TrackEvent("TicketDeleted", new Dictionary<string, string>
{
    { "UserId", userId.ToString() },
    { "TicketId", request.Id.ToString() }
});

_telemetry.TrackMetric("DeleteTicketDuration", stopwatch.ElapsedMilliseconds);
```

### 8.5 Rate Limiting (opcjonalnie)

**Problem:** Użytkownik może spamować DELETE requests, próbując wywołać DoS.

**Rozwiązanie:** Rate limiting na poziomie użytkownika

```csharp
// Program.cs
services.AddRateLimiter(options =>
{
    options.AddPolicy("PerUser", context =>
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return RateLimitPartition.GetFixedWindowLimiter(userId, _ =>
            new FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(1),
                PermitLimit = 50 // Max 50 DELETE/min na użytkownika
            });
    });
});

// Endpoint.cs
.RequireRateLimiting("PerUser")
```

---

## 9. Etapy wdrożenia

### Krok 1: Utworzenie struktury katalogów

```powershell
# PowerShell
New-Item -Path "src/server/LottoTM.Server.Api/Features/Tickets/Delete" -ItemType Directory -Force
```

**Lokalizacja:** `src/server/LottoTM.Server.Api/Features/Tickets/Delete/`

---

### Krok 2: Utworzenie pliku Contracts.cs

**Plik:** `Features/Tickets/Delete/Contracts.cs`

```csharp
using MediatR;

namespace LottoTM.Server.Api.Features.Tickets.Delete;

public class Contracts
{
    /// <summary>
    /// Żądanie usunięcia zestawu liczb LOTTO
    /// </summary>
    /// <param name="Id">ID zestawu do usunięcia</param>
    public record Request(int Id) : IRequest<Response>;

    /// <summary>
    /// Odpowiedź po pomyślnym usunięciu zestawu
    /// </summary>
    /// <param name="Message">Komunikat potwierdzający operację</param>
    public record Response(string Message);
}
```

---

### Krok 3: Utworzenie pliku Validator.cs

**Plik:** `Features/Tickets/Delete/Validator.cs`

```csharp
using FluentValidation;

namespace LottoTM.Server.Api.Features.Tickets.Delete;

public class Validator : AbstractValidator<Contracts.Request>
{
    public Validator()
    {
        // Walidacja formatu int jest już zapewniona przez routing ASP.NET Core
        // Dodatkowa walidacja: Id musi być większe od 0
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("ID zestawu musi być większe od 0");
    }
}
```

---

### Krok 4: Utworzenie pliku Handler.cs

**Plik:** `Features/Tickets/Delete/Handler.cs`

```csharp
using FluentValidation;
using LottoTM.Server.Api.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LottoTM.Server.Api.Features.Tickets.Delete;

public class DeleteTicketHandler : IRequestHandler<Contracts.Request, Contracts.Response>
{
    private readonly AppDbContext _dbContext;
    private readonly IValidator<Contracts.Request> _validator;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<DeleteTicketHandler> _logger;

    public DeleteTicketHandler(
        AppDbContext dbContext,
        IValidator<Contracts.Request> validator,
        IHttpContextAccessor httpContextAccessor,
        ILogger<DeleteTicketHandler> logger)
    {
        _dbContext = dbContext;
        _validator = validator;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<Contracts.Response> Handle(Contracts.Request request, CancellationToken cancellationToken)
    {
        // 1. Walidacja żądania
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // 2. Pobranie UserId z JWT tokenu
        var httpContext = _httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HttpContext is not available");

        var userIdClaim = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User ID not found in token");

        if (!int.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid User ID format in token");
        }

        // 3. Znalezienie zestawu z weryfikacją własności
        // Jedno zapytanie SQL: WHERE Id = @id AND UserId = @userId
        var ticket = await _dbContext.Tickets
            .FirstOrDefaultAsync(
                t => t.Id == request.Id && t.UserId == userId,
                cancellationToken);

        // 4. Weryfikacja istnienia i własności zasobu
        if (ticket == null)
        {
            // Ochrona przed IDOR i enumeration attacks:
            // Nie ujawniamy czy zasób nie istnieje czy należy do innego użytkownika
            _logger.LogDebug(
                "User {UserId} attempted to delete ticket {TicketId} - access denied (ticket not found or belongs to another user)",
                userId,
                request.Id);

            throw new UnauthorizedAccessException("Brak dostępu do zasobu");
        }

        // 5. Usunięcie zestawu (CASCADE DELETE automatycznie usuwa TicketNumbers)
        _dbContext.Tickets.Remove(ticket);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogDebug(
            "User {UserId} successfully deleted ticket {TicketId}",
            userId,
            request.Id);

        // 6. Zwrot odpowiedzi sukcesu
        return new Contracts.Response("Zestaw usunięty pomyślnie");
    }
}
```

---

### Krok 5: Utworzenie pliku Endpoint.cs

**Plik:** `Features/Tickets/Delete/Endpoint.cs`

```csharp
using MediatR;

namespace LottoTM.Server.Api.Features.Tickets.Delete;

public static class Endpoint
{
    /// <summary>
    /// Rejestruje endpoint DELETE /api/tickets/{id}
    /// </summary>
    public static void AddEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapDelete("api/tickets/{id}", async (
            int id,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var request = new Contracts.Request(id);
            var result = await mediator.Send(request, cancellationToken);
            return Results.Ok(result);
        })
        .RequireAuthorization() // Wymaga JWT tokenu
        .WithName("DeleteTicket")
        .WithTags("Tickets")
        .Produces<Contracts.Response>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .WithOpenApi(operation =>
        {
            operation.Summary = "Usuwa zestaw liczb LOTTO użytkownika";
            operation.Description = "Usuwa zestaw liczb wraz z powiązanymi liczbami (CASCADE DELETE). Wymaga autoryzacji JWT.";
            return operation;
        });
    }
}
```

---

### Krok 6: Rejestracja endpointa w Program.cs

**Plik:** `Program.cs`

Dodaj rejestrację endpointa w sekcji `// Endpoints`:

```csharp
// Existing code...

// Endpoints
app.MapGet("/", () => "LottoTM API is running!");

LottoTM.Server.Api.Features.ApiVersion.Endpoint.AddEndpoint(app);

// ↓ DODAJ TEN WIERSZ ↓
LottoTM.Server.Api.Features.Tickets.Delete.Endpoint.AddEndpoint(app);

app.Run();
```

---

### Krok 7: Rejestracja Validator w DI Container

**Plik:** `Program.cs`

Dodaj rejestrację walidatora w sekcji `// Services`:

```csharp
// Existing validators...
builder.Services.AddScoped<IValidator<Features.ApiVersion.Contracts.Request>, Features.ApiVersion.Validator>();

// ↓ DODAJ TEN WIERSZ ↓
builder.Services.AddScoped<IValidator<Features.Tickets.Delete.Contracts.Request>, Features.Tickets.Delete.Validator>();
```

---

### Krok 8: Upewnienie się że IHttpContextAccessor jest zarejestrowany

**Plik:** `Program.cs`

Sprawdź czy w sekcji `// Services` jest zarejestrowany `IHttpContextAccessor`:

```csharp
// HTTP Context Accessor (do pobierania UserId z JWT)
builder.Services.AddHttpContextAccessor();
```

Jeśli nie ma, dodaj tę linię.

---

### Krok 9: Weryfikacja konfiguracji JWT

**Plik:** `Program.cs`

Upewnij się, że JWT jest poprawnie skonfigurowany:

```csharp
// JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorization();

// ...

// Middleware (w odpowiedniej kolejności!)
app.UseAuthentication(); // ← Musi być PRZED UseAuthorization
app.UseAuthorization();  // ← Musi być PRZED MapEndpoints
```

---

### Krok 10: Weryfikacja konfiguracji ExceptionHandlingMiddleware

**Plik:** `Middlewares/ExceptionHandlingMiddleware.cs`

Upewnij się, że middleware obsługuje `UnauthorizedAccessException`:

```csharp
catch (UnauthorizedAccessException ex)
{
    context.Response.StatusCode = StatusCodes.Status403Forbidden;
    await context.Response.WriteAsJsonAsync(new
    {
        message = ex.Message
    });

    _logger.LogDebug(ex, "Forbidden access attempt - User: {UserId}, Path: {Path}",
        context.User.FindFirstValue(ClaimTypes.NameIdentifier),
        context.Request.Path);
}
```

Jeśli nie ma obsługi tego wyjątku, dodaj powyższy kod.

---

### Krok 11: Testowanie endpointa - Happy Path

**Narzędzie:** REST Client (VS Code), Postman, curl

#### Test 1: Pomyślne usunięcie zestawu

**Przygotowanie:**
1. Zaloguj się jako użytkownik (POST /api/auth/login) → otrzymaj JWT
2. Utwórz zestaw (POST /api/tickets) → otrzymaj ID zestawu (int)

**Żądanie:**
```http
DELETE http://localhost:5000/api/tickets/1
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Oczekiwana odpowiedź:**
```json
HTTP/1.1 200 OK
Content-Type: application/json

{
  "message": "Zestaw usunięty pomyślnie"
}
```

**Weryfikacja w bazie:**
```sql
-- Sprawdź czy zestaw został usunięty
SELECT * FROM Tickets WHERE Id = 1;
-- Wynik: 0 wierszy

-- Sprawdź czy liczby zostały usunięte (CASCADE DELETE)
SELECT * FROM TicketNumbers WHERE TicketId = 1;
-- Wynik: 0 wierszy
```

---

### Krok 12: Testowanie endpointa - Error Paths

#### Test 2: Brak tokenu JWT

**Żądanie:**
```http
DELETE http://localhost:5000/api/tickets/1
# Brak headera Authorization
```

**Oczekiwana odpowiedź:**
```json
HTTP/1.1 401 Unauthorized

{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
  "title": "Unauthorized",
  "status": 401
}
```

#### Test 3: Próba usunięcia cudzego zestawu

**Przygotowanie:**
1. Zaloguj się jako User A → JWT_A
2. Utwórz zestaw jako User A → TICKET_ID_A
3. Zaloguj się jako User B → JWT_B
4. Próbuj usunąć TICKET_ID_A używając JWT_B

**Żądanie:**
```http
DELETE http://localhost:5000/api/tickets/TICKET_ID_A
Authorization: Bearer JWT_B
```

**Oczekiwana odpowiedź:**
```json
HTTP/1.1 403 Forbidden

{
  "message": "Brak dostępu do zasobu"
}
```

#### Test 4: Nieprawidłowy format ID

**Żądanie:**
```http
DELETE http://localhost:5000/api/tickets/invalid-id-format
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Oczekiwana odpowiedź:**
```json
HTTP/1.1 400 Bad Request

{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "The value 'invalid-id-format' is not valid for parameter id."
}
```

#### Test 5: ID mniejsze lub równe 0

**Żądanie:**
```http
DELETE http://localhost:5000/api/tickets/0
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Oczekiwana odpowiedź:**
```json
HTTP/1.1 400 Bad Request

{
  "message": "Błędy walidacji",
  "errors": [
    {
      "propertyName": "Id",
      "errorMessage": "ID zestawu musi być większe od 0"
    }
  ]
}
```

#### Test 6: Nieistniejący zestaw

**Żądanie:**
```http
DELETE http://localhost:5000/api/tickets/99999
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Oczekiwana odpowiedź:**
```json
HTTP/1.1 403 Forbidden

{
  "message": "Brak dostępu do zasobu"
}
```

**Uwaga:** Zwracamy 403 (nie 404) aby nie ujawniać czy zasób istnieje.

---

### Krok 13: Testy jednostkowe (opcjonalnie)

**Lokalizacja:** `tests/server/LottoTM.Server.Api.Tests/Features/Tickets/Delete/HandlerTests.cs`

**Przykładowy test:**

```csharp
using FluentValidation;
using LottoTM.Server.Api.Entities;
using LottoTM.Server.Api.Features.Tickets.Delete;
using LottoTM.Server.Api.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;

namespace LottoTM.Server.Api.Tests.Features.Tickets.Delete;

public class HandlerTests
{
    private readonly Mock<IValidator<Contracts.Request>> _validatorMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<ILogger<DeleteTicketHandler>> _loggerMock;
    private readonly AppDbContext _dbContext;

    public HandlerTests()
    {
        _validatorMock = new Mock<IValidator<Contracts.Request>>();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _loggerMock = new Mock<ILogger<DeleteTicketHandler>>();

        // In-memory database dla testów
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new AppDbContext(options);
    }

    [Fact]
    public async Task Handle_ValidRequest_DeletesTicketSuccessfully()
    {
        // Arrange
        var userId = 1;
        var ticketId = 1;

        // Mock HttpContext z JWT claims
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        var httpContextMock = new Mock<HttpContext>();
        httpContextMock.Setup(x => x.User).Returns(claimsPrincipal);
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock.Object);

        // Mock validator (valid request)
        _validatorMock.Setup(x => x.ValidateAsync(It.IsAny<Contracts.Request>(), default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        // Dodaj testowy zestaw do bazy
        var ticket = new Ticket
        {
            Id = ticketId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Tickets.Add(ticket);
        await _dbContext.SaveChangesAsync();

        var handler = new DeleteTicketHandler(
            _dbContext,
            _validatorMock.Object,
            _httpContextAccessorMock.Object,
            _loggerMock.Object);

        var request = new Contracts.Request(ticketId);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Zestaw usunięty pomyślnie", result.Message);

        // Weryfikacja usunięcia z bazy
        var deletedTicket = await _dbContext.Tickets.FindAsync(ticketId);
        Assert.Null(deletedTicket);
    }

    [Fact]
    public async Task Handle_TicketBelongsToAnotherUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var userId = 1;
        var anotherUserId = 2;
        var ticketId = 2;

        // Mock HttpContext (user 1)
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        var httpContextMock = new Mock<HttpContext>();
        httpContextMock.Setup(x => x.User).Returns(claimsPrincipal);
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock.Object);

        // Mock validator
        _validatorMock.Setup(x => x.ValidateAsync(It.IsAny<Contracts.Request>(), default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        // Dodaj zestaw należący do innego użytkownika
        var ticket = new Ticket
        {
            Id = ticketId,
            UserId = anotherUserId, // ← Inny użytkownik!
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Tickets.Add(ticket);
        await _dbContext.SaveChangesAsync();

        var handler = new DeleteTicketHandler(
            _dbContext,
            _validatorMock.Object,
            _httpContextAccessorMock.Object,
            _loggerMock.Object);

        var request = new Contracts.Request(ticketId);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(request, CancellationToken.None));

        // Weryfikacja że zestaw NIE został usunięty
        var ticketStillExists = await _dbContext.Tickets.FindAsync(ticketId);
        Assert.NotNull(ticketStillExists);
    }

    [Fact]
    public async Task Handle_TicketDoesNotExist_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var userId = 1;
        var nonExistentTicketId = 999;

        // Mock HttpContext
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        var httpContextMock = new Mock<HttpContext>();
        httpContextMock.Setup(x => x.User).Returns(claimsPrincipal);
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock.Object);

        // Mock validator
        _validatorMock.Setup(x => x.ValidateAsync(It.IsAny<Contracts.Request>(), default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        var handler = new DeleteTicketHandler(
            _dbContext,
            _validatorMock.Object,
            _httpContextAccessorMock.Object,
            _loggerMock.Object);

        var request = new Contracts.Request(nonExistentTicketId);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(request, CancellationToken.None));
    }
}
```

**Uruchomienie testów:**
```powershell
cd tests/server/LottoTM.Server.Api.Tests
dotnet test
```

---

### Krok 14: Dokumentacja API (Swagger)

Po uruchomieniu aplikacji, Swagger UI automatycznie wygeneruje dokumentację dla endpointa.

**URL:** `http://localhost:5000/swagger/index.html`

**Wygląd w Swagger:**

```
DELETE /api/tickets/{id}
Usuwa zestaw liczb LOTTO użytkownika

Parameters:
  id* (path) - integer - ID zestawu do usunięcia

Responses:
  200 - Success
    {
      "message": "Zestaw usunięty pomyślnie"
    }
  
  400 - Bad Request
  401 - Unauthorized
  403 - Forbidden

Security:
  Bearer Authentication (JWT)
```

---

### Krok 15: Weryfikacja logów (Serilog)

**Lokalizacja logów:** `src/server/LottoTM.Server.Api/Logs/applog-YYYYMMDD.txt`

**Przykładowe logi:**

```
[2025-11-05 10:30:15 INF] User 1 successfully deleted ticket a3bb189e-8bf9-3888-9912-ace4e6543002

[2025-11-05 10:31:42 WRN] User 1 attempted to delete ticket b4cc289f-9cf0-4999-0023-bdf5f7654003 - access denied (ticket not found or belongs to another user)

[2025-11-05 10:32:10 ERR] Unhandled exception occurred
System.Data.SqlClient.SqlException: Timeout expired. The timeout period elapsed...
```

---

### Krok 16: Checklist przed wdrożeniem na produkcję

- [ ] Wszystkie testy jednostkowe przechodzą
- [ ] Testy integracyjne (REST API) przechodzą dla happy path i error paths
- [ ] JWT authentication działa poprawnie
- [ ] Weryfikacja własności zasobu działa (IDOR protection)
- [ ] Ochrona przed enumeration attacks zaimplementowana (403 zamiast 404)
- [ ] CASCADE DELETE usuwa TicketNumbers
- [ ] ExceptionHandlingMiddleware obsługuje UnauthorizedAccessException
- [ ] Logowanie (Serilog) działa poprawnie
- [ ] Swagger UI wyświetla dokumentację endpointa
- [ ] Rate limiting skonfigurowany (opcjonalnie)
- [ ] Monitoring i metryki skonfigurowane (Application Insights, opcjonalnie)
- [ ] Code review przeprowadzony
- [ ] Dokumentacja API zaktualizowana

---

## 10. Podsumowanie

### Kluczowe decyzje projektowe

1. **Bezpieczeństwo:**
   - Zawsze weryfikujemy własność zasobu (WHERE Id = @id AND UserId = @userId)
   - Zwracamy 403 Forbidden zarówno gdy zasób nie istnieje, jak i gdy należy do innego użytkownika (ochrona przed enumeration)
   - JWT claims są źródłem prawdy dla UserId (nie ufamy parametrom z klienta)

2. **Wydajność:**
   - Jedno zapytanie SQL z filtrowaniem po Id i UserId
   - Wykorzystanie indeksów (PK + IX_Tickets_UserId)
   - CASCADE DELETE wykonywane w jednej transakcji

3. **Architektura:**
   - Vertical Slice Architecture: cała logika endpointa w jednym folderze
   - MediatR dla separacji concerns (Endpoint → Handler)
   - FluentValidation dla walidacji danych wejściowych
   - Middleware dla globalnej obsługi błędów

4. **Obsługa błędów:**
   - ValidationException → 400 Bad Request
   - UnauthorizedAccessException → 403 Forbidden
   - Exception → 500 Internal Server Error
   - Logowanie wszystkich błędów przez Serilog

### Potencjalne rozszerzenia (poza MVP)

- **Soft delete:** Zamiast usuwania, dodać flagę `IsDeleted` i filtrować zapytania
- **Historia usunięć:** Tabela audytowa z informacjami o usuniętych zestawach
- **Undo delete:** Możliwość przywrócenia usuniętego zestawu w ciągu X minut
- **Batch delete:** Endpoint do usuwania wielu zestawów naraz (DELETE /api/tickets z body: [id1, id2, ...])
- **Rate limiting:** Ograniczenie liczby DELETE requests na użytkownika

---

**Koniec planu implementacji**
