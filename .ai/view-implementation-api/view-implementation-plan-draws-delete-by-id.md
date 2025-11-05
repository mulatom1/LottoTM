# API Endpoint Implementation Plan: DELETE /api/draws/{id}

## 1. Przegląd punktu końcowego

Endpoint służy do usunięcia wyniku losowania LOTTO z systemu. Dostęp do tego endpointu mają **wyłącznie użytkownicy z uprawnieniami administratora** (IsAdmin = true).

Usunięcie losowania automatycznie usuwa wszystkie powiązane liczby z tabeli `DrawNumbers` dzięki konfiguracji CASCADE DELETE w relacji Foreign Key.

**Endpoint:** `DELETE /api/draws/{id}`

**Cel biznesowy:** Umożliwienie administratorom usunięcia błędnie wprowadzonych lub nieaktualnych wyników losowań.

---

## 2. Szczegóły żądania

### Metoda HTTP
**DELETE**

### Struktura URL
```
DELETE /api/draws/{id}
```

### Parametry URL

| Parametr | Typ | Wymagany | Opis | Walidacja |
|----------|-----|----------|------|-----------|
| `id` | INT | ✅ Tak | ID losowania do usunięcia | Musi być > 0 |

### Przykład żądania
```http
DELETE /api/draws/123
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### Headers

| Header | Wartość | Wymagany | Opis |
|--------|---------|----------|------|
| `Authorization` | `Bearer <token>` | ✅ Tak | JWT token użytkownika z flagą IsAdmin = true |
| `Content-Type` | `application/json` | ❌ Nie | Brak payload w DELETE |

### Request Body
**Brak** - DELETE nie wymaga body, ID przekazywane w URL

---

## 3. Wykorzystywane typy

### Contracts (DTOs)

```csharp
namespace LottoTM.Server.Api.Features.Draws.Delete;

public class Contracts
{
    public record Request(int Id) : IRequest<Response>;

    public record Response(string Message);
}
```

### Encje wykorzystywane z bazy danych

```csharp
// Draw.cs (istniejąca encja)
public class Draw
{
    public int Id { get; set; }
    public DateTime DrawDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedByUserId { get; set; }

    // Navigation properties
    public User CreatedByUser { get; set; } = null!;
    public ICollection<DrawNumber> Numbers { get; set; } = new List<DrawNumber>();
}

// DrawNumber.cs (istniejąca encja)
public class DrawNumber
{
    public int Id { get; set; }
    public int DrawId { get; set; }
    public int Number { get; set; }
    public byte Position { get; set; }

    public Draw Draw { get; set; } = null!;
}
```

---

## 4. Szczegóły odpowiedzi

### Odpowiedź sukcesu (200 OK)

**Kod stanu:** `200 OK`

**Payload:**
```json
{
  "message": "Losowanie usunięte pomyślnie"
}
```

### Odpowiedzi błędów

#### 400 Bad Request - Nieprawidłowe ID
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Błąd walidacji",
  "status": 400,
  "errors": {
    "Id": ["ID musi być większe od 0"]
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

#### 403 Forbidden - Użytkownik nie ma uprawnień administratora
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.3",
  "title": "Forbidden",
  "status": 403,
  "detail": "Brak uprawnień administratora. Tylko administratorzy mogą usuwać losowania."
}
```

#### 404 Not Found - Losowanie nie istnieje
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Not Found",
  "status": 404,
  "detail": "Losowanie o podanym ID nie istnieje"
}
```

#### 500 Internal Server Error - Błąd serwera
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.6.1",
  "title": "Internal Server Error",
  "status": 500,
  "detail": "Wystąpił nieoczekiwany błąd podczas przetwarzania żądania"
}
```

---

## 5. Przepływ danych

### Diagram przepływu

```
┌─────────────┐
│   Client    │
│  (Admin)    │
└──────┬──────┘
       │ DELETE /api/draws/{id}
       │ Authorization: Bearer <token>
       ▼
┌─────────────────────────────────────────┐
│  1. ASP.NET Core Middleware Pipeline    │
│     - Authentication (JWT)              │
│     - Authorization                     │
└──────────────┬──────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────┐
│  2. Endpoint.cs                         │
│     - Wyciągnięcie ID z route           │
│     - Utworzenie Request                │
│     - Wywołanie MediatR                 │
└──────────────┬──────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────┐
│  3. Validator.cs                        │
│     - Walidacja ID > 0                  │
│     - Throw ValidationException jeśli   │
│       błąd                              │
└──────────────┬──────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────┐
│  4. Handler.cs                          │
│     - Pobranie UserId z JWT claims      │
│     - Sprawdzenie IsAdmin z bazy        │
│     - Jeśli !IsAdmin → 403 Forbidden    │
│     - Wyszukanie Draw po ID             │
│     - Jeśli !exists → 404 Not Found     │
│     - Usunięcie Draw (CASCADE DrawNumbers)│
│     - SaveChangesAsync()                │
│     - Zwrot Response                    │
└──────────────┬──────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────┐
│  5. AppDbContext (EF Core)              │
│     DELETE FROM Draws WHERE Id = @id    │
│     (CASCADE DELETE DrawNumbers)        │
└──────────────┬──────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────┐
│  6. SQL Server Database                 │
│     - Usunięcie rekordu z Draws         │
│     - Automatyczne usunięcie DrawNumbers│
│       (ON DELETE CASCADE)               │
└──────────────┬──────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────┐
│  7. Response do Client                  │
│     200 OK: {"message": "..."}          │
│     lub kod błędu                       │
└─────────────────────────────────────────┘
```

### Szczegółowy przepływ w Handler

```csharp
// Pseudokod przepływu w Handler
1. Walidacja request (FluentValidation)
2. Pobranie UserId z JWT: HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)
3. Pobranie User z bazy po UserId
4. IF User == null → 401 Unauthorized
5. IF User.IsAdmin == false → 403 Forbidden
6. Wyszukanie Draw: db.Draws.FindAsync(id)
7. IF Draw == null → 404 Not Found
8. Usunięcie: db.Draws.Remove(draw)
9. Zapis: db.SaveChangesAsync()
10. RETURN 200 OK z komunikatem sukcesu
```

### Interakcje z bazą danych

**Zapytanie wyszukania:**
```sql
SELECT * FROM Draws WHERE Id = @id
```

**Zapytanie usunięcia:**
```sql
-- Automatyczne przez EF Core
DELETE FROM Draws WHERE Id = @id

-- CASCADE DELETE automatycznie usuwa:
DELETE FROM DrawNumbers WHERE DrawId = @id
```

**Wykorzystane indeksy:**
- Primary Key na `Draws.Id` (szybkie wyszukiwanie O(log n))
- Foreign Key z CASCADE DELETE na `DrawNumbers.DrawId`

---

## 6. Względy bezpieczeństwa

### 6.1 Uwierzytelnianie (Authentication)

**Mechanizm:** JWT Bearer Token

**Implementacja:**
```csharp
// W Handler.cs
var userIdClaim = httpContextAccessor.HttpContext?.User
    .FindFirst(ClaimTypes.NameIdentifier);

if (userIdClaim == null)
{
    throw new UnauthorizedAccessException("Brak tokenu autoryzacji");
}

var userId = int.Parse(userIdClaim.Value);
```

**Konfiguracja JWT w Program.cs:**
- Token musi być prawidłowy (podpis, expiration, issuer, audience)
- Middleware `app.UseAuthentication()` automatycznie waliduje token

### 6.2 Autoryzacja (Authorization)

**Wymaganie:** Tylko administratorzy (IsAdmin = true) mogą usuwać losowania

**Implementacja:**
```csharp
// W Handler.cs
var user = await db.Users.FindAsync(userId, cancellationToken);

if (user == null)
{
    throw new UnauthorizedAccessException("Użytkownik nie istnieje");
}

if (!user.IsAdmin)
{
    return Results.Forbid(); // 403 Forbidden
}
```

**Alternatywnie - użycie [Authorize] attribute z Policy:**
```csharp
// W Endpoint.cs (opcjonalne ulepszenie post-MVP)
app.MapDelete("api/draws/{id}", async (...) => {...})
   .RequireAuthorization("AdminOnly"); // Policy z IsAdmin claim
```

### 6.3 Walidacja danych wejściowych

**FluentValidation w Validator.cs:**
```csharp
public class Validator : AbstractValidator<Contracts.Request>
{
    public Validator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("ID musi być większe od 0");
    }
}
```

### 6.4 SQL Injection

**Ochrona:** Entity Framework Core automatycznie parametryzuje wszystkie zapytania

```csharp
// BEZPIECZNE - EF Core parametryzuje
var draw = await db.Draws.FindAsync(id); // Parametr @p0

// NIEBEZPIECZNE - NIE UŻYWAĆ
// var draw = db.Draws.FromSqlRaw($"DELETE FROM Draws WHERE Id = {id}");
```

### 6.5 Ochrona przed Mass Deletion

**Strategia:**
1. Endpoint usuwa **jedno losowanie** po ID (nie batch delete)
2. Sprawdzenie istnienia przed usunięciem (404 jeśli nie istnieje)
3. Brak parametru query `?deleteAll` - tylko pojedyncze ID w route

### 6.6 HTTPS

**Wymaganie (NFR-007):** HTTPS wymagane na produkcji

**Konfiguracja:**
```csharp
// W Program.cs (odkomentować na produkcji)
app.UseHttpsRedirection();
```

### 6.7 Rate Limiting (post-MVP)

**Rekomendacja:** Ograniczenie liczby wywołań DELETE per IP/użytkownika

**Przykład:**
```csharp
// AspNetCoreRateLimit
// Limit: 10 DELETE requests / minuta / użytkownika
```

---

## 7. Obsługa błędów

### 7.1 Tabela scenariuszy błędów

| Scenariusz | Kod HTTP | Response | Logika w kodzie |
|------------|----------|----------|-----------------|
| ID ≤ 0 | 400 Bad Request | ValidationException | FluentValidation w Validator |
| Brak tokenu JWT | 401 Unauthorized | ProblemDetails | Middleware Authentication |
| Token wygasły/nieprawidłowy | 401 Unauthorized | ProblemDetails | Middleware Authentication |
| User nie ma IsAdmin | 403 Forbidden | ProblemDetails | Handler sprawdza User.IsAdmin |
| Losowanie nie istnieje | 404 Not Found | ProblemDetails | Handler sprawdza FindAsync |
| Błąd bazy danych | 500 Internal Server Error | ProblemDetails | ExceptionHandlingMiddleware |
| Wyjątek aplikacji | 500 Internal Server Error | ProblemDetails | ExceptionHandlingMiddleware |

### 7.2 Implementacja obsługi błędów

#### ValidationException (400)
```csharp
// W Handler.cs
var validationResult = await validator.ValidateAsync(request, cancellationToken);
if (!validationResult.IsValid)
{
    throw new ValidationException(validationResult.Errors);
}
// ExceptionHandlingMiddleware przekształci to na 400 Bad Request
```

#### UnauthorizedAccessException (401)
```csharp
// W Handler.cs
if (userIdClaim == null || user == null)
{
    throw new UnauthorizedAccessException("Użytkownik nie jest uwierzytelniony");
}
// Middleware przekształci na 401
```

#### Forbidden (403)
```csharp
// W Handler.cs - custom exception
if (!user.IsAdmin)
{
    throw new ForbiddenException("Brak uprawnień administratora. Tylko administratorzy mogą usuwać losowania.");
}
// LUB użycie Results.Forbid() bezpośrednio w endpoincie
```

#### NotFoundException (404)
```csharp
// W Handler.cs
var draw = await db.Draws.FindAsync(id, cancellationToken);
if (draw == null)
{
    throw new NotFoundException($"Losowanie o ID {id} nie istnieje");
}
```

#### DbUpdateException (500)
```csharp
// Automatycznie obsłużone przez ExceptionHandlingMiddleware
// Logowanie przez Serilog
try
{
    await db.SaveChangesAsync(cancellationToken);
}
catch (DbUpdateException ex)
{
    // Middleware przechwytuje i zwraca 500
    logger.LogError(ex, "Błąd podczas usuwania losowania {DrawId}", id);
    throw;
}
```

### 7.3 Logowanie błędów (Serilog)

**Konfiguracja w Program.cs:**
```csharp
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionHandlingMiddleware>();
```

**Logi w Handler:**
```csharp
// W Handler.cs
private readonly ILogger<DeleteDrawHandler> _logger;

public async Task<Contracts.Response> Handle(...)
{
    _logger.LogInformation("Rozpoczęto usuwanie losowania {DrawId} przez użytkownika {UserId}",
        request.Id, userId);

    // ... logika

    _logger.LogInformation("Pomyślnie usunięto losowanie {DrawId}", request.Id);
    return new Contracts.Response("Losowanie usunięte pomyślnie");
}
```

---

## 8. Rozważania dotyczące wydajności

### 8.1 Optymalizacja zapytań

**Wykorzystanie indeksów:**
- **Primary Key Index** na `Draws.Id` - O(log n) dla FindAsync
- **Foreign Key Index** na `DrawNumbers.DrawId` - szybkie CASCADE DELETE

**Query Performance:**
```csharp
// DOBRE - FindAsync używa PK index
var draw = await db.Draws.FindAsync(id);

// NIEPOTRZEBNE - nie trzeba eager loading dla DELETE
// var draw = await db.Draws.Include(d => d.Numbers).FirstOrDefaultAsync(d => d.Id == id);
```

### 8.2 Transakcje

**Domyślne zachowanie EF Core:**
- `SaveChangesAsync()` automatycznie opakowuje operacje w transakcję
- CASCADE DELETE wykonywany atomowo (albo wszystko, albo nic)

**Nie potrzeba explicite transaction dla pojedynczego DELETE:**
```csharp
// WYSTARCZAJĄCE (domyślna transakcja)
db.Draws.Remove(draw);
await db.SaveChangesAsync();

// NIEPOTRZEBNE dla pojedynczego DELETE
// using var transaction = await db.Database.BeginTransactionAsync();
// ...
// await transaction.CommitAsync();
```

### 8.3 Wąskie gardła

**Potencjalne problemy:**
1. **Sprawdzenie IsAdmin:** Wymaga dodatkowego zapytania do Users
   - **Optymalizacja:** Cache User claims w JWT (dodać IsAdmin do JWT payload)

2. **CASCADE DELETE dużej liczby DrawNumbers:**
   - MVP: każde losowanie ma tylko 6 DrawNumbers → brak problemu
   - Produkcja z tysiącami losowań: index na `DrawNumbers.DrawId` zapewnia wydajność

3. **Jednoczesne usunięcia:**
   - MVP: mała liczba adminów → brak problemu z konkurencją
   - Jeśli potrzebne: Optimistic Concurrency Control (RowVersion)

### 8.4 Strategie optymalizacji

#### Cache IsAdmin w JWT Claims
```csharp
// Przy generowaniu JWT (w Login endpoint)
var claims = new[]
{
    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
    new Claim(ClaimTypes.Email, user.Email),
    new Claim("IsAdmin", user.IsAdmin.ToString()) // Dodać IsAdmin do JWT
};

// W Handler.cs
var isAdminClaim = httpContextAccessor.HttpContext?.User.FindFirst("IsAdmin");
var isAdmin = isAdminClaim != null && bool.Parse(isAdminClaim.Value);

if (!isAdmin)
{
    throw new ForbiddenException("Brak uprawnień administratora");
}
// Eliminuje zapytanie do bazy dla User.IsAdmin
```

#### Soft Delete (opcjonalne post-MVP)
```csharp
// Zamiast fizycznego DELETE, można dodać flagę IsDeleted
draw.IsDeleted = true;
draw.DeletedAt = DateTime.UtcNow;
await db.SaveChangesAsync();
// Pozwala na "undo" i audyt, ale komplikuje queries (filtrowanie IsDeleted = false)
```

---

## 9. Etapy wdrożenia

### Krok 1: Utworzenie struktury folderów i plików
```bash
src/server/LottoTM.Server.Api/Features/Draws/Delete/
    ├── Endpoint.cs
    ├── Contracts.cs
    ├── Handler.cs
    └── Validator.cs
```

### Krok 2: Implementacja Contracts.cs
```csharp
using MediatR;

namespace LottoTM.Server.Api.Features.Draws.Delete;

public class Contracts
{
    public record Request(int Id) : IRequest<Response>;

    public record Response(string Message);
}
```

### Krok 3: Implementacja Validator.cs
```csharp
using FluentValidation;

namespace LottoTM.Server.Api.Features.Draws.Delete;

public class Validator : AbstractValidator<Contracts.Request>
{
    public Validator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("ID musi być większe od 0");
    }
}
```

### Krok 4: Implementacja Handler.cs
```csharp
using FluentValidation;
using LottoTM.Server.Api.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LottoTM.Server.Api.Features.Draws.Delete;

public class DeleteDrawHandler : IRequestHandler<Contracts.Request, Contracts.Response>
{
    private readonly AppDbContext _db;
    private readonly IValidator<Contracts.Request> _validator;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<DeleteDrawHandler> _logger;

    public DeleteDrawHandler(
        AppDbContext db,
        IValidator<Contracts.Request> validator,
        IHttpContextAccessor httpContextAccessor,
        ILogger<DeleteDrawHandler> logger)
    {
        _db = db;
        _validator = validator;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<Contracts.Response> Handle(
        Contracts.Request request,
        CancellationToken cancellationToken)
    {
        // 1. Walidacja request
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // 2. Pobranie UserId z JWT
        var userIdClaim = _httpContextAccessor.HttpContext?.User
            .FindFirst(ClaimTypes.NameIdentifier);

        if (userIdClaim == null)
        {
            _logger.LogWarning("Próba usunięcia losowania bez tokenu JWT");
            throw new UnauthorizedAccessException("Brak tokenu autoryzacji");
        }

        var userId = int.Parse(userIdClaim.Value);

        // 3. Pobranie użytkownika i sprawdzenie IsAdmin
        var user = await _db.Users.FindAsync(new object[] { userId }, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Użytkownik {UserId} nie istnieje", userId);
            throw new UnauthorizedAccessException("Użytkownik nie istnieje");
        }

        if (!user.IsAdmin)
        {
            _logger.LogWarning(
                "Użytkownik {UserId} ({Email}) próbował usunąć losowanie bez uprawnień administratora",
                userId, user.Email);
            throw new ForbiddenException(
                "Brak uprawnień administratora. Tylko administratorzy mogą usuwać losowania.");
        }

        // 4. Wyszukanie losowania
        var draw = await _db.Draws.FindAsync(new object[] { request.Id }, cancellationToken);

        if (draw == null)
        {
            _logger.LogWarning(
                "Użytkownik {UserId} próbował usunąć nieistniejące losowanie {DrawId}",
                userId, request.Id);
            throw new NotFoundException($"Losowanie o ID {request.Id} nie istnieje");
        }

        // 5. Usunięcie losowania (CASCADE DELETE DrawNumbers)
        _logger.LogInformation(
            "Użytkownik {UserId} ({Email}) usuwa losowanie {DrawId} z daty {DrawDate}",
            userId, user.Email, draw.Id, draw.DrawDate);

        _db.Draws.Remove(draw);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Pomyślnie usunięto losowanie {DrawId}", request.Id);

        return new Contracts.Response("Losowanie usunięte pomyślnie");
    }
}
```

### Krok 5: Implementacja Endpoint.cs
```csharp
using MediatR;

namespace LottoTM.Server.Api.Features.Draws.Delete;

public static class Endpoint
{
    public static void AddEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapDelete("api/draws/{id:int}", async (int id, IMediator mediator) =>
        {
            var request = new Contracts.Request(id);
            var result = await mediator.Send(request);
            return Results.Ok(result);
        })
        .RequireAuthorization() // Wymaga JWT
        .WithName("DeleteDraw")
        .Produces<Contracts.Response>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithOpenApi();
    }
}
```

### Krok 6: Utworzenie custom exceptions (jeśli nie istnieją)
```csharp
// src/server/LottoTM.Server.Api/Exceptions/ForbiddenException.cs
namespace LottoTM.Server.Api.Exceptions;

public class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message) { }
}

// src/server/LottoTM.Server.Api/Exceptions/NotFoundException.cs
namespace LottoTM.Server.Api.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}
```

### Krok 7: Aktualizacja ExceptionHandlingMiddleware
```csharp
// W ExceptionHandlingMiddleware.cs - dodać obsługę ForbiddenException i NotFoundException
catch (ForbiddenException ex)
{
    context.Response.StatusCode = StatusCodes.Status403Forbidden;
    problemDetails = new ProblemDetails
    {
        Status = StatusCodes.Status403Forbidden,
        Title = "Forbidden",
        Detail = ex.Message
    };
}
catch (NotFoundException ex)
{
    context.Response.StatusCode = StatusCodes.Status404NotFound;
    problemDetails = new ProblemDetails
    {
        Status = StatusCodes.Status404NotFound,
        Title = "Not Found",
        Detail = ex.Message
    };
}
```

### Krok 8: Rejestracja IHttpContextAccessor w Program.cs
```csharp
// W Program.cs - dodać przed builder.Build()
builder.Services.AddHttpContextAccessor();
```

### Krok 9: Rejestracja endpointu w Program.cs
```csharp
// W Program.cs - dodać przed app.RunAsync()
LottoTM.Server.Api.Features.Draws.Delete.Endpoint.AddEndpoint(app);
```

### Krok 10: Utworzenie testów integracyjnych
```csharp
// tests/server/LottoTM.Server.Api.Tests/Features/Draws/Delete/EndpointTests.cs

using LottoTM.Server.Api.Repositories;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Headers;
using Xunit;

namespace LottoTM.Server.Api.Tests.Features.Draws.Delete;

public class EndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public EndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task DeleteDraw_WithAdminUser_ReturnsOk()
    {
        // Arrange
        var client = _factory.CreateClient();

        // TODO: Utworzyć testowego admina, wygenerować JWT, dodać losowanie
        var token = "test-admin-token";
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var drawId = 1; // ID testowego losowania

        // Act
        var response = await client.DeleteAsync($"/api/draws/{drawId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DeleteDraw_WithNonAdminUser_ReturnsForbidden()
    {
        // Arrange
        var client = _factory.CreateClient();

        // TODO: Utworzyć testowego użytkownika (IsAdmin = false)
        var token = "test-user-token";
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var drawId = 1;

        // Act
        var response = await client.DeleteAsync($"/api/draws/{drawId}");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteDraw_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();
        var drawId = 1;

        // Act
        var response = await client.DeleteAsync($"/api/draws/{drawId}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteDraw_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var client = _factory.CreateClient();

        var token = "test-admin-token";
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var nonExistentId = 99999;

        // Act
        var response = await client.DeleteAsync($"/api/draws/{nonExistentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteDraw_WithInvalidId_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();

        var token = "test-admin-token";
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var invalidId = 0;

        // Act
        var response = await client.DeleteAsync($"/api/draws/{invalidId}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
```

### Krok 11: Uruchomienie testów
```bash
# Test jednostkowy walidacji
dotnet test --filter "FullyQualifiedName~Draws.Delete.Validator"

# Testy integracyjne endpointu
dotnet test --filter "FullyQualifiedName~Draws.Delete.EndpointTests"

# Wszystkie testy projektu
dotnet test LottoTM.sln
```

### Krok 12: Testowanie manualne (Swagger/Postman)
```http
# 1. Login jako admin
POST https://localhost:5001/api/auth/login
Content-Type: application/json

{
  "email": "admin@example.com",
  "password": "AdminPass123!"
}

# Response: { "token": "eyJhbGc..." }

# 2. Usunięcie losowania
DELETE https://localhost:5001/api/draws/123
Authorization: Bearer eyJhbGc...

# Expected: 200 OK
# {
#   "message": "Losowanie usunięte pomyślnie"
# }
```

### Krok 13: Weryfikacja CASCADE DELETE w bazie danych
```sql
-- Przed usunięciem
SELECT * FROM Draws WHERE Id = 123;
SELECT * FROM DrawNumbers WHERE DrawId = 123; -- Powinno być 6 rekordów

-- Po wywołaniu DELETE endpoint
SELECT * FROM Draws WHERE Id = 123; -- Pusty wynik
SELECT * FROM DrawNumbers WHERE DrawId = 123; -- Pusty wynik (CASCADE DELETE)
```

### Krok 14: Dokumentacja API (opcjonalnie)
- Aktualizacja Swagger UI (automatycznie przez `.WithOpenApi()`)
- Dodanie przykładów do dokumentacji REST API Plan
- Aktualizacja Postman Collection

### Krok 15: Code Review Checklist
- [ ] Walidacja ID > 0 w Validator
- [ ] Sprawdzenie JWT i IsAdmin w Handler
- [ ] Obsługa wszystkich błędów (400, 401, 403, 404, 500)
- [ ] Logowanie zdarzeń (Serilog)
- [ ] CASCADE DELETE działa poprawnie
- [ ] Testy jednostkowe i integracyjne przechodzą
- [ ] Endpoint zarejestrowany w Program.cs
- [ ] Dokumentacja Swagger wygenerowana
- [ ] Kod zgodny z Vertical Slice Architecture

---

## 10. Checklist przed wdrożeniem na produkcję

### Bezpieczeństwo
- [ ] HTTPS wymuszony (`app.UseHttpsRedirection()` odkomentowany)
- [ ] JWT poprawnie skonfigurowany (Issuer, Audience, Key)
- [ ] IsAdmin claim dodany do JWT payload (optymalizacja)
- [ ] Rate limiting dla DELETE endpoints (opcjonalnie)
- [ ] CORS policies zrewidowane (obecnie AllowAnyOrigin)

### Wydajność
- [ ] Indeksy na Draws.Id i DrawNumbers.DrawId istnieją
- [ ] Testy wydajnościowe dla 1000+ losowań
- [ ] Monitoring czasu odpowiedzi (<500ms dla 95 percentyla)

### Logowanie i monitoring
- [ ] Serilog zapisuje logi do pliku i konsoli
- [ ] Alerty dla 403/404/500 errors (opcjonalnie)
- [ ] Audyt usunięć (kto, kiedy, co usunął) w logach

### Testy
- [ ] 100% code coverage dla Handler
- [ ] Testy integracyjne dla wszystkich scenariuszy błędów
- [ ] Testy manualne na środowisku staging

### Dokumentacja
- [ ] Swagger UI dostępny (jeśli Swagger:Enabled = true)
- [ ] Przykłady w Postman Collection
- [ ] README zaktualizowany z nowym endpointem

---

## 11. Potencjalne rozszerzenia (post-MVP)

### Soft Delete zamiast Hard Delete
```csharp
// Dodać kolumnę IsDeleted do Draws
draw.IsDeleted = true;
draw.DeletedAt = DateTime.UtcNow;
await db.SaveChangesAsync();

// Wszystkie queries muszą filtrować IsDeleted = false
```

### Bulk Delete
```csharp
// DELETE /api/draws?ids=1,2,3
public record Request(int[] Ids) : IRequest<Response>;

// W Handler
foreach (var id in request.Ids)
{
    var draw = await db.Draws.FindAsync(id);
    if (draw != null) db.Draws.Remove(draw);
}
await db.SaveChangesAsync();
```

### Audit Trail
```csharp
// Tabela DrawAudit
public class DrawAudit
{
    public int Id { get; set; }
    public int DrawId { get; set; }
    public string Action { get; set; } // "DELETE"
    public int PerformedByUserId { get; set; }
    public DateTime PerformedAt { get; set; }
    public string Details { get; set; } // JSON z danymi losowania
}

// W Handler po usunięciu
db.DrawAudits.Add(new DrawAudit
{
    DrawId = draw.Id,
    Action = "DELETE",
    PerformedByUserId = userId,
    PerformedAt = DateTime.UtcNow,
    Details = JsonSerializer.Serialize(draw)
});
```

### Undo Delete (wymaga Soft Delete)
```csharp
// POST /api/draws/{id}/restore
var draw = await db.Draws.IgnoreQueryFilters()
    .FirstOrDefaultAsync(d => d.Id == id && d.IsDeleted);

if (draw != null)
{
    draw.IsDeleted = false;
    draw.DeletedAt = null;
    await db.SaveChangesAsync();
}
```

---

## 12. Podsumowanie

### Kluczowe punkty implementacji:
1. ✅ **Autoryzacja:** Tylko administratorzy (IsAdmin = true) mogą usuwać
2. ✅ **Walidacja:** ID > 0, sprawdzenie istnienia losowania
3. ✅ **CASCADE DELETE:** Automatyczne usunięcie DrawNumbers przez FK
4. ✅ **Obsługa błędów:** 400, 401, 403, 404, 500 z odpowiednimi komunikatami
5. ✅ **Logowanie:** Serilog rejestruje wszystkie operacje
6. ✅ **Testy:** Testy jednostkowe i integracyjne dla wszystkich scenariuszy
7. ✅ **Vertical Slice:** Struktura Endpoint → Validator → Handler → DB

### Czas implementacji (szacunkowy):
- Krok 1-6 (kod): **1-2 godziny**
- Krok 7-9 (infrastruktura): **30 minut**
- Krok 10-11 (testy): **1-2 godziny**
- Krok 12-14 (weryfikacja i dokumentacja): **30 minut**
- **TOTAL: 3-5 godzin**

### Główne zależności:
- Entity Framework Core (AppDbContext)
- MediatR (Request/Handler pattern)
- FluentValidation (Validator)
- IHttpContextAccessor (JWT claims)
- Serilog (Logging)
- Custom Exceptions (ForbiddenException, NotFoundException)

### Gotowość na produkcję:
Po wykonaniu wszystkich kroków i przejściu testów, endpoint jest gotowy do wdrożenia na produkcję. Pamiętaj o włączeniu HTTPS i weryfikacji konfiguracji JWT w środowisku produkcyjnym.
