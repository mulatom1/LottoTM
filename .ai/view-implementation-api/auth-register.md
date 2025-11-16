# API Endpoint Implementation Plan: POST /api/auth/register

## 1. Przegląd punktu końcowego

Endpoint służący do rejestracji nowego użytkownika w systemie LottoTM z automatycznym logowaniem po pomyślnej rejestracji. Po utworzeniu konta użytkownik otrzymuje komunikat o sukcesie i może rozpocząć korzystanie z systemu.

**Metoda HTTP:** POST
**URL:** `/api/auth/register`
**Autoryzacja:** NIE (endpoint publiczny)
**Zwracany kod sukcesu:** 201 Created

## 2. Szczegóły żądania

### 2.1 Struktura URL
```
POST /api/auth/register
```

### 2.2 Parametry zapytania
Brak parametrów URL ani query string.

### 2.3 Request Body (JSON)
```json
{
  "email": "user@example.com",
  "password": "SecurePass123!",
  "confirmPassword": "SecurePass123!"
}
```

### 2.4 Parametry Request Body

| Pole | Typ | Wymagane | Opis |
|------|-----|----------|------|
| `email` | string | TAK | Adres email użytkownika (login) |
| `password` | string | TAK | Hasło użytkownika |
| `confirmPassword` | string | TAK | Potwierdzenie hasła |

## 3. Wykorzystywane typy

### 3.1 DTO (Data Transfer Objects)

**Namespace:** `LottoTM.Server.Api.Features.Auth.Register`

```csharp
public class Contracts
{
    public record Request(
        string Email,
        string Password,
        string ConfirmPassword
    ) : IRequest<Response>;

    public record Response(
        string Token,
        int UserId,
        string Email,
        bool IsAdmin,
        DateTime ExpiresAt
    );
}
```

### 3.2 Entity Model

**Namespace:** `LottoTM.Server.Api.Repositories`

```csharp
public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public bool IsAdmin { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    public ICollection<Draw> CreatedDraws { get; set; } = new List<Draw>();
}
```

## 4. Szczegóły odpowiedzi

### 4.1 Odpowiedź sukcesu (201 Created)

```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "userId": 123,
  "email": "user@example.com",
  "isAdmin": false,
  "expiresAt": "2025-11-04T10:30:00Z"
}
```

### 4.2 Odpowiedzi błędów

#### 400 Bad Request - Nieprawidłowe dane wejściowe

**Scenariusz 1: Email jest już zajęty**
```json
{
  "errors": {
    "email": ["Email jest już zajęty"]
  }
}
```

**Scenariusz 2: Hasło zbyt krótkie**
```json
{
  "errors": {
    "password": ["Hasło musi mieć min. 8 znaków"]
  }
}
```

**Scenariusz 3: Hasła nie są identyczne**
```json
{
  "errors": {
    "confirmPassword": ["Hasła nie są identyczne"]
  }
}
```

**Scenariusz 4: Nieprawidłowy format email**
```json
{
  "errors": {
    "email": ["Nieprawidłowy format email"]
  }
}
```

**Scenariusz 5: Wiele błędów walidacji**
```json
{
  "errors": {
    "email": ["Nieprawidłowy format email"],
    "password": [
      "Hasło musi mieć min. 8 znaków",
      "Hasło musi zawierać wielką literę",
      "Hasło musi zawierać cyfrę",
      "Hasło musi zawierać znak specjalny"
    ],
    "confirmPassword": ["Hasła nie są identyczne"]
  }
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
       │ POST /api/auth/register
       │ { email, password, confirmPassword }
       ▼
┌─────────────────────────────────────┐
│         Endpoint.cs                 │
│  MapPost("/api/auth/register")      │
└──────┬──────────────────────────────┘
       │
       │ Send(Request) via MediatR
       ▼
┌─────────────────────────────────────┐
│        Validator.cs                 │
│  FluentValidation rules             │
│  - Email format                     │
│  - Email uniqueness (async)         │
│  - Password min length (8)          │
│  - Password complexity              │
│  - ConfirmPassword match            │
└──────┬──────────────────────────────┘
       │
       │ ValidationResult
       ▼
┌─────────────────────────────────────┐
│         Handler.cs                  │
│  RegisterHandler                    │
│  1. Validate request                │
│  2. Hash password (BCrypt)          │
│  3. Create User entity              │
│  4. Save to database                │
└──────┬──────────────────────────────┘
       │
       │ DbContext.Users.Add()
       │ DbContext.SaveChangesAsync()
       ▼
┌─────────────────────────────────────┐
│       SQL Server Database           │
│  INSERT INTO Users                  │
│  (Email, PasswordHash,              │
│   IsAdmin, CreatedAt)               │
└──────┬──────────────────────────────┘
       │
       │ Success/Failure
       ▼
┌─────────────────────────────────────┐
│         Handler.cs                  │
│  Generate JWT Token                 │
│  Return Response                    │
│  { token, userId, email, ... }      │
└──────┬──────────────────────────────┘
       │
       │ 201 Created or 400 Bad Request
       ▼
┌─────────────┐
│   Client    │
└─────────────┘
```

### 5.2 Szczegółowy przepływ operacji

1. **Klient wysyła żądanie POST** z danymi JSON (email, password, confirmPassword)
2. **Endpoint odbiera żądanie** i przekazuje do MediatR
3. **MediatR uruchamia Validator** (FluentValidation)
   - Walidacja formatu email (regex)
   - Walidacja unikalności email w bazie danych (async)
   - Walidacja długości hasła (min. 8 znaków)
   - Walidacja złożoności hasła (wielka litera, cyfra, znak specjalny)
   - Walidacja zgodności haseł (password == confirmPassword)
4. **Jeśli walidacja nie powiedzie się:**
   - Rzucany jest `ValidationException`
   - Middleware `ExceptionHandlingMiddleware` przechwytuje wyjątek
   - Zwracana jest odpowiedź 400 Bad Request z listą błędów
5. **Jeśli walidacja się powiedzie:**
   - Handler hashuje hasło używając BCrypt (10 rounds)
   - Tworzy nowy obiekt `User` z:
     - Email z żądania
     - PasswordHash (hash BCrypt)
     - IsAdmin = false
     - CreatedAt = DateTime.UtcNow
   - Dodaje użytkownika do DbContext
   - Zapisuje zmiany w bazie danych
6. **Generowanie JWT tokenu (automatyczne logowanie):**
   - Generowanie tokenu JWT (ważność 24h)
   - Claims: UserId, Email, IsAdmin
   - Zwrot tokenu wraz z danymi użytkownika
7. **Obsługa błędów bazy danych:**
   - UNIQUE constraint violation → 400 Bad Request
   - Inne błędy → 500 Internal Server Error
8. **Zwrócenie odpowiedzi:**
   - 201 Created z tokenem JWT i danymi użytkownika (automatyczne logowanie po rejestracji)

## 6. Względy bezpieczeństwa

### 6.1 Haszowanie haseł

**Algorytm:** BCrypt
**Biblioteka:** `BCrypt.Net-Next` (NuGet)
**Work factor:** 10 rounds (minimum), rekomendacja: 12 rounds dla produkcji

**Implementacja:**
```csharp
var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 10);
```

**Przechowywanie:**
- Hash jest przechowywany w kolumnie `Users.PasswordHash` (NVARCHAR(255))
- Hash BCrypt ma długość 60 znaków
- Format: `$2a$10$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy`

**Dlaczego BCrypt?**
- Odporny na ataki brute-force (wolny celowo)
- Wbudowana sól (automatyczna, losowa)
- Industry standard
- Work factor można zwiększyć w przyszłości (migracja)

### 6.2 Walidacja email

**Format:** Walidacja przez FluentValidation `.EmailAddress()`
**Unikalność:** Sprawdzenie w bazie danych przez async validator

**Ochrona przed:**
- SQL Injection: Entity Framework parametryzuje zapytania automatycznie
- Email enumeration: Zwracamy ogólny błąd, nie ujawniamy czy email istnieje

### 6.3 Walidacja hasła

**Minimalne wymagania (MVP):**
- Długość: min. 8 znaków

**Zalecane wymagania (rekomendacja):**
- Min. 1 wielka litera (A-Z)
- Min. 1 cyfra (0-9)
- Min. 1 znak specjalny (!@#$%^&*()_+)

**Implementacja regex:**
```csharp
RuleFor(x => x.Password)
    .NotEmpty().WithMessage("Hasło jest wymagane")
    .MinimumLength(8).WithMessage("Hasło musi mieć min. 8 znaków")
    .Matches(@"[A-Z]").WithMessage("Hasło musi zawierać wielką literę")
    .Matches(@"[0-9]").WithMessage("Hasło musi zawierać cyfrę")
    .Matches(@"[\W_]").WithMessage("Hasło musi zawierać znak specjalny");
```

### 6.4 Ochrona przed atakami

**Rate Limiting:**
- Middleware `AspNetCoreRateLimit` (NFR-011)
- Limit: 5 prób/minutę/IP
- Dotyczy endpointów `/api/auth/register` i `/api/auth/login`

**HTTPS:**
- Wymagane na produkcji (NFR-007)
- Azure App Service / Webio.pl zapewniają SSL/TLS
- Middleware: `app.UseHttpsRedirection()`

**CORS:**
- Skonfigurowane dla frontendu React
- Obecnie: `AllowAnyOrigin()` (dla MVP)
- Produkcja: restrykcja do konkretnych domen

**Secure Headers:**
- X-Frame-Options: DENY
- X-Content-Type-Options: nosniff
- Referrer-Policy: strict-origin-when-cross-origin
- Content-Security-Policy: default-src 'self'

### 6.5 Podatności do uniknięcia

| Podatność | Jak uniknąć |
|-----------|-------------|
| **SQL Injection** | Entity Framework parametryzuje automatycznie |
| **Weak Passwords** | FluentValidation z regex complexity rules |
| **Brute Force** | Rate limiting (5 prób/min) + BCrypt (wolny) |
| **Email Enumeration** | Ogólne komunikaty błędów |
| **XSS** | Brak renderowania danych użytkownika w tym endpoincie |
| **CSRF** | JWT w header Authorization (nie w cookies) |

## 7. Obsługa błędów

### 7.1 Scenariusze błędów i kody statusu

| Scenariusz | Kod HTTP | Komunikat |
|------------|----------|-----------|
| Email jest już zajęty | 400 | `{ "errors": { "email": ["Email jest już zajęty"] } }` |
| Nieprawidłowy format email | 400 | `{ "errors": { "email": ["Nieprawidłowy format email"] } }` |
| Hasło zbyt krótkie | 400 | `{ "errors": { "password": ["Hasło musi mieć min. 8 znaków"] } }` |
| Brak wielkiej litery w haśle | 400 | `{ "errors": { "password": ["Hasło musi zawierać wielką literę"] } }` |
| Brak cyfry w haśle | 400 | `{ "errors": { "password": ["Hasło musi zawierać cyfrę"] } }` |
| Brak znaku specjalnego | 400 | `{ "errors": { "password": ["Hasło musi zawierać znak specjalny"] } }` |
| Hasła nie są identyczne | 400 | `{ "errors": { "confirmPassword": ["Hasła nie są identyczne"] } }` |
| Pusty email | 400 | `{ "errors": { "email": ["Email jest wymagany"] } }` |
| Puste hasło | 400 | `{ "errors": { "password": ["Hasło jest wymagane"] } }` |
| Puste confirmPassword | 400 | `{ "errors": { "confirmPassword": ["Potwierdzenie hasła jest wymagane"] } }` |
| Błąd połączenia z bazą | 500 | `{ "type": "...", "title": "An error occurred...", "status": 500 }` |
| UNIQUE constraint violation | 400 | `{ "errors": { "email": ["Email jest już zajęty"] } }` |

### 7.2 Globalna obsługa wyjątków

System wykorzystuje `ExceptionHandlingMiddleware` skonfigurowany w `Program.cs`:

```csharp
app.UseMiddleware<ExceptionHandlingMiddleware>();
```

**Obsługiwane typy wyjątków:**

1. **ValidationException** (FluentValidation)
   - Status: 400 Bad Request
   - Format: `{ "errors": { "field": ["message"] } }`
   - Zwraca wszystkie błędy walidacji zgrupowane po polach

2. **DbUpdateException** (Entity Framework)
   - Status: 400 Bad Request (UNIQUE constraint)
   - Status: 500 Internal Server Error (inne błędy DB)
   - Logowanie z Serilog

3. **Exception** (nieobsłużone wyjątki)
   - Status: 500 Internal Server Error
   - Format: ProblemDetails (RFC 9110)
   - Pełny stack trace w logach Serilog

### 7.3 Logowanie błędów

**Biblioteka:** Serilog
**Konfiguracja:** `appsettings.json` → `Serilog` section

**Logi dla endpointa register:**
- INFO: Pomyślna rejestracja (email użytkownika)
- WARNING: Próba rejestracji z istniejącym emailem
- ERROR: Błędy bazy danych, wyjątki systemowe
- DEBUG: Szczegóły walidacji (tylko Development)

**Przykładowy log:**
```json
{
  "Timestamp": "2025-11-05T10:30:00Z",
  "Level": "Information",
  "Message": "User registered successfully",
  "Properties": {
    "Email": "user@example.com",
    "UserId": 123,
    "Endpoint": "POST /api/auth/register",
    "SourceContext": "LottoTM.Server.Api.Features.Auth.Register.Handler"
  }
}
```

## 8. Wydajność

### 8.1 Wymagania wydajnościowe

**NFR-002:** CRUD operacje ≤ 500ms (95 percentyl)

### 8.2 Potencjalne wąskie gardła

| Operacja | Szacowany czas | Optymalizacja |
|----------|----------------|---------------|
| Walidacja FluentValidation | ~5-10ms | Brak (wystarczające) |
| Sprawdzenie unikalności email | ~10-50ms | Indeks `IX_Users_Email` (UNIQUE) |
| Haszowanie hasła BCrypt (10 rounds) | ~100-200ms | Work factor 10 (ok dla MVP) |
| INSERT do bazy danych | ~10-50ms | Brak (pojedynczy INSERT) |
| **TOTAL** | **~125-310ms** | **< 500ms (spełnione)** |

### 8.3 Strategie optymalizacji

**Jeśli wydajność < 500ms (aktualnie OK):**
- ✅ Indeks na `Users.Email` (UNIQUE) dla szybkiego sprawdzenia unikalności
- ✅ BCrypt work factor 10 (kompromis bezpieczeństwo/wydajność)
- ✅ Entity Framework z parametryzowanymi zapytaniami

**Jeśli w przyszłości potrzebna dalsza optymalizacja:**
- Async I/O dla operacji bazodanowych (już używane)
- Connection pooling w SQL Server (domyślnie włączone)
- Zwiększenie work factor BCrypt do 12 na produkcji (trade-off)

### 8.4 Cache

**Czy potrzebny cache?** NIE dla endpointa rejestracji

**Dlaczego?**
- Rejestracja to operacja jednorazowa per użytkownik
- Nie ma sensu cache'ować wyników rejestracji
- Cache mógłby być użyty dla sprawdzenia unikalności email, ale indeks jest wystarczający

## 9. Etapy wdrożenia

### Krok 1: Utworzenie struktury folderów

```
src/server/LottoTM.Server.Api/Features/Auth/Register/
├── Endpoint.cs
├── Contracts.cs
├── Handler.cs
└── Validator.cs
```

### Krok 2: Implementacja Contracts.cs

```csharp
using MediatR;

namespace LottoTM.Server.Api.Features.Auth.Register;

public class Contracts
{
    public record Request(
        string Email,
        string Password,
        string ConfirmPassword
    ) : IRequest<Response>;

    public record Response(
        string Token,
        int UserId,
        string Email,
        bool IsAdmin,
        DateTime ExpiresAt
    );
}
```

### Krok 3: Implementacja Validator.cs

```csharp
using FluentValidation;
using LottoTM.Server.Api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LottoTM.Server.Api.Features.Auth.Register;

public class Validator : AbstractValidator<Contracts.Request>
{
    public Validator(AppDbContext dbContext)
    {
        // Email: wymagane
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email jest wymagany");

        // Email: format
        RuleFor(x => x.Email)
            .EmailAddress()
            .WithMessage("Nieprawidłowy format email");

        // Email: unikalność (async validator)
        RuleFor(x => x.Email)
            .MustAsync(async (email, cancellation) =>
            {
                var exists = await dbContext.Users
                    .AnyAsync(u => u.Email == email, cancellation);
                return !exists;
            })
            .WithMessage("Email jest już zajęty");

        // Password: wymagane
        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Hasło jest wymagane");

        // Password: długość
        RuleFor(x => x.Password)
            .MinimumLength(8)
            .WithMessage("Hasło musi mieć min. 8 znaków");

        // Password: wielka litera
        RuleFor(x => x.Password)
            .Matches(@"[A-Z]")
            .WithMessage("Hasło musi zawierać wielką literę");

        // Password: cyfra
        RuleFor(x => x.Password)
            .Matches(@"[0-9]")
            .WithMessage("Hasło musi zawierać cyfrę");

        // Password: znak specjalny
        RuleFor(x => x.Password)
            .Matches(@"[\W_]")
            .WithMessage("Hasło musi zawierać znak specjalny");

        // ConfirmPassword: wymagane
        RuleFor(x => x.ConfirmPassword)
            .NotEmpty()
            .WithMessage("Potwierdzenie hasła jest wymagane");

        // ConfirmPassword: zgodność z Password
        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password)
            .WithMessage("Hasła nie są identyczne");
    }
}
```

### Krok 4: Implementacja Handler.cs

```csharp
using FluentValidation;
using LottoTM.Server.Api.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LottoTM.Server.Api.Features.Auth.Register;

public class RegisterHandler : IRequestHandler<Contracts.Request, Contracts.Response>
{
    private readonly AppDbContext _dbContext;
    private readonly IValidator<Contracts.Request> _validator;
    private readonly IJwtService _jwtService;
    private readonly ILogger<RegisterHandler> _logger;

    public RegisterHandler(
        AppDbContext dbContext,
        IValidator<Contracts.Request> validator,
        IJwtService jwtService,
        ILogger<RegisterHandler> logger)
    {
        _dbContext = dbContext;
        _validator = validator;
        _jwtService = jwtService;
        _logger = logger;
    }

    public async Task<Contracts.Response> Handle(
        Contracts.Request request,
        CancellationToken cancellationToken)
    {
        // 1. Walidacja żądania
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // 2. Haszowanie hasła (BCrypt, 10 rounds)
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 10);

        // 3. Utworzenie nowego użytkownika
        var user = new User
        {
            Email = request.Email,
            PasswordHash = passwordHash,
            IsAdmin = false,
            CreatedAt = DateTime.UtcNow
        };

        // 4. Dodanie do bazy danych
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // 5. Generowanie JWT tokenu (automatyczne logowanie)
        var token = _jwtService.GenerateToken(user.Id, user.Email, user.IsAdmin);
        var expiresAt = DateTime.UtcNow.AddHours(24);

        // 6. Logowanie sukcesu
        _logger.LogDebug(
            "User registered successfully: {Email}",
            request.Email);

        // 7. Zwrócenie odpowiedzi z tokenem JWT
        return new Contracts.Response(
            Token: token,
            UserId: user.Id,
            Email: user.Email,
            IsAdmin: user.IsAdmin,
            ExpiresAt: expiresAt
        );
    }
}
```

### Krok 5: Implementacja Endpoint.cs

```csharp
using MediatR;

namespace LottoTM.Server.Api.Features.Auth.Register;

public static class Endpoint
{
    public static void AddEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("api/auth/register", async (
            Contracts.Request request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(request, cancellationToken);
            return Results.Created($"/api/auth/register", result);
        })
        .WithName("RegisterUser")
        .Produces<Contracts.Response>(StatusCodes.Status201Created)
        .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
        .WithOpenApi();
    }
}
```

### Krok 6: Rejestracja endpointa w Program.cs

Dodaj następującą linię w pliku `Program.cs` po linii 105:

```csharp
LottoTM.Server.Api.Features.Auth.Register.Endpoint.AddEndpoint(app);
```

**Pełny kontekst:**
```csharp
// Istniejące
LottoTM.Server.Api.Features.ApiVersion.Endpoint.AddEndpoint(app);

// NOWE
LottoTM.Server.Api.Features.Auth.Register.Endpoint.AddEndpoint(app);

await app.RunAsync();
```

### Krok 7: Instalacja pakietu BCrypt.Net-Next

Dodaj pakiet NuGet do projektu `LottoTM.Server.Api`:

```bash
dotnet add src/server/LottoTM.Server.Api/LottoTM.Server.Api.csproj package BCrypt.Net-Next
```

### Krok 8: Weryfikacja encji User w AppDbContext

Upewnij się, że encja `User` jest poprawnie skonfigurowana w `AppDbContext`:

```csharp
// AppDbContext.cs
public DbSet<User> Users { get; set; } = null!;

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<User>(entity =>
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => e.Email).IsUnique();
        entity.Property(e => e.Email).HasMaxLength(255).IsRequired();
        entity.Property(e => e.PasswordHash).HasMaxLength(255).IsRequired();
        entity.Property(e => e.IsAdmin).HasDefaultValue(false).IsRequired();
        entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
    });
}
```

### Krok 9: Utworzenie testów jednostkowych

Utworzenie pliku testowego:
```
tests/server/LottoTM.Server.Api.Tests/Features/Auth/Register/EndpointTests.cs
```

**Przykładowy test:**
```csharp
using FluentAssertions;
using LottoTM.Server.Api.Features.Auth.Register;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace LottoTM.Server.Api.Tests.Features.Auth.Register;

public class EndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public EndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithValidData_Returns201Created()
    {
        // Arrange
        var request = new Contracts.Request(
            Email: "test@example.com",
            Password: "SecurePass123!",
            ConfirmPassword: "SecurePass123!"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<Contracts.Response>();
        result.Should().NotBeNull();
        result!.Token.Should().NotBeEmpty();
        result!.UserId.Should().BeGreaterThan(0);
        result!.Email.Should().Be("test@example.com");
        result!.IsAdmin.Should().BeFalse();
        result!.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns400BadRequest()
    {
        // Arrange
        var request1 = new Contracts.Request(
            Email: "duplicate@example.com",
            Password: "SecurePass123!",
            ConfirmPassword: "SecurePass123!"
        );

        var request2 = new Contracts.Request(
            Email: "duplicate@example.com",
            Password: "AnotherPass456!",
            ConfirmPassword: "AnotherPass456!"
        );

        // Act
        await _client.PostAsJsonAsync("/api/auth/register", request1);
        var response = await _client.PostAsJsonAsync("/api/auth/register", request2);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithInvalidEmail_Returns400BadRequest()
    {
        // Arrange
        var request = new Contracts.Request(
            Email: "invalid-email",
            Password: "SecurePass123!",
            ConfirmPassword: "SecurePass123!"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithShortPassword_Returns400BadRequest()
    {
        // Arrange
        var request = new Contracts.Request(
            Email: "test2@example.com",
            Password: "Short1!",
            ConfirmPassword: "Short1!"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithMismatchedPasswords_Returns400BadRequest()
    {
        // Arrange
        var request = new Contracts.Request(
            Email: "test3@example.com",
            Password: "SecurePass123!",
            ConfirmPassword: "DifferentPass456!"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
```

### Krok 10: Uruchomienie testów

```bash
dotnet test tests/server/LottoTM.Server.Api.Tests/LottoTM.Server.Api.Tests.csproj
```

### Krok 11: Testowanie ręczne przez Swagger/Postman

**Swagger URL:** `http://localhost:{port}/swagger/index.html`

**Przykładowe żądanie (Postman):**
```
POST http://localhost:5000/api/auth/register
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "SecurePass123!",
  "confirmPassword": "SecurePass123!"
}
```

**Oczekiwana odpowiedź:**
```
HTTP/1.1 201 Created
Content-Type: application/json

{
  "message": "Rejestracja zakończona sukcesem"
}
```

### Krok 12: Weryfikacja w bazie danych

Sprawdź czy użytkownik został utworzony w tabeli `Users`:

```sql
SELECT Id, Email, PasswordHash, IsAdmin, CreatedAt
FROM Users
WHERE Email = 'user@example.com';
```

**Oczekiwany wynik:**
- `Id`: auto-generated (np. 1)
- `Email`: `user@example.com`
- `PasswordHash`: hash BCrypt (60 znaków, zaczyna się od `$2a$10$`)
- `IsAdmin`: 0 (FALSE)
- `CreatedAt`: data w UTC

---

## 10. Checklist wdrożenia

- [ ] Utworzenie folderów `Features/Auth/Register/`
- [ ] Implementacja `Contracts.cs` (Request/Response records)
- [ ] Implementacja `Validator.cs` (FluentValidation rules)
- [ ] Implementacja `Handler.cs` (RegisterHandler z BCrypt)
- [ ] Implementacja `Endpoint.cs` (MapPost)
- [ ] Instalacja pakietu `BCrypt.Net-Next`
- [ ] Rejestracja endpointa w `Program.cs`
- [ ] Weryfikacja konfiguracji `User` entity w `AppDbContext`
- [ ] Utworzenie testów jednostkowych w `EndpointTests.cs`
- [ ] Uruchomienie testów i weryfikacja pass
- [ ] Testowanie manualne przez Swagger/Postman
- [ ] Weryfikacja w bazie danych (SQL query)
- [ ] Code review (bezpieczeństwo, wydajność)
- [ ] Merge do głównej gałęzi

---

## 11. Uwagi implementacyjne

### 11.1 Dependency Injection

**AppDbContext** i **ILogger** są automatycznie wstrzykiwane przez DI w konstruktorze `RegisterHandler`:

```csharp
public RegisterHandler(
    AppDbContext dbContext,
    IValidator<Contracts.Request> validator,
    ILogger<RegisterHandler> logger)
```

**FluentValidation** jest automatycznie rejestrowany w `Program.cs`:

```csharp
builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
```

### 11.2 Async/Await

Wszystkie operacje I/O są asynchroniczne:
- `ValidateAsync()` - walidacja z async rule (sprawdzenie email w DB)
- `SaveChangesAsync()` - zapis do bazy danych
- `Send()` - MediatR request handling

### 11.3 CancellationToken

Token anulowania jest przekazywany przez cały pipeline:
- Endpoint → MediatR → Handler → DbContext
- Umożliwia anulowanie długotrwałych operacji

### 11.4 Transakcje

Entity Framework automatycznie opakowuje `SaveChangesAsync()` w transakcję:
- Jeśli INSERT się nie powiedzie, baza danych pozostaje bez zmian
- Nie trzeba ręcznie zarządzać transakcjami dla pojedynczego INSERT

### 11.5 Error Handling

`ExceptionHandlingMiddleware` automatycznie obsługuje:
- `ValidationException` → 400 Bad Request z errors dictionary
- `DbUpdateException` → 400/500 w zależności od typu błędu
- `Exception` → 500 Internal Server Error z ProblemDetails

---

## 12. Post-MVP rozszerzenia

### 12.1 Email weryfikacyjny

**Po MVP:** Wysłanie email z linkiem aktywacyjnym po rejestracji

**Implementacja:**
- Kolumna `Users.EmailVerified` (BIT)
- Kolumna `Users.VerificationToken` (NVARCHAR(100))
- Endpoint `/api/auth/verify-email?token={token}`
- Integracja z SendGrid/SMTP

### 12.2 Captcha

**Po MVP:** Google reCAPTCHA v3 dla ochrony przed botami

**Implementacja:**
- Frontend: reCAPTCHA widget
- Backend: weryfikacja tokenu przez Google API

### 12.3 Social login

**Po MVP:** Logowanie przez Google/Facebook

**Implementacja:**
- ASP.NET Core Identity z External Providers
- OAuth 2.0 flow

### 12.4 Rate limiting zaawansowany

**Po MVP:** Rate limiting per email (nie tylko per IP)

**Implementacja:**
- Redis cache dla licznika prób per email
- Blokada konta po 5 nieudanych próbach

---

## Koniec planu implementacji
