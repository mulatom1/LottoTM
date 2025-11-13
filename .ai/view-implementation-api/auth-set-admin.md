# API Endpoint Implementation Plan: PUT /api/auth/setadmin/{id} ⚠️ TYMCZASOWY

## ⚠️ UWAGA: ENDPOINT TYMCZASOWY DLA MVP

**Ten endpoint jest wyłącznie dla fazy MVP i zostanie usunięty lub całkowicie przeprojektowany przed wdrożeniem produkcyjnym.** W produkcji wymagany będzie odpowiedni system zarządzania uprawnieniami z dodatkowymi warstwami bezpieczeństwa.

## 1. Przegląd punktu końcowego

Endpoint służący do przełączania uprawnień administratora dla użytkownika w systemie LottoTM. Znajduje użytkownika po emailu i przełącza flagę `IsAdmin` na wartość przeciwną (toggle).

**Metoda HTTP:** PUT
**URL:** `/api/auth/setadmin`
**Autoryzacja:** TAK (wymagany JWT token)
**Zwracany kod sukcesu:** 200 OK
**Status:** TYMCZASOWY - tylko dla MVP

## 2. Szczegóły żądania

### 2.1 Struktura URL

```
PUT /api/auth/setadmin
```

### 2.2 Parametry URL

Brak parametrów w URL.

### 2.3 Parametry zapytania

Brak parametrów query string.

### 2.4 Request Body (JSON)

```json
{
  "email": "user@example.com"
}
```

### 2.5 Parametry Request Body

| Pole | Typ | Wymagane | Opis |
|------|-----|----------|------|
| `email` | string | TAK | Email użytkownika, którego uprawnienia mają zostać zmienione |

## 3. Wykorzystywane typy

### 3.1 DTO (Data Transfer Objects)

**Namespace:** `LottoTM.Server.Api.Features.Auth.SetAdmin`

```csharp
public class Contracts
{
    public record Request(
        string Email
    ) : IRequest<Response>;

    public record Response(
         int UserId,
         string Email,
         bool IsAdmin,
         DateTime UpdatedAt
     );
}
```

### 3.2 Entity Model

**Namespace:** `LottoTM.Server.Api.Entities`

```csharp
public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    public ICollection<Draw> CreatedDraws { get; set; } = new List<Draw>();
}
```

## 4. Szczegóły odpowiedzi

### 4.1 Odpowiedź sukcesu (200 OK)

```json
{
  "userId": 123,
  "email": "user@example.com",
  "isAdmin": true,
  "updatedAt": "2025-11-12T10:30:00Z"
}
```

### 4.2 Odpowiedzi błędów

#### 400 Bad Request - Nieprawidłowe dane wejściowe

**Scenariusz 1: Email jest wymagany**
```json
{
  "errors": {
    "email": ["Email jest wymagany"]
  }
}
```

**Scenariusz 2: Nieprawidłowy format email**
```json
{
  "errors": {
    "email": ["Nieprawidłowy format email"]
  }
}
```

**Scenariusz 3: Użytkownik nie istnieje**
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "": ["Użytkownik z emailem user@example.com nie istnieje"]
  }
}
```

#### 401 Unauthorized - Brak tokenu lub token nieprawidłowy

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.2",
  "title": "Unauthorized",
  "status": 401
}
```

#### 500 Internal Server Error - Błąd serwera

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.6.1",
  "title": "An error occurred while processing your request.",
  "status": 500,
  "detail": "Database connection failed"
}
```

## 5. Przepływ danych

### 5.1 Diagram przepływu

```
┌─────────────┐
│   Client    │
└──────┬──────┘
       │
       │ PUT /api/auth/setadmin
       │ { email }
       │ Authorization: Bearer <token>
       ▼
┌─────────────────────────────────────┐
│         Endpoint.cs                 │
│  MapPut("/api/auth/setadmin")       │
│  - Requires Authentication          │
└──────┬──────────────────────────────┘
       │
       │ Send(Request) via MediatR
       │ (email from body)
       ▼
┌─────────────────────────────────────┐
│        Validator.cs                 │
│  FluentValidation rules             │
│  - Email format                     │
│  - Email required                   │
└──────┬──────────────────────────────┘
       │
       │ ValidationResult
       ▼
┌─────────────────────────────────────┐
│         Handler.cs                  │
│  SetAdminHandler                    │
│  1. Validate request                │
│  2. Find user by email              │
│  3. Toggle IsAdmin flag             │
│  4. Save to database                │
│  5. Log change                      │
│  6. Return response                 │
└──────┬──────────────────────────────┘
       │
       │ DbContext.Users.FirstOrDefault()
       │ DbContext.SaveChangesAsync()
       ▼
┌─────────────────────────────────────┐
│       SQL Server Database           │
│  UPDATE Users                       │
│  SET IsAdmin = NOT IsAdmin          │
│  WHERE Email = @email               │
└──────┬──────────────────────────────┘
       │
       │ Success/Failure
       ▼
┌─────────────────────────────────────┐
│         Handler.cs                  │
│  Return Response                    │
│  { userId, email, isAdmin, ... }    │
└──────┬──────────────────────────────┘
       │
       │ 200 OK or 400 Bad Request
       ▼
┌─────────────┐
│   Client    │
└─────────────┘
```

### 5.2 Szczegółowy przepływ operacji

1. **Klient wysyła żądanie PUT** z emailem użytkownika w body JSON
2. **Endpoint odbiera żądanie** z wymaganym JWT tokenem w header
3. **Middleware autoryzacji** weryfikuje token JWT
4. **Endpoint przekazuje do MediatR** dane z body (email)
5. **MediatR uruchamia Validator** (FluentValidation)
   - Walidacja formatu email (regex)
   - Walidacja wymaganego pola email
6. **Jeśli walidacja nie powiedzie się:**
   - Rzucany jest `ValidationException`
   - Middleware `ExceptionHandlingMiddleware` przechwytuje wyjątek
   - Zwracana jest odpowiedź 400 Bad Request z listą błędów
7. **Jeśli walidacja się powiedzie:**
   - Handler szuka użytkownika po emailu
   - Sprawdza czy użytkownik istnieje
   - Przełącza flagę IsAdmin: `user.IsAdmin = !user.IsAdmin`
   - Zapisuje zmiany w bazie danych
   - Loguje zmianę uprawnień
8. **Obsługa błędów:**
   - Użytkownik nie istnieje → ValidationException (400)
   - Błędy bazy danych → 500 Internal Server Error
9. **Zwrócenie odpowiedzi:**
   - 200 OK z zaktualizowanymi danymi użytkownika

## 6. Względy bezpieczeństwa

### 6.1 Uwierzytelnianie

**Wymagane:** JWT token w header `Authorization: Bearer <token>`

**Weryfikacja:**
- Token musi być poprawny
- Token nie może być wygasły
- UserId z tokenu jest dostępny w kontekście (nie jest wykorzystywany w tym endpoincie)

### 6.2 Identyfikacja przez email

**Cel:** Użycie emaila jako jednoznacznego identyfikatora użytkownika

**Dlaczego email?**
- Email jest unikalny w systemie (UNIQUE constraint)
- Bardziej czytelny dla operatora niż ID numeryczne
- Eliminuje ryzyko pomyłki ID
- Bezpośrednia identyfikacja użytkownika

**Implementacja:**
```csharp
var user = await _dbContext.Users
    .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

if (user == null)
{
    throw new ValidationException($"Użytkownik z emailem {request.Email} nie istnieje");
}
```

### 6.3 Ograniczenia bezpieczeństwa (MVP)

⚠️ **UWAGA:** Ten endpoint ma znaczne ograniczenia bezpieczeństwa:

| Brak | Dlaczego to problem | Rozwiązanie produkcyjne |
|------|---------------------|-------------------------|
| **Brak kontroli uprawnień** | Każdy uwierzytelniony użytkownik może zmieniać uprawnienia | Tylko super-admin może nadawać uprawnienia |
| **Brak audytu** | Nie zapisujemy KTO zmienił uprawnienia | Tabela UserRoleChanges z CreatedBy |
| **Brak potwierdzenia** | Zmiana następuje natychmiast | Wymagane potwierdzenie przez email/2FA |
| **Brak historii** | Nie wiemy jakie były poprzednie uprawnienia | Tabela AuditLog z historią zmian |
| **Prosty toggle** | Tylko IsAdmin true/false | System ról z wieloma poziomami uprawnień |

### 6.4 Mitigacja ryzyka w MVP

**Kroki zmniejszające ryzyko:**
1. Identyfikacja przez email (unikalny, czytelny)
2. Dokładne logowanie zmian uprawnień (Serilog)
3. Endpoint oznaczony jako TYMCZASOWY w dokumentacji
4. Brak promocji tego endpointa w UI (dostępny tylko przez API)
5. Planowane usunięcie przed produkcją

## 7. Obsługa błędów

### 7.1 Scenariusze błędów i kody statusu

| Scenariusz | Kod HTTP | Komunikat |
|------------|----------|-----------|
| Email pusty | 400 | `{ "errors": { "email": ["Email jest wymagany"] } }` |
| Nieprawidłowy format email | 400 | `{ "errors": { "email": ["Nieprawidłowy format email"] } }` |
| Użytkownik nie istnieje | 400 | ValidationException - "Użytkownik z emailem {email} nie istnieje" |
| Brak tokenu JWT | 401 | Unauthorized |
| Token wygasły | 401 | Unauthorized |
| Błąd bazy danych | 500 | Internal Server Error |

### 7.2 Globalna obsługa wyjątków

System wykorzystuje `ExceptionHandlingMiddleware` skonfigurowany w `Program.cs`:

```csharp
app.UseMiddleware<ExceptionHandlingMiddleware>();
```

**Obsługiwane typy wyjątków:**

1. **ValidationException** (FluentValidation)
   - Status: 400 Bad Request
   - Format: `{ "errors": { "field": ["message"] } }`

2. **DbUpdateException** (Entity Framework)
   - Status: 500 Internal Server Error
   - Logowanie z Serilog

3. **Exception** (nieobsłużone wyjątki)
   - Status: 500 Internal Server Error
   - Format: ProblemDetails (RFC 9110)

### 7.3 Logowanie zmian

**Biblioteka:** Serilog
**Poziom:** Information

**Log dla endpointa SetAdmin:**
```json
{
  "Timestamp": "2025-11-12T10:30:00Z",
  "Level": "Information",
  "Message": "Admin status toggled for user {Email} (ID: {UserId}). New IsAdmin value: {IsAdmin}",
  "Properties": {
    "Email": "user@example.com",
    "UserId": 123,
    "IsAdmin": true,
    "Endpoint": "PUT /api/auth/setadmin/123",
    "SourceContext": "LottoTM.Server.Api.Features.Auth.SetAdmin.Handler"
  }
}
```

## 8. Wydajność

### 8.1 Wymagania wydajnościowe

**NFR-002:** CRUD operacje ≤ 500ms (95 percentyl)

### 8.2 Potencjalne wąskie gardła

| Operacja | Szacowany czas | Optymalizacja |
|----------|----------------|---------------|
| Walidacja FluentValidation | ~5ms | Brak (wystarczające) |
| Wyszukanie użytkownika (Email) | ~5-15ms | Indeks IX_Users_Email (UNIQUE) |
| UPDATE bazy danych | ~10-20ms | Pojedynczy UPDATE |
| **TOTAL** | **~20-40ms** | **<< 500ms (doskonałe)** |

### 8.3 Optymalizacje

**Już zaimplementowane:**
- ✅ Indeks UNIQUE na kolumnie Email (szybkie wyszukanie)
- ✅ Pojedynczy UPDATE (nie batch)
- ✅ Brak JOIN (operacja na pojedynczej tabeli)

**Nie potrzebne dalsze optymalizacje** - endpoint jest bardzo szybki.

## 9. Etapy wdrożenia

### Krok 1: Utworzenie struktury folderów

```
src/server/LottoTM.Server.Api/Features/Auth/SetAdmin/
├── Endpoint.cs
├── Contracts.cs
├── Handler.cs
└── Validator.cs
```

### Krok 2: Implementacja Contracts.cs

```csharp
using MediatR;

namespace LottoTM.Server.Api.Features.Auth.SetAdmin;

/// <summary>
/// Contracts for SetAdmin endpoint
/// TEMPORARY: This endpoint is only for MVP. Will be replaced with proper admin management in production.
/// </summary>
public class Contracts
{
    /// <summary>
    /// SetAdmin request with user email
    /// </summary>
    /// <param name="Email">User's email address</param>
    public record Request(
        string Email
    ) : IRequest<Response>;

    /// <summary>
    /// SetAdmin response with updated user information
    /// </summary>
    /// <param name="UserId">Unique user identifier</param>
    /// <param name="Email">User's email address</param>
    /// <param name="IsAdmin">Updated admin flag value (toggled)</param>
    /// <param name="UpdatedAt">Timestamp when the admin status was updated (UTC)</param>
    public record Response(
         int UserId,
         string Email,
         bool IsAdmin,
         DateTime UpdatedAt
     );
}
```

### Krok 3: Implementacja Validator.cs

```csharp
using FluentValidation;

namespace LottoTM.Server.Api.Features.Auth.SetAdmin;

/// <summary>
/// Validator for SetAdmin requests
/// Validates email format
/// TEMPORARY: This endpoint is only for MVP. Will be replaced with proper admin management in production.
/// </summary>
public class Validator : AbstractValidator<Contracts.Request>
{
    public Validator()
    {
        // Email: required
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email jest wymagany");

        // Email: format validation
        RuleFor(x => x.Email)
            .EmailAddress()
            .WithMessage("Nieprawidłowy format email");
    }
}
```

### Krok 4: Implementacja Handler.cs

```csharp
using FluentValidation;
using LottoTM.Server.Api.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LottoTM.Server.Api.Features.Auth.SetAdmin;

/// <summary>
/// Handler for SetAdmin requests
/// Toggles the IsAdmin flag for a user
/// TEMPORARY: This endpoint is only for MVP. Will be replaced with proper admin management in production.
/// </summary>
public class SetAdminHandler : IRequestHandler<Contracts.Request, Contracts.Response>
{
    private readonly ILogger<SetAdminHandler> _logger;
    private readonly IValidator<Contracts.Request> _validator;
    private readonly AppDbContext _dbContext;

    public SetAdminHandler(
        ILogger<SetAdminHandler> logger,
        IValidator<Contracts.Request> validator,
        AppDbContext dbContext)
    {
        _logger = logger;
        _validator = validator;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Handles SetAdmin request by:
    /// 1. Validating the request
    /// 2. Finding the user by email
    /// 3. Toggling the IsAdmin flag
    /// 4. Saving to database
    /// 5. Logging the change
    /// 6. Returning the response
    /// </summary>
    public async Task<Contracts.Response> Handle(
        Contracts.Request request,
        CancellationToken cancellationToken)
    {
        // 1. Validate the request
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // 2. Find the user by email
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (user == null)
        {
            throw new ValidationException($"Użytkownik z emailem {request.Email} nie istnieje");
        }

        // 3. Toggle the IsAdmin flag
        user.IsAdmin = !user.IsAdmin;
        var updatedAt = DateTime.UtcNow;

        // 4. Save to database
        await _dbContext.SaveChangesAsync(cancellationToken);

        // 5. Log the change
        _logger.LogInformation(
            "Admin status toggled for user {Email} (ID: {UserId}). New IsAdmin value: {IsAdmin}",
            user.Email,
            user.Id,
            user.IsAdmin);

        // 6. Return success response
        return new Contracts.Response(
            UserId: user.Id,
            Email: user.Email,
            IsAdmin: user.IsAdmin,
            UpdatedAt: updatedAt
        );
    }
}
```

### Krok 5: Implementacja Endpoint.cs

```csharp
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LottoTM.Server.Api.Features.Auth.SetAdmin;

/// <summary>
/// Minimal API endpoint for toggling user admin status
/// TEMPORARY: This endpoint is only for MVP. Will be replaced with proper admin management in production.
/// </summary>
public static class Endpoint
{
    /// <summary>
    /// Registers the SetAdmin endpoint
    /// PUT /api/auth/setadmin - Requires authentication
    /// WARNING: This is a temporary MVP endpoint and should be replaced with proper admin management
    /// </summary>
    public static void AddEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPut("api/auth/setadmin", async (
            [FromBody] SetAdminBodyRequest body,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            // Create request from body (only email)
            var request = new Contracts.Request(body.Email);
            var result = await mediator.Send(request, cancellationToken);
            return Results.Ok(result);
        })
        .RequireAuthorization()
        .WithName("AuthSetAdmin")
        .WithTags("Auth")
        .Produces<Contracts.Response>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
        .WithOpenApi(operation =>
        {
            operation.Summary = "[TEMPORARY MVP] Toggle user admin status";
            operation.Description = "Toggles the IsAdmin flag for a user. This is a temporary endpoint for MVP only and will be replaced with proper admin management in production.";
            return operation;
        });
    }

    /// <summary>
    /// Request body for SetAdmin endpoint
    /// </summary>
    /// <param name="Email">User's email address</param>
    public record SetAdminBodyRequest(string Email);
}
```

### Krok 6: Rejestracja endpointa w Program.cs

Dodaj następującą linię w pliku `Program.cs` po linii rejestrującej endpoint Register:

```csharp
LottoTM.Server.Api.Features.Auth.SetAdmin.Endpoint.AddEndpoint(app);
```

**Pełny kontekst:**
```csharp
// Register endpoints
LottoTM.Server.Api.Features.ApiVersion.Endpoint.AddEndpoint(app);
LottoTM.Server.Api.Features.Auth.Login.Endpoint.AddEndpoint(app);
LottoTM.Server.Api.Features.Auth.Register.Endpoint.AddEndpoint(app);
LottoTM.Server.Api.Features.Auth.SetAdmin.Endpoint.AddEndpoint(app); // NOWE - TYMCZASOWE
LottoTM.Server.Api.Features.Draws.DrawsCreate.Endpoint.AddEndpoint(app);
// ...
```

### Krok 7: Testowanie ręczne przez Swagger/Postman

**Swagger URL:** `http://localhost:{port}/swagger/index.html`

**Przykładowe żądanie (Postman):**
```
PUT http://localhost:5000/api/auth/setadmin
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json

{
  "email": "user@example.com"
}
```

**Oczekiwana odpowiedź:**
```
HTTP/1.1 200 OK
Content-Type: application/json

{
  "userId": 123,
  "email": "user@example.com",
  "isAdmin": true,
  "updatedAt": "2025-11-12T10:30:00Z"
}
```

### Krok 8: Weryfikacja w bazie danych

Sprawdź czy flaga IsAdmin została zmieniona:

```sql
SELECT Id, Email, IsAdmin
FROM Users
WHERE Id = 123;
```

**Oczekiwany wynik:**
- `IsAdmin`: 1 (TRUE) jeśli było 0, lub 0 (FALSE) jeśli było 1

---

## 10. Checklist wdrożenia

- [x] Utworzenie folderów `Features/Auth/SetAdmin/`
- [x] Implementacja `Contracts.cs` (Request/Response records)
- [x] Implementacja `Validator.cs` (FluentValidation rules)
- [x] Implementacja `Handler.cs` (SetAdminHandler)
- [x] Implementacja `Endpoint.cs` (MapPut)
- [x] Rejestracja endpointa w `Program.cs`
- [x] Aktualizacja dokumentacji (.ai/prd.md, .ai/api-plan.md)
- [ ] Testowanie manualne przez Swagger/Postman
- [ ] Weryfikacja w bazie danych (SQL query)
- [ ] Code review (bezpieczeństwo, tymczasowość)
- [ ] Dodanie komentarzy TEMPORARY w kodzie
- [ ] Dokumentacja planu usunięcia przed produkcją

---

## 11. Uwagi implementacyjne

### 11.1 Dependency Injection

**AppDbContext**, **IValidator** i **ILogger** są automatycznie wstrzykiwane przez DI:

```csharp
public SetAdminHandler(
    ILogger<SetAdminHandler> logger,
    IValidator<Contracts.Request> validator,
    AppDbContext dbContext)
```

### 11.2 Async/Await

Wszystkie operacje I/O są asynchroniczne:
- `ValidateAsync()` - walidacja
- `FirstOrDefaultAsync()` - wyszukanie użytkownika
- `SaveChangesAsync()` - zapis do bazy danych

### 11.3 Email jako identyfikator

Email służy jako jednoznaczny identyfikator użytkownika:
- Unikalny w systemie (UNIQUE constraint)
- Bardziej czytelny dla operatora niż ID numeryczne
- Eliminuje ryzyko pomyłki przez błędne ID
- Bezpośrednia identyfikacja użytkownika

### 11.4 Tymczasowość endpointa

**Przypomnienia w kodzie:**
- Komentarze XML `/// TEMPORARY`
- Komentarze w Swagger: `[TEMPORARY MVP]`
- Warning emoji w dokumentacji: ⚠️
- Jasna komunikacja w OpenAPI description

---

## 12. Plan usunięcia (Pre-Production)

### 12.1 Przed wdrożeniem produkcyjnym

**Kroki do wykonania:**

1. **Usunięcie endpointa:**
   - Usuń folder `Features/Auth/SetAdmin/`
   - Usuń rejestrację w `Program.cs`

2. **Implementacja właściwego systemu:**
   - Tabela `UserRoles` z różnymi poziomami uprawnień
   - Endpoint `POST /api/admin/users/{id}/roles` (tylko super-admin)
   - Audit log dla zmian uprawnień
   - Potwierdzenie przez email/2FA

3. **Migracja danych:**
   - Zachowanie informacji o obecnych adminach
   - Mapowanie IsAdmin → nowa tabela ról

4. **Aktualizacja dokumentacji:**
   - Usunięcie SetAdmin z PRD i API Plan
   - Dodanie nowego systemu ról

### 12.2 System ról produkcyjny (rekomendacja)

**Tabele:**
```sql
CREATE TABLE Roles (
    Id INT PRIMARY KEY IDENTITY,
    Name NVARCHAR(50) NOT NULL,
    Permissions NVARCHAR(MAX) -- JSON
);

CREATE TABLE UserRoles (
    UserId INT NOT NULL,
    RoleId INT NOT NULL,
    GrantedByUserId INT NOT NULL,
    GrantedAt DATETIME2 NOT NULL,
    PRIMARY KEY (UserId, RoleId),
    FOREIGN KEY (UserId) REFERENCES Users(Id),
    FOREIGN KEY (RoleId) REFERENCES Roles(Id),
    FOREIGN KEY (GrantedByUserId) REFERENCES Users(Id)
);
```

**Przykładowe role:**
- SuperAdmin (wszystkie uprawnienia)
- Admin (zarządzanie losowaniami)
- Moderator (readonly losowań)
- User (podstawowy użytkownik)

---

## 13. Ostrzeżenia i ryzyka

### ⚠️ KRYTYCZNE OSTRZEŻENIA

1. **NIE używaj tego endpointa w produkcji** - tylko dla MVP
2. **NIE udostępniaj w UI** - dostęp tylko przez API/Swagger
3. **Loguj wszystkie użycia** - audyt zmian uprawnień
4. **Planuj usunięcie** - przed wdrożeniem produkcyjnym
5. **Informuj zespół** - o tymczasowości rozwiązania

### 13.1 Ryzyka bezpieczeństwa

| Ryzyko | Poziom | Mitigacja w MVP |
|--------|--------|-----------------|
| Każdy user może zmieniać uprawnienia | KRYTYCZNE | Tylko dla MVP, brak UI |
| Brak audytu WHO changed | WYSOKIE | Dokładne logowanie Serilog |
| Brak potwierdzenia | ŚREDNIE | Weryfikacja email |
| Prosty toggle | NISKIE | Wystarczające dla MVP |

---

## Koniec planu implementacji

**Status:** ✅ ZAIMPLEMENTOWANE (MVP)
**Do zrobienia:** Usunięcie przed produkcją
**Deadline usunięcia:** Przed wdrożeniem produkcyjnym
