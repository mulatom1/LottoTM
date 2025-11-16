# Plan Wdrożenia Endpointa API: POST /api/tickets/generate-random

**Wersja:** 1.1  
**Data:** 2025-11-12  
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

**Kontekst biznesowy:**  
Użytkownik chce szybko wygenerować losowy zestaw liczb LOTTO bez ręcznego wyboru. System zwraca propozycję, którą użytkownik może zaakceptować i zapisać ręcznie używając `POST /api/tickets`.

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
    /// UserId is extracted from JWT claims in the endpoint.
    /// </summary>
    public record Request(int UserId) : IRequest<Response>;

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
    /// </summary>
    int[] GenerateRandomNumbers();
}
```

**Uwaga:** Usunięto metody związane z zapisem do bazy - generator tylko zwraca propozycję.

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

**Uwaga:** To jest tylko **propozycja** - liczby nie są zapisane do bazy. Użytkownik musi ręcznie zapisać używając `POST /api/tickets` jeśli chce zachować ten zestaw.

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

**Scenariusz:** Nieoczekiwany błąd podczas generowania liczb.

**Scenariusz:** Błąd bazy danych, błąd generatora liczb, inne nieoczekiwane wyjątki.

**Logowanie:** Pełny stack trace logowany przez Serilog do pliku `Logs/applog-{Date}.txt`.

---

## 5. Przepływ danych

### 5.1 Diagram sekwencji

```
Client                  Endpoint              Handler               Service             Database
  |                        |                     |                     |                     |
  |-- POST /generate-random-|                     |                     |                     |
  |   + JWT Token          |                     |                     |                     |
  |                        |                     |                     |                     |
  |                        |-- Extract UserId ---|                     |                     |
  |                        |   from JWT          |                     |                     |
  |                        |                     |                     |                     |
  |                        |-- Send Request ---->|                     |                     |
  |                        |   (UserId)          |                     |                     |
  |                        |                     |                     |                     |
  |                        |                     |-- Validate -------->|                     |
  |                        |                     |   (empty request)   |                     |
  |                        |                     |                     |                     |
  |                        |                     |-- Get Count ------->|                     |
  |                        |                     |   (UserId)          |                     |
  |                        |                     |                     |-- SELECT COUNT ---->|
  |                        |                     |                     |<-- count = X ------|
  |                        |                     |<-- count ----------|                     |
  |                        |                     |                     |                     |
  |                        |                     |-- IF count >= 100 ->|                     |
  |                        |                     |   THROW ValidationException               |
  |                        |                     |                     |                     |
  |                        |                     |-- Generate Random ->|                     |
  |                        |                     |   Numbers           |                     |
  |                        |                     |<-- [6 numbers] -----|                     |
  |                        |                     |                     |                     |
  |                        |                     |-- Create Ticket --->|                     |
  |                        |                     |   (UserId, numbers) |                     |
  |                        |                     |                     |-- BEGIN TRANSACTION->|
  |                        |                     |                     |-- INSERT Ticket ---->|
  |                        |                     |                     |-- INSERT 6x Numbers->|
  |                        |                     |                     |<-- COMMIT ----------|
  |                        |                     |<-- TicketId --------|                     |
  |                        |                     |                     |                     |
  |                        |<-- Response --------|                     |                     |
  |                        |   (Message, Id)     |                     |                     |
  |                        |                     |                     |                     |
  |<-- 201 Created --------|                     |                     |                     |
  |   + Location header    |                     |                     |                     |
```

### 5.2 Opis kroków

1. **Klient wysyła żądanie POST** z JWT tokenem w headerze `Authorization`
2. **ASP.NET Core Middleware** weryfikuje JWT i wypełnia `HttpContext.User`
3. **Endpoint** wyciąga `UserId` z JWT claims i tworzy obiekt `Request`
4. **MediatR** wysyła `Request` do `Handler`
5. **Validator** sprawdza poprawność żądania (pusty request, opcjonalnie UserId > 0)
6. **Handler** pobiera liczbę zestawów użytkownika przez `ITicketService.GetUserTicketCountAsync()`
7. **Service** wykonuje zapytanie SQL: `SELECT COUNT(*) FROM Tickets WHERE UserId = @userId`
8. **Handler** sprawdza limit:
   - Jeśli count >= 100: rzuca `ValidationException` z komunikatem o limicie
   - Jeśli count < 100: kontynuuje
9. **Handler** wywołuje `ITicketService.GenerateRandomNumbers()` dla wygenerowania 6 losowych liczb
10. **Service** generuje liczby algorytmem:
    ```csharp
    var random = new Random();
    var numbers = Enumerable.Range(1, 49)
        .OrderBy(x => random.Next())
        .Take(6)
        .OrderBy(x => x)
        .ToArray();
    ```
11. **Handler** wywołuje `ITicketService.CreateTicketWithNumbersAsync(userId, numbers)`
12. **Service** rozpoczyna transakcję bazodanową:
    - INSERT do `Tickets` (Id = GUID, UserId, CreatedAt = UTC NOW)
    - 6x INSERT do `TicketNumbers` (TicketId, Number, Position 1-6)
    - COMMIT transakcji
13. **Service** zwraca `TicketId` (GUID)
14. **Handler** tworzy obiekt `Response` z komunikatem i ID
15. **Endpoint** zwraca `201 Created` z headerem `Location: /api/tickets/{ticketId}`

### 5.3 Interakcje z bazą danych

**Zapytania:**

1. **Sprawdzenie limitu:**
   ```sql
   SELECT COUNT(*) FROM Tickets WHERE UserId = @userId;
   ```

2. **INSERT Ticket:**
   ```sql
   INSERT INTO Tickets (UserId, CreatedAt)
   VALUES (@userId, GETUTCDATE());
   SELECT SCOPE_IDENTITY();
   ```

3. **INSERT TicketNumbers (6x):**
   ```sql
   INSERT INTO TicketNumbers (TicketId, Number, Position)
   VALUES 
     (@ticketId, @number1, 1),
     (@ticketId, @number2, 2),
     (@ticketId, @number3, 3),
     (@ticketId, @number4, 4),
     (@ticketId, @number5, 5),
     (@ticketId, @number6, 6);
   ```

**Indeksy wykorzystywane:**
- `IX_Tickets_UserId` - dla COUNT query (linia 2 w AppDbContext)

**Transakcja:**  
Wszystkie operacje INSERT w jednej transakcji (EF Core SaveChangesAsync z implicit transaction).

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
- **Izolacja danych:** Ticket tworzony wyłącznie dla zalogowanego użytkownika (UserId z JWT)

### 6.3 Walidacja danych

**Walidacja wejścia (Validator.cs):**
**Walidacja żądania (Endpoint.cs):**
- Brak parametrów do walidacji (Request pusty oprócz UserId)
- Opcjonalnie: sprawdzenie czy `UserId > 0`

**Walidacja biznesowa (Handler.cs):**
- Walidacja wygenerowanych liczb:
  - Dokładnie 6 liczb
  - Wszystkie w zakresie 1-49
  - Wszystkie unikalne (algorytm zapewnia)

**Uwaga:** Brak sprawdzenia limitu zestawów - endpoint tylko generuje propozycję, nie zapisuje do bazy.

### 6.4 Ochrona przed atakami

| Typ ataku | Mitigacja |
|-----------|-----------|
| **SQL Injection** | EF Core używa parametrów (chroni automatycznie) |
| **CSRF** | Nie dotyczy API REST z JWT (stateless) |
| **XSS** | Dane wejściowe minimalne, zwracany tylko komunikat tekstowy |
| **Rate Limiting** | Opcjonalnie: dodać throttling (np. 10 requestów/minutę) - poza MVP |
| **Token theft** | HTTPS dla wszystkich połączeń (konfiguracja infrastruktury) |
| **Replay attack** | JWT expiration (24h), opcjonalnie: refresh tokens |

### 6.5 Bezpieczeństwo danych

- **Auto-increment ID dla Ticket.Id:** Bezpieczne sekwencyjne generowanie przez bazę danych
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
| **Błąd generatora** | 500 | `Exception` | "An error occurred..." | Error + stack trace |

### 7.2 Implementacja w Handler

**Generowanie i zwrot:**
```csharp
// Generate random numbers
var numbers = _ticketService.GenerateRandomNumbers();

_logger.LogDebug(
    "Generated random numbers for user {UserId}: {Numbers}",
    request.UserId, string.Join(", ", numbers));

// Return as preview/proposal
return new Contracts.Response(numbers);
```

**Uwaga:** Usunięto logikę sprawdzania limitu i zapisu do bazy - to tylko generator propozycji.

### 7.3 Globalna obsługa (ExceptionHandlingMiddleware)

**Obsługiwane wyjątki:**

1. **ValidationException (FluentValidation):**
   - Status: 400 Bad Request
   - Body: `{ "errors": { "fieldName": ["error1", "error2"] } }`

2. **UnauthorizedException (custom):**
   - Status: 401 Unauthorized
   - Body: `{ "message": "Unauthorized" }`

3. **DbUpdateException:**
   - Status: 500 Internal Server Error
   - Body: `{ "message": "An error occurred..." }`
   - Logowanie: Error level z pełnym stack trace

4. **Exception (inne):**
   - Status: 500 Internal Server Error
   - Body: `{ "message": "An error occurred..." }`
   - Logowanie: Error level z pełnym stack trace

### 7.4 Logowanie (Serilog)

**Poziomy logowania:**

- **Information:** Pomyślne utworzenie ticketu
  ```csharp
  _logger.LogDebug("Generated random ticket {TicketId} for user {UserId}", 
      ticketId, userId);
  ```

- **Warning:** Próba przekroczenia limitu
  ```csharp
  _logger.LogWarning("User {UserId} attempted to generate ticket but reached limit of 100", 
      userId);
  ```

- **Error:** Błędy bazy danych, wyjątki
  ```csharp
  _logger.LogError(ex, "Failed to create ticket for user {UserId}", userId);
  ```

**Lokalizacja logów:** `Logs/applog-{Date}.txt`

---

## 8. Rozważania dotyczące wydajności

### 8.1 Wąskie gardła

| Obszar | Potencjalny problem | Mitygacja |
|--------|-------------------|-----------|
| **COUNT query** | Przy dużej liczbie ticketów (100/user) może być wolne | Indeks `IX_Tickets_UserId` (już istnieje) |
| **Random generation** | `Random()` nie jest thread-safe | Użyć `Random.Shared` (C# 10+) lub `RandomNumberGenerator` |
| **INSERT 6 rekordów** | 6 osobnych INSERT może być wolne | Bulk insert lub ExecuteSqlRaw z VALUES (1,2,3),(4,5,6) |
| **Transakcja** | Lock na tabeli Tickets podczas INSERT | EF Core używa row-level locks (SQL Server), minimal impact |
| **Generowanie ID** | Auto-increment wymaga dwóch SaveChanges | Optymalne dla int ID, najpierw ticket, potem numbers |

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

**2. Bulk insert dla TicketNumbers:**
```csharp
// Najpierw zapisz ticket aby otrzymać Id
await _context.SaveChangesAsync(cancellationToken);

var ticketNumbers = numbers.Select((num, index) => new TicketNumber
{
    TicketId = ticket.Id,
    Number = num,
    Position = (byte)(index + 1)
}).ToList();

await _context.TicketNumbers.AddRangeAsync(ticketNumbers, cancellationToken);
await _context.SaveChangesAsync(cancellationToken);
```

**Korzyści:**
- Jeden INSERT z wieloma VALUES zamiast 6 osobnych
- Mniej round-tripów do bazy

**3. Cached count (przyszła optymalizacja, poza MVP):**
- Dodać kolumnę `User.TicketCount` i aktualizować triggerem lub w aplikacji
- Eliminuje COUNT query (SELECT zamiast aggregate)

### 8.3 Indeksy wykorzystywane

| Indeks | Tabela | Kolumny | Cel |
|--------|--------|---------|-----|
| `IX_Tickets_UserId` | Tickets | UserId | COUNT query, filtrowanie |
| `PK_Tickets` | Tickets | Id | INSERT (auto-increment int) |
| `PK_TicketNumbers` | TicketNumbers | Id | INSERT (auto-increment) |
| `IX_TicketNumbers_TicketId` | TicketNumbers | TicketId | Foreign key lookup |

### 8.4 Szacowana wydajność

- **COUNT query:** ~1-5ms (dzięki indeksowi, 100 rekordów)
- **Random generation:** ~0.1ms (CPU-bound, 49 liczb)
- **INSERT Ticket:** ~2-5ms (auto-increment ID)
- **SaveChanges #1:** ~2-3ms (uzyskanie ID)
- **INSERT 6x TicketNumbers:** ~5-10ms (bulk insert)
- **TOTAL:** ~10-25ms (bez opóźnień sieciowych)

**Przepustowość:** ~50-100 requestów/sekundę na standardowym serwerze

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
    /// Gets the count of tickets owned by a specific user.
    /// </summary>
    /// <param name="userId">User ID to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of tickets owned by user</returns>
    Task<int> GetUserTicketCountAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates 6 random unique numbers in range 1-49 for lottery ticket.
    /// </summary>
    /// <returns>Array of 6 sorted unique numbers</returns>
    int[] GenerateRandomNumbers();

    /// <summary>
    /// Creates a ticket with associated numbers in a database transaction.
    /// </summary>
    /// <param name="userId">Owner user ID</param>
    /// <param name="numbers">Array of exactly 6 unique numbers (1-49)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>ID of created ticket (int)</returns>
    /// <exception cref="ArgumentException">If numbers array is invalid</exception>
    /// <exception cref="DbUpdateException">If database operation fails</exception>
    Task<int> CreateTicketWithNumbersAsync(int userId, int[] numbers, CancellationToken cancellationToken = default);
}
```

**4.2 Implementation:**

**Plik:** `Services/TicketService.cs`

```csharp
using LottoTM.Server.Api.Entities;
using LottoTM.Server.Api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LottoTM.Server.Api.Services;

public class TicketService : ITicketService
{
    private readonly AppDbContext _context;
    private readonly ILogger<TicketService> _logger;

    public TicketService(AppDbContext context, ILogger<TicketService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<int> GetUserTicketCountAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _context.Tickets
            .Where(t => t.UserId == userId)
            .CountAsync(cancellationToken);
    }

    public int[] GenerateRandomNumbers()
    {
        // Używamy Random.Shared (C# 10+) dla thread-safety
        return Enumerable.Range(1, 49)
            .OrderBy(x => Random.Shared.Next())
            .Take(6)
            .OrderBy(x => x) // Sortowanie dla czytelności
            .ToArray();
    }

    public async Task<int> CreateTicketWithNumbersAsync(
        int userId, 
        int[] numbers, 
        CancellationToken cancellationToken = default)
    {
        // Walidacja parametrów
        if (numbers == null || numbers.Length != 6)
        {
            throw new ArgumentException("Numbers array must contain exactly 6 elements", nameof(numbers));
        }

        if (numbers.Any(n => n < 1 || n > 49))
        {
            throw new ArgumentException("All numbers must be in range 1-49", nameof(numbers));
        }

        if (numbers.Distinct().Count() != 6)
        {
            throw new ArgumentException("All numbers must be unique", nameof(numbers));
        }

        // Rozpoczęcie transakcji (EF Core implicit transaction przez SaveChangesAsync)
        var ticket = new Ticket
        {
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Tickets.Add(ticket);
        
        // Zapisz ticket aby otrzymać wygenerowane Id
        await _context.SaveChangesAsync(cancellationToken);

        // Bulk insert liczb
        var ticketNumbers = numbers.Select((num, index) => new TicketNumber
        {
            TicketId = ticket.Id,
            Number = num,
            Position = (byte)(index + 1)
        }).ToList();

        await _context.TicketNumbers.AddRangeAsync(ticketNumbers, cancellationToken);

        // Zapis transakcyjny (COMMIT)
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug(
            "Created ticket {TicketId} for user {UserId} with numbers: {Numbers}",
            ticket.Id, userId, string.Join(", ", numbers));

        return ticket.Id;
    }
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
        // 1. Walidacja żądania (FluentValidation)
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // Logowanie UserId dla audytu
        _logger.LogDebug(
            "Generating random numbers for user {UserId}",
            request.UserId);

        // Generowanie losowych liczb
        var numbers = _ticketService.GenerateRandomNumbers();

        _logger.LogDebug(
            "Generated random numbers for user {UserId}: {Numbers}",
            request.UserId, string.Join(", ", numbers));

        // Zwrócenie liczb jako propozycja (bez zapisu do bazy)
        return new Contracts.Response(numbers);
    }
}
```

**Uwagi:**
- Brak walidacji limitu - to tylko generator propozycji
- Brak zapisu do bazy - użytkownik zdecyduje czy zapisać
- Logowanie dla audytu i monitorowania
- Prosta implementacja - tylko generowanie i zwrot
    }
}
```

**Uwagi:**
- Walidacja przez FluentValidation
- Sprawdzenie limitu z jasnym komunikatem
- Logowanie na poziomach Information (sukces) i Warning (limit)
- Obsługa wyjątków z przekazaniem do globalnego middleware

---

### 9.6 Krok 6: Implementacja Endpoint.cs

**Plik:** `Features/Tickets/GenerateRandom/Endpoint.cs`

**Kod:**
```csharp
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;

namespace LottoTM.Server.Api.Features.Tickets.GenerateRandom;

public static class Endpoint
{
    public static void AddEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("api/tickets/generate-random", 
            [Authorize] async (HttpContext httpContext, IMediator mediator) =>
            {
                // Pobranie UserId z JWT claims
                var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    return Results.Unauthorized();
                }

                // Utworzenie żądania
                var request = new Contracts.Request(userId);

                // Wysłanie do MediatR
                var result = await mediator.Send(request);

                // Zwrot 201 Created z Location header
                return Results.Created(
                    $"/api/tickets/{result.TicketId}", 
                    result);
            })
        .WithName("GenerateRandomTicket")
        .Produces<Contracts.Response>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status500InternalServerError)
        .WithOpenApi(operation => 
        {
            operation.Summary = "Generuje losowy zestaw liczb LOTTO";
            operation.Description = "Generuje pojedynczy losowy zestaw 6 unikalnych liczb (1-49) " +
                                   "i zapisuje go jako nowy ticket użytkownika. " +
                                   "Maksymalny limit: 100 zestawów na użytkownika.";
            return operation;
        });
    }
}
```

**Uwagi:**
- Atrybut `[Authorize]` wymaga JWT tokenu
- Wyciąganie `UserId` z claim `ClaimTypes.NameIdentifier` (standard JWT)
- Zwrot `201 Created` z header `Location` wskazującym na nowy zasób
- Dokumentacja OpenAPI dla Swagger

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

**8.2 Test Case 1: Sukces (201 Created)**

**Request:**
```http
POST http://localhost:5000/api/tickets/generate-random
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Expected Response:**
```json
HTTP/1.1 201 Created
Location: /api/tickets/123
Content-Type: application/json

{
  "message": "Zestaw wygenerowany pomyślnie",
  "ticketId": 123
}
```

**Weryfikacja w bazie:**
```sql
SELECT * FROM Tickets WHERE UserId = <userId> ORDER BY CreatedAt DESC;
SELECT * FROM TicketNumbers WHERE TicketId = 123;
```

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

**8.4 Test Case 3: Limit osiągnięty (400)**

**Przygotowanie:** Dodać 100 ticketów dla użytkownika (skrypt SQL lub loop w aplikacji)

**Request:**
```http
POST http://localhost:5000/api/tickets/generate-random
Authorization: Bearer <valid-token>
```

**Expected Response:**
```json
HTTP/1.1 400 Bad Request
Content-Type: application/json

{
  "errors": {
    "limit": ["Osiągnięto limit 100 zestawów. Usuń istniejące zestawy, aby wygenerować nowe."]
  }
}
```

**8.5 Test Case 4: Wielokrotne wywołanie (unikalność liczb)**

**Request:** Wywołaj endpoint 10x z tym samym tokenem

**Weryfikacja:**
- Każde wywołanie zwraca inne liczby (losowość)
- Wszystkie zestawy zapisane w bazie
- Liczby w każdym zestawie są unikalne i w zakresie 1-49

---

### 9.9 Krok 9: Testowanie jednostkowe (opcjonalne, rekomendowane)

**9.1 Test Service - GenerateRandomNumbers:**

**Plik:** `tests/server/LottoTM.Server.Api.Tests/Services/TicketServiceTests.cs`

```csharp
[Fact]
public void GenerateRandomNumbers_ShouldReturn6UniqueNumbersInRange()
{
    // Arrange
    var service = new TicketService(_mockContext.Object, _mockLogger.Object);

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
    var service = new TicketService(_mockContext.Object, _mockLogger.Object);

    // Act
    var set1 = service.GenerateRandomNumbers();
    var set2 = service.GenerateRandomNumbers();

    // Assert
    Assert.NotEqual(set1, set2); // Statystycznie różne (może czasem failować, ale ~0.000001% szansy)
}
```

**9.2 Test Handler - Limit check:**

**Plik:** `tests/server/LottoTM.Server.Api.Tests/Features/Tickets/GenerateRandom/HandlerTests.cs`

```csharp
[Fact]
public async Task Handle_WhenLimitReached_ShouldThrowValidationException()
{
    // Arrange
    _mockTicketService.Setup(s => s.GetUserTicketCountAsync(It.IsAny<int>(), default))
        .ReturnsAsync(100);

    var handler = new Handler(_mockTicketService.Object, _mockValidator.Object, _mockLogger.Object);
    var request = new Contracts.Request(UserId: 123);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ValidationException>(
        () => handler.Handle(request, default));

    Assert.Contains("limit", exception.Errors.First().PropertyName);
    Assert.Contains("100", exception.Errors.First().ErrorMessage);
}

[Fact]
public async Task Handle_WhenBelowLimit_ShouldCreateTicketSuccessfully()
{
    // Arrange
    var expectedTicketId = 123;
    _mockTicketService.Setup(s => s.GetUserTicketCountAsync(It.IsAny<int>(), default))
        .ReturnsAsync(50);
    _mockTicketService.Setup(s => s.GenerateRandomNumbers())
        .Returns(new[] { 1, 2, 3, 4, 5, 6 });
    _mockTicketService.Setup(s => s.CreateTicketWithNumbersAsync(
            It.IsAny<int>(), It.IsAny<int[]>(), default))
        .ReturnsAsync(expectedTicketId);

    var handler = new Handler(_mockTicketService.Object, _mockValidator.Object, _mockLogger.Object);
    var request = new Contracts.Request(UserId: 123);

    // Act
    var response = await handler.Handle(request, default);

    // Assert
    Assert.Equal("Zestaw wygenerowany pomyślnie", response.Message);
    Assert.Equal(expectedTicketId, response.TicketId);
}
```

---

### 9.10 Krok 10: Dokumentacja i wdrożenie

**10.1 Aktualizacja README.md:**

Dodać sekcję z przykładem użycia endpointa:

```markdown
### Generowanie losowego zestawu LOTTO

**Endpoint:** `POST /api/tickets/generate-random`

**Opis:** Generuje pojedynczy losowy zestaw 6 liczb (1-49) i zapisuje jako nowy ticket.

**Przykład:**
```bash
curl -X POST http://localhost:5000/api/tickets/generate-random \
  -H "Authorization: Bearer <your-jwt-token>"
```

**Odpowiedź:**
```json
{
  "message": "Zestaw wygenerowany pomyślnie",
  "ticketId": 123
}
```
```

**10.2 Commit do repozytorium:**

```bash
git add .
git commit -m "feat: Add POST /api/tickets/generate-random endpoint

- Implemented Vertical Slice architecture with MediatR
- Added TicketService with random number generation
- Enforced 100 tickets limit per user
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
