# Plan Wdrożenia Punktu Końcowego API: /api/auth/login

## 1. Przegląd punktu końcowego

**Cel:** Uwierzytelnienie użytkownika w systemie LottoTM poprzez weryfikację adresu email i hasła, oraz wygenerowanie tokenu JWT umożliwiającego dostęp do chronionych zasobów.

**Funkcjonalność:**
- Przyjęcie danych logowania (email, hasło)
- Weryfikacja tożsamości użytkownika względem bazy danych
- Walidacja hasła za pomocą algorytmu bcrypt
- Generowanie tokenu JWT z claims użytkownika
- Zwrócenie tokenu oraz danych użytkownika (userId, email, isAdmin, expiresAt)

**Poziom dostępu:** Publiczny (bez wymaganej autoryzacji)

---

## 2. Szczegóły żądania

### Metoda HTTP
**POST**

### Struktura URL
```
POST /api/auth/login
```

### Parametry

**Parametry zapytania (Query Parameters):** Brak

**Nagłówki (Headers):**
- `Content-Type: application/json` - wymagany

**Ciało żądania (Request Body):**
```json
{
  "email": "user@example.com",
  "password": "SecurePass123!"
}
```

**Wymagane pola:**
- `email` (string): Adres email użytkownika
  - Format: poprawny format email (walidacja regex)
  - Przykład: `"user@example.com"`
- `password` (string): Hasło użytkownika
  - Wymagane (bez dodatkowych warunków przy logowaniu)
  - Przykład: `"SecurePass123!"`

**Opcjonalne pola:** Brak

---

## 3. Wykorzystywane typy

### DTOs (Data Transfer Objects)

#### LoginRequest (Contracts.cs)
```csharp
public record LoginRequest : IRequest<LoginResponse>
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}
```

#### LoginResponse (Contracts.cs)
```csharp
public record LoginResponse(
    string Token,
    int UserId,
    string Email,
    bool IsAdmin,
    DateTime ExpiresAt
);
```

### Command Modele
Wykorzystujemy MediatR pattern - `LoginRequest` implementuje `IRequest<LoginResponse>`

### Encje bazodanowe
- **User** (z db-plan.md):
  - `Id` (int, PK)
  - `Email` (string, UNIQUE)
  - `PasswordHash` (string)
  - `IsAdmin` (bool)
  - `CreatedAt` (DateTime)

---

## 4. Szczegóły odpowiedzi

### Odpowiedź pomyślna

**Kod statusu:** `200 OK`

**Struktura odpowiedzi:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VySWQiOiIxMjMiLCJlbWFpbCI6InVzZXJAZXhhbXBsZS5jb20iLCJpc0FkbWluIjpmYWxzZSwiZXhwIjoxNzMwNjM4ODAwLCJpYXQiOjE3MzA1NTI0MDB9.signature",
  "userId": 123,
  "email": "user@example.com",
  "isAdmin": false,
  "expiresAt": "2025-11-04T10:30:00Z"
}
```

**Pola odpowiedzi:**
- `token` (string): Token JWT do użycia w nagłówku Authorization
- `userId` (int): Unikalny identyfikator użytkownika
- `email` (string): Adres email użytkownika
- `isAdmin` (bool): Flaga określająca uprawnienia administratora
- `expiresAt` (DateTime): Data i czas wygaśnięcia tokenu (ISO 8601, UTC)

### Odpowiedzi błędów

#### 400 Bad Request - Nieprawidłowe dane wejściowe
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Email": ["Nieprawidłowy format email"],
    "Password": ["Hasło jest wymagane"]
  }
}
```

**Przykładowe błędy walidacji:**
- Email:
  - `"Email jest wymagany"` - brak pola email
  - `"Nieprawidłowy format email"` - niepoprawny format email
- Password:
  - `"Hasło jest wymagane"` - brak pola password

#### 401 Unauthorized - Nieprawidłowe credentials
```json
{
  "error": "Nieprawidłowy email lub hasło"
}
```

**Scenariusze:**
- Email nie istnieje w bazie danych
- Hasło nie pasuje do zahashowanego hasła w bazie

**WAŻNE:** Komunikat błędu nie powinien ujawniać czy email istnieje w systemie (security by obscurity - ochrona przed enumeracją użytkowników)

#### 429 Too Many Requests - Rate limiting
```json
{
  "error": "Za dużo prób logowania. Spróbuj ponownie później."
}
```

**Scenariusz:** Przekroczono limit 5 prób logowania/minutę/IP

#### 500 Internal Server Error - Błąd serwera
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.6.1",
  "title": "An error occurred while processing your request.",
  "status": 500
}
```

**Scenariusze:**
- Błąd połączenia z bazą danych
- Błąd generowania tokenu JWT
- Nieobsłużony wyjątek w handlerze

---

## 5. Przepływ danych

### Diagram przepływu
```
[Client]
   |
   | POST /api/auth/login
   | { email, password }
   ↓
[Endpoint.cs] (Minimal API)
   |
   | LoginRequest
   ↓
[MediatR]
   |
   ↓
[Validator.cs] (FluentValidation)
   |
   | Walidacja formatu email
   | Sprawdzenie wymaganych pól
   ↓
[Handler.cs] (LoginHandler)
   |
   | 1. Wyszukanie użytkownika po email
   |    ↓
   |    [AppDbContext] → SELECT * FROM Users WHERE Email = @email
   |    ↓
   | 2. Sprawdzenie czy użytkownik istnieje
   |    ↓
   |    Jeśli NIE → 401 Unauthorized
   |    ↓
   | 3. Weryfikacja hasła (BCrypt.Verify)
   |    ↓
   |    [PasswordHashingService]
   |    ↓
   |    Jeśli nieprawidłowe → 401 Unauthorized
   |    ↓
   | 4. Generowanie tokenu JWT
   |    ↓
   |    [JwtService]
   |    - Claims: UserId, Email, IsAdmin
   |    - Algorytm: HMAC SHA-256
   |    - Ważność: 24h
   |    ↓
   | 5. Utworzenie odpowiedzi
   |    ↓
   ↓
[LoginResponse]
   |
   | 200 OK
   | { token, userId, email, isAdmin, expiresAt }
   ↓
[Client]
   |
   | Zapisanie tokenu w localStorage
   | Dodanie tokenu do nagłówka Authorization: Bearer <token>
```

### Sekwencja kroków

1. **Przyjęcie żądania:**
   - Endpoint odbiera żądanie POST z ciałem JSON
   - Deserializacja do obiektu `LoginRequest`

2. **Walidacja danych wejściowych (FluentValidation):**
   - Sprawdzenie formatu email (regex)
   - Sprawdzenie czy pola nie są puste
   - Jeśli błędy → zwrot 400 Bad Request z details

3. **Wyszukanie użytkownika:**
   - Query do bazy: `SELECT * FROM Users WHERE Email = @email`
   - Wykorzystanie indeksu `IX_Users_Email` (O(log n))
   - Jeśli użytkownik nie istnieje → przejście do kroku 7 (401)

4. **Weryfikacja hasła:**
   - `BCrypt.Net.BCrypt.Verify(password, user.PasswordHash)`
   - Algorytm bcrypt z work factor 10+
   - Jeśli nieprawidłowe → przejście do kroku 7 (401)

5. **Generowanie tokenu JWT:**
   - Utworzenie claims:
     - `sub` lub `userId`: user.Id
     - `email`: user.Email
     - `isAdmin`: user.IsAdmin
     - `iat` (issued at): DateTime.UtcNow (Unix timestamp)
     - `exp` (expiration): DateTime.UtcNow.AddHours(24) (Unix timestamp)
   - Podpisanie tokenu HMAC SHA-256 z kluczem z konfiguracji
   - Serializacja do string (JWT format)

6. **Utworzenie odpowiedzi:**
   - Utworzenie obiektu `LoginResponse`
   - Zwrot 200 OK z JSON payload

7. **Obsługa błędu credentials:**
   - Stały czas odpowiedzi (ochrona przed timing attacks)
   - Zwrot 401 Unauthorized z komunikatem "Nieprawidłowy email lub hasło"
   - Logowanie próby nieudanego logowania (Serilog) z IP adresem

---

## 6. Względy bezpieczeństwa

### 6.1 Uwierzytelnianie i autoryzacja

**Brak wymaganej autoryzacji:**
- Endpoint publiczny - nie wymaga tokenu JWT
- Każdy może próbować się zalogować

**Generowanie tokenu JWT:**
- Algorytm: HMAC SHA-256 (HS256)
- Secret key: Przechowywany w `appsettings.json` (min. 32 znaki)
  - **Produkcja:** Przechowywanie w Azure Key Vault / zmiennych środowiskowych
- Ważność tokenu: 24 godziny (1440 minut)
- Claims w tokenie:
  - `sub` lub `userId`: ID użytkownika (INT)
  - `email`: Email użytkownika
  - `isAdmin`: Flaga uprawnień (BOOL)
  - `exp`: Timestamp wygaśnięcia
  - `iat`: Timestamp wystawienia

**Konfiguracja JWT (appsettings.json):**
```json
{
  "Jwt": {
    "Key": "super-secret-key-min-32-characters-long",
    "Issuer": "LottoTM",
    "Audience": "LottoTM-Users",
    "ExpiryInMinutes": 1440
  }
}
```

### 6.2 Walidacja danych wejściowych

**FluentValidation rules:**
```csharp
RuleFor(x => x.Email)
    .NotEmpty().WithMessage("Email jest wymagany")
    .EmailAddress().WithMessage("Nieprawidłowy format email");

RuleFor(x => x.Password)
    .NotEmpty().WithMessage("Hasło jest wymagane");
```

**Backend validation:**
- Sprawdzenie istnienia użytkownika
- Weryfikacja hasła z bcrypt

### 6.3 Hashowanie haseł

**Algorytm:** BCrypt (biblioteka `BCrypt.Net-Next`)

**Weryfikacja:**
```csharp
var isValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
```

**Work factor:** Min. 10 rounds (rekomendacja: 12)

**Przechowywanie:**
- Hash w kolumnie `Users.PasswordHash` (NVARCHAR(256))
- Hash bcrypt: 60 znaków (np. `$2a$10$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy`)

### 6.4 Ochrona przed atakami

#### SQL Injection
- **Zabezpieczenie:** Entity Framework Core automatycznie parametryzuje zapytania
- **NIE używać:** `FromSqlRaw()` z konkatenacją stringów

#### Brute Force Attacks
- **Rate Limiting:** AspNetCoreRateLimit middleware
- **Limit:** 5 prób logowania/minutę/IP (NFR-011)
- **Konfiguracja:**
```json
{
  "IpRateLimiting": {
    "EnableEndpointRateLimiting": true,
    "StackBlockedRequests": false,
    "RealIpHeader": "X-Real-IP",
    "HttpStatusCode": 429,
    "EndpointWhitelist": [],
    "GeneralRules": [
      {
        "Endpoint": "POST:/api/auth/login",
        "Period": "1m",
        "Limit": 5
      }
    ]
  }
}
```

#### Timing Attacks
- **Problem:** Różne czasy odpowiedzi ujawniają czy email istnieje
- **Rozwiązanie:** Stały czas odpowiedzi niezależnie od scenariusza
```csharp
// Zawsze weryfikuj hash, nawet jeśli użytkownik nie istnieje
var dummyHash = "$2a$10$dummy...";
BCrypt.Net.BCrypt.Verify(password, user?.PasswordHash ?? dummyHash);
```

#### User Enumeration
- **Problem:** Różne komunikaty błędów ujawniają czy email istnieje
- **Rozwiązanie:** Generyczny komunikat "Nieprawidłowy email lub hasło"
- **NIE ujawniaj:**
  - "Email nie istnieje"
  - "Hasło nieprawidłowe"

### 6.5 HTTPS i bezpieczne nagłówki

**HTTPS (NFR-007):**
- Wymagane na produkcji
- Middleware: `app.UseHttpsRedirection()`
- Hosting: Azure App Service / Webio.pl zapewniają SSL/TLS

**Secure Headers (middleware):**
```csharp
app.Use(async (context, next) => {
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Add("Content-Security-Policy", "default-src 'self'");
    await next();
});
```

### 6.6 CORS

**Konfiguracja dozwolonych origins:**
```csharp
services.AddCors(options => {
    options.AddPolicy("AllowFrontend", builder => {
        builder.WithOrigins("https://lottotm.netlify.app")
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials();
    });
});
```

**MVP:** Dla developmentu można użyć `AllowAnyOrigin()`, ale **NIE na produkcji**

### 6.7 Logowanie zdarzeń bezpieczeństwa

**Serilog:**
- Logowanie nieudanych prób logowania (z IP)
- Logowanie pomyślnych logowań
- **NIE logować:** Hasła, tokenów JWT

```csharp
_logger.LogWarning("Nieudana próba logowania dla email: {Email} z IP: {IpAddress}",
    email, httpContext.Connection.RemoteIpAddress);
```

---

## 7. Obsługa błędów

### 7.1 Scenariusze błędów

| Kod | Scenariusz | Komunikat | Logowanie |
|-----|------------|-----------|-----------|
| **400** | Brak pola email | `"Email jest wymagany"` | Info |
| **400** | Nieprawidłowy format email | `"Nieprawidłowy format email"` | Info |
| **400** | Brak pola password | `"Hasło jest wymagane"` | Info |
| **401** | Email nie istnieje | `"Nieprawidłowy email lub hasło"` | Warning + IP |
| **401** | Hasło nieprawidłowe | `"Nieprawidłowy email lub hasło"` | Warning + IP |
| **429** | Za dużo prób logowania | `"Za dużo prób logowania. Spróbuj ponownie później."` | Warning + IP |
| **500** | Błąd bazy danych | `"An error occurred while processing your request."` | Error + stack trace |
| **500** | Błąd generowania JWT | `"An error occurred while processing your request."` | Error + stack trace |

### 7.2 Implementacja obsługi błędów

**FluentValidation errors (400):**
- Automatyczna walidacja przez MediatR pipeline behavior
- Zwrot `ValidationException` → globalne middleware konwertuje na 400

**Nieprawidłowe credentials (401):**
```csharp
if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
{
    _logger.LogWarning("Nieudana próba logowania dla email: {Email}", request.Email);
    return Results.Unauthorized(new { error = "Nieprawidłowy email lub hasło" });
}
```

**Rate limiting (429):**
- Obsługiwane przez AspNetCoreRateLimit middleware
- Automatyczny zwrot 429 po przekroczeniu limitu

**Błędy serwera (500):**
- Obsługiwane przez `ExceptionHandlingMiddleware` (już skonfigurowany)
- Logowanie do Serilog
- Zwrot standardowego `ProblemDetails` JSON

### 7.3 Globalna obsługa wyjątków

Wykorzystanie istniejącego `ExceptionHandlingMiddleware`:
- Logowanie błędów z Serilog
- Zwrot `ProblemDetails` zgodnie z RFC 7807
- Ukrywanie szczegółów implementacji na produkcji

---

## 8. Względy wydajnościowe

### 8.1 Optymalizacje bazy danych

**Indeks na email:**
- `IX_Users_Email` (UNIQUE INDEX)
- Wyszukanie użytkownika: O(log n) zamiast O(n)
- Query: `SELECT * FROM Users WHERE Email = @email`

**Brak JOIN:**
- Endpoint pobiera tylko dane z tabeli `Users`
- Brak potrzeby eager loading relacji

### 8.2 Wydajność algorytmu bcrypt

**Work factor 10:**
- Weryfikacja hasła: ~50-100ms
- Trade-off: bezpieczeństwo vs. wydajność
- **Rekomendacja:** Work factor 12 (jeśli serwer wydajny)

**Optymalizacja:**
- Użycie async/await dla operacji I/O (baza danych)
- Bcrypt.Verify jest synchroniczny - rozważyć `Task.Run()` jeśli potrzebne

### 8.3 Cache

**MVP:** Brak cache dla endpointu logowania

**Post-MVP (jeśli potrzebne):**
- Redis cache dla sprawdzania rate limiting
- Distributed cache dla sesji użytkownika

### 8.4 Monitorowanie wydajności

**Metryki do śledzenia:**
- Średni czas odpowiedzi (target: <500ms - NFR-002)
- Liczba nieudanych prób logowania
- Liczba zablokowanych IP (rate limiting)

**Narzędzia:**
- Application Insights (Azure)
- Serilog z Application Performance Monitoring (APM)

---

## 9. Kroki implementacji

### Krok 1: Utworzenie struktury katalogów
```
src/server/LottoTM.Server.Api/Features/Auth/Login/
├── Endpoint.cs
├── Contracts.cs
├── Handler.cs
└── Validator.cs
```

### Krok 2: Definicja kontraktów (Contracts.cs)
```csharp
using MediatR;

namespace LottoTM.Server.Api.Features.Auth.Login;

public class Contracts
{
    public record LoginRequest : IRequest<LoginResponse>
    {
        public string Email { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
    }

    public record LoginResponse(
        string Token,
        int UserId,
        string Email,
        bool IsAdmin,
        DateTime ExpiresAt
    );
}
```

### Krok 3: Walidacja danych wejściowych (Validator.cs)
```csharp
using FluentValidation;

namespace LottoTM.Server.Api.Features.Auth.Login;

public class Validator : AbstractValidator<Contracts.LoginRequest>
{
    public Validator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email jest wymagany")
            .EmailAddress().WithMessage("Nieprawidłowy format email");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Hasło jest wymagane");
    }
}
```

### Krok 4: Utworzenie serwisu JWT (Services/JwtService.cs)
```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace LottoTM.Server.Api.Services;

public interface IJwtService
{
    string GenerateToken(int userId, string email, bool isAdmin, out DateTime expiresAt);
}

public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;

    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(int userId, string email, bool isAdmin, out DateTime expiresAt)
    {
        var jwtKey = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT Key not configured");
        var jwtIssuer = _configuration["Jwt:Issuer"];
        var jwtAudience = _configuration["Jwt:Audience"];
        var expiryMinutes = _configuration.GetValue<int>("Jwt:ExpiryInMinutes", 1440);

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var issuedAt = DateTime.UtcNow;
        expiresAt = issuedAt.AddMinutes(expiryMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim("isAdmin", isAdmin.ToString().ToLower()),
            new Claim(JwtRegisteredClaimNames.Iat, new DateTimeOffset(issuedAt).ToUnixTimeSeconds().ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

### Krok 5: Rejestracja serwisu JWT (Program.cs)
```csharp
// Dodaj w Program.cs przed builder.Build()
builder.Services.AddScoped<IJwtService, JwtService>();
```

### Krok 6: Implementacja handlera (Handler.cs)
```csharp
using FluentValidation;
using LottoTM.Server.Api.Data;
using LottoTM.Server.Api.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LottoTM.Server.Api.Features.Auth.Login;

public class LoginHandler : IRequestHandler<Contracts.LoginRequest, Contracts.LoginResponse>
{
    private readonly AppDbContext _dbContext;
    private readonly IJwtService _jwtService;
    private readonly IValidator<Contracts.LoginRequest> _validator;
    private readonly ILogger<LoginHandler> _logger;

    public LoginHandler(
        AppDbContext dbContext,
        IJwtService jwtService,
        IValidator<Contracts.LoginRequest> validator,
        ILogger<LoginHandler> logger)
    {
        _dbContext = dbContext;
        _jwtService = jwtService;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Contracts.LoginResponse> Handle(
        Contracts.LoginRequest request,
        CancellationToken cancellationToken)
    {
        // 1. Walidacja danych wejściowych
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // 2. Wyszukanie użytkownika po email (wykorzystanie indeksu IX_Users_Email)
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        // 3. Weryfikacja credentials (stały czas odpowiedzi)
        var dummyHash = "$2a$10$dummyhashtopreventtimingattacksxxxxxxxxxxxxxxxxxxxxxxxxxx";
        var isValidPassword = BCrypt.Net.BCrypt.Verify(
            request.Password,
            user?.PasswordHash ?? dummyHash
        );

        if (user == null || !isValidPassword)
        {
            _logger.LogWarning(
                "Nieudana próba logowania dla email: {Email}",
                request.Email
            );
            throw new UnauthorizedAccessException("Nieprawidłowy email lub hasło");
        }

        // 4. Generowanie tokenu JWT
        var token = _jwtService.GenerateToken(
            user.Id,
            user.Email,
            user.IsAdmin,
            out var expiresAt
        );

        _logger.LogDebug(
            "Użytkownik {UserId} ({Email}) zalogowany pomyślnie",
            user.Id,
            user.Email
        );

        // 5. Utworzenie odpowiedzi
        return new Contracts.LoginResponse(
            Token: token,
            UserId: user.Id,
            Email: user.Email,
            IsAdmin: user.IsAdmin,
            ExpiresAt: expiresAt
        );
    }
}
```

### Krok 7: Definicja endpointu (Endpoint.cs)
```csharp
using MediatR;

namespace LottoTM.Server.Api.Features.Auth.Login;

public static class Endpoint
{
    public static void AddEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("api/auth/login", async (
            Contracts.LoginRequest request,
            IMediator mediator) =>
        {
            try
            {
                var result = await mediator.Send(request);
                return Results.Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Results.Json(
                    new { error = ex.Message },
                    statusCode: StatusCodes.Status401Unauthorized
                );
            }
        })
        .WithName("Login")
        .Produces<Contracts.LoginResponse>(StatusCodes.Status200OK)
        .Produces<object>(StatusCodes.Status400BadRequest)
        .Produces<object>(StatusCodes.Status401Unauthorized)
        .AllowAnonymous()
        .WithOpenApi();
    }
}
```

### Krok 8: Rejestracja endpointu (Program.cs)
```csharp
// Dodaj w Program.cs po app.MapControllers() lub innych endpoints
LottoTM.Server.Api.Features.Auth.Login.Endpoint.AddEndpoint(app);
```

### Krok 9: Konfiguracja JWT w appsettings.json
```json
{
  "Jwt": {
    "Key": "your-super-secret-key-minimum-32-characters-long-for-security",
    "Issuer": "LottoTM",
    "Audience": "LottoTM-Users",
    "ExpiryInMinutes": 1440
  }
}
```

**WAŻNE:** Dla produkcji przenieś `Jwt:Key` do Azure Key Vault lub zmiennych środowiskowych

### Krok 10: Konfiguracja JWT Authentication (Program.cs)
```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

// Dodaj przed builder.Build()
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
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)
            )
        };
    });

builder.Services.AddAuthorization();

// Dodaj po app.UseRouting() lub app.UseHttpsRedirection()
app.UseAuthentication();
app.UseAuthorization();
```

### Krok 11: Dodanie pakietu BCrypt.Net-Next (jeśli nie istnieje)
```bash
dotnet add package BCrypt.Net-Next
```

### Krok 12: Dodanie pakietu JWT (jeśli nie istnieje)
```bash
dotnet add package System.IdentityModel.Tokens.Jwt
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
```

### Krok 13: Konfiguracja Rate Limiting (opcjonalne dla MVP)
```bash
dotnet add package AspNetCoreRateLimit
```

**Program.cs:**
```csharp
// Przed builder.Build()
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// Po app.UseRouting()
app.UseIpRateLimiting();
```

**appsettings.json:**
```json
{
  "IpRateLimiting": {
    "EnableEndpointRateLimiting": true,
    "StackBlockedRequests": false,
    "RealIpHeader": "X-Real-IP",
    "HttpStatusCode": 429,
    "GeneralRules": [
      {
        "Endpoint": "POST:/api/auth/login",
        "Period": "1m",
        "Limit": 5
      }
    ]
  }
}
```

### Krok 14: Testy jednostkowe (Tests/Features/Auth/Login/HandlerTests.cs)
```csharp
using FluentAssertions;
using LottoTM.Server.Api.Features.Auth.Login;
using Xunit;

namespace LottoTM.Server.Api.Tests.Features.Auth.Login;

public class HandlerTests
{
    [Fact]
    public async Task Handle_ValidCredentials_ReturnsTokenAndUserData()
    {
        // Arrange
        // Setup in-memory database with test user
        // Setup mock IJwtService
        // Setup validator

        // Act
        // var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        // result.Token.Should().NotBeNullOrEmpty();
        // result.UserId.Should().Be(expectedUserId);
        // result.Email.Should().Be(expectedEmail);
    }

    [Fact]
    public async Task Handle_InvalidEmail_ThrowsUnauthorizedException()
    {
        // Test nieprawidłowego email
    }

    [Fact]
    public async Task Handle_InvalidPassword_ThrowsUnauthorizedException()
    {
        // Test nieprawidłowego hasła
    }

    [Fact]
    public async Task Handle_EmptyEmail_ThrowsValidationException()
    {
        // Test walidacji pustego email
    }
}
```

### Krok 15: Testy integracyjne (Tests/Features/Auth/Login/EndpointTests.cs)
```csharp
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace LottoTM.Server.Api.Tests.Features.Auth.Login;

public class EndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public EndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithToken()
    {
        // Arrange
        var request = new
        {
            email = "test@example.com",
            password = "TestPassword123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(result);
        Assert.NotEmpty(result.Token);
    }

    [Fact]
    public async Task Login_InvalidCredentials_Returns401()
    {
        // Test dla nieprawidłowych credentials
    }

    [Fact]
    public async Task Login_InvalidEmail_Returns400()
    {
        // Test dla nieprawidłowego formatu email
    }
}
```

---

## 10. Checklist wdrożenia

### Przygotowanie środowiska
- [ ] Weryfikacja .NET 8 SDK
- [ ] Instalacja pakietów NuGet:
  - [ ] BCrypt.Net-Next
  - [ ] System.IdentityModel.Tokens.Jwt
  - [ ] Microsoft.AspNetCore.Authentication.JwtBearer
  - [ ] AspNetCoreRateLimit (opcjonalne)
- [ ] Konfiguracja połączenia z bazą danych

### Implementacja
- [ ] Utworzenie struktury katalogów `Features/Auth/Login/`
- [ ] Implementacja `Contracts.cs` (LoginRequest, LoginResponse)
- [ ] Implementacja `Validator.cs` (FluentValidation rules)
- [ ] Implementacja `Services/JwtService.cs`
- [ ] Rejestracja IJwtService w DI container
- [ ] Implementacja `Handler.cs` (LoginHandler)
- [ ] Implementacja `Endpoint.cs` (Minimal API)
- [ ] Rejestracja endpointu w `Program.cs`

### Konfiguracja
- [ ] Dodanie sekcji `Jwt` w `appsettings.json`
- [ ] Konfiguracja JWT Authentication w `Program.cs`
- [ ] Konfiguracja Rate Limiting (opcjonalne)
- [ ] Konfiguracja CORS
- [ ] Weryfikacja middleware order w `Program.cs`:
  1. `UseHttpsRedirection()`
  2. `UseRouting()`
  3. `UseCors()`
  4. `UseAuthentication()`
  5. `UseAuthorization()`
  6. `UseIpRateLimiting()` (opcjonalne)
  7. `MapEndpoints()`

### Bezpieczeństwo
- [ ] Weryfikacja długości klucza JWT (min. 32 znaki)
- [ ] Implementacja stałego czasu odpowiedzi (timing attack protection)
- [ ] Generyczny komunikat błędu (user enumeration protection)
- [ ] Konfiguracja secure headers
- [ ] Wymuszenie HTTPS na produkcji
- [ ] Rate limiting dla endpointu login

### Testy
- [ ] Testy jednostkowe handlera
- [ ] Testy walidatora (FluentValidation)
- [ ] Testy integracyjne endpointu
- [ ] Testy scenariuszy błędów (401, 400)
- [ ] Testy rate limiting (opcjonalne)
- [ ] Manual testing z Postman/Thunder Client

### Dokumentacja
- [ ] Swagger/OpenAPI annotations
- [ ] Komentarze XML dla public API
- [ ] Aktualizacja README.md
- [ ] Dokumentacja konfiguracji JWT dla zespołu

### Deployment
- [ ] Weryfikacja konfiguracji JWT na środowisku produkcyjnym
- [ ] Przeniesienie Jwt:Key do Azure Key Vault / zmiennych środowiskowych
- [ ] Konfiguracja HTTPS
- [ ] Weryfikacja CORS origins
- [ ] Monitoring logów nieudanych prób logowania
- [ ] Smoke tests na produkcji

---

## 11. Potencjalne wyzwania i rozwiązania

### Wyzwanie 1: Timing Attacks
**Problem:** Różne czasy odpowiedzi ujawniają czy email istnieje

**Rozwiązanie:**
```csharp
// Zawsze weryfikuj hash, nawet dla nieistniejącego użytkownika
var dummyHash = "$2a$10$dummy...";
BCrypt.Net.BCrypt.Verify(password, user?.PasswordHash ?? dummyHash);
```

### Wyzwanie 2: User Enumeration
**Problem:** Różne komunikaty błędów ujawniają istnienie użytkownika

**Rozwiązanie:**
- Generyczny komunikat: "Nieprawidłowy email lub hasło"
- Nie różnicuj komunikatów dla "email nie istnieje" vs "hasło nieprawidłowe"

### Wyzwanie 3: Brute Force
**Problem:** Atakujący może próbować zgadywać hasła

**Rozwiązanie:**
- Rate limiting: 5 prób/minutę/IP
- Opcjonalnie: Account lockout po N nieudanych próbach
- Opcjonalnie: CAPTCHA po 3 nieudanych próbach

### Wyzwanie 4: JWT Secret w kodzie
**Problem:** Hardcoded secret key w appsettings.json

**Rozwiązanie:**
- Development: appsettings.Development.json (lokalnie)
- Production: Azure Key Vault lub zmienne środowiskowe
```csharp
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? Environment.GetEnvironmentVariable("JWT_KEY")
    ?? throw new InvalidOperationException("JWT Key not configured");
```

### Wyzwanie 5: Token Refresh
**Problem:** Token wygasa po 24h, użytkownik musi się ponownie logować

**Rozwiązanie MVP:** Brak refresh tokens w MVP
**Post-MVP:** Implementacja refresh tokens z tabelą `RefreshTokens`

### Wyzwanie 6: Wydajność bcrypt
**Problem:** Weryfikacja hasła może być wolna (work factor 12 ≈ 200ms)

**Rozwiązanie:**
- Użycie async/await
- Opcjonalnie: `Task.Run(() => BCrypt.Verify())` dla offloading
- Monitorowanie czasu odpowiedzi

---

## 12. Podsumowanie

### Kluczowe decyzje projektowe
1. **JWT zamiast session-based auth** - stateless, skalowalny
2. **BCrypt do hashowania** - industry standard, bezpieczny
3. **Stały czas odpowiedzi** - ochrona przed timing attacks
4. **Generyczny komunikat błędu** - ochrona przed user enumeration
5. **Rate limiting** - ochrona przed brute force
6. **FluentValidation** - spójna walidacja w całym systemie
7. **MediatR pattern** - separation of concerns

### Zależności między komponentami
- `Endpoint.cs` → `MediatR` → `Handler.cs`
- `Handler.cs` → `Validator.cs` (FluentValidation)
- `Handler.cs` → `AppDbContext` (EF Core)
- `Handler.cs` → `JwtService` (generowanie tokenu)
- `Handler.cs` → `BCrypt.Net` (weryfikacja hasła)

### Metryki sukcesu
- Czas odpowiedzi < 500ms (95 percentyl) - NFR-002
- Brak błędów 500 w Serilog
- Rate limiting działa poprawnie (429 po 5 próbach/min)
- Wszystkie testy przechodzą (unit + integration)

### Następne kroki po wdrożeniu
1. Implementacja `/api/auth/register`
2. Implementacja `/api/auth/logout` (opcjonalne - blacklist tokenu)
3. Implementacja refresh tokens (post-MVP)
4. Dodanie Multi-Factor Authentication (post-MVP)
5. Analiza logów nieudanych prób logowania
6. Optymalizacja wydajności (jeśli > 500ms)

---

**Koniec planu wdrożenia dla /api/auth/login**
