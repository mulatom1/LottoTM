# Plan Implementacji Endpointa API: POST /api/auth/logout

<analysis>

## Analiza wstępna

### 1. Podsumowanie specyfikacji API

Endpoint **POST /api/auth/logout** służy do wylogowania użytkownika z systemu. Zgodnie ze specyfikacją z `api-plan.md`:

- **Opis:** Wylogowanie użytkownika (opcjonalnie: blacklisting tokenu)
- **Autoryzacja:** Wymagany JWT w header `Authorization: Bearer <token>`
- **Metoda HTTP:** POST
- **URL:** `/api/auth/logout`
- **Payload żądania:** Brak
- **Payload odpowiedzi (sukces):** `{ "message": "Wylogowano pomyślnie" }`
- **Kod sukcesu:** 200 OK
- **Kod błędu:** 401 Unauthorized (brak tokenu lub token nieprawidłowy)

### 2. Parametry wymagane i opcjonalne

**Wymagane:**
- Header `Authorization: Bearer <token>` - token JWT użytkownika

**Opcjonalne:**
- Brak

### 3. Typy DTO i Command Modele

#### Contracts.cs
```csharp
public record Request : IRequest<Response>;
public record Response(string Message);
```

**Uwaga:** Request jest pusty, ponieważ endpoint nie przyjmuje payload'u. Autoryzacja odbywa się przez JWT token w header.

### 4. Wyodrębnienie logiki do serwisu

W MVP nie implementujemy blacklistingu tokenów JWT. Wylogowanie odbywa się **po stronie klienta** poprzez usunięcie tokenu z `localStorage` lub `sessionStorage`.

Backend zwraca tylko potwierdzenie, że endpoint został wywołany prawidłowo. Nie ma potrzeby tworzenia dedykowanego serwisu.

**Post-MVP:** Jeśli w przyszłości potrzebny będzie blacklisting tokenów:
- Utworzenie tabeli `TokenBlacklist` w bazie danych
- Serwis `TokenBlacklistService` do zarządzania unieważnionymi tokenami
- Middleware sprawdzający, czy token nie znajduje się na czarnej liście

### 5. Walidacja danych wejściowych

**Walidacja FluentValidation:**
- Brak parametrów do walidacji w payload (Request jest pusty)
- Walidacja tokenu JWT odbywa się automatycznie przez middleware `UseAuthentication()`

**Walidacja w Handlerze:**
- Sprawdzenie, czy użytkownik jest uwierzytelniony (`HttpContext.User.Identity.IsAuthenticated`)
- Brak dodatkowych warunków biznesowych

### 6. Rejestrowanie błędów

Zgodnie z architekturą, wszystkie błędy są przechwytywane przez `ExceptionHandlingMiddleware`:
- Logowanie błędów przez Serilog
- Zwracanie standardowych `ProblemDetails` JSON

**Potencjalne błędy:**
- 401 Unauthorized: token JWT nieprawidłowy, wygasły lub brak tokenu
- 500 Internal Server Error: nieoczekiwany błąd serwera (rzadki przypadek dla tego prostego endpointa)

### 7. Zagrożenia bezpieczeństwa

**Identyfikowane zagrożenia:**

1. **Brak blacklistingu tokenów (MVP):**
   - Token JWT pozostaje ważny do momentu wygaśnięcia (24h)
   - Jeśli token zostanie przechwycony, może być nadużyty mimo wylogowania użytkownika
   - **Mitygacja MVP:** Krótki czas życia tokenu (24h), HTTPS, komunikacja przez SSL/TLS

2. **Potencjalne CSRF (Cross-Site Request Forgery):**
   - Endpoint POST może być potencjalnie podatny na CSRF
   - **Mitygacja:** JWT w header (nie w cookie), CORS policies, brak state po stronie serwera

3. **Token Hijacking:**
   - Token może być przechwycony przez XSS lub MITM
   - **Mitygacja:** HTTPS, Secure Headers (CSP, X-Frame-Options), sanityzacja danych wejściowych w UI

4. **Replay Attacks:**
   - Ten sam token może być użyty wielokrotnie do wylogowania
   - **Mitygacja:** Endpoint jest idempotentny (wielokrotne wywołanie nie powoduje szkody)

**Rekomendacje post-MVP:**
- Implementacja token blacklistingu (Redis lub tabela w SQL)
- Implementacja refresh tokenów (krótszy czas życia access token)
- Rate limiting dla endpointów autentykacji

### 8. Potencjalne scenariusze błędów i kody stanu

| Scenariusz | Kod stanu | Komunikat | Działanie |
|------------|-----------|-----------|-----------|
| Brak tokenu w header | 401 Unauthorized | "Unauthorized" (automatyczny) | Middleware `UseAuthentication()` |
| Token wygasły | 401 Unauthorized | "Unauthorized" (automatyczny) | Middleware `UseAuthentication()` |
| Token nieprawidłowy (zły podpis) | 401 Unauthorized | "Unauthorized" (automatyczny) | Middleware `UseAuthentication()` |
| Pomyślne wylogowanie | 200 OK | "Wylogowano pomyślnie" | Handler zwraca Response |
| Nieoczekiwany błąd serwera | 500 Internal Server Error | ProblemDetails JSON | ExceptionHandlingMiddleware |

</analysis>

---

## 1. Przegląd punktu końcowego

**Endpoint:** `POST /api/auth/logout`

**Cel:** Wylogowanie użytkownika z systemu poprzez zwrócenie potwierdzenia, które sygnalizuje frontendowi konieczność usunięcia tokenu JWT z pamięci przeglądarki.

**Funkcjonalność:**
- Sprawdzenie, czy użytkownik jest uwierzytelniony (JWT token prawidłowy)
- Zwrócenie komunikatu o pomyślnym wylogowaniu
- **Uwaga:** W MVP wylogowanie odbywa się po stronie klienta (usunięcie tokenu z `localStorage`)

**Charakterystyka:**
- Endpoint wymaga autoryzacji (JWT Bearer token)
- Idempotentny (wielokrotne wywołanie daje ten sam rezultat)
- Brak side effects po stronie serwera w MVP
- Prosty endpoint bez złożonej logiki biznesowej

---

## 2. Szczegóły żądania

**Metoda HTTP:** POST

**Struktura URL:** `/api/auth/logout`

**Parametry:**
- **Wymagane:** Brak parametrów w URL ani w payload
- **Opcjonalne:** Brak

**Request Body:** Brak (pusty payload)

**Headers:**
- `Authorization: Bearer <token>` - **WYMAGANY** - token JWT użytkownika
- `Content-Type: application/json` - opcjonalny (brak payload)

**Przykład żądania:**
```http
POST /api/auth/logout HTTP/1.1
Host: api.lottotm.com
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

---

## 3. Wykorzystywane typy

### Contracts.cs

```csharp
using MediatR;

namespace LottoTM.Server.Api.Features.AuthLogout;

public class Contracts
{
    /// <summary>
    /// Żądanie wylogowania użytkownika.
    /// Request jest pusty, ponieważ autoryzacja odbywa się przez JWT token w header.
    /// </summary>
    public record Request : IRequest<Response>;

    /// <summary>
    /// Odpowiedź po pomyślnym wylogowaniu.
    /// </summary>
    /// <param name="Message">Komunikat potwierdzający wylogowanie</param>
    public record Response(string Message);
}
```

**Uzasadnienie:**
- `Request` implementuje `IRequest<Response>` dla integracji z MediatR
- `Request` jest pusty (brak pól), ponieważ wszystkie dane autoryzacyjne są w JWT token
- `Response` zawiera tylko komunikat tekstowy
- Zastosowanie `record` dla immutability i concise syntax

---

## 4. Szczegóły odpowiedzi

### Odpowiedź sukcesu (200 OK)

**Struktura JSON:**
```json
{
  "message": "Wylogowano pomyślnie"
}
```

**Headers:**
```http
HTTP/1.1 200 OK
Content-Type: application/json
```

### Odpowiedź błędu (401 Unauthorized)

**Scenariusz:** Brak tokenu, token wygasły lub nieprawidłowy

**Struktura JSON:**
```json
{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
  "title": "Unauthorized",
  "status": 401
}
```

**Headers:**
```http
HTTP/1.1 401 Unauthorized
WWW-Authenticate: Bearer
Content-Type: application/problem+json
```

**Uwaga:** 401 jest zwracany automatycznie przez middleware `UseAuthentication()` jeśli token jest nieprawidłowy.

### Odpowiedź błędu (500 Internal Server Error)

**Scenariusz:** Nieoczekiwany błąd serwera

**Struktura JSON:**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.6.1",
  "title": "An error occurred while processing your request.",
  "status": 500,
  "detail": "Internal server error details"
}
```

**Uwaga:** Obsługiwane przez `ExceptionHandlingMiddleware` z logowaniem przez Serilog.

---

## 5. Przepływ danych

### Diagram przepływu

```
┌─────────────┐
│   Klient    │
│ (Frontend)  │
└──────┬──────┘
       │ POST /api/auth/logout
       │ Header: Authorization: Bearer <token>
       ▼
┌──────────────────────────────────────────┐
│  ASP.NET Core Middleware Pipeline        │
├──────────────────────────────────────────┤
│  1. ExceptionHandlingMiddleware          │
│  2. Serilog Request Logging              │
│  3. CORS Middleware                      │
│  4. UseAuthentication() ◄────────────────┼─── Walidacja JWT token
│     - Sprawdza token JWT                 │
│     - Weryfikuje podpis, exp, issuer     │
│     - Ustawia HttpContext.User           │
│  5. UseAuthorization()                   │
└──────┬───────────────────────────────────┘
       │ Token prawidłowy ✓
       ▼
┌──────────────────────────────────────────┐
│  Endpoint.cs - MapPost                   │
│  - Wywołanie .RequireAuthorization()     │
│  - Przekazanie do MediatR                │
└──────┬───────────────────────────────────┘
       │
       ▼
┌──────────────────────────────────────────┐
│  MediatR Pipeline                        │
│  - Wysłanie Request do Handler           │
└──────┬───────────────────────────────────┘
       │
       ▼
┌──────────────────────────────────────────┐
│  Validator.cs (FluentValidation)         │
│  - Brak reguł walidacji (Request pusty)  │
└──────┬───────────────────────────────────┘
       │
       ▼
┌──────────────────────────────────────────┐
│  Handler.cs - IRequestHandler            │
│  - Sprawdzenie User.Identity             │
│  - Zwrócenie Response                    │
└──────┬───────────────────────────────────┘
       │
       ▼
┌──────────────────────────────────────────┐
│  Response Serialization                  │
│  - Zwrócenie 200 OK + JSON               │
└──────┬───────────────────────────────────┘
       │
       ▼
┌──────────────┐
│   Klient     │
│  - Usuwa JWT │
│    z localStorage                        │
└──────────────┘
```

### Szczegółowy opis przepływu

1. **Klient wysyła żądanie:**
   - POST `/api/auth/logout`
   - Header: `Authorization: Bearer <token>`

2. **Middleware Pipeline:**
   - `ExceptionHandlingMiddleware`: Przechwytuje wyjątki
   - `UseSerilogRequestLogging`: Loguje szczegóły żądania
   - `UseCors`: Sprawdza polityki CORS
   - `UseAuthentication`: **Kluczowy krok** - waliduje JWT token
     - Sprawdza podpis (HMAC SHA256)
     - Weryfikuje expiration time (exp claim)
     - Sprawdza issuer i audience
     - Ustawia `HttpContext.User` z claims z tokenu
   - `UseAuthorization`: Sprawdza uprawnienia (wymaga uwierzytelnionego użytkownika)

3. **Endpoint routing:**
   - Endpoint z `.RequireAuthorization()` wymaga, aby `HttpContext.User.Identity.IsAuthenticated == true`
   - Jeśli nie - automatyczny zwrot 401 Unauthorized

4. **MediatR:**
   - Przekazanie `Contracts.Request` do handlera przez `IMediator.Send()`

5. **FluentValidation:**
   - Walidator jest pusty (brak reguł), ale zachowujemy spójność architektury

6. **Handler:**
   - Sprawdzenie, czy użytkownik jest uwierzytelniony (opcjonalne, już sprawdzone przez middleware)
   - Zwrócenie `Response` z komunikatem "Wylogowano pomyślnie"

7. **Response:**
   - Serializacja do JSON
   - Zwrot 200 OK

8. **Klient:**
   - Frontend otrzymuje 200 OK
   - Usuwa token JWT z `localStorage.removeItem('token')`
   - Przekierowanie na stronę logowania

### Interakcje z zewnętrznymi systemami

**Brak w MVP:**
- Brak interakcji z bazą danych
- Brak wywołań zewnętrznych API
- Brak Redis/cache

**Post-MVP (jeśli blacklisting tokenów):**
- INSERT do tabeli `TokenBlacklist` (token, expiryDate, userId)
- Lub INSERT do Redis (`SET token:blacklist:<token_id> 1 EX 86400`)

---

## 6. Względy bezpieczeństwa

### 6.1 Uwierzytelnianie i autoryzacja

**Mechanizm:** JWT Bearer Authentication

**Implementacja:**
- Middleware `UseAuthentication()` automatycznie sprawdza token w header `Authorization: Bearer <token>`
- Endpoint z atrybutem `.RequireAuthorization()` wymaga uwierzytelnionego użytkownika
- Claims z tokenu dostępne w `HttpContext.User`

**Konfiguracja JWT (Program.cs):**
```csharp
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
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });
```

### 6.2 Walidacja danych wejściowych

**Request:**
- Brak payload do walidacji
- Token JWT jest walidowany przez middleware (podpis, exp, issuer, audience)

**Walidacja FluentValidation:**
- Validator jest pusty, ale zachowujemy spójność architektury

### 6.3 Zagrożenia bezpieczeństwa i mitygacje

| Zagrożenie | Opis | Mitygacja MVP | Mitygacja Post-MVP |
|------------|------|---------------|-------------------|
| **Brak blacklistingu tokenów** | Token pozostaje ważny po wylogowaniu | Krótki czas życia tokenu (24h), HTTPS | Implementacja token blacklist (Redis lub SQL) |
| **CSRF (Cross-Site Request Forgery)** | Nieautoryzowane żądania z innej domeny | JWT w header (nie cookie), CORS policies | CSRF tokens, SameSite cookie policy |
| **XSS (Cross-Site Scripting)** | Przechwycenie tokenu z localStorage | Sanityzacja danych w UI, CSP headers | HttpOnly cookies, secure storage |
| **Token Hijacking** | Przechwycenie tokenu przez MITM | HTTPS wymagane (NFR-007) | Certificate pinning, secure communication |
| **Replay Attacks** | Ponowne użycie przechwyconego żądania | Endpoint idempotentny (brak szkody) | Nonce, timestamp validation |

### 6.4 HTTPS i bezpieczne nagłówki

**HTTPS:**
- Wymagane na produkcji (NFR-007)
- Middleware `app.UseHttpsRedirection()` (zakomentowany w MVP, do włączenia na prod)

**Secure Headers (dodać w middleware):**
```csharp
app.Use(async (context, next) => {
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});
```

### 6.5 CORS

**Konfiguracja (Program.cs):**
```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});
```

**Uwaga:** W MVP dozwolone wszystkie origins. Na produkcji ograniczyć do konkretnych domen:
```csharp
builder.WithOrigins("https://lottotm.netlify.app")
       .AllowAnyMethod()
       .AllowAnyHeader()
       .AllowCredentials();
```

---

## 7. Obsługa błędów

### 7.1 Potencjalne scenariusze błędów

| Błąd | Kod | Przyczyna | Obsługa |
|------|-----|-----------|---------|
| Brak tokenu | 401 | Header `Authorization` nie zawiera tokenu | Middleware zwraca 401 automatycznie |
| Token wygasły | 401 | Claim `exp` < bieżąca data | Middleware zwraca 401 automatycznie |
| Token nieprawidłowy | 401 | Zły podpis, issuer, audience | Middleware zwraca 401 automatycznie |
| Nieoczekiwany błąd | 500 | Błąd serwera, baza danych niedostępna | ExceptionHandlingMiddleware + Serilog |

### 7.2 Struktura odpowiedzi błędów

**401 Unauthorized (automatyczny):**
```json
{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
  "title": "Unauthorized",
  "status": 401
}
```

**500 Internal Server Error:**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.6.1",
  "title": "An error occurred while processing your request.",
  "status": 500,
  "detail": "Error details for debugging"
}
```

### 7.3 Logowanie błędów

**Serilog:**
- Wszystkie błędy logowane przez `ExceptionHandlingMiddleware`
- Logi zapisywane do `Logs/applog-{Date}.txt`
- Logi konsolowe dla development

**Przykład logowania w Handler (opcjonalne):**
```csharp
_logger.LogInformation("User {UserId} logged out successfully", userId);
```

### 7.4 Kody statusu HTTP

| Kod | Znaczenie | Kiedy zwracany |
|-----|-----------|----------------|
| 200 OK | Pomyślne wylogowanie | Token prawidłowy, handler zwrócił Response |
| 401 Unauthorized | Brak autoryzacji | Brak tokenu, token wygasły lub nieprawidłowy |
| 500 Internal Server Error | Błąd serwera | Nieoczekiwany wyjątek w handlerze |

---

## 8. Rozważania dotyczące wydajności

### 8.1 Potencjalne wąskie gardła

**Brak wąskich gardeł w MVP:**
- Endpoint nie wykonuje operacji na bazie danych
- Brak wywołań zewnętrznych API
- Brak skomplikowanej logiki biznesowej
- Jedyna operacja: zwrócenie statycznego komunikatu

**Overhead:**
- Walidacja JWT token przez middleware (~1-2ms)
- Serializacja JSON Response (~<1ms)

**Całkowity czas odpowiedzi:** <10ms (bardzo szybki endpoint)

### 8.2 Strategie optymalizacji

**MVP:**
- Brak potrzeby optymalizacji
- Endpoint jest już maksymalnie wydajny

**Post-MVP (jeśli blacklisting tokenów):**

1. **Redis dla blacklist:**
   - Zamiast SQL Server użyć Redis dla szybszego dostępu
   - `SET token:blacklist:<token_id> 1 EX 86400` (TTL = 24h)
   - Middleware sprawdza blacklist przed walidacją

2. **Caching:**
   - Brak potrzeby cachowania (endpoint nie zwraca dynamicznych danych)

3. **Connection pooling:**
   - EF Core automatycznie zarządza connection pooling dla SQL Server

### 8.3 Monitoring wydajności

**Metryki do śledzenia:**
- Średni czas odpowiedzi (target: <50ms)
- 95 percentyl czasu odpowiedzi (target: <100ms)
- Liczba wywołań 401 Unauthorized (monitoring prób nieautoryzowanego dostępu)

**Narzędzia:**
- Application Insights (Azure)
- Serilog metrics
- Swagger UI dla testów manualnych

---

## 9. Etapy wdrożenia

### Krok 1: Utworzenie struktury folderów

```bash
mkdir -p src/server/LottoTM.Server.Api/Features/AuthLogout
```

**Pliki do utworzenia:**
- `Endpoint.cs` - definicja endpointa Minimal API
- `Contracts.cs` - Request i Response DTOs
- `Handler.cs` - MediatR request handler
- `Validator.cs` - FluentValidation validator (pusty)

### Krok 2: Implementacja Contracts.cs

**Plik:** `src/server/LottoTM.Server.Api/Features/AuthLogout/Contracts.cs`

```csharp
using MediatR;

namespace LottoTM.Server.Api.Features.AuthLogout;

public class Contracts
{
    /// <summary>
    /// Żądanie wylogowania użytkownika.
    /// Request jest pusty, ponieważ autoryzacja odbywa się przez JWT token w header.
    /// </summary>
    public record Request : IRequest<Response>;

    /// <summary>
    /// Odpowiedź po pomyślnym wylogowaniu.
    /// </summary>
    /// <param name="Message">Komunikat potwierdzający wylogowanie</param>
    public record Response(string Message);
}
```

### Krok 3: Implementacja Validator.cs

**Plik:** `src/server/LottoTM.Server.Api/Features/AuthLogout/Validator.cs`

```csharp
using FluentValidation;

namespace LottoTM.Server.Api.Features.AuthLogout;

public class Validator : AbstractValidator<Contracts.Request>
{
    public Validator()
    {
        // Brak parametrów do walidacji.
        // Request jest pusty, autoryzacja odbywa się przez JWT token.
    }
}
```

### Krok 4: Implementacja Handler.cs

**Plik:** `src/server/LottoTM.Server.Api/Features/AuthLogout/Handler.cs`

```csharp
using FluentValidation;
using MediatR;

namespace LottoTM.Server.Api.Features.AuthLogout;

public class AuthLogoutHandler : IRequestHandler<Contracts.Request, Contracts.Response>
{
    private readonly IValidator<Contracts.Request> _validator;
    private readonly ILogger<AuthLogoutHandler> _logger;

    public AuthLogoutHandler(
        IValidator<Contracts.Request> validator,
        ILogger<AuthLogoutHandler> logger)
    {
        _validator = validator;
        _logger = logger;
    }

    public async Task<Contracts.Response> Handle(
        Contracts.Request request,
        CancellationToken cancellationToken)
    {
        // Walidacja (w tym przypadku pusta, ale zachowujemy spójność architektury)
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // Logowanie informacji o wylogowaniu (opcjonalne)
        // Uwaga: Dostęp do HttpContext.User wymaga wstrzyknięcia IHttpContextAccessor
        // W MVP pomijamy, ponieważ nie mamy UserId do zalogowania
        _logger.LogInformation("User logged out successfully");

        // Zwrócenie odpowiedzi
        return await Task.FromResult(
            new Contracts.Response("Wylogowano pomyślnie")
        );
    }
}
```

**Uwaga o logowaniu UserId:**
Jeśli chcemy logować `UserId`, musimy wstrzyknąć `IHttpContextAccessor`:

```csharp
private readonly IHttpContextAccessor _httpContextAccessor;

public AuthLogoutHandler(
    IValidator<Contracts.Request> validator,
    ILogger<AuthLogoutHandler> logger,
    IHttpContextAccessor httpContextAccessor)
{
    _validator = validator;
    _logger = logger;
    _httpContextAccessor = httpContextAccessor;
}

public async Task<Contracts.Response> Handle(...)
{
    // ...

    var userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value;
    _logger.LogInformation("User {UserId} logged out successfully", userId);

    // ...
}
```

I zarejestrować w `Program.cs`:
```csharp
builder.Services.AddHttpContextAccessor();
```

### Krok 5: Implementacja Endpoint.cs

**Plik:** `src/server/LottoTM.Server.Api/Features/AuthLogout/Endpoint.cs`

```csharp
using MediatR;

namespace LottoTM.Server.Api.Features.AuthLogout;

public static class Endpoint
{
    public static void AddEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("api/auth/logout", async (IMediator mediator) =>
        {
            var request = new Contracts.Request();
            var result = await mediator.Send(request);
            return Results.Ok(result);
        })
        .RequireAuthorization() // KRYTYCZNE: wymaga uwierzytelnionego użytkownika
        .WithName("Logout")
        .Produces<Contracts.Response>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .WithOpenApi(operation =>
        {
            operation.Summary = "Wylogowanie użytkownika";
            operation.Description = "Wylogowanie użytkownika z systemu. Po stronie klienta należy usunąć token JWT z localStorage.";
            return operation;
        })
        .WithTags("Authentication");
    }
}
```

**Kluczowe elementy:**
- `.RequireAuthorization()` - endpoint wymaga JWT token w header
- `.WithName("Logout")` - nazwa dla Swagger UI
- `.Produces<T>()` - dokumentacja odpowiedzi dla Swagger
- `.WithTags("Authentication")` - grupowanie w Swagger UI

### Krok 6: Rejestracja endpointa w Program.cs

**Plik:** `src/server/LottoTM.Server.Api/Program.cs`

Dodać przed `await app.RunAsync();`:

```csharp
// Existing endpoints
LottoTM.Server.Api.Features.ApiVersion.Endpoint.AddEndpoint(app);

// New logout endpoint
LottoTM.Server.Api.Features.AuthLogout.Endpoint.AddEndpoint(app);

await app.RunAsync();
```

### Krok 7: Testowanie manualne

**Użycie Swagger UI:**

1. Uruchom aplikację:
   ```bash
   dotnet run --project src/server/LottoTM.Server.Api
   ```

2. Otwórz Swagger UI (jeśli włączone w `appsettings.json`):
   ```
   https://localhost:5001/swagger
   ```

3. Najpierw zaloguj się przez endpoint `POST /api/auth/login` (gdy zostanie zaimplementowany)

4. Skopiuj token JWT z odpowiedzi

5. Kliknij "Authorize" w Swagger UI i wklej token

6. Wywołaj `POST /api/auth/logout`

7. Sprawdź odpowiedź 200 OK z komunikatem "Wylogowano pomyślnie"

**Użycie curl:**

```bash
# Najpierw uzyskaj token JWT (przykład)
TOKEN="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."

# Wywołaj logout endpoint
curl -X POST https://localhost:5001/api/auth/logout \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json"

# Oczekiwana odpowiedź:
# {
#   "message": "Wylogowano pomyślnie"
# }
```

**Test bez tokenu (oczekiwany 401):**

```bash
curl -X POST https://localhost:5001/api/auth/logout \
  -H "Content-Type: application/json"

# Oczekiwana odpowiedź: 401 Unauthorized
```

### Krok 8: Testy jednostkowe

**Plik:** `tests/server/LottoTM.Server.Api.Tests/Features/AuthLogout/EndpointTests.cs`

```csharp
using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace LottoTM.Server.Api.Tests.Features.AuthLogout;

public class EndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public EndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Logout_WithValidToken_ReturnsOk()
    {
        // Arrange
        var token = await GetValidJwtToken(); // Helper method to get token
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.PostAsync("/api/auth/logout", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Wylogowano pomyślnie", content);
    }

    [Fact]
    public async Task Logout_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        // Nie ustawiamy header Authorization

        // Act
        var response = await _client.PostAsync("/api/auth/logout", null);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_WithExpiredToken_ReturnsUnauthorized()
    {
        // Arrange
        var expiredToken = GenerateExpiredJwtToken(); // Helper method
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", expiredToken);

        // Act
        var response = await _client.PostAsync("/api/auth/logout", null);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_WithInvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "invalid_token_123");

        // Act
        var response = await _client.PostAsync("/api/auth/logout", null);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Helper methods (do implementacji)
    private async Task<string> GetValidJwtToken()
    {
        // TODO: Wywołanie /api/auth/login lub generowanie tokenu testowego
        throw new NotImplementedException();
    }

    private string GenerateExpiredJwtToken()
    {
        // TODO: Generowanie tokenu z exp w przeszłości
        throw new NotImplementedException();
    }
}
```

**Uruchomienie testów:**
```bash
dotnet test tests/server/LottoTM.Server.Api.Tests
```

### Krok 9: Dokumentacja API

**Swagger OpenAPI:**
- Automatycznie generowana przez `.WithOpenApi()`
- Endpoint widoczny w Swagger UI w grupie "Authentication"

**README.md (do aktualizacji):**

Dodać sekcję w dokumentacji API:

```markdown
### POST /api/auth/logout

Wylogowanie użytkownika z systemu.

**Autoryzacja:** Wymagany JWT Bearer token

**Request:**
```http
POST /api/auth/logout
Authorization: Bearer <token>
```

**Response (200 OK):**
```json
{
  "message": "Wylogowano pomyślnie"
}
```

**Response (401 Unauthorized):**
```json
{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
  "title": "Unauthorized",
  "status": 401
}
```

**Uwaga:** Po otrzymaniu 200 OK, klient powinien usunąć token JWT z `localStorage`:
```javascript
localStorage.removeItem('token');
```
```

### Krok 10: Integracja z frontendem

**Frontend (React):**

**ApiService.ts (dodać metodę):**

```typescript
export class ApiService {
  // ... existing code

  async logout(): Promise<void> {
    const response = await fetch(`${this.apiUrl}/api/auth/logout`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${this.getToken()}`,
        'X-TOKEN': this.appToken
      }
    });

    if (response.ok) {
      // Usuń token z localStorage
      localStorage.removeItem('token');
      // Wyczyść context
      this.appContext?.logout();
    } else if (response.status === 401) {
      // Token wygasły lub nieprawidłowy - wyczyść storage
      localStorage.removeItem('token');
      throw new Error('Unauthorized');
    } else {
      throw new Error('Logout failed');
    }
  }

  private getToken(): string | null {
    return localStorage.getItem('token');
  }
}
```

**Użycie w komponencie (np. Header):**

```typescript
const handleLogout = async () => {
  try {
    await apiService.logout();
    navigate('/login');
  } catch (error) {
    console.error('Logout error:', error);
    // Nawet jeśli backend zwrócił błąd, wyloguj użytkownika lokalnie
    localStorage.removeItem('token');
    navigate('/login');
  }
};

return (
  <button onClick={handleLogout}>
    Wyloguj
  </button>
);
```

### Krok 11: Weryfikacja i deployment

**Checklist przed deployment:**

- [ ] Endpoint zwraca 200 OK dla prawidłowego tokenu
- [ ] Endpoint zwraca 401 dla brakującego tokenu
- [ ] Endpoint zwraca 401 dla wygasłego tokenu
- [ ] Endpoint zwraca 401 dla nieprawidłowego tokenu
- [ ] Testy jednostkowe przechodzą
- [ ] Swagger UI dokumentuje endpoint prawidłowo
- [ ] Frontend prawidłowo usuwa token po wywołaniu
- [ ] Logowanie Serilog działa poprawnie
- [ ] HTTPS jest wymuszony na produkcji
- [ ] CORS jest skonfigurowany dla domeny frontendu

**Deployment:**

1. Zbuduj projekt:
   ```bash
   dotnet build LottoTM.sln --configuration Release
   ```

2. Uruchom testy:
   ```bash
   dotnet test LottoTM.sln --configuration Release
   ```

3. Opublikuj API:
   ```bash
   dotnet publish src/server/LottoTM.Server.Api \
     --configuration Release \
     --output ./publish
   ```

4. Deploy na Azure App Service lub Webio.pl

---

## 10. Post-MVP: Blacklisting tokenów (opcjonalnie)

### Scenariusz

Użytkownik wylogowuje się, ale token JWT pozostaje ważny przez 24h. Jeśli token zostanie przechwycony, może być nadużyty.

### Rozwiązanie: Token Blacklist

#### Opcja A: Redis

**Zalety:**
- Szybki dostęp (in-memory)
- Automatyczne TTL (expire)

**Implementacja:**

1. **Dodaj Redis do projektu:**
   ```bash
   dotnet add package StackExchange.Redis
   ```

2. **Konfiguracja w Program.cs:**
   ```csharp
   builder.Services.AddSingleton<IConnectionMultiplexer>(
       ConnectionMultiplexer.Connect(
           builder.Configuration.GetConnectionString("Redis")
       )
   );
   ```

3. **Handler z blacklistingiem:**
   ```csharp
   public class AuthLogoutHandler : IRequestHandler<Contracts.Request, Contracts.Response>
   {
       private readonly IConnectionMultiplexer _redis;
       private readonly IHttpContextAccessor _httpContextAccessor;

       public async Task<Contracts.Response> Handle(...)
       {
           // Pobierz token z header
           var token = _httpContextAccessor.HttpContext?
               .Request.Headers["Authorization"]
               .ToString()
               .Replace("Bearer ", "");

           if (!string.IsNullOrEmpty(token))
           {
               var db = _redis.GetDatabase();
               var tokenId = GetTokenId(token); // Ekstrahuj jti claim
               var expiryTime = GetTokenExpiry(token); // Ekstrahuj exp claim
               var ttl = expiryTime - DateTime.UtcNow;

               // Dodaj token do blacklist z TTL
               await db.StringSetAsync(
                   $"token:blacklist:{tokenId}",
                   "1",
                   ttl
               );
           }

           return new Contracts.Response("Wylogowano pomyślnie");
       }
   }
   ```

4. **Middleware sprawdzający blacklist:**
   ```csharp
   public class TokenBlacklistMiddleware
   {
       private readonly RequestDelegate _next;
       private readonly IConnectionMultiplexer _redis;

       public async Task InvokeAsync(HttpContext context)
       {
           var token = context.Request.Headers["Authorization"]
               .ToString()
               .Replace("Bearer ", "");

           if (!string.IsNullOrEmpty(token))
           {
               var db = _redis.GetDatabase();
               var tokenId = GetTokenId(token);
               var isBlacklisted = await db.KeyExistsAsync(
                   $"token:blacklist:{tokenId}"
               );

               if (isBlacklisted)
               {
                   context.Response.StatusCode = 401;
                   await context.Response.WriteAsync("Token has been revoked");
                   return;
               }
           }

           await _next(context);
       }
   }
   ```

#### Opcja B: SQL Server (tabela TokenBlacklist)

**Schemat:**
```sql
CREATE TABLE TokenBlacklist (
    Id INT PRIMARY KEY IDENTITY(1,1),
    TokenId NVARCHAR(255) NOT NULL, -- jti claim z JWT
    UserId INT NOT NULL,
    ExpiryDate DATETIME2 NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_TokenBlacklist_Users FOREIGN KEY (UserId)
        REFERENCES Users(Id) ON DELETE CASCADE
);

CREATE INDEX IX_TokenBlacklist_TokenId ON TokenBlacklist(TokenId);
CREATE INDEX IX_TokenBlacklist_ExpiryDate ON TokenBlacklist(ExpiryDate);
```

**Handler:**
```csharp
public async Task<Contracts.Response> Handle(...)
{
    var tokenId = GetTokenId(token);
    var userId = GetUserId(token);
    var expiryDate = GetTokenExpiry(token);

    await _dbContext.TokenBlacklist.AddAsync(new TokenBlacklist
    {
        TokenId = tokenId,
        UserId = userId,
        ExpiryDate = expiryDate
    });
    await _dbContext.SaveChangesAsync();

    return new Contracts.Response("Wylogowano pomyślnie");
}
```

**Middleware:**
```csharp
var tokenId = GetTokenId(token);
var isBlacklisted = await _dbContext.TokenBlacklist
    .AnyAsync(t => t.TokenId == tokenId && t.ExpiryDate > DateTime.UtcNow);

if (isBlacklisted)
{
    context.Response.StatusCode = 401;
    await context.Response.WriteAsync("Token has been revoked");
    return;
}
```

**Czyszczenie wygasłych tokenów (background job):**
```csharp
// Cron job usuwający wygasłe tokeny co godzinę
await _dbContext.Database.ExecuteSqlRawAsync(
    "DELETE FROM TokenBlacklist WHERE ExpiryDate < GETUTCDATE()"
);
```

### Porównanie opcji

| Aspekt | Redis | SQL Server |
|--------|-------|------------|
| Wydajność | ⚡ Bardzo szybka (in-memory) | 🐢 Wolniejsza (disk I/O) |
| Automatyczne TTL | ✅ Wbudowane | ❌ Wymaga background job |
| Koszty infrastruktury | 💰 Dodatkowy serwis | ✅ Istniejąca baza |
| Złożoność | ⚙️ Dodatkowa dependencja | ✅ Prosta integracja |
| Audyt | ❌ Trudny | ✅ Łatwy (tabela SQL) |

**Rekomendacja:** Redis dla produkcji, SQL Server dla MVP jeśli blacklisting jest wymagany.

---

## Podsumowanie

Endpoint `POST /api/auth/logout` jest prostym endpointem autoryzacyjnym, który:

1. Wymaga JWT Bearer token w header `Authorization`
2. Zwraca komunikat potwierdzający wylogowanie
3. Deleguje faktyczne wylogowanie na frontend (usunięcie tokenu z localStorage)
4. Nie wykonuje operacji na bazie danych w MVP
5. Jest zabezpieczony przez middleware `UseAuthentication()` i `UseAuthorization()`

**Kluczowe pliki do implementacji:**
- `Features/AuthLogout/Endpoint.cs`
- `Features/AuthLogout/Contracts.cs`
- `Features/AuthLogout/Handler.cs`
- `Features/AuthLogout/Validator.cs`
- `Program.cs` (rejestracja endpointa)

**Czas implementacji:** ~1-2 godziny (bez testów)

**Czas testowania:** ~1 godzina (testy jednostkowe + manualne)

**Całkowity effort:** ~3 godziny

---

**Data utworzenia:** 2025-11-05
**Wersja:** 1.0
**Autor:** Claude Code (AI Assistant)
**Endpoint:** POST /api/auth/logout
