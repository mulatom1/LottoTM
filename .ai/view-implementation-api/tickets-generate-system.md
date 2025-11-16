# Plan Wdrożenia Endpointu API: POST /api/tickets/generate-system

**Data:** 2025-11-16
**Wersja:** 1.2
**Endpoint:** POST /api/tickets/generate-system
**Moduł:** Tickets (Zestawy)

---

## 1. Przegląd punktu końcowego

### Opis
Endpoint generuje 9 zestawów liczb LOTTO (po 6 liczb każdy) według algorytmu systemowego, który gwarantuje pokrycie wszystkich liczb od 1 do 49. Wygenerowane zestawy są zwracane jako **propozycja** (bez zapisu do bazy danych).

### Cel biznesowy
Umożliwienie użytkownikom szybkiego wygenerowania zestawów systemowych, które zapewniają pełne pokrycie przestrzeni liczb LOTTO (1-49) w minimalnej liczbie zestawów (9). Użytkownik może zobaczyć podgląd i zdecydować czy zapisać zestawy.

### Charakterystyka
- **Metoda HTTP:** POST
- **Ścieżka:** `/api/tickets/generate-system`
- **Autoryzacja:** Wymagany JWT Bearer token
- **Payload żądania:** Brak (empty body)
- **Zwrot:** Propozycja 9 zestawów (bez zapisu do bazy)
- **Idempotentność:** Nie - każde wywołanie generuje nowe, losowe zestawy
- **Sprawdzanie limitu:** NIE - endpoint tylko generuje propozycję, nie zapisuje

---

## 2. Szczegóły żądania

### 2.1 Metoda i URL
```http
POST /api/tickets/generate-system
```

### 2.2 Nagłówki
| Nagłówek | Typ | Wymagany | Opis |
|----------|-----|----------|------|
| `Authorization` | string | Tak | Format: `Bearer <JWT_TOKEN>` |
| `Content-Type` | string | Nie | `application/json` (body pusty, ale header opcjonalny) |

### 2.3 Parametry
**Brak parametrów URL ani query string.**

### 2.4 Request Body
**Brak - żądanie nie wymaga body.**

### 2.5 Przykład żądania
```http
POST /api/tickets/generate-system HTTP/1.1
Host: localhost:5000
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

---

## 3. Wykorzystywane typy

### 3.1 Contracts (DTOs)

#### Request
```csharp
namespace LottoTM.Server.Api.Features.Tickets.GenerateSystem;

public class Contracts
{
    /// <summary>
    /// Request for generating 9 system tickets covering all numbers 1-49.
    /// No parameters required - UserId extracted from JWT.
    /// </summary>
    public record Request : IRequest<Response>;
    
    /// <summary>
    /// Response containing array of 9 generated tickets (preview/proposal).
    /// Each ticket contains 6 numbers.
    /// User must manually save using POST /api/tickets if they want to keep them.
    /// </summary>
    public record Response(List<TicketDto> Tickets);
    
    /// <summary>
    /// DTO representing a single generated ticket with only numbers.
    /// </summary>
    public record TicketDto(List<int> Numbers);
}
```

### 3.2 Command Model (Handler Input)
```csharp
// Request zawiera wszystkie niezbędne dane
// UserId będzie wyodrębniany z ClaimsPrincipal w Handlerze
```

### 3.3 Entity Models (Database)
- **Ticket** (z `Entities/Ticket.cs`)
  - `int Id` - Primary key
  - `int UserId` - Foreign key do Users
  - `DateTime CreatedAt` - UTC timestamp
  - `ICollection<TicketNumber> Numbers` - Navigation property

- **TicketNumber** (z `Entities/TicketNumber.cs`)
  - `int Id` - Primary key
  - `int TicketId` - Foreign key to Tickets
  - `int Number` - Lottery number (1-49)
  - `byte Position` - Position in ticket (1-6)

---

## 4. Szczegóły odpowiedzi

### 4.1 Odpowiedź sukcesu (200 OK)

#### Status Code
`200 OK`

#### Response Body
```json
{
  "tickets": [
    {
      "numbers": [7, 15, 23, 31, 39, 47]
    },
    {
      "numbers": [2, 10, 18, 26, 34, 42]
    },
    {
      "numbers": [3, 11, 19, 27, 35, 43]
    }
    // ... pozostałe 6 zestawów
  ]
}
```

**Uwaga:** To jest tylko **propozycja** - zestawy nie są zapisane do bazy. Użytkownik musi ręcznie zapisać każdy zestaw używając `POST /api/tickets` jeśli chce je zachować.

### 4.2 Odpowiedzi błędów

#### 401 Unauthorized
**Przyczyna:** Brak tokenu JWT lub token nieprawidłowy/wygasły

```json
{
  "status": 401,
  "title": "Unauthorized",
  "detail": "Token JWT jest nieprawidłowy lub wygasł"
}
```

#### 500 Internal Server Error
**Przyczyna:** Błąd algorytmu generowania

```json
{
  "status": 500,
  "title": "Internal Server Error",
  "detail": "Wystąpił nieoczekiwany błąd podczas generowania zestawów"
}
```

**Uwaga:**
- Usunięto błąd 400 Bad Request (limit) - endpoint nie sprawdza limitu, tylko generuje propozycję
- Sprawdzenie limitu 100 zestawów następuje dopiero przy zapisie przez `POST /api/tickets`

---

## 5. Przepływ danych

### 5.1 Diagram przepływu

```
┌─────────────┐
│   Client    │
│  (React UI) │
└──────┬──────┘
       │ POST /api/tickets/generate-system
       │ Authorization: Bearer <JWT>
       ▼
┌─────────────────────────────────────────┐
│  Endpoint (GenerateSystem/Endpoint.cs)  │
│  - Weryfikacja autoryzacji (JWT)        │
└──────┬──────────────────────────────────┘
       │ Wywołanie MediatR
       ▼
┌─────────────────────────────────────────┐
│  Validator (Validator.cs)               │
│  - Minimalna walidacja (request pusty)  │
└──────┬──────────────────────────────────┘
       │
       ▼
┌─────────────────────────────────────────┐
│  Handler (Handler.cs)                   │
│  1. Generuje 9 zestawów (algorytm)      │
│  2. Waliduje pokrycie liczb 1-49        │
│  3. Zwraca Response z liczbami          │
└──────┬──────────────────────────────────┘
       │ Response (9 zestawów)
       ▼
┌─────────────────────────────────────────┐
│  ExceptionHandlingMiddleware            │
│  - Loguje błędy (Serilog)               │
│  - Formatuje odpowiedzi błędów          │
└──────┬──────────────────────────────────┘
       │ JSON Response (200 OK)
       ▼
┌─────────────┐
│   Client    │
│  (200 OK)   │
│  - Wyświetla 9 zestawów                 │
│  - Użytkownik decyduje czy zapisać      │
│  - Zapis: 9x POST /api/tickets          │
└─────────────┘
```

### 5.2 Interakcje z bazą danych

**Brak interakcji z bazą danych** - endpoint tylko generuje liczby i zwraca je jako propozycję.

**Uwaga:**
- Endpoint nie wykonuje żadnych zapytań SQL
- Nie sprawdza limitu zestawów (to robi `POST /api/tickets`)
- Nie zapisuje danych do tabel `Tickets` ani `TicketNumbers`
- Jest to czysta operacja obliczeniowa (generation-only)

### 5.3 Algorytm generowania systemowego

**Cel:** Pokryć wszystkie liczby 1-49 w 9 zestawach (każda liczba pojawia się dokładnie raz, 4 liczby się powtarzają)

**Pseudokod:**
```
1. Utwórz 9 pustych zestawów (każdy ma 6 pozycji)
2. Utwórz pulę liczb [1..49]
3. Dla każdej pozycji 1-5:
   a. Przetasuj pulę losowo
   b. Przypisz pierwsze 9 liczb do zestawów 1-9 na pozycji i
   c. Usuń te 9 liczb z puli (pozostaje 4 liczby)
4. Pozostałe 4 liczby przypisz losowo do zestawów 1-4 na pozycji 6
5. Zestawy 5-9 na pozycji 6 otrzymują losowe liczby z już użytych
```

**Implementacja C#:**
```csharp
private List<int[]> GenerateSystemTickets()
{
    var tickets = new List<int[]>();
    var random = new Random();
    var pool = Enumerable.Range(1, 49).ToList();

    // Inicjalizacja 9 zestawów (każdy ma 6 miejsc)
    for (int i = 0; i < 9; i++)
    {
        tickets.Add(new int[6]);
    }

    // Wypełnienie pozycji 1-5 (5 × 9 = 45 liczb)
    for (int position = 0; position < 5; position++)
    {
        // Losowe przetasowanie puli
        pool = pool.OrderBy(x => random.Next()).ToList();
        
        // Przypisanie pierwszych 9 liczb do zestawów
        for (int ticketIndex = 0; ticketIndex < 9; ticketIndex++)
        {
            tickets[ticketIndex][position] = pool[ticketIndex];
        }
        
        // Usunięcie użytych liczb
        pool.RemoveRange(0, 9);
    }

    // Po 5 iteracjach: wykorzystano 45 liczb, pozostało 4
    // Przypisanie pozostałych 4 liczb do pierwszych 4 zestawów (pozycja 6)
    var remaining = pool.ToList();
    remaining = remaining.OrderBy(x => random.Next()).ToList();
    
    for (int i = 0; i < 4; i++)
    {
        tickets[i][5] = remaining[i];
    }

    // Zestawy 5-9 (pozycja 6): losuj z już użytych liczb
    var usedNumbers = tickets.SelectMany(t => t.Take(5)).ToList();
    usedNumbers = usedNumbers.OrderBy(x => random.Next()).ToList();
    
    for (int i = 4; i < 9; i++)
    {
        tickets[i][5] = usedNumbers[i - 4];
    }

    return tickets;
}
```

**Walidacja pokrycia:**
```csharp
// Weryfikacja: wszystkie liczby 1-49 pojawiają się co najmniej raz
var allNumbers = tickets.SelectMany(t => t).Distinct().OrderBy(n => n).ToList();
if (allNumbers.Count != 49 || allNumbers.First() != 1 || allNumbers.Last() != 49)
{
    throw new InvalidOperationException("Algorytm nie pokrył wszystkich liczb 1-49");
}
```

---

## 6. Względy bezpieczeństwa

### 6.1 Uwierzytelnianie
- **Mechanizm:** JWT Bearer token
- **Middleware:** ASP.NET Core Authentication (konfiguracja w `Program.cs`)
- **Endpoint:** Wymaga atrybutu `.RequireAuthorization()`
- **Token Claims:**
  - `UserId` - identyfikator użytkownika
  - `Email` - email użytkownika (opcjonalnie)
  - `IsAdmin` - flaga administratora (nie używana w tym endpoincie)

### 6.2 Autoryzacja
- **Poziom zasobów:** Użytkownik może generować zestawy tylko dla siebie
- **UserId:** Pobierany z `ClaimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier)`
- **Brak manipulacji:** UserId nie jest przekazywany w żądaniu, tylko w JWT

### 6.3 Walidacja danych
| Walidacja | Opis | Mechanizm |
|-----------|------|-----------|
| JWT Token | Sprawdzenie obecności i ważności | ASP.NET Core Middleware |
| Limit zestawów | Użytkownik ma ≤ 91 zestawów | FluentValidation w Validator.cs |
| Integralność algorytmu | Pokrycie liczb 1-49 | Walidacja post-generation w Handler |
| Unikalność liczb | Brak duplikatów w zestawie | Gwarantowane przez algorytm |
| Zakres liczb | 1-49 | Gwarantowane przez algorytm |

### 6.4 Ochrona przed atakami

#### SQL Injection
- **Ochrona:** N/A - endpoint nie wykonuje zapytań SQL

#### JWT Tampering
- **Ochrona:** Podpis HMAC SHA256 weryfikowany przez middleware
- **Konfiguracja:** `TokenValidationParameters` w `Program.cs`

#### Brute-force Generation
- **Ryzyko:** Użytkownik wywołuje endpoint wiele razy
- **Ochrona:** Endpoint jest stateless i szybki (~5-10ms)
- **Rekomendacja przyszłości:** Rate limiting (np. 100 wywołań/minutę)

#### DoS via Computation
- **Ryzyko:** Algorytm generowania jest kosztowny
- **Ochrona:** Algorytm ma stały czas ~5-10ms (O(1))
- **Bez wpływu na bazę:** Endpoint nie wykonuje I/O

---

## 7. Obsługa błędów

### 7.1 Tabela błędów

| Kod | Nazwa | Przyczyna | Komunikat | Obsługa |
|-----|-------|-----------|-----------|---------|
| 401 | Unauthorized | Brak/nieprawidłowy JWT | "Token JWT jest nieprawidłowy lub wygasł" | Middleware autoryzacji |
| 500 | Internal Server Error | Błąd algorytmu | "Błąd generowania systemowego - spróbuj ponownie" | Try-catch w Handler |

**Uwaga:** Scenariusze limitu zestawów i błędów bazy danych NIE występują - endpoint nie zapisuje do bazy.

### 7.2 Strategie obsługi

#### Algorithm Validation Error (500)
```csharp
// W Handler.cs po wygenerowaniu
var allNumbers = generatedTickets.SelectMany(t => t).Distinct().Count();
if (allNumbers != 49)
{
    _logger.LogCritical("System ticket algorithm failed - coverage: {Count}/49", allNumbers);
    throw new InvalidOperationException("Błąd algorytmu generowania - spróbuj ponownie");
}
```

### 7.3 Logging (Serilog)

**Poziomy logowania:**
- **Information:** Sukces generowania (liczba zestawów, UserId)
- **Warning:** Próba generowania przy limicie >91
- **Error:** Błędy bazy danych, wyjątki walidacji
- **Critical:** Błąd algorytmu (niepokrycie liczb 1-49)

**Przykład:**
```csharp
_logger.LogDebug(
    "Generated 9 system tickets for UserId={UserId}, TotalTickets={TotalCount}", 
    userId, 
    newTotalCount
);
```

---

## 8. Rozważania dotyczące wydajności

### 8.1 Potencjalne wąskie gardła

| Obszar | Wąskie gardło | Wpływ | Mitygacja |
|--------|---------------|-------|-----------|
| Baza danych | Bulk insert 63 rekordów (9+54) | Średni | `AddRangeAsync()` + transakcja |
| Algorytm | Generowanie 9 zestawów | Niski | O(1) - stały czas |
| Query limitu | COUNT na Tickets | Niski | Indeks IX_Tickets_UserId |
| Transakcja | Lock na tabeli Tickets | Niski | Krótki czas trwania |

### 8.2 Optymalizacje

#### Bulk Insert
```csharp
// Zamiast 9 osobnych SaveChangesAsync():
_context.Tickets.AddRange(tickets); // Batching
await _context.SaveChangesAsync();  // Jedna roundtrip

// EF Core automatycznie wykryje TicketNumbers przez navigation property
```

#### Eager Loading (nie dotyczy zapisu)
```csharp
// Przy zwracaniu Response - nie trzeba osobnych zapytań o Numbers
// EF Core już ma je w pamięci po AddRange
var response = new Response(
    tickets.Select(t => new TicketDto(
        t.Numbers.OrderBy(n => n.Position).Select(n => n.Number).ToList()
    )).ToList()
);
```

#### Indeksowanie
- **IX_Tickets_UserId:** Już istnieje (z db-plan.md)
- **IX_TicketNumbers_TicketId:** Już istnieje
- **Brak dodatkowych indeksów:** Nie są potrzebne

### 8.3 Metryki wydajności (szacunkowe)

| Operacja | Czas (ms) | Notatki |
|----------|-----------|---------|
| Algorytm generowania | 5-10 ms | In-memory, CPU-bound |
| Walidacja pokrycia | <1 ms | Sprawdzenie 49 liczb |
| JSON serialization | <1 ms | 9 zestawów × 6 liczb |
| **Całkowity czas** | **<15 ms** | Brak I/O operations |

**Przepustowość:** ~100-200 requestów/sekundę na standardowym serwerze (CPU-bound)

**Uwaga:** Endpoint jest ekstremalnie szybki - brak I/O, tylko operacje w pamięci.

---

## 9. Etapy wdrożenia

### Faza 1: Struktura projektu i kontrakty

#### Krok 1.1: Utworzenie struktury katalogów
```
src/server/LottoTM.Server.Api/Features/Tickets/GenerateSystem/
├── Contracts.cs
├── Validator.cs
├── Handler.cs
└── Endpoint.cs
```

**Polecenie:**
```bash
mkdir -p src/server/LottoTM.Server.Api/Features/Tickets/GenerateSystem
```

#### Krok 1.2: Implementacja Contracts.cs
**Plik:** `Features/Tickets/GenerateSystem/Contracts.cs`

**Zawartość:**
```csharp
using MediatR;

namespace LottoTM.Server.Api.Features.Tickets.GenerateSystem;

public class Contracts
{
    /// <summary>
    /// Request for generating 9 system tickets covering all numbers 1-49.
    /// No parameters required - this is a simple generation endpoint.
    /// </summary>
    public record Request : IRequest<Response>;

    /// <summary>
    /// Response containing array of 9 generated tickets (preview/proposal).
    /// Each ticket contains 6 numbers.
    /// User must manually save using POST /api/tickets if they want to keep them.
    /// </summary>
    public record Response(List<TicketDto> Tickets);

    /// <summary>
    /// DTO representing a single generated ticket with only numbers.
    /// </summary>
    public record TicketDto(List<int> Numbers);
}
```

---

### Faza 2: Walidacja

#### Krok 2.1: Implementacja Validator.cs
**Plik:** `Features/Tickets/GenerateSystem/Validator.cs`

**Zawartość:**
```csharp
using FluentValidation;
using LottoTM.Server.Api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LottoTM.Server.Api.Features.Tickets.GenerateSystem;

/// <summary>
/// Minimal validator for system tickets generation.
/// No business logic validation - request is empty.
/// </summary>
public class Validator : AbstractValidator<Contracts.Request>
{
    public Validator()
    {
        // Request is empty - minimal validation
        // No limit check - this is only a preview/proposal generator
    }
}
```

**Uwaga:**
- Validator jest minimalny - request nie ma parametrów
- Brak sprawdzania limitu - to tylko generator propozycji
- Brak dependency na AppDbContext lub IHttpContextAccessor

---

### Faza 3: Logika biznesowa (Handler)

#### Krok 3.1: Implementacja Handler.cs
**Plik:** `Features/Tickets/GenerateSystem/Handler.cs`

**Zawartość:**
```csharp
using FluentValidation;
using LottoTM.Server.Api.Entities;
using LottoTM.Server.Api.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LottoTM.Server.Api.Features.Tickets.GenerateSystem;

/// <summary>
/// Handles generation of 9 system tickets covering all lottery numbers 1-49.
/// Each ticket contains 6 unique numbers. Algorithm ensures all 49 numbers
/// appear at least once across the 9 tickets.
/// Returns proposal only - does NOT save to database.
/// </summary>
public class GenerateSystemTicketsHandler : IRequestHandler<Contracts.Request, Contracts.Response>
{
    private readonly IValidator<Contracts.Request> _validator;
    private readonly ILogger<GenerateSystemTicketsHandler> _logger;

    public GenerateSystemTicketsHandler(
        IValidator<Contracts.Request> validator,
        ILogger<GenerateSystemTicketsHandler> logger)
    {
        _validator = validator;
        _logger = logger;
    }

    public async Task<Contracts.Response> Handle(Contracts.Request request, CancellationToken cancellationToken)
    {
        // 1. Validate request (minimal - request is empty)
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // 2. Generate 9 system tickets
        var generatedNumbers = GenerateSystemTickets();

        // 3. Validate coverage (all numbers 1-49 must appear)
        ValidateCoverage(generatedNumbers);

        // 4. Optional logging
        _logger.LogDebug("Generated 9 system tickets");

        // 5. Build response with generated numbers only (no database save)
        var response = new Contracts.Response(
            generatedNumbers.Select(numbers => new Contracts.TicketDto(
                numbers.ToList()
            )).ToList()
        );

        return response;
    }

    /// <summary>
    /// Generates 9 tickets covering all numbers 1-49 using system algorithm.
    /// Algorithm ensures each number 1-49 appears at least once.
    /// </summary>
    private List<int[]> GenerateSystemTickets()
    {
        var tickets = new List<int[]>();
        var random = new Random();
        var pool = Enumerable.Range(1, 49).ToList();

        // Initialize 9 empty tickets (6 numbers each)
        for (int i = 0; i < 9; i++)
        {
            tickets.Add(new int[6]);
        }

        // Fill positions 1-5 (5 × 9 = 45 numbers used)
        for (int position = 0; position < 5; position++)
        {
            // Shuffle pool randomly
            pool = pool.OrderBy(x => random.Next()).ToList();

            // Assign first 9 numbers to tickets
            for (int ticketIndex = 0; ticketIndex < 9; ticketIndex++)
            {
                tickets[ticketIndex][position] = pool[ticketIndex];
            }

            // Remove used numbers
            pool.RemoveRange(0, 9);
        }

        // After 5 iterations: 45 numbers used, 4 remaining
        // Assign remaining 4 numbers to first 4 tickets (position 6)
        var remaining = pool.ToList();
        remaining = remaining.OrderBy(x => random.Next()).ToList();

        for (int i = 0; i < 4; i++)
        {
            tickets[i][5] = remaining[i];
        }

        // Tickets 5-9 (position 6): randomly select from already used numbers
        var usedNumbers = tickets.SelectMany(t => t.Take(5)).ToList();
        usedNumbers = usedNumbers.OrderBy(x => random.Next()).Take(5).ToList();

        for (int i = 4; i < 9; i++)
        {
            tickets[i][5] = usedNumbers[i - 4];
        }

        return tickets;
    }

    /// <summary>
    /// Validates that generated tickets cover all numbers 1-49.
    /// Throws exception if validation fails.
    /// </summary>
    private void ValidateCoverage(List<int[]> tickets)
    {
        var allNumbers = tickets
            .SelectMany(t => t)
            .Distinct()
            .OrderBy(n => n)
            .ToList();

        if (allNumbers.Count < 49 || allNumbers.First() != 1 || allNumbers.Last() != 49)
        {
            var missing = Enumerable.Range(1, 49).Except(allNumbers).ToList();
            _logger.LogCritical(
                "System ticket algorithm failed. Coverage: {Count}/49. Missing: {Missing}",
                allNumbers.Count,
                string.Join(", ", missing)
            );
            throw new InvalidOperationException(
                "Błąd algorytmu generowania systemowego - spróbuj ponownie"
            );
        }

        // Validate each ticket has 6 unique numbers in range 1-49
        foreach (var ticket in tickets)
        {
            if (ticket.Length != 6 ||
                ticket.Distinct().Count() != 6 ||
                ticket.Any(n => n < 1 || n > 49))
            {
                _logger.LogCritical(
                    "Invalid ticket generated: {Ticket}",
                    string.Join(", ", ticket)
                );
                throw new InvalidOperationException("Błąd walidacji wygenerowanego zestawu");
            }
        }
    }
}
```

---

### Faza 4: Routing i endpoint

#### Krok 4.1: Implementacja Endpoint.cs
**Plik:** `Features/Tickets/GenerateSystem/Endpoint.cs`

**Zawartość:**
```csharp
using MediatR;

namespace LottoTM.Server.Api.Features.Tickets.GenerateSystem;

/// <summary>
/// Endpoint for generating 9 system lottery tickets covering all numbers 1-49 (proposal).
/// Requires JWT authentication. Returns preview only - does NOT save to database.
/// </summary>
public static class Endpoint
{
    public static void AddEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("api/tickets/generate-system", async (IMediator mediator) =>
        {
            var request = new Contracts.Request();
            var result = await mediator.Send(request);
            return Results.Ok(result);
        })
        .RequireAuthorization() // Requires JWT token
        .WithName("GenerateSystemTickets")
        .Produces<Contracts.Response>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
        .WithOpenApi(operation =>
        {
            operation.Summary = "Generowanie 9 zestawów systemowych (propozycja)";
            operation.Description =
                "Generuje 9 zestawów liczb LOTTO pokrywających wszystkie liczby 1-49 i zwraca jako propozycję. " +
                "Każdy zestaw zawiera 6 unikalnych liczb. NIE zapisuje do bazy danych. " +
                "Użytkownik musi ręcznie zapisać używając POST /api/tickets jeśli chce zachować zestawy.";
            return operation;
        });
    }
}
```

---

### Faza 5: Rejestracja w aplikacji

#### Krok 5.1: Aktualizacja Program.cs
**Plik:** `Program.cs`

**Dodaj przed `await app.RunAsync();`:**
```csharp
// Rejestracja endpointu (dodaj do istniejących)
LottoTM.Server.Api.Features.Tickets.GenerateSystem.Endpoint.AddEndpoint(app);
```

**Pełna sekcja rejestracji endpointów:**
```csharp
// Endpoints registration
LottoTM.Server.Api.Features.ApiVersion.Endpoint.AddEndpoint(app);
LottoTM.Server.Api.Features.Tickets.GenerateSystem.Endpoint.AddEndpoint(app);

await app.RunAsync();
```

**Uwaga:** Nie ma potrzeby rejestrowania `IHttpContextAccessor` - endpoint nie potrzebuje dostępu do HttpContext.

---

### Faza 6: Testy

#### Krok 6.1: Testy jednostkowe algorytmu
**Plik:** `tests/server/LottoTM.Server.Api.Tests/Features/Tickets/GenerateSystem/AlgorithmTests.cs`

**Przykładowe testy:**
```csharp
[Fact]
public void GenerateSystemTickets_ShouldCoverAllNumbers1To49()
{
    // Test pokrycia wszystkich liczb 1-49
}

[Fact]
public void GenerateSystemTickets_ShouldGenerateExactly9Tickets()
{
    // Test liczby zestawów
}

[Fact]
public void GenerateSystemTickets_EachTicketShouldHave6UniqueNumbers()
{
    // Test unikalności liczb w zestawie
}
```

#### Krok 6.2: Testy integracyjne endpointu
**Plik:** `tests/server/LottoTM.Server.Api.Tests/Features/Tickets/GenerateSystem/EndpointTests.cs`

**Przykładowe testy:**
```csharp
[Fact]
public async Task POST_GenerateSystem_WithValidToken_ReturnsOk()
{
    // Test sukcesu generowania - zwraca 200 OK z 9 zestawami
}

[Fact]
public async Task POST_GenerateSystem_WithoutToken_ReturnsUnauthorized()
{
    // Test autoryzacji - zwraca 401 Unauthorized
}

[Fact]
public async Task POST_GenerateSystem_DoesNotSaveToDatabase()
{
    // Test że endpoint NIE zapisuje do bazy - tylko generuje propozycję
}
```

**Uwaga:** Usunięto test walidacji limitu - endpoint nie sprawdza limitu.

---

### Faza 7: Dokumentacja i weryfikacja

#### Krok 7.1: Testowanie manualne (Swagger/Postman)
1. Uruchom aplikację: `dotnet run`
2. Przejdź do Swagger UI: `http://localhost:5000/swagger`
3. Zaloguj się przez `/api/auth/login` i skopiuj JWT token
4. Przetestuj endpoint:
   - **Authorization:** `Bearer <token>`
   - **Metoda:** POST
   - **URL:** `/api/tickets/generate-system`
   - **Body:** (pusty)
5. Sprawdź odpowiedź 200 OK z 9 zestawami

#### Krok 7.2: Weryfikacja odpowiedzi
```json
{
  "tickets": [
    {
      "numbers": [1, 7, 13, 19, 25, 31]
    },
    {
      "numbers": [2, 8, 14, 20, 26, 32]
    },
    // ... 7 więcej zestawów
  ]
}
```

**Weryfikacja:**
- Response zawiera 9 zestawów
- Każdy zestaw ma 6 liczb
- Wszystkie liczby 1-49 występują co najmniej raz
- **Brak zapisu do bazy** - to tylko propozycja

#### Krok 7.3: Aktualizacja dokumentacji API
- Zaktualizuj `api-plan.md` (jeśli są zmiany w implementacji)
- Dodaj przykłady żądań/odpowiedzi
- Udokumentuj edge cases (limit, błędy)

---

## 10. Checklist wdrożenia

### Pre-implementacja
- [ ] Przeczytaj i zrozum specyfikację z `api-plan.md` (linie 713-780)
- [ ] Przeanalizuj algorytm systemowy z `db-plan.md` (linie 374-422)
- [ ] Zrozum architekturę Vertical Slice (przykład: `Features/ApiVersion/`)
- [ ] Sprawdź istniejące encje `Ticket` i `TicketNumber`

### Implementacja
- [ ] Utwórz strukturę katalogów `Features/Tickets/GenerateSystem/`
- [ ] Zaimplementuj `Contracts.cs` (Request/Response/TicketDto - tylko liczby)
- [ ] Zaimplementuj `Validator.cs` (minimalny - request pusty)
- [ ] Zaimplementuj `Handler.cs` (algorytm + walidacja pokrycia)
- [ ] Zaimplementuj `Endpoint.cs` (routing + autoryzacja)
- [ ] Zarejestruj endpoint w `Program.cs`

### Testy
- [ ] Napisz testy jednostkowe algorytmu (pokrycie 1-49)
- [ ] Napisz testy integracyjne endpointu (sukces/błędy)
- [ ] Przetestuj manualnie przez Swagger UI
- [ ] Zweryfikuj że NIE zapisuje do bazy

### Optymalizacja i bezpieczeństwo
- [ ] Sprawdź wydajność generowania (≤15ms)
- [ ] Przetestuj autoryzację JWT (brak tokenu = 401)
- [ ] Sprawdź logowanie Serilog (debug/error)
- [ ] Code review (algorytm, walidacja pokrycia, obsługa błędów)

### Dokumentacja
- [ ] Dodaj komentarze XML do publicznych metod
- [ ] Zaktualizuj Swagger descriptions
- [ ] Udokumentuj edge cases (>91 zestawów)
- [ ] Dodaj przykłady do README (opcjonalnie)

---

## 11. Załączniki

### 11.1 Przykład wywołania (cURL)

```bash
curl -X POST "http://localhost:5000/api/tickets/generate-system" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." \
  -H "Content-Type: application/json"
```

### 11.2 Przykład odpowiedzi JSON

```json
{
  "message": "9 zestawów wygenerowanych i zapisanych pomyślnie",
  "generatedCount": 9,
  "tickets": [
    {
      "id": 1,
      "numbers": [7, 15, 23, 31, 39, 47],
      "createdAt": "2025-11-05T10:30:00Z"
    },
    {
      "id": 2,
      "numbers": [2, 10, 18, 26, 34, 42],
      "createdAt": "2025-11-05T10:30:00Z"
    },
    {
      "id": 3,
      "numbers": [1, 9, 17, 25, 33, 41],
      "createdAt": "2025-11-05T10:30:00Z"
    },
    {
      "id": 4,
      "numbers": [4, 12, 20, 28, 36, 44],
      "createdAt": "2025-11-05T10:30:00Z"
    },
    {
      "id": 5,
      "numbers": [6, 14, 22, 30, 38, 3],
      "createdAt": "2025-11-05T10:30:00Z"
    },
    {
      "id": 6,
      "numbers": [8, 16, 24, 32, 40, 11],
      "createdAt": "2025-11-05T10:30:00Z"
    },
    {
      "id": 7,
      "numbers": [5, 13, 21, 29, 37, 19],
      "createdAt": "2025-11-05T10:30:00Z"
    },
    {
      "id": 8,
      "numbers": [49, 48, 46, 45, 43, 27],
      "createdAt": "2025-11-05T10:30:00Z"
    },
    {
      "id": 9,
      "numbers": [35, 11, 19, 27, 35, 43],
      "createdAt": "2025-11-05T10:30:00Z"
    }
  ]
}
```

### 11.3 Referencje

- **Specyfikacja API:** `.ai/api-plan.md`, linie 713-780
- **Schemat bazy danych:** `.ai/db-plan.md`, linie 140-240 (Tickets/TicketNumbers)
- **Algorytm systemowy:** `.ai/db-plan.md`, linie 374-422
- **Tech stack:** `.ai/tech-stack.md`
- **Przykład implementacji:** `Features/ApiVersion/` (wzorzec Vertical Slice)

---

**Koniec planu wdrożenia**

**Autor:** AI Assistant  
**Data:** 2025-11-05  
**Status:** Gotowy do implementacji
