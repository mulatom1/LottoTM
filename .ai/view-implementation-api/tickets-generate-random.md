# Plan Wdrożenia Endpointa API: POST /api/tickets/generate-random

**Wersja:** 1.2
**Data:** 2025-11-16
**Moduł:** Tickets (Zestawy)
**Architektura:** Vertical Slice (MediatR + FluentValidation)

---

## 1. Przegląd punktu końcowego

**Cel:**
Generowanie pojedynczego losowego zestawu 6 unikalnych liczb z zakresu 1-49 i zwrócenie jako **propozycja** (bez zapisu do bazy danych).

**Funkcjonalność:**
- Weryfikacja autoryzacji użytkownika (JWT)
- Generowanie 6 losowych, unikalnych liczb (1-49)
- Zwrot liczb jako propozycja - użytkownik może zdecydować czy zapisać
- **NIE zapisuje do bazy danych** - tylko generuje i zwraca
- **NIE sprawdza limitu zestawów** - to tylko generator propozycji

**Kontekst biznesowy:**
Użytkownik chce szybko wygenerować losowy zestaw liczb LOTTO bez ręcznego wyboru. System zwraca propozycję, którą użytkownik może zaakceptować i zapisać ręcznie używając `POST /api/tickets`. Sprawdzenie limitu i zapis odbywają się dopiero przy wywołaniu `POST /api/tickets`.

---

## 2. Szczegóły żądania

### 2.1 Metoda HTTP i URL

- **Metoda:** `POST`
- **URL:** `/api/tickets/generate-random`
- **Content-Type:** Brak (puste body)

### 2.2 Autoryzacja

- **Wymagana:** TAK
- **Typ:** JWT Bearer Token
- **Header:** `Authorization: Bearer <token>`
- **Claims:**
  - `UserId` (int) - ID zalogowanego użytkownika
  - `Email` (string) - email użytkownika

### 2.3 Parametry

**Parametry URL (query string):** BRAK

**Parametry body (payload):** BRAK

**Źródło danych:**
- `UserId` - wyciągany z JWT claims w endpoincie i przekazywany do Handler

### 2.4 Przykład żądania

```http
POST /api/tickets/generate-random HTTP/1.1
Host: localhost:5000
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Length: 0
```

---

## 3. Wykorzystywane typy

### 3.1 Contracts (DTO)

**Plik:** `Features/Tickets/GenerateRandom/Contracts.cs`

```csharp
using MediatR;

namespace LottoTM.Server.Api.Features.Tickets.GenerateRandom;

public class Contracts
{
    /// <summary>
    /// Request for generating a random ticket.
    /// No parameters required - this is a simple generation endpoint.
    /// </summary>
    public record Request : IRequest<Response>;

    /// <summary>
    /// Response with generated numbers array (preview/proposal).
    /// User must manually save using POST /api/tickets if they want to keep it.
    /// </summary>
    public record Response(int[] Numbers);
}
```

### 3.2 Encje bazodanowe (już istniejące)

**Ticket.cs:**
```csharp
public class Ticket
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string GroupName { get; set; } = string.Empty; // Pole grupujące zestawy
    public DateTime CreatedAt { get; set; }
    public User User { get; set; } = null!;
    public ICollection<TicketNumber> Numbers { get; set; } = new List<TicketNumber>();
}
```

**TicketNumber.cs:**
```csharp
public class TicketNumber
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public int Number { get; set; }
    public byte Position { get; set; }
    public Ticket Ticket { get; set; } = null!;
}
```

### 3.3 Service Interface (do utworzenia)

**ITicketService.cs:**
```csharp
namespace LottoTM.Server.Api.Services;

public interface ITicketService
{
    /// <summary>
    /// Generates 6 random unique numbers in range 1-49.
    /// This is a pure generation method - does not interact with database.
    /// </summary>
    int[] GenerateRandomNumbers();
}
```

**Uwaga:** Generator tylko zwraca propozycję - brak metod do zapisu do bazy lub sprawdzania limitu.

---

## 4. Szczegóły odpowiedzi

### 4.1 Sukces (200 OK)

**Status:** `200 OK`

**Body:**
```json
{
  "numbers": [7, 19, 22, 33, 38, 45]
}
```

**Headers:**
```
Content-Type: application/json; charset=utf-8
```

**Uwaga:** To jest tylko **propozycja** - liczby nie są zapisane do bazy. Użytkownik musi ręcznie zapisać używając `POST /api/tickets` jeśli chce zachować ten zestaw. Limit 100 zestawów i walidacja unikalności są sprawdzane dopiero przy zapisie przez `POST /api/tickets`.

### 4.2 Błąd: Brak autoryzacji (401 Unauthorized)

**Status:** `401 Unauthorized`

**Body:**
```json
{
  "message": "Unauthorized"
}
```

**Scenariusz:** Brak tokenu JWT lub token nieprawidłowy/wygasły.

### 4.3 Błąd: Błąd serwera (500 Internal Server Error)

**Status:** `500 Internal Server Error`

**Body:**
```json
{
  "message": "An error occurred while processing your request."
}
```

**Scenariusz:** Nieoczekiwany błąd podczas generowania liczb (błąd algorytmu generowania, inne nieoczekiwane wyjątki).

**Logowanie:** Pełny stack trace logowany przez Serilog do pliku `Logs/applog-{Date}.txt`.

---

## 5. Przepływ danych

### 5.1 Diagram sekwencji

```
Client                  Endpoint              Handler               Service
  |                        |                     |                     |
  |-- POST /generate-random-|                     |                     |
  |   + JWT Token          |                     |                     |
  |                        |                     |                     |
  |                        |-- Verify Auth ------|                     |
  |                        |   (JWT middleware)  |                     |
  |                        |                     |                     |
  |                        |-- Send Request ---->|                     |
  |                        |   (empty)           |                     |
  |                        |                     |                     |
  |                        |                     |-- Validate ---------|
  |                        |                     |   (no params)       |
  |                        |                     |                     |
  |                        |                     |-- Generate Random ->|
  |                        |                     |   Numbers           |
  |                        |                     |<-- [6 numbers] -----|
  |                        |                     |                     |
  |                        |<-- Response --------|                     |
  |                        |   { numbers: [...] }|                     |
  |                        |                     |                     |
  |<-- 200 OK -------------|                     |                     |
  |   { numbers: [...] }   |                     |                     |
  |                        |                     |                     |
  |-- (User decision)      |                     |                     |
  |-- POST /api/tickets ---|-------------------->|-------------------->|
  |   (to save if desired) |  (separate call)    |   (validates limit) |
```

### 5.2 Opis kroków

1. **Klient wysyła żądanie POST** z JWT tokenem w headerze `Authorization`
2. **ASP.NET Core Middleware** weryfikuje JWT i wypełnia `HttpContext.User`
3. **Endpoint** tworzy obiekt `Request` (pusty - bez parametrów)
4. **MediatR** wysyła `Request` do `Handler`
5. **Validator** sprawdza poprawność żądania (minimalny - request jest pusty)
6. **Handler** wywołuje `ITicketService.GenerateRandomNumbers()` dla wygenerowania 6 losowych liczb
7. **Service** generuje liczby algorytmem:
    ```csharp
    var random = Random.Shared; // Thread-safe (C# 10+)
    var numbers = Enumerable.Range(1, 49)
        .OrderBy(x => random.Next())
        .Take(6)
        .OrderBy(x => x)
        .ToArray();
    ```
8. **Handler** tworzy obiekt `Response` z wygenerowanymi liczbami
9. **Endpoint** zwraca `200 OK` z body `{ "numbers": [...] }`
10. **Klient** wyświetla wygenerowane liczby użytkownikowi
11. **Użytkownik decyduje** czy zapisać zestaw:
    - Jeśli TAK: Klient wywołuje `POST /api/tickets` z tymi liczbami (osobne żądanie)
    - `POST /api/tickets` sprawdza limit 100 zestawów i zapisuje do bazy

### 5.3 Interakcje z bazą danych

**Brak interakcji z bazą danych** - ten endpoint tylko generuje liczby losowe i zwraca je jako propozycję.

**Uwaga:**
- Endpoint nie wykonuje żadnych zapytań SQL
- Nie sprawdza limitu zestawów (to robi `POST /api/tickets`)
- Nie zapisuje danych do tabel `Tickets` ani `TicketNumbers`
- Jest to czysta operacja obliczeniowa (generation-only)

---

## 6. Względy bezpieczeństwa

### 6.1 Uwierzytelnianie

- **Mechanizm:** JWT Bearer Token
- **Implementacja:** ASP.NET Core middleware `UseAuthentication()`
- **Weryfikacja:** Automatyczna przez framework (podpis, wygaśnięcie)
- **Claims wymagane:** `UserId` (int), `Email` (string)

### 6.2 Autoryzacja

- **Poziom:** Użytkownik zalogowany (dowolna rola)
- **Sprawdzenie:** `[Authorize]` attribute lub `RequireAuthorization()` w Minimal API
- **Izolacja danych:** Endpoint nie tworzy zasobów - tylko generuje liczby (stateless)

### 6.3 Walidacja danych

**Walidacja wejścia (Validator.cs):**
- Minimalna walidacja - request jest pusty, brak parametrów
- Opcjonalnie: podstawowa weryfikacja że żądanie jest prawidłowo sformułowane

**Walidacja wygenerowanych liczb (Service):**
- Algorytm zapewnia:
  - Dokładnie 6 liczb
  - Wszystkie w zakresie 1-49
  - Wszystkie unikalne (dzięki Enumerable.Range + Take(6))

**Walidacja NIE wykonywana:**
- ❌ Limit 100 zestawów - sprawdzany dopiero w `POST /api/tickets`
- ❌ Unikalność zestawu - sprawdzana dopiero w `POST /api/tickets`
- ❌ Dostęp do bazy danych - ten endpoint jest stateless

### 6.4 Ochrona przed atakami

| Typ ataku | Mitigacja |
|-----------|-----------|
| **SQL Injection** | N/A - endpoint nie wykonuje zapytań SQL |
| **CSRF** | Nie dotyczy API REST z JWT (stateless) |
| **XSS** | Zwracane tylko liczby (integers) - brak ryzyka |
| **Rate Limiting** | Opcjonalnie: dodać throttling (np. 100 requestów/minutę) - poza MVP |
| **Token theft** | HTTPS dla wszystkich połączeń (konfiguracja infrastruktury) |
| **Replay attack** | JWT expiration (24h), opcjonalnie: refresh tokens |
| **DoS via computation** | Algorytm generowania jest O(1) - stały czas ~1ms |

### 6.5 Bezpieczeństwo danych

- **Brak dostępu do bazy:** Endpoint nie modyfikuje ani nie odczytuje danych z bazy
- **Haszowanie haseł:** bcrypt (min. 10 rounds) - już zaimplementowane w module Auth
- **Komunikaty błędów:** Ogólne dla 500, szczegółowe tylko dla błędów walidacji (400)

---

## 7. Obsługa błędów

### 7.1 Tabela scenariuszy błędów

| Scenariusz | Kod HTTP | Typ wyjątku | Komunikat | Logowanie |
|-----------|----------|-------------|-----------|-----------|
| **Brak JWT tokenu** | 401 | - (middleware) | "Unauthorized" | Info |
| **Token nieprawidłowy** | 401 | - (middleware) | "Invalid token" | Warning |
| **Token wygasły** | 401 | - (middleware) | "Token expired" | Info |
| **Błąd algorytmu** | 500 | `Exception` | "An error occurred..." | Error + stack trace |

**Uwaga:** Scenariusz limitu 100 zestawów NIE występuje w tym endpointcie - jest sprawdzany w `POST /api/tickets`.

### 7.2 Implementacja w Handler

**Generowanie i zwrot:**
```csharp
public async Task<Contracts.Response> Handle(
    Contracts.Request request,
    CancellationToken cancellationToken)
{
    // 1. Walidacja żądania (minimalna - request pusty)
    var validationResult = await _validator.ValidateAsync(request, cancellationToken);
    if (!validationResult.IsValid)
    {
        throw new ValidationException(validationResult.Errors);
    }

    // 2. Generowanie losowych liczb
    var numbers = _ticketService.GenerateRandomNumbers();

    // 3. Logowanie dla audytu (opcjonalne)
    _logger.LogDebug(
        "Generated random numbers: {Numbers}",
        string.Join(", ", numbers));

    // 4. Zwrot jako propozycja
    return new Contracts.Response(numbers);
}
```

**Uwaga:** Brak sprawdzania limitu, brak dostępu do bazy - to tylko generator propozycji.

### 7.3 Globalna obsługa (ExceptionHandlingMiddleware)

**Obsługiwane wyjątki:**

1. **ValidationException (FluentValidation):**
   - Status: 400 Bad Request
   - Body: `{ "errors": { "fieldName": ["error1", "error2"] } }`
   - Uwaga: W tym endpointcie praktycznie nie występuje (request pusty)

2. **UnauthorizedException (custom):**
   - Status: 401 Unauthorized
   - Body: `{ "message": "Unauthorized" }`
   - Obsługiwane przez ASP.NET Core middleware

3. **Exception (inne - błędy algorytmu):**
   - Status: 500 Internal Server Error
   - Body: `{ "message": "An error occurred..." }`
   - Logowanie: Error level z pełnym stack trace

**Uwaga:** Brak DbUpdateException - endpoint nie wykonuje operacji na bazie danych.

### 7.4 Logowanie (Serilog)

**Poziomy logowania:**

- **Debug:** Pomyślne wygenerowanie liczb (opcjonalne)
  ```csharp
  _logger.LogDebug("Generated random numbers: {Numbers}",
      string.Join(", ", numbers));
  ```

- **Error:** Błędy algorytmu, nieoczekiwane wyjątki
  ```csharp
  _logger.LogError(ex, "Failed to generate random numbers");
  ```

**Uwaga:**
- Brak logowania limitu - nie jest sprawdzany w tym endpointcie
- Brak logowania zapisu do bazy - nie występuje
- Minimalny logging (Debug level) - endpoint jest bardzo prosty

**Lokalizacja logów:** `Logs/applog-{Date}.txt`

---

## 8. Rozważania dotyczące wydajności

### 8.1 Wąskie gardła

| Obszar | Potencjalny problem | Mitygacja |
|--------|-------------------|-----------|
| **Random generation** | `Random()` nie jest thread-safe | Użyć `Random.Shared` (C# 10+) - thread-safe |
| **Wysokie obciążenie** | Wielu użytkowników jednocześnie | Endpoint jest stateless - doskonale skaluje się |

**Uwaga:**
- ❌ COUNT query - nie występuje (brak sprawdzania limitu)
- ❌ INSERT - nie występuje (brak zapisu do bazy)
- ❌ Transakcja - nie występuje
- ❌ Generowanie ID - nie występuje

### 8.2 Optymalizacje

**1. Generator liczb - użycie `Random.Shared`:**
```csharp
public int[] GenerateRandomNumbers()
{
    return Enumerable.Range(1, 49)
        .OrderBy(x => Random.Shared.Next())
        .Take(6)
        .OrderBy(x => x)
        .ToArray();
}
```

**Korzyści:**
- Thread-safe (C# 10+)
- Lepsza wydajność (reużycie instancji)
- Brak potrzeby tworzenia nowych instancji Random

**2. Endpoint jest już zoptymalizowany:**
- Stateless - brak stanu między wywołaniami
- Brak I/O (database, file system, network)
- Tylko operacje w pamięci (~1ms)
- Doskonale skaluje się horyzontalnie

### 8.3 Indeksy wykorzystywane

**Brak** - endpoint nie wykonuje żadnych zapytań SQL, więc żadne indeksy nie są wykorzystywane.

### 8.4 Szacowana wydajność

- **Random generation:** ~0.5-1ms (CPU-bound, 49 liczb)
- **JSON serialization:** ~0.1-0.5ms (6 liczb)
- **TOTAL:** ~1-2ms (bez opóźnień sieciowych)

**Przepustowość:** ~500-1000 requestów/sekundę na standardowym serwerze (CPU-bound)

**Uwaga:** Endpoint jest ekstremalnie szybki - brak I/O, tylko operacje w pamięci.

---

## 9. Etapy wdrożenia

### 9.1 Krok 1: Przygotowanie struktury Feature

**Akcja:** Utworzenie folderu i plików zgodnie z architekturą Vertical Slice

**Lokalizacja:** `src/server/LottoTM.Server.Api/Features/Tickets/GenerateRandom/`

**Pliki do utworzenia:**
1. `Contracts.cs` - definicja Request i Response
2. `Endpoint.cs` - konfiguracja endpointa (Minimal API)
3. `Handler.cs` - logika biznesowa (MediatR handler)
4. `Validator.cs` - walidacja danych wejściowych (FluentValidation)

**Szablon struktury:**
```
Features/
└── Tickets/
    └── GenerateRandom/
        ├── Contracts.cs
        ├── Endpoint.cs
        ├── Handler.cs
        └── Validator.cs
```

---

### 9.2 Krok 2: Implementacja Contracts.cs

**Plik:** `Features/Tickets/GenerateRandom/Contracts.cs`

**Kod:**
```csharp
using MediatR;

namespace LottoTM.Server.Api.Features.Tickets.GenerateRandom;

public class Contracts
{
    /// <summary>
    /// Request for generating a random lottery ticket.
    /// UserId is extracted from JWT claims in the endpoint.
    /// </summary>
    public record Request(int UserId) : IRequest<Response>;

    /// <summary>
    /// Response with success message and generated ticket ID.
    /// </summary>
    public record Response(string Message, int TicketId);
}
```

**Uwagi:**
- `Request` przyjmuje tylko `UserId` (z JWT)
- `Response` zwraca komunikat + ID dla potencjalnego Location header

---

### 9.3 Krok 3: Implementacja Validator.cs

**Plik:** `Features/Tickets/GenerateRandom/Validator.cs`

**Kod:**
```csharp
using FluentValidation;

namespace LottoTM.Server.Api.Features.Tickets.GenerateRandom;

public class Validator : AbstractValidator<Contracts.Request>
{
    public Validator()
    {
        // Walidacja UserId (powinna być zawsze > 0 z JWT)
        RuleFor(x => x.UserId)
            .GreaterThan(0)
            .WithMessage("UserId musi być większy od 0");
    }
}
```

**Uwagi:**
- Minimalna walidacja (request praktycznie pusty)
- UserId powinien być zawsze poprawny (z JWT), ale dodajemy safeguard

---

### 9.4 Krok 4: Implementacja Service Interface i Class

**4.1 Interface:**

**Plik:** `Services/ITicketService.cs`

```csharp
namespace LottoTM.Server.Api.Services;

/// <summary>
/// Service for managing lottery tickets and related operations.
/// </summary>
public interface ITicketService
{
    /// <summary>
    /// Generates 6 random unique numbers in range 1-49 for lottery ticket.
    /// This is a pure generation method - does not interact with database.
    /// </summary>
    /// <returns>Array of 6 sorted unique numbers</returns>
    int[] GenerateRandomNumbers();
}
```

**Uwaga:** W tym endpointcie wykorzystujemy tylko metodę `GenerateRandomNumbers()`. Inne metody (`GetUserTicketCountAsync`, `CreateTicketWithNumbersAsync`) będą potrzebne w innych endpointach (np. `POST /api/tickets`).

**4.2 Implementation:**

**Plik:** `Services/TicketService.cs`

```csharp
namespace LottoTM.Server.Api.Services;

public class TicketService : ITicketService
{
    public int[] GenerateRandomNumbers()
    {
        // Używamy Random.Shared (C# 10+) dla thread-safety
        return Enumerable.Range(1, 49)
            .OrderBy(x => Random.Shared.Next())
            .Take(6)
            .OrderBy(x => x) // Sortowanie dla czytelności
            .ToArray();
    }

    // Inne metody (GetUserTicketCountAsync, CreateTicketWithNumbersAsync)
    // będą dodane w kolejnych endpointach
}
```

**4.3 Rejestracja w DI Container:**

**Plik:** `Program.cs`

```csharp
// W sekcji services.AddScoped()
builder.Services.AddScoped<ITicketService, TicketService>();
```

---

### 9.5 Krok 5: Implementacja Handler.cs

**Plik:** `Features/Tickets/GenerateRandom/Handler.cs`

**Kod:**
```csharp
using FluentValidation;
using LottoTM.Server.Api.Services;
using MediatR;

namespace LottoTM.Server.Api.Features.Tickets.GenerateRandom;

public class Handler : IRequestHandler<Contracts.Request, Contracts.Response>
{
    private readonly ITicketService _ticketService;
    private readonly IValidator<Contracts.Request> _validator;
    private readonly ILogger<Handler> _logger;

    public Handler(
        ITicketService ticketService,
        IValidator<Contracts.Request> validator,
        ILogger<Handler> logger)
    {
        _ticketService = ticketService;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Contracts.Response> Handle(
        Contracts.Request request,
        CancellationToken cancellationToken)
    {
        // 1. Walidacja żądania (minimalna - request pusty)
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // 2. Generowanie losowych liczb
        var numbers = _ticketService.GenerateRandomNumbers();

        // 3. Opcjonalne logowanie
        _logger.LogDebug(
            "Generated random numbers: {Numbers}",
            string.Join(", ", numbers));

        // 4. Zwrot jako propozycja (bez zapisu do bazy)
        return new Contracts.Response(numbers);
    }
}
```

**Uwagi:**
- Brak walidacji limitu - to tylko generator propozycji
- Brak zapisu do bazy - użytkownik zdecyduje czy zapisać
- Brak logowania UserId - request nie zawiera tego parametru
- Prosta implementacja - tylko generowanie i zwrot

---

### 9.6 Krok 6: Implementacja Endpoint.cs

**Plik:** `Features/Tickets/GenerateRandom/Endpoint.cs`

**Kod:**
```csharp
using MediatR;
using Microsoft.AspNetCore.Authorization;

namespace LottoTM.Server.Api.Features.Tickets.GenerateRandom;

public static class Endpoint
{
    public static void AddEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("api/tickets/generate-random",
            [Authorize] async (IMediator mediator) =>
            {
                // Utworzenie żądania (pusty request)
                var request = new Contracts.Request();

                // Wysłanie do MediatR
                var result = await mediator.Send(request);

                // Zwrot 200 OK z wygenerowanymi liczbami
                return Results.Ok(result);
            })
        .WithName("GenerateRandomTicket")
        .Produces<Contracts.Response>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
        .WithOpenApi(operation =>
        {
            operation.Summary = "Generuje losowy zestaw liczb LOTTO (propozycja)";
            operation.Description =
                "Generuje pojedynczy losowy zestaw 6 unikalnych liczb (1-49) " +
                "i zwraca jako propozycję. NIE zapisuje do bazy danych. " +
                "Użytkownik musi ręcznie zapisać używając POST /api/tickets jeśli chce zachować zestaw.";
            return operation;
        });
    }
}
```

**Uwagi:**
- Atrybut `[Authorize]` wymaga JWT tokenu
- Request pusty - brak parametrów, brak UserId
- Zwrot `200 OK` (nie 201 Created) - nie jest tworzony zasób
- Brak Location header - nie ma zasobu do wskazania
- Dokumentacja OpenAPI wyjaśnia że to tylko propozycja

---

### 9.7 Krok 7: Rejestracja w Program.cs

**Plik:** `Program.cs`

**Dodanie w sekcji rejestracji endpointów:**

```csharp
// Po zarejestrowaniu innych endpointów (np. ApiVersion)
LottoTM.Server.Api.Features.Tickets.GenerateRandom.Endpoint.AddEndpoint(app);
```

**Dodanie w sekcji DI (MediatR handlers):**

```csharp
// MediatR automatycznie znajdzie Handler przez assembly scan
builder.Services.AddMediatR(cfg => 
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

// Ticket Service
builder.Services.AddScoped<ITicketService, TicketService>();
```

**Pełna lokalizacja w Program.cs:**

```csharp
// ... (istniejący kod)

// Rejestracja services
builder.Services.AddScoped<ITicketService, TicketService>();

// ... (istniejący kod z MediatR, FluentValidation)

var app = builder.Build();

// ... (middleware)

// Rejestracja endpointów
LottoTM.Server.Api.Features.ApiVersion.Endpoint.AddEndpoint(app);
LottoTM.Server.Api.Features.Tickets.GenerateRandom.Endpoint.AddEndpoint(app);

// ... (reszta konfiguracji)

app.Run();
```

---

### 9.8 Krok 8: Testowanie manualne

**8.1 Przygotowanie:**
1. Uruchomienie aplikacji: `dotnet run` w folderze `LottoTM.Server.Api`
2. Sprawdzenie dostępności Swagger UI: `http://localhost:5000/swagger`

**8.2 Test Case 1: Sukces (200 OK)**

**Request:**
```http
POST http://localhost:5000/api/tickets/generate-random
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Expected Response:**
```json
HTTP/1.1 200 OK
Content-Type: application/json

{
  "numbers": [7, 19, 22, 33, 38, 45]
}
```

**Weryfikacja:**
- Response zawiera tablicę 6 liczb
- Wszystkie liczby w zakresie 1-49
- Wszystkie liczby unikalne
- **Brak zapisu do bazy** - to tylko propozycja

**8.3 Test Case 2: Brak autoryzacji (401)**

**Request:**
```http
POST http://localhost:5000/api/tickets/generate-random
(brak header Authorization)
```

**Expected Response:**
```json
HTTP/1.1 401 Unauthorized

{
  "message": "Unauthorized"
}
```

**8.4 Test Case 3: Wielokrotne wywołanie (losowość)**

**Request:** Wywołaj endpoint 10x z tym samym tokenem

**Weryfikacja:**
- Każde wywołanie zwraca inne liczby (losowość)
- Liczby w każdym zestawie są unikalne i w zakresie 1-49
- **Brak zapisu do bazy** - każde wywołanie tylko generuje liczby

**Uwaga:** Test Case "Limit osiągnięty" NIE występuje - endpoint nie sprawdza limitu.

---

### 9.9 Krok 9: Testowanie jednostkowe (opcjonalne, rekomendowane)

**9.1 Test Service - GenerateRandomNumbers:**

**Plik:** `tests/server/LottoTM.Server.Api.Tests/Services/TicketServiceTests.cs`

```csharp
[Fact]
public void GenerateRandomNumbers_ShouldReturn6UniqueNumbersInRange()
{
    // Arrange
    var service = new TicketService();

    // Act
    var numbers = service.GenerateRandomNumbers();

    // Assert
    Assert.Equal(6, numbers.Length);
    Assert.All(numbers, n => Assert.InRange(n, 1, 49));
    Assert.Equal(6, numbers.Distinct().Count()); // Wszystkie unikalne
}

[Fact]
public void GenerateRandomNumbers_CalledMultipleTimes_ShouldReturnDifferentSets()
{
    // Arrange
    var service = new TicketService();

    // Act
    var set1 = service.GenerateRandomNumbers();
    var set2 = service.GenerateRandomNumbers();

    // Assert
    Assert.NotEqual(set1, set2); // Statystycznie różne
}
```

**9.2 Test Handler - Generowanie:**

**Plik:** `tests/server/LottoTM.Server.Api.Tests/Features/Tickets/GenerateRandom/HandlerTests.cs`

```csharp
[Fact]
public async Task Handle_ShouldReturnGeneratedNumbers()
{
    // Arrange
    var expectedNumbers = new[] { 1, 2, 3, 4, 5, 6 };
    _mockTicketService.Setup(s => s.GenerateRandomNumbers())
        .Returns(expectedNumbers);

    var handler = new Handler(_mockTicketService.Object, _mockValidator.Object, _mockLogger.Object);
    var request = new Contracts.Request();

    // Act
    var response = await handler.Handle(request, default);

    // Assert
    Assert.Equal(expectedNumbers, response.Numbers);
}

[Fact]
public async Task Handle_ShouldNotInteractWithDatabase()
{
    // Arrange
    var handler = new Handler(_mockTicketService.Object, _mockValidator.Object, _mockLogger.Object);
    var request = new Contracts.Request();

    // Act
    await handler.Handle(request, default);

    // Assert
    // Weryfikacja że żadne metody dostępu do bazy NIE zostały wywołane
    _mockTicketService.Verify(s => s.GenerateRandomNumbers(), Times.Once);
    _mockTicketService.VerifyNoOtherCalls(); // Brak innych wywołań (np. do bazy)
}
```

**Uwaga:** Usunięto testy sprawdzania limitu i zapisu do bazy - nie są potrzebne w tym endpointcie.

---

### 9.10 Krok 10: Dokumentacja i wdrożenie

**10.1 Aktualizacja README.md:**

Dodać sekcję z przykładem użycia endpointa:

```markdown
### Generowanie losowego zestawu LOTTO (propozycja)

**Endpoint:** `POST /api/tickets/generate-random`

**Opis:** Generuje pojedynczy losowy zestaw 6 liczb (1-49) jako propozycję. NIE zapisuje do bazy.

**Przykład:**
```bash
curl -X POST http://localhost:5000/api/tickets/generate-random \
  -H "Authorization: Bearer <your-jwt-token>"
```

**Odpowiedź:**
```json
{
  "numbers": [7, 19, 22, 33, 38, 45]
}
```

**Uwaga:** Użytkownik musi ręcznie zapisać zestaw używając `POST /api/tickets` jeśli chce go zachować.
```

**10.2 Commit do repozytorium:**

```bash
git add .
git commit -m "feat: Add POST /api/tickets/generate-random endpoint (preview only)

- Implemented Vertical Slice architecture with MediatR
- Added TicketService with random number generation
- Returns proposal only - does NOT save to database
- Added comprehensive error handling and logging
- Included Swagger/OpenAPI documentation"
```

**10.3 Wdrożenie na środowisko testowe:**

1. Merge do branch `develop`
2. Uruchomienie CI/CD pipeline
3. Deploy na środowisko test/staging
4. Testy E2E (Postman collection lub automated tests)
5. Weryfikacja logów w Serilog
6. Merge do `main` po pomyślnych testach

---

## 10. Podsumowanie

### 10.1 Checklist implementacji

- [ ] Utworzenie folderu `Features/Tickets/GenerateRandom/`
- [ ] Implementacja `Contracts.cs` (Request, Response)
- [ ] Implementacja `Validator.cs` (walidacja UserId)
- [ ] Utworzenie `Services/ITicketService.cs` (interface)
- [ ] Implementacja `Services/TicketService.cs` (GetCount, Generate, Create)
- [ ] Rejestracja `ITicketService` w DI container (Program.cs)
- [ ] Implementacja `Handler.cs` (logika biznesowa, limit check)
- [ ] Implementacja `Endpoint.cs` (Minimal API, JWT claims)
- [ ] Rejestracja endpointa w `Program.cs`
- [ ] Testy manualne (Swagger/Postman): 201, 400, 401
- [ ] Weryfikacja w bazie danych (Tickets, TicketNumbers)
- [ ] Testy jednostkowe (Service, Handler)
- [ ] Dokumentacja w README.md
- [ ] Commit i deploy

### 10.2 Kluczowe decyzje techniczne

1. **Vertical Slice Architecture:** Każdy feature jest niezależnym "slajsem" z własnym Contracts, Endpoint, Handler, Validator
2. **MediatR:** Separacja endpointa od logiki biznesowej, łatwiejsze testowanie
3. **FluentValidation:** Deklaratywna walidacja z jasnymi komunikatami błędów
4. **ITicketService:** Abstrakcja logiki ticketów dla reużywalności (przyszłe endpointy: POST /api/tickets, PUT, etc.)
5. **Random.Shared:** Thread-safe generator liczb (C# 10+)
6. **Bulk insert:** Dodanie 6 liczb za pomocą AddRangeAsync dla wydajności
7. **GUID dla Ticket.Id:** Bezpieczeństwo (niemożliwe do odgadnięcia)
8. **Location header:** RESTful practice - zwrot URI nowo utworzonego zasobu
9. **Comprehensive logging:** Information/Warning/Error levels dla monitoringu

### 10.3 Możliwe rozszerzenia (poza MVP)

1. **Rate limiting:** Throttling (np. 10 requestów/minutę) dla ochrony przed spamem
2. **Cached count:** Kolumna `User.TicketCount` dla eliminacji COUNT query
3. **Background job:** Generowanie wielu zestawów asynchronicznie (SignalR notifications)
4. **Duplicate check:** Sprawdzenie czy wygenerowany zestaw już istnieje u użytkownika (obecnie akceptujemy duplikaty losowe)
5. **Statistics API:** Endpoint zwracający statystyki użytkownika (liczba zestawów, najczęstsze liczby, etc.)

---

**Dokument końcowy - gotowy do implementacji przez zespół developerski.**
