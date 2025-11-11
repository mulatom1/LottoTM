# API Endpoint Implementation Plan: GET /api/tickets

## 1. Przegląd punktu końcowego

Endpoint służy do pobierania kompletnej listy zestawów liczb LOTTO należących do zalogowanego użytkownika. Każdy zestaw zawiera 6 liczb w zakresie 1-49, przechowywanych w znormalizowanej strukturze (tabele Tickets + TicketNumbers). Endpoint zwraca dane z metadanymi paginacji oraz informacją o limicie 100 zestawów na użytkownika.

**Główne cele:**
- Wyświetlenie wszystkich zestawów użytkownika w interfejsie użytkownika
- Zapewnienie bezpieczeństwa poprzez izolację danych (każdy użytkownik widzi tylko swoje zestawy)
- Obsługa znormalizowanej struktury bazy danych (eager loading TicketNumbers)

---

## 2. Szczegóły żądania

**Metoda HTTP:** GET

**Struktura URL:** `/api/tickets`

**Parametry:**
- **Wymagane:** Brak parametrów zapytania
- **Opcjonalne:** Brak

**Headers:**
- `Authorization: Bearer <JWT token>` (wymagany)
- `Content-Type: application/json`
- `X-TOKEN: <app token>` (jeśli używany)

**Request Body:** Brak (metoda GET)

**Autoryzacja:**
- Wymagany ważny JWT token w header Authorization
- Token musi zawierać claim `UserId` (lub `sub`) do filtrowania danych
- Middleware JWT automatycznie waliduje token przed wykonaniem endpointu

**Przykład wywołania:**
```http
GET /api/tickets HTTP/1.1
Host: api.lottotm.com
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json
```

---

## 3. Wykorzystywane typy

### DTOs (Data Transfer Objects)

```csharp
namespace LottoTM.Server.Api.Features.Tickets;

public class Contracts
{
    // Request implementuje IRequest<Response> z MediatR
    public record GetTicketsRequest : IRequest<GetTicketsResponse>;

    // Response DTO
    public record GetTicketsResponse(
        List<TicketDto> Tickets,
        int TotalCount,
        int Limit
    );

    // DTO dla pojedynczego zestawu
    public record TicketDto(
        int Id,
        int UserId,
        string GroupName,
        int[] Numbers,
        DateTime CreatedAt
    );
}
```

### Command Models

Endpoint używa wzorca CQRS z MediatR, więc `GetTicketsRequest` jest jednocześnie command modelem:

```csharp
public record GetTicketsRequest : IRequest<GetTicketsResponse>;
```

### Encje bazodanowe (już istniejące)

```csharp
// Ticket.cs (encja główna)
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

// TicketNumber.cs (encja relacyjna)
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

---

## 4. Szczegóły odpowiedzi

### Struktura odpowiedzi (sukces - 200 OK)

```json
{
  "tickets": [
    {
      "id": 1,
      "userId": 123,
      "groupName": "Ulubione",
      "numbers": [5, 14, 23, 29, 37, 41],
      "createdAt": "2025-10-25T10:00:00Z"
    },
    {
      "id": 2,
      "userId": 123,
      "groupName": "",
      "numbers": [3, 12, 18, 25, 31, 44],
      "createdAt": "2025-10-24T15:30:00Z"
    }    
  ],
  "totalCount": 42,
  "limit": 100
}
```

### Kody statusu HTTP

| Kod | Nazwa | Scenariusz |
|-----|-------|-----------|
| **200 OK** | Sukces | Lista zestawów zwrócona pomyślnie (nawet jeśli pusta) |
| **401 Unauthorized** | Brak autoryzacji | Brak tokenu JWT lub token nieprawidłowy/wygasły |
| **500 Internal Server Error** | Błąd serwera | Błąd bazy danych lub nieobsłużony wyjątek |

### Struktura odpowiedzi błędów

**401 Unauthorized:**
```json
{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
  "title": "Unauthorized",
  "status": 401,
  "detail": "Brak tokenu autoryzacji lub token nieprawidłowy"
}
```

**500 Internal Server Error (ProblemDetails):**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.6.1",
  "title": "Internal Server Error",
  "status": 500,
  "detail": "Wystąpił błąd podczas przetwarzania żądania",
  "traceId": "00-abc123...",
  "errorSource": "GetTicketsHandler"
}
```

### Pusta lista (użytkownik nie ma zestawów)

```json
{
  "tickets": []
}
```

---

## 5. Przepływ danych

### Diagram przepływu

```
┌─────────────┐
│   Client    │
│  (React UI) │
└──────┬──────┘
       │ 1. GET /api/tickets
       │    Authorization: Bearer <JWT>
       ▼
┌─────────────────────────────────┐
│   ASP.NET Core Middleware       │
│  - JWT Authentication           │
│  - Authorization                │
└──────┬──────────────────────────┘
       │ 2. User.Claims (UserId)
       ▼
┌─────────────────────────────────┐
│   Endpoint (Minimal API)        │
│  - MapGet("api/tickets")        │
│  - mediator.Send(request)       │
└──────┬──────────────────────────┘
       │ 3. GetTicketsRequest
       ▼
┌─────────────────────────────────┐
│   MediatR Pipeline              │
│  - FluentValidation (jeśli są)  │
└──────┬──────────────────────────┘
       │ 4. Validated Request
       ▼
┌─────────────────────────────────┐
│   GetTicketsHandler             │
│  - Pobranie UserId z JWT        │
│  - Zapytanie EF Core            │
│  - Eager loading TicketNumbers  │
│  - Mapowanie encji → DTO        │
└──────┬──────────────────────────┘
       │ 5. SQL Query
       ▼
┌─────────────────────────────────┐
│   Entity Framework Core         │
│  - SELECT FROM Tickets          │
│    INNER JOIN TicketNumbers     │
│    WHERE UserId = @userId       │
│    ORDER BY CreatedAt DESC      │
└──────┬──────────────────────────┘
       │ 6. Wynik zapytania
       ▼
┌─────────────────────────────────┐
│   SQL Server Database           │
│  - Tabele: Tickets              │
│             TicketNumbers       │
│  - Indeksy: IX_Tickets_UserId   │
│             IX_TicketNumbers... │
└──────┬──────────────────────────┘
       │ 7. Dane (encje)
       ▼
┌─────────────────────────────────┐
│   GetTicketsHandler             │
│  - Grupowanie liczb per zestaw  │
│  - Kalkulacja metadanych        │
│  - Zwrot GetTicketsResponse     │
└──────┬──────────────────────────┘
       │ 8. Response DTO
       ▼
┌─────────────────────────────────┐
│   Endpoint                      │
│  - Results.Ok(response)         │
└──────┬──────────────────────────┘
       │ 9. HTTP 200 + JSON
       ▼
┌─────────────────────────────────┐
│   Client (React UI)             │
│  - Wyświetlenie listy zestawów  │
└─────────────────────────────────┘
```

### Szczegółowy opis przepływu

**Faza 1: Uwierzytelnianie (Middleware)**
1. Klient wysyła żądanie GET z JWT tokenem w header Authorization
2. ASP.NET Core JWT Authentication Middleware:
   - Waliduje sygnaturę tokenu (HMAC SHA-256)
   - Sprawdza czas wygaśnięcia (exp claim)
   - Dodaje claims do `HttpContext.User`
3. Authorization Middleware sprawdza czy użytkownik jest uwierzytelniony
4. Jeśli token nieprawidłowy → 401 Unauthorized (przepływ kończy się tutaj)

**Faza 2: Endpoint i MediatR**
5. Endpoint tworzy obiekt `GetTicketsRequest` (pusty rekord)
6. Wywołuje `mediator.Send(request)` przekazując żądanie do handlera
7. MediatR Pipeline:
   - Wywołuje FluentValidation Validator (jeśli istnieje)
   - W tym przypadku brak parametrów do walidacji
   - Przekazuje request do odpowiedniego handlera

**Faza 3: Handler - logika biznesowa**
8. `GetTicketsHandler.Handle()`:
   - Pobiera `UserId` z `User.FindFirstValue(ClaimTypes.NameIdentifier)`
   - Wykonuje zapytanie do bazy:
     ```csharp
     var tickets = await _dbContext.Tickets
         .Where(t => t.UserId == currentUserId)
         .Include(t => t.Numbers.OrderBy(tn => tn.Position))
         .OrderByDescending(t => t.CreatedAt)
         .ToListAsync(cancellationToken);
     ```
   - **KRYTYCZNE:** Filtrowanie po `UserId` zapewnia izolację danych

**Faza 4: Baza danych (SQL Server)**
9. Entity Framework Core generuje SQL:
   ```sql
   SELECT t.Id, t.UserId, t.CreatedAt,
          tn.Number, tn.Position
   FROM Tickets t
   INNER JOIN TicketNumbers tn ON t.Id = tn.TicketId
   WHERE t.UserId = @userId
   ORDER BY t.CreatedAt DESC, tn.Position
   ```
10. SQL Server wykonuje zapytanie używając indeksów:
    - `IX_Tickets_UserId` dla filtrowania WHERE
    - `IX_TicketNumbers_TicketId` dla JOIN
11. Zwraca wyniki (encje Ticket z kolekcją TicketNumber)

**Faza 5: Mapowanie danych**
12. Handler mapuje encje na DTOs:
    ```csharp
    var ticketDtos = tickets.Select(t => new TicketDto(
        t.Id,
        t.UserId,
        t.GroupName,
        t.Numbers.OrderBy(n => n.Position).Select(n => n.Number).ToArray(),
        t.CreatedAt
    )).ToList();
    ```
13. Kalkuluje metadane: brak paginacji w MVP


**Faza 6: Zwrot odpowiedzi**
14. Handler zwraca `GetTicketsResponse` z danymi
15. Endpoint serializuje response do JSON
16. Zwraca HTTP 200 OK z body JSON
17. Klient React otrzymuje dane i renderuje listę zestawów

### Interakcje z zewnętrznymi usługami/bazą

**Baza danych (SQL Server):**
- **Operacja:** SELECT z JOIN (odczyt)
- **Tabele:** Tickets, TicketNumbers
- **Indeksy używane:**
  - `IX_Tickets_UserId` - filtrowanie WHERE
  - `IX_TicketNumbers_TicketId` - JOIN
- **Wydajność:** O(log n) dzięki indeksom, przewidywany czas < 100ms dla 100 zestawów

**Brak zewnętrznych API** - endpoint działa wyłącznie z lokalną bazą danych

---

## 6. Względy bezpieczeństwa

### Uwierzytelnianie (Authentication)

**JWT Token Validation:**
- **Middleware:** `UseAuthentication()` w Program.cs
- **Schemat:** JWT Bearer Authentication
- **Algorytm:** HMAC SHA-256 (HS256)
- **Secret Key:** Przechowywany w `appsettings.json` (dla MVP) lub Azure Key Vault (produkcja)
- **Walidacja:**
  - Sygnatura tokenu (HMAC SHA-256)
  - Czas wygaśnięcia (exp claim < current time)
  - Issuer i Audience (jeśli skonfigurowane)

**Konfiguracja JWT (Program.cs):**
```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });
```

### Autoryzacja (Authorization)

**Izolacja danych użytkowników (F-AUTH-004):**
- **KRYTYCZNE:** Endpoint MUSI filtrować wyniki po `UserId` z JWT tokenu
- **Implementacja:**
  ```csharp
  var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
  var tickets = await _dbContext.Tickets
      .Where(t => t.UserId == currentUserId) // MUST HAVE!
      .ToListAsync();
  ```
- **Konsekwencje braku filtracji:** Użytkownik A mógłby zobaczyć zestawy użytkownika B → **CRITICAL SECURITY VULNERABILITY**

**Dlaczego to jest krytyczne dla tego endpointu:**
- Endpoint zwraca WSZYSTKIE zestawy, więc brak filtracji = pełny wyciek danych
- GUID w Id nie chroni (UserId w response ujawniłby innych użytkowników)
- Musi być na poziomie zapytania SQL, nie w filtrze JS na kliencie

### Walidacja danych wejściowych

**Brak parametrów do walidacji:**
- Request nie ma parametrów zapytania
- Jedyne "dane wejściowe" to JWT token (walidowany przez middleware)

**FluentValidation Validator (opcjonalny):**
```csharp
public class GetTicketsValidator : AbstractValidator<GetTicketsRequest>
{
    public GetTicketsValidator()
    {
        // Brak pól do walidacji - request jest pusty
        // Validator pozostaje pusty lub można go pominąć
    }
}
```

### Ochrona przed atakami

**SQL Injection:**
- ✅ **Zabezpieczone:** Entity Framework Core automatycznie parametryzuje zapytania
- ✅ **Dobre praktyki:** NIE używać `.FromSqlRaw()` z konkatenacją stringów
- ✅ **Brak parametrów od użytkownika:** Endpoint nie przyjmuje danych wejściowych

**Over-fetching / DoS:**
- ⚠️ **Potencjalny problem:** Endpoint zwraca wszystkie zestawy (max 100)
- ✅ **Mitigacja:** Limit 100 zestawów/użytkownik (walidacja w POST /api/tickets)
- ✅ **Wydajność:** Eager loading zapobiega N+1 queries
- 📝 **Post-MVP:** Rozważyć dodanie paginacji dla lepszej wydajności UI

**CORS (Cross-Origin Resource Sharing):**
- ✅ **Konfiguracja:** Dozwolone tylko z domeny frontendu React
- ✅ **Credentials:** AllowCredentials() dla JWT tokenów
- Przykład z tech-stack.md:
  ```csharp
  builder.Services.AddCors(options => {
      options.AddPolicy("AllowFrontend", builder => {
          builder.WithOrigins("https://lottotm.netlify.app")
                 .AllowAnyMethod()
                 .AllowAnyHeader()
                 .AllowCredentials();
      });
  });
  ```

**HTTPS:**
- ✅ **Wymagane na produkcji** (NFR-007)
- ✅ **Middleware:** `app.UseHttpsRedirection()`
- JWT token przesyłany przez HTTPS zapobiega man-in-the-middle attacks

**Secure Headers:**
- ✅ **Dodatkowa warstwa bezpieczeństwa:**
  ```csharp
  app.Use(async (context, next) => {
      context.Response.Headers.Add("X-Frame-Options", "DENY");
      context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
      context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
      await next();
  });
  ```

### Rate Limiting

**MVP:**
- Brak rate limiting dla GET endpointów (tylko dla /auth/login i /auth/register)

**Post-MVP (jeśli potrzebne):**
- Limit: 100 requests/minute/user dla GET /api/tickets
- Middleware: AspNetCoreRateLimit

---

## 7. Obsługa błędów

### Potencjalne błędy i ich obsługa

| Błąd | Scenariusz | Kod HTTP | Obsługa |
|------|-----------|----------|---------|
| **Brak tokenu JWT** | Header Authorization nie zawiera tokenu | 401 | Middleware JWT zwraca Unauthorized |
| **Token wygasły** | JWT exp claim < current time | 401 | Middleware JWT zwraca Unauthorized |
| **Token nieprawidłowy** | Zła sygnatura lub format | 401 | Middleware JWT zwraca Unauthorized |
| **Błąd połączenia z bazą** | SQL Server niedostępny | 500 | ExceptionHandlingMiddleware |
| **Timeout zapytania** | Zapytanie SQL > 30s | 500 | ExceptionHandlingMiddleware |
| **Nieobsłużony wyjątek** | Błąd w kodzie handlera | 500 | ExceptionHandlingMiddleware |

### Szczegółowa obsługa błędów

**1. Błędy autoryzacji (401 Unauthorized)**

**Middleware JWT automatycznie obsługuje:**
```csharp
// W Program.cs - konfiguracja JWT
.AddJwtBearer(options =>
{
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            // Logowanie błędu
            _logger.LogWarning("JWT authentication failed: {Error}", context.Exception.Message);
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            // Customowa odpowiedź 401
            context.HandleResponse();
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            return context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                type = "https://tools.ietf.org/html/rfc7235#section-3.1",
                title = "Unauthorized",
                status = 401,
                detail = "Brak tokenu autoryzacji lub token nieprawidłowy"
            }));
        }
    };
});
```

**Zwracana odpowiedź:**
```json
{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
  "title": "Unauthorized",
  "status": 401,
  "detail": "Brak tokenu autoryzacji lub token nieprawidłowy"
}
```

**2. Błędy bazy danych (500 Internal Server Error)**

**ExceptionHandlingMiddleware (globalny):**
```csharp
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database update error in {Source}", GetSourceFromStackTrace(ex));
            await HandleExceptionAsync(context, ex, "Błąd podczas zapisu do bazy danych");
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "SQL Server error in {Source}", GetSourceFromStackTrace(ex));
            await HandleExceptionAsync(context, ex, "Błąd połączenia z bazą danych");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in {Source}", GetSourceFromStackTrace(ex));
            await HandleExceptionAsync(context, ex, "Wystąpił nieoczekiwany błąd");
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception, string detail)
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";

        var problemDetails = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            Title = "Internal Server Error",
            Status = 500,
            Detail = detail,
            Instance = context.Request.Path
        };

        problemDetails.Extensions["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier;
        problemDetails.Extensions["errorSource"] = GetSourceFromStackTrace(exception);

        return context.Response.WriteAsJsonAsync(problemDetails);
    }

    private static string GetSourceFromStackTrace(Exception exception)
    {
        var stackTrace = new StackTrace(exception, true);
        var frame = stackTrace.GetFrame(0);
        return frame?.GetMethod()?.DeclaringType?.Name ?? "Unknown";
    }
}
```

**Zwracana odpowiedź:**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.6.1",
  "title": "Internal Server Error",
  "status": 500,
  "detail": "Błąd połączenia z bazą danych",
  "instance": "/api/tickets",
  "traceId": "00-abc123def456...",
  "errorSource": "GetTicketsHandler"
}
```

**3. Pusta lista (NIE jest błędem)**

**Scenariusz:** Użytkownik nie ma jeszcze żadnych zestawów

**Odpowiedź (200 OK):**
```json
{
  "tickets": [],
  "totalCount": 0,
  "page": 1,
  "pageSize": 0,
  "totalPages": 0,
  "limit": 100
}
```

**Implementacja w handlerze:**
```csharp
// Jeśli brak zestawów, zwróć pustą listę (NIE rzucaj wyjątku)
var ticketDtos = tickets.Any()
    ? tickets.Select(t => /* mapowanie */).ToList()
    : new List<TicketDto>();

return new GetTicketsResponse(
    ticketDtos,
    ticketDtos.Count,
    1,
    ticketDtos.Count,
    ticketDtos.Any() ? 1 : 0,
    100
);
```

### Logowanie z Serilog

**Poziomy logowania:**
- **Information:** Pomyślne wykonanie endpointu
- **Warning:** JWT authentication failed
- **Error:** Błędy bazy danych, nieobsłużone wyjątki

**Przykład w handlerze:**
```csharp
public async Task<GetTicketsResponse> Handle(GetTicketsRequest request, CancellationToken cancellationToken)
{
    var userId = int.Parse(_httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    _logger.LogInformation("Pobieranie zestawów dla użytkownika {UserId}", userId);

    try
    {
        var tickets = await _dbContext.Tickets
            .Where(t => t.UserId == userId)
            .Include(t => t.Numbers.OrderBy(n => n.Position))
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Znaleziono {Count} zestawów dla użytkownika {UserId}", tickets.Count, userId);

        // ... mapowanie i zwrot
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Błąd podczas pobierania zestawów dla użytkownika {UserId}", userId);
        throw; // Rzuć dalej, ExceptionHandlingMiddleware obsłuży
    }
}
```

**Struktura logów (Serilog):**
```json
{
  "@t": "2025-11-05T10:30:00.123Z",
  "@mt": "Pobieranie zestawów dla użytkownika {UserId}",
  "UserId": 123,
  "SourceContext": "LottoTM.Server.Api.Features.Tickets.GetTicketsHandler"
}
```

---

## 8. Wydajność

### Potencjalne wąskie gardła

**1. N+1 Query Problem**
- **Problem:** Bez eager loading, każdy zestaw powoduje osobne zapytanie dla TicketNumbers
- **Rozwiązanie:** `.Include(t => t.Numbers)` - eager loading
- **Wynik:** 1 zapytanie SQL zamiast N+1

**2. Brak indeksów**
- **Problem:** Bez indeksu na UserId, query wykonuje full table scan
- **Rozwiązanie:** Indeks `IX_Tickets_UserId` (już w db-plan.md)
- **Wynik:** O(log n) zamiast O(n)

**3. Over-fetching**
- **Problem:** Zwracanie wszystkich 100 zestawów może być wolne na słabym połączeniu
- **Rozwiązanie (MVP):** Limit 100 zestawów jest akceptowalny
- **Rozwiązanie (post-MVP):** Dodać paginację (page, pageSize parametry)

**4. Brak cache'owania**
- **Problem:** Każde odświeżenie strony = nowe zapytanie SQL
- **Rozwiązanie (post-MVP):** Redis cache z invalidacją przy POST/PUT/DELETE

### Strategie optymalizacji

**Optymalizacja 1: Eager Loading (MUST HAVE)**

```csharp
// ❌ ZŁE - N+1 queries
var tickets = await _dbContext.Tickets
    .Where(t => t.UserId == userId)
    .ToListAsync();
// Każdy ticket.Numbers powoduje osobne zapytanie!

// ✅ DOBRE - 1 query z JOIN
var tickets = await _dbContext.Tickets
    .Where(t => t.UserId == userId)
    .Include(t => t.Numbers.OrderBy(n => n.Position))
    .ToListAsync();
```

**Wygenerowany SQL:**
```sql
SELECT t.Id, t.UserId, t.CreatedAt,
       tn.Id, tn.TicketId, tn.Number, tn.Position
FROM Tickets t
INNER JOIN TicketNumbers tn ON t.Id = tn.TicketId
WHERE t.UserId = @userId
ORDER BY t.CreatedAt DESC, tn.Position
```

**Optymalizacja 2: AsNoTracking (dla read-only)**

```csharp
var tickets = await _dbContext.Tickets
    .AsNoTracking() // Wyłącza change tracking (szybsze dla read-only)
    .Where(t => t.UserId == userId)
    .Include(t => t.Numbers.OrderBy(n => n.Position))
    .OrderByDescending(t => t.CreatedAt)
    .ToListAsync(cancellationToken);
```

**Korzyść:** ~20-30% szybsze dla dużych kolekcji (brak overhead change trackingu)

**Optymalizacja 3: Projection (SelectMany) - alternatywa**

Jeśli potrzebujemy tylko wybranych pól, możemy użyć projekcji zamiast encji:

```csharp
var ticketData = await _dbContext.Tickets
    .Where(t => t.UserId == userId)
    .Select(t => new
    {
        t.Id,
        t.UserId,
        t.CreatedAt,
        Numbers = t.Numbers.OrderBy(n => n.Position).Select(n => n.Number).ToArray()
    })
    .OrderByDescending(t => t.CreatedAt)
    .ToListAsync(cancellationToken);

var ticketDtos = ticketData.Select(t => new TicketDto(
    t.Id,
    t.UserId,
    t.Numbers,
    t.CreatedAt
)).ToList();
```

**Korzyść:** Mniejszy transfer danych (tylko potrzebne pola, nie cała encja)

**Optymalizacja 4: Indeksy (już zaimplementowane w db-plan.md)**

```sql
-- Indeks na UserId (filtrowanie WHERE)
CREATE INDEX IX_Tickets_UserId ON Tickets(UserId);

-- Indeks na TicketId (JOIN z TicketNumbers)
CREATE INDEX IX_TicketNumbers_TicketId ON TicketNumbers(TicketId);
```

**Optymalizacja 5: Compiled Queries (post-MVP, jeśli potrzebne)**

Dla często wykonywanych zapytań można użyć compiled queries:

```csharp
private static readonly Func<ApplicationDbContext, int, CancellationToken, Task<List<Ticket>>> CompiledQuery =
    EF.CompileAsyncQuery(
        (ApplicationDbContext context, int userId, CancellationToken ct) =>
            context.Tickets
                .AsNoTracking()
                .Where(t => t.UserId == userId)
                .Include(t => t.Numbers.OrderBy(n => n.Position))
                .OrderByDescending(t => t.CreatedAt)
                .ToList()
    );

// Użycie
var tickets = await CompiledQuery(_dbContext, userId, cancellationToken);
```

**Korzyść:** ~10-15% szybsze (query jest pre-compiled)

### Benchmarki wydajności

**Cel (NFR-002):** CRUD operations ≤ 500ms (95 percentile)

**Przewidywane czasy (GET /api/tickets):**

| Liczba zestawów | Bez optymalizacji | Z eager loading + indeksy | Z AsNoTracking |
|-----------------|-------------------|---------------------------|----------------|
| 10 zestawów | ~150ms | ~30ms | ~25ms |
| 50 zestawów | ~700ms | ~80ms | ~65ms |
| 100 zestawów | ~1400ms | ~150ms | ~120ms |

**Wniosek:** Z eager loading + indeksami endpoint spełnia wymaganie NFR-002 (≤500ms) nawet dla 100 zestawów.

**Monitoring wydajności:**

```csharp
public async Task<GetTicketsResponse> Handle(GetTicketsRequest request, CancellationToken cancellationToken)
{
    var stopwatch = Stopwatch.StartNew();
    var userId = int.Parse(_httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // ... zapytanie do bazy

    stopwatch.Stop();
    _logger.LogInformation(
        "Pobrano {Count} zestawów dla użytkownika {UserId} w {ElapsedMs}ms",
        tickets.Count,
        userId,
        stopwatch.ElapsedMilliseconds
    );

    // ... mapowanie i zwrot
}
```

### Cache (post-MVP)

**Redis cache pattern:**

```csharp
var cacheKey = $"tickets:user:{userId}";
var cachedData = await _cache.GetStringAsync(cacheKey, cancellationToken);

if (cachedData != null)
{
    return JsonSerializer.Deserialize<GetTicketsResponse>(cachedData);
}

// Jeśli brak w cache, pobierz z bazy
var tickets = await _dbContext.Tickets...

// Zapisz do cache (TTL 5 minut)
await _cache.SetStringAsync(
    cacheKey,
    JsonSerializer.Serialize(response),
    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) },
    cancellationToken
);

return response;
```

**Invalidacja cache:**
- Przy POST /api/tickets → `_cache.RemoveAsync($"tickets:user:{userId}")`
- Przy PUT /api/tickets/{id} → `_cache.RemoveAsync($"tickets:user:{userId}")`
- Przy DELETE /api/tickets/{id} → `_cache.RemoveAsync($"tickets:user:{userId}")`

---

## 9. Kroki implementacji

### Krok 1: Utworzenie struktury folderów i plików

**Lokalizacja:** `src/server/LottoTM.Server.Api/Features/Tickets/`

**Pliki do utworzenia:**
- `Endpoint.cs` - definicja endpointu Minimal API
- `Contracts.cs` - DTOs (GetTicketsRequest, GetTicketsResponse, TicketDto)
- `Handler.cs` - GetTicketsHandler (logika biznesowa)
- `Validator.cs` - FluentValidation validator (opcjonalny, pusty dla tego endpointu)

**Uwaga:** Folder `Tickets` może już istnieć jeśli inne endpointy ticketów są zaimplementowane. Wtedy dodajemy tylko nowe pliki lub rozszerzamy istniejące.

```bash
mkdir -p src/server/LottoTM.Server.Api/Features/Tickets
touch src/server/LottoTM.Server.Api/Features/Tickets/Endpoint.cs
touch src/server/LottoTM.Server.Api/Features/Tickets/Contracts.cs
touch src/server/LottoTM.Server.Api/Features/Tickets/GetTicketsHandler.cs
touch src/server/LottoTM.Server.Api/Features/Tickets/GetTicketsValidator.cs
```

### Krok 2: Implementacja Contracts.cs

**Plik:** `Features/Tickets/Contracts.cs`

```csharp
using MediatR;

namespace LottoTM.Server.Api.Features.Tickets;

public static class GetTicketsContracts
{
    // Request - implementuje IRequest<Response> dla MediatR
    public record Request : IRequest<Response>;

    // Response - główny DTO zwracany przez endpoint
    public record Response(
        List<TicketDto> Tickets,
        int TotalCount,
        int Page,
        int PageSize,
        int TotalPages,
        int Limit
    );

    // DTO dla pojedynczego zestawu
    public record TicketDto(
        Guid Id,
        int UserId,
        int[] Numbers,  // 6 liczb posortowanych wg Position
        DateTime CreatedAt
    );
}
```

**Wyjaśnienie:**
- `Request` jest pusty (brak parametrów), ale musi implementować `IRequest<Response>` dla MediatR
- `Response` zawiera listę zestawów + metadane (totalCount, paginacja, limit)
- `TicketDto` to struktura pojedynczego zestawu w odpowiedzi
- `Numbers` to tablica 6 liczb (int[]) posortowana według Position

### Krok 3: Implementacja GetTicketsHandler.cs

**Plik:** `Features/Tickets/GetTicketsHandler.cs`

```csharp
using System.Security.Claims;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using LottoTM.Server.Api.Data; // Załóżmy, że DbContext jest tutaj

namespace LottoTM.Server.Api.Features.Tickets;

public class GetTicketsHandler : IRequestHandler<GetTicketsContracts.Request, GetTicketsContracts.Response>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IValidator<GetTicketsContracts.Request> _validator;
    private readonly ILogger<GetTicketsHandler> _logger;

    public GetTicketsHandler(
        ApplicationDbContext dbContext,
        IHttpContextAccessor httpContextAccessor,
        IValidator<GetTicketsContracts.Request> validator,
        ILogger<GetTicketsHandler> logger)
    {
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
        _validator = validator;
        _logger = logger;
    }

    public async Task<GetTicketsContracts.Response> Handle(
        GetTicketsContracts.Request request,
        CancellationToken cancellationToken)
    {
        // 1. Walidacja (opcjonalna, request jest pusty)
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // 2. Pobranie UserId z JWT tokenu (KRYTYCZNE dla bezpieczeństwa)
        var userIdClaim = _httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim))
        {
            _logger.LogWarning("UserId claim not found in JWT token");
            throw new UnauthorizedAccessException("Brak identyfikatora użytkownika w tokenie");
        }

        var currentUserId = int.Parse(userIdClaim);
        _logger.LogInformation("Pobieranie zestawów dla użytkownika {UserId}", currentUserId);

        try
        {
            // 3. Zapytanie do bazy z eager loading (MUST HAVE - zapobiega N+1)
            var tickets = await _dbContext.Tickets
                .AsNoTracking() // Read-only, szybsze
                .Where(t => t.UserId == currentUserId) // KRYTYCZNE - izolacja danych
                .Include(t => t.Numbers.OrderBy(n => n.Position)) // Eager loading TicketNumbers
                .OrderByDescending(t => t.CreatedAt) // Sortowanie (najnowsze pierwsze)
                .ToListAsync(cancellationToken);

            _logger.LogInformation(
                "Znaleziono {Count} zestawów dla użytkownika {UserId}",
                tickets.Count,
                currentUserId);

            // 4. Mapowanie encji na DTOs
            var ticketDtos = tickets.Select(ticket => new GetTicketsContracts.TicketDto(
                Id: ticket.Id,
                UserId: ticket.UserId,
                Numbers: ticket.Numbers
                    .OrderBy(n => n.Position)
                    .Select(n => n.Number)
                    .ToArray(), // Tablica 6 liczb
                CreatedAt: ticket.CreatedAt
            )).ToList();

            // 5. Kalkulacja metadanych
            var totalCount = ticketDtos.Count;
            var page = 1; // Brak paginacji w MVP
            var pageSize = totalCount; // Wszystkie zestawy na jednej stronie
            var totalPages = totalCount > 0 ? 1 : 0;
            var limit = 100; // Max limit zestawów/użytkownik

            // 6. Zwrot response
            return new GetTicketsContracts.Response(
                Tickets: ticketDtos,
                TotalCount: totalCount,
                Page: page,
                PageSize: pageSize,
                TotalPages: totalPages,
                Limit: limit
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas pobierania zestawów dla użytkownika {UserId}", currentUserId);
            throw; // Rzuć dalej, ExceptionHandlingMiddleware obsłuży
        }
    }
}
```

**Kluczowe elementy:**
- **Line 36-41:** Pobranie UserId z JWT - KRYTYCZNE dla bezpieczeństwa
- **Line 47:** `AsNoTracking()` - optymalizacja dla read-only
- **Line 48:** `Where(t => t.UserId == currentUserId)` - izolacja danych
- **Line 49:** `.Include(t => t.Numbers...)` - eager loading, zapobiega N+1
- **Line 57-64:** Mapowanie encji na DTOs z sortowaniem liczb według Position
- **Line 67-71:** Kalkulacja metadanych (totalCount, page, limit)

### Krok 4: Implementacja GetTicketsValidator.cs

**Plik:** `Features/Tickets/GetTicketsValidator.cs`

```csharp
using FluentValidation;

namespace LottoTM.Server.Api.Features.Tickets;

public class GetTicketsValidator : AbstractValidator<GetTicketsContracts.Request>
{
    public GetTicketsValidator()
    {
        // Request nie ma parametrów, więc validator jest pusty
        // Możemy go pominąć lub zostawić dla spójności architektury
    }
}
```

**Uwaga:** Validator jest pusty, ponieważ request nie ma parametrów do walidacji. Można go pominąć, ale zostawiamy dla spójności z wzorcem Vertical Slice Architecture.

### Krok 5: Implementacja Endpoint.cs

**Plik:** `Features/Tickets/Endpoint.cs`

```csharp
using MediatR;

namespace LottoTM.Server.Api.Features.Tickets;

public static class GetTicketsEndpoint
{
    public static void AddEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("api/tickets", async (IMediator mediator) =>
        {
            var request = new GetTicketsContracts.Request();
            var result = await mediator.Send(request);
            return Results.Ok(result);
        })
        .WithName("GetTickets")
        .RequireAuthorization() // KRYTYCZNE - wymaga JWT tokenu
        .Produces<GetTicketsContracts.Response>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status500InternalServerError)
        .WithOpenApi(operation =>
        {
            operation.Summary = "Pobiera listę zestawów liczb użytkownika";
            operation.Description = "Zwraca wszystkie zestawy LOTTO należące do zalogowanego użytkownika. Wymaga autoryzacji JWT.";
            return operation;
        });
    }
}
```

**Kluczowe elementy:**
- **Line 16:** `.RequireAuthorization()` - MUST HAVE, wymusza JWT
- **Line 17-19:** Deklaracja możliwych kodów odpowiedzi dla Swagger
- **Line 20-24:** Dokumentacja OpenAPI (opcjonalna, ale zalecana)

### Krok 6: Rejestracja endpointu w Program.cs

**Plik:** `Program.cs`

Dodaj rejestrację endpointu w sekcji gdzie są mapowane endpointy:

```csharp
// ... inne using statements
using LottoTM.Server.Api.Features.Tickets;

var builder = WebApplication.CreateBuilder(args);

// ... konfiguracja services (AddDbContext, AddMediatR, AddFluentValidation, etc.)

var app = builder.Build();

// ... middleware (UseAuthentication, UseAuthorization, etc.)

// Rejestracja endpointów
LottoTM.Server.Api.Features.ApiVersion.Endpoint.AddEndpoint(app);
LottoTM.Server.Api.Features.Tickets.GetTicketsEndpoint.AddEndpoint(app); // NOWA LINIA

app.Run();
```

**Uwaga:** Jeśli już istnieje plik z innymi endpointami Tickets (np. POST, DELETE), można rozszerzyć istniejącą klasę `Endpoint` zamiast tworzyć `GetTicketsEndpoint`.

### Krok 7: Rejestracja handlera i validatora w Dependency Injection

**Opcja A: Automatyczna rejestracja przez MediatR**

Jeśli masz skonfigurowane `AddMediatR` w Program.cs, handlery są automatycznie rejestrowane:

```csharp
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
```

**Opcja B: Manualna rejestracja validatorów (jeśli używasz FluentValidation)**

```csharp
builder.Services.AddValidatorsFromAssemblyContaining<GetTicketsValidator>();
```

**Opcja C: Rejestracja IHttpContextAccessor (jeśli jeszcze nie)**

```csharp
builder.Services.AddHttpContextAccessor();
```

### Krok 8: Weryfikacja struktury bazy danych

**Sprawdź czy tabele i indeksy istnieją:**

```sql
-- Sprawdź strukturę Tickets
SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Tickets';

-- Sprawdź strukturę TicketNumbers
SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'TicketNumbers';

-- Sprawdź indeksy
EXEC sp_helpindex 'Tickets';
EXEC sp_helpindex 'TicketNumbers';
```

**Wymagane indeksy (z db-plan.md):**
- `IX_Tickets_UserId` na `Tickets(UserId)`
- `IX_TicketNumbers_TicketId` na `TicketNumbers(TicketId)`

**Jeśli indeksy nie istnieją, utwórz migrację:**

```bash
dotnet ef migrations add AddTicketsIndexes --project src/server/LottoTM.Server.Api
dotnet ef database update --project src/server/LottoTM.Server.Api
```

### Krok 9: Testy jednostkowe

**Plik:** `tests/server/LottoTM.Server.Api.Tests/Features/Tickets/GetTicketsHandlerTests.cs`

```csharp
using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using LottoTM.Server.Api.Features.Tickets;
using LottoTM.Server.Api.Data;

namespace LottoTM.Server.Api.Tests.Features.Tickets;

public class GetTicketsHandlerTests
{
    [Fact]
    public async Task Handle_WhenUserHasTickets_ReturnsTicketsList()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_GetTickets")
            .Options;

        await using var context = new ApplicationDbContext(options);

        var userId = 123;
        var ticket1 = new Ticket { Id = Guid.NewGuid(), UserId = userId, CreatedAt = DateTime.UtcNow };
        var ticket2 = new Ticket { Id = Guid.NewGuid(), UserId = userId, CreatedAt = DateTime.UtcNow.AddHours(-1) };

        context.Tickets.AddRange(ticket1, ticket2);

        // Dodaj TicketNumbers dla ticket1
        for (int i = 1; i <= 6; i++)
        {
            context.TicketNumbers.Add(new TicketNumber
            {
                TicketId = ticket1.Id,
                Number = i,
                Position = (byte)i
            });
        }

        await context.SaveChangesAsync();

        var handler = new GetTicketsHandler(context, mockHttpContextAccessor, mockValidator, mockLogger);
        var request = new GetTicketsContracts.Request();

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(2);
        result.Tickets.Should().HaveCount(2);
        result.Tickets[0].Numbers.Should().HaveCount(6);
        result.Limit.Should().Be(100);
    }

    [Fact]
    public async Task Handle_WhenUserHasNoTickets_ReturnsEmptyList()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_GetTickets_Empty")
            .Options;

        await using var context = new ApplicationDbContext(options);
        var handler = new GetTicketsHandler(context, mockHttpContextAccessor, mockValidator, mockLogger);
        var request = new GetTicketsContracts.Request();

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(0);
        result.Tickets.Should().BeEmpty();
        result.TotalPages.Should().Be(0);
    }

    [Fact]
    public async Task Handle_OnlyReturnsTicketsForCurrentUser()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_GetTickets_Isolation")
            .Options;

        await using var context = new ApplicationDbContext(options);

        var currentUserId = 123;
        var otherUserId = 456;

        var ticket1 = new Ticket { Id = Guid.NewGuid(), UserId = currentUserId, CreatedAt = DateTime.UtcNow };
        var ticket2 = new Ticket { Id = Guid.NewGuid(), UserId = otherUserId, CreatedAt = DateTime.UtcNow };

        context.Tickets.AddRange(ticket1, ticket2);
        await context.SaveChangesAsync();

        var handler = new GetTicketsHandler(context, mockHttpContextAccessor, mockValidator, mockLogger);
        var request = new GetTicketsContracts.Request();

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.TotalCount.Should().Be(1);
        result.Tickets.All(t => t.UserId == currentUserId).Should().BeTrue();
    }
}
```

**Uwaga:** Mock'i dla IHttpContextAccessor, IValidator i ILogger muszą być zaimplementowane osobno.

### Krok 10: Testy integracyjne

**Plik:** `tests/server/LottoTM.Server.Api.Tests/Features/Tickets/GetTicketsEndpointTests.cs`

```csharp
using System.Net;
using System.Net.Http.Headers;
using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace LottoTM.Server.Api.Tests.Features.Tickets;

public class GetTicketsEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public GetTicketsEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetTickets_WithValidToken_Returns200AndTicketsList()
    {
        // Arrange
        var client = _factory.CreateClient();
        var token = await GetValidJwtToken(); // Helper method
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/tickets");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("tickets");
        content.Should().Contain("totalCount");
        content.Should().Contain("limit");
    }

    [Fact]
    public async Task GetTickets_WithoutToken_Returns401()
    {
        // Arrange
        var client = _factory.CreateClient();
        // Brak tokenu w header

        // Act
        var response = await client.GetAsync("/api/tickets");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetTickets_WithInvalidToken_Returns401()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid_token");

        // Act
        var response = await client.GetAsync("/api/tickets");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
```

### Krok 11: Testowanie manualne (Swagger / Postman)

**A) Przez Swagger UI:**

1. Uruchom aplikację: `dotnet run --project src/server/LottoTM.Server.Api`
2. Otwórz: `https://localhost:5001/swagger` (jeśli Swagger włączony)
3. Zarejestruj użytkownika przez `POST /api/auth/register`
4. Zaloguj się przez `POST /api/auth/login` (otrzymasz JWT token)
5. Kliknij "Authorize" w Swagger, wklej token
6. Wywołaj `GET /api/tickets`
7. Sprawdź odpowiedź (pusta lista jeśli brak zestawów)

**B) Przez Postman:**

```http
GET https://localhost:5001/api/tickets
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json
```

**Oczekiwana odpowiedź (przykład):**
```json
{
  "tickets": [],
  "totalCount": 0,
  "page": 1,
  "pageSize": 0,
  "totalPages": 0,
  "limit": 100
}
```

**C) Dodanie testowych zestawów:**

1. Wywołaj `POST /api/tickets` z body:
   ```json
   {
     "numbers": [3, 12, 25, 31, 42, 48]
   }
   ```
2. Dodaj kilka zestawów
3. Ponownie wywołaj `GET /api/tickets`
4. Sprawdź czy zestawy są zwrócone

### Krok 12: Dokumentacja OpenAPI (opcjonalna)

**Rozszerzenie Endpoint.cs o szczegółową dokumentację:**

```csharp
.WithOpenApi(operation =>
{
    operation.Summary = "Pobiera listę zestawów liczb użytkownika";
    operation.Description = @"
        Zwraca wszystkie zestawy liczb LOTTO należące do zalogowanego użytkownika.

        Endpoint wymaga autoryzacji JWT. Zwraca listę zestawów posortowaną według daty utworzenia (najnowsze pierwsze).

        Każdy użytkownik może mieć maksymalnie 100 zestawów.

        **Uwaga:** Endpoint zwraca WSZYSTKIE zestawy użytkownika (brak paginacji w MVP).
    ";

    operation.Responses["200"].Description = "Lista zestawów zwrócona pomyślnie (może być pusta)";
    operation.Responses["401"].Description = "Brak tokenu JWT lub token nieprawidłowy";
    operation.Responses["500"].Description = "Błąd serwera (baza danych, nieobsłużony wyjątek)";

    return operation;
});
```

### Krok 13: Performance testing (opcjonalny)

**Utworzenie testowych danych (100 zestawów):**

```sql
DECLARE @userId INT = 1; -- Zamień na ID testowego użytkownika
DECLARE @i INT = 1;

WHILE @i <= 100
BEGIN
    DECLARE @ticketId UNIQUEIDENTIFIER = NEWID();

    INSERT INTO Tickets (Id, UserId, CreatedAt)
    VALUES (@ticketId, @userId, GETUTCDATE());

    INSERT INTO TicketNumbers (TicketId, Number, Position)
    VALUES
        (@ticketId, @i % 49 + 1, 1),
        (@ticketId, (@i + 1) % 49 + 1, 2),
        (@ticketId, (@i + 2) % 49 + 1, 3),
        (@ticketId, (@i + 3) % 49 + 1, 4),
        (@ticketId, (@i + 4) % 49 + 1, 5),
        (@ticketId, (@i + 5) % 49 + 1, 6);

    SET @i = @i + 1;
END
```

**Pomiar czasu wykonania:**

```csharp
var stopwatch = Stopwatch.StartNew();
var result = await handler.Handle(request, CancellationToken.None);
stopwatch.Stop();
Console.WriteLine($"Czas wykonania: {stopwatch.ElapsedMilliseconds}ms");
```

**Cel:** < 500ms dla 100 zestawów (zgodnie z NFR-002)

### Krok 14: Deployment checklist

**Przed wdrożeniem na produkcję:**

- [ ] Wszystkie testy jednostkowe przechodzą
- [ ] Wszystkie testy integracyjne przechodzą
- [ ] Performance test pokazuje < 500ms dla 100 zestawów
- [ ] Indeksy `IX_Tickets_UserId` i `IX_TicketNumbers_TicketId` są utworzone w bazie produkcyjnej
- [ ] JWT authentication jest poprawnie skonfigurowane (secret key w Azure Key Vault, nie w appsettings.json)
- [ ] HTTPS jest wymuszony (UseHttpsRedirection)
- [ ] CORS jest ograniczony do domeny frontendu (nie AllowAnyOrigin)
- [ ] Logowanie Serilog działa poprawnie (logi w pliku/konsoli)
- [ ] ExceptionHandlingMiddleware jest zarejestrowany i testowany
- [ ] Swagger jest WYŁĄCZONY na produkcji (lub zabezpieczony)
- [ ] Connection string do produkcyjnej bazy jest w Azure Configuration/KeyVault
- [ ] Migracje Entity Framework zostały zaaplikowane na produkcji

---

## 10. Checklist weryfikacyjny

### Funkcjonalność

- [ ] Endpoint zwraca listę zestawów użytkownika
- [ ] Każdy zestaw zawiera dokładnie 6 liczb posortowanych według Position
- [ ] Response zawiera metadane (totalCount, page, pageSize, totalPages, limit)
- [ ] Pusta lista jest zwracana poprawnie (nie błąd 404)
- [ ] Zestawy są sortowane według CreatedAt DESC (najnowsze pierwsze)

### Bezpieczeństwo

- [ ] Endpoint wymaga JWT tokenu (`.RequireAuthorization()`)
- [ ] Handler filtruje zestawy po UserId z JWT (`.Where(t => t.UserId == currentUserId)`)
- [ ] Użytkownik A nie może zobaczyć zestawów użytkownika B (test izolacji danych)
- [ ] Brak SQL injection (używa EF Core z parametryzowanymi zapytaniami)
- [ ] HTTPS wymuszony na produkcji
- [ ] CORS ograniczony do domeny frontendu

### Wydajność

- [ ] Używa eager loading (`.Include(t => t.Numbers)`) - zapobiega N+1
- [ ] Używa `AsNoTracking()` dla read-only queries
- [ ] Indeksy `IX_Tickets_UserId` i `IX_TicketNumbers_TicketId` są w bazie
- [ ] Czas wykonania < 500ms dla 100 zestawów (zgodnie z NFR-002)

### Obsługa błędów

- [ ] 401 Unauthorized zwracany gdy brak tokenu
- [ ] 401 Unauthorized zwracany gdy token nieprawidłowy/wygasły
- [ ] 500 Internal Server Error obsłużony przez ExceptionHandlingMiddleware
- [ ] Błędy są logowane przez Serilog z odpowiednim poziomem (Error, Warning)

### Testy

- [ ] Testy jednostkowe dla handlera (happy path, empty list, izolacja danych)
- [ ] Testy integracyjne dla endpointu (200 OK, 401 Unauthorized)
- [ ] Performance test (< 500ms dla 100 zestawów)

### Dokumentacja

- [ ] OpenAPI/Swagger dokumentacja zawiera opis endpointu
- [ ] Przykłady request/response są w dokumentacji
- [ ] Kody statusu HTTP są opisane (200, 401, 500)

### Deployment

- [ ] Migracje EF Core zaaplikowane na produkcji
- [ ] Indeksy utworzone w produkcyjnej bazie
- [ ] JWT secret key w Azure Key Vault (nie w appsettings.json)
- [ ] HTTPS wymuszony
- [ ] Swagger wyłączony na produkcji

---

## 11. Potencjalne rozszerzenia (post-MVP)

### Paginacja

**Obecnie:** Endpoint zwraca wszystkie zestawy (max 100)

**Post-MVP:** Dodać parametry `page` i `pageSize` do query string

```csharp
// Contracts.cs
public record Request(int Page = 1, int PageSize = 20) : IRequest<Response>;

// Handler.cs
var skip = (request.Page - 1) * request.PageSize;
var tickets = await _dbContext.Tickets
    .Where(t => t.UserId == currentUserId)
    .Include(t => t.Numbers)
    .OrderByDescending(t => t.CreatedAt)
    .Skip(skip)
    .Take(request.PageSize)
    .ToListAsync(cancellationToken);

var totalCount = await _dbContext.Tickets.CountAsync(t => t.UserId == currentUserId, cancellationToken);
var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);
```

### Sortowanie

**Post-MVP:** Dodać parametr `sortBy` (CreatedAt, Id)

```csharp
// Contracts.cs
public record Request(string SortBy = "CreatedAt", string SortOrder = "desc") : IRequest<Response>;

// Handler.cs
var query = _dbContext.Tickets.Where(t => t.UserId == currentUserId);

query = request.SortBy switch
{
    "Id" => request.SortOrder == "asc" ? query.OrderBy(t => t.Id) : query.OrderByDescending(t => t.Id),
    _ => request.SortOrder == "asc" ? query.OrderBy(t => t.CreatedAt) : query.OrderByDescending(t => t.CreatedAt)
};
```

### Filtrowanie

**Post-MVP:** Filtrowanie po liczbach (np. zestawy zawierające liczbę 7)

```csharp
// Contracts.cs
public record Request(int? ContainsNumber = null) : IRequest<Response>;

// Handler.cs
var query = _dbContext.Tickets.Where(t => t.UserId == currentUserId);

if (request.ContainsNumber.HasValue)
{
    query = query.Where(t => t.Numbers.Any(n => n.Number == request.ContainsNumber.Value));
}
```

### Cache Redis

**Post-MVP:** Cache wyników dla lepszej wydajności

```csharp
var cacheKey = $"tickets:user:{currentUserId}";
var cachedData = await _cache.GetStringAsync(cacheKey, cancellationToken);

if (cachedData != null)
{
    return JsonSerializer.Deserialize<Response>(cachedData);
}

// Pobranie z bazy...
var response = new Response(...);

// Zapis do cache (TTL 5 minut)
await _cache.SetStringAsync(
    cacheKey,
    JsonSerializer.Serialize(response),
    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) },
    cancellationToken
);

return response;
```

### Statystyki

**Post-MVP:** Dodatkowe metadane w response (najczęstsza liczba, średnia wieku zestawów)

```csharp
public record Response(
    List<TicketDto> Tickets,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages,
    int Limit,
    Statistics? Stats = null // NOWE
);

public record Statistics(
    int MostFrequentNumber,
    TimeSpan AverageTicketAge
);
```

---

## 12. Podsumowanie

Endpoint `GET /api/tickets` jest kluczowym elementem aplikacji LottoTM, umożliwiającym użytkownikom przeglądanie swoich zestawów liczb LOTTO. Implementacja musi zapewnić:

**Priorytet 1 (MUST HAVE):**
- ✅ Bezpieczeństwo: izolacja danych (filtrowanie po UserId z JWT)
- ✅ Wydajność: eager loading (`.Include()`) + indeksy
- ✅ Poprawność: mapowanie znormalizowanej struktury (TicketNumbers → int[])

**Priorytet 2 (SHOULD HAVE):**
- ✅ Obsługa błędów: 401/500 przez middleware
- ✅ Logowanie: Serilog z poziomami Information/Error
- ✅ Testy: jednostkowe + integracyjne

**Priorytet 3 (NICE TO HAVE):**
- 📝 Dokumentacja OpenAPI/Swagger
- 📝 Performance monitoring (stopwatch w logach)
- 📝 Cache Redis (post-MVP)

**Kluczowe metryki sukcesu:**
- Czas wykonania < 500ms dla 100 zestawów (NFR-002)
- 100% izolacja danych (użytkownik A nie widzi zestawów B)
- Zero N+1 queries (eager loading)
- Brak błędów 500 w testach integracyjnych

**Ryzyko i mitigacja:**
- **Ryzyko:** Zapomnienie o filtracji po UserId → **Mitigacja:** Testy izolacji danych
- **Ryzyko:** N+1 queries → **Mitigacja:** Eager loading + monitoring wydajności
- **Ryzyko:** Brak indeksów → **Mitigacja:** Sprawdzenie indeksów w bazie przed deploymentem

Implementacja zgodna z powyższym planem zapewni bezpieczny, wydajny i skalowalny endpoint gotowy do produkcji.
