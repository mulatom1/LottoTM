# Plan Wdrożenia Endpointu API: POST /api/tickets/generate-system

**Data:** 2025-11-05  
**Wersja:** 1.0  
**Endpoint:** POST /api/tickets/generate-system  
**Moduł:** Tickets (Zestawy)

---

## 1. Przegląd punktu końcowego

### Opis
Endpoint generuje 9 zestawów liczb LOTTO (po 6 liczb każdy) według algorytmu systemowego, który gwarantuje pokrycie wszystkich liczb od 1 do 49. Wygenerowane zestawy są automatycznie zapisywane do bazy danych i przypisywane do użytkownika.

### Cel biznesowy
Umożliwienie użytkownikom szybkiego wygenerowania zestawów systemowych, które zapewniają pełne pokrycie przestrzeni liczb LOTTO (1-49) w minimalnej liczbie zestawów (9).

### Charakterystyka
- **Metoda HTTP:** POST
- **Ścieżka:** `/api/tickets/generate-system`
- **Autoryzacja:** Wymagany JWT Bearer token
- **Payload żądania:** Brak (empty body)
- **Atomowość:** Wszystkie 9 zestawów zapisywane w jednej transakcji (all-or-nothing)
- **Idempotentność:** Nie - każde wywołanie generuje nowe, losowe zestawy

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
    /// Response containing success message and generated ticket details.
    /// </summary>
    public record Response(
        string Message,
        int GeneratedCount,
        List<TicketDto> Tickets
    );
    
    /// <summary>
    /// DTO representing a single generated ticket.
    /// </summary>
    public record TicketDto(
        int Id,
        List<int> Numbers,
        DateTime CreatedAt
    );
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
  - `int UserId` - Foreign key to Users
  - `DateTime CreatedAt` - UTC timestamp
  - `ICollection<TicketNumber> Numbers` - Navigation property

- **TicketNumber** (z `Entities/TicketNumber.cs`)
  - `int Id` - Primary key
  - `int TicketId` - Foreign key to Tickets
  - `int Number` - Lottery number (1-49)
  - `byte Position` - Position in ticket (1-6)

---

## 4. Szczegóły odpowiedzi

### 4.1 Odpowiedź sukcesu (201 Created)

#### Status Code
`201 Created`

#### Response Body
```json
{
  "message": "9 zestawów wygenerowanych i zapisanych pomyślnie",
  "generatedCount": 9,
  "tickets": [
    {
      "id": "a3bb189e-8bf9-3888-9912-ace4e6543002",
      "numbers": [7, 15, 23, 31, 39, 47],
      "createdAt": "2025-11-05T10:30:00Z"
    },
    {
      "id": "b4cc289f-9cf0-4999-0023-bdf5f7654113",
      "numbers": [2, 10, 18, 26, 34, 42],
      "createdAt": "2025-11-05T10:30:00Z"
    }
    // ... pozostałe 7 zestawów
  ]
}
```

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

#### 400 Bad Request - Brak miejsca na zestawy
**Przyczyna:** Użytkownik ma > 91 zestawów (brak miejsca na 9 nowych)

```json
{
  "status": 400,
  "title": "Bad Request",
  "errors": {
    "limit": [
      "Brak miejsca na 9 zestawów. Masz 95/100 zestawów. Usuń co najmniej 4 zestawy."
    ]
  }
}
```

#### 500 Internal Server Error
**Przyczyna:** Błąd bazy danych lub algorytmu generowania

```json
{
  "status": 500,
  "title": "Internal Server Error",
  "detail": "Wystąpił nieoczekiwany błąd podczas generowania zestawów"
}
```

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
│  - Wymaga autoryzacji                   │
│  - Wyodrębnia UserId z JWT              │
└──────┬──────────────────────────────────┘
       │ Wywołanie MediatR
       ▼
┌─────────────────────────────────────────┐
│  Validator (Validator.cs)               │
│  1. Sprawdza liczbę zestawów użytkownika│
│  2. Waliduje limit (≤ 91)               │
└──────┬──────────────────────────────────┘
       │ Jeśli OK
       ▼
┌─────────────────────────────────────────┐
│  Handler (Handler.cs)                   │
│  1. Generuje 9 zestawów (algorytm)      │
│  2. Tworzy encje Ticket + TicketNumber  │
│  3. Zapisuje w transakcji (bulk insert) │
│  4. Zwraca Response z ID-ami            │
└──────┬──────────────────────────────────┘
       │ Response
       ▼
┌─────────────────────────────────────────┐
│  ExceptionHandlingMiddleware            │
│  - Loguje błędy (Serilog)               │
│  - Formatuje odpowiedzi błędów          │
└──────┬──────────────────────────────────┘
       │ JSON Response
       ▼
┌─────────────┐
│   Client    │
│  (201/400)  │
└─────────────┘
```

### 5.2 Interakcje z bazą danych

#### Query 1: Sprawdzenie limitu zestawów
```sql
SELECT COUNT(*) 
FROM Tickets 
WHERE UserId = @userId;
```
**Indeks:** `IX_Tickets_UserId`

#### Query 2: Bulk Insert - Tickets (9 rekordów)
```sql
INSERT INTO Tickets (Id, UserId, CreatedAt)
VALUES 
  (NEWID(), @userId, GETUTCDATE()),
  (NEWID(), @userId, GETUTCDATE()),
  -- ... 7 więcej
```

#### Query 3: Bulk Insert - TicketNumbers (54 rekordy: 9 × 6)
```sql
INSERT INTO TicketNumbers (TicketId, Number, Position)
VALUES 
  (@ticketId1, 7, 1),
  (@ticketId1, 15, 2),
  -- ... 52 więcej
```

**Optymalizacja:** Użycie `AddRangeAsync()` w EF Core dla bulk insert.

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
- **Ochrona:** EF Core automatycznie parametryzuje zapytania
- **Brak raw SQL:** Wszystkie operacje przez LINQ/EF Core

#### JWT Tampering
- **Ochrona:** Podpis HMAC SHA256 weryfikowany przez middleware
- **Konfiguracja:** `TokenValidationParameters` w `Program.cs`

#### Brute-force Generation
- **Ryzyko:** Użytkownik generuje 100 zestawów szybko
- **Obecna ochrona:** Limit 100 zestawów globalny
- **Rekomendacja przyszłości:** Rate limiting (np. 10 wywołań/minutę)

#### Overflow Attack
- **Ryzyko:** Przepełnienie bazy danych
- **Ochrona:** Hard limit 100 zestawów/użytkownik
- **Walidacja:** Sprawdzenie w Validator przed generowaniem

---

## 7. Obsługa błędów

### 7.1 Tabela błędów

| Kod | Nazwa | Przyczyna | Komunikat | Obsługa |
|-----|-------|-----------|-----------|---------|
| 401 | Unauthorized | Brak/nieprawidłowy JWT | "Token JWT jest nieprawidłowy lub wygasł" | Middleware autoryzacji |
| 400 | Bad Request | Limit zestawów (>91) | "Brak miejsca na 9 zestawów. Masz X/100 zestawów." | Validator + ValidationException |
| 500 | Internal Server Error | Błąd bazy danych | "Wystąpił nieoczekiwany błąd podczas generowania zestawów" | ExceptionHandlingMiddleware |
| 500 | Internal Server Error | Błąd algorytmu | "Błąd generowania systemowego - spróbuj ponownie" | Try-catch w Handler |

### 7.2 Strategie obsługi

#### ValidationException (400)
```csharp
// W Validator.cs
if (ticketCount > 91)
{
    var availableSlots = 100 - ticketCount;
    throw new ValidationException(
        $"Brak miejsca na 9 zestawów. Masz {ticketCount}/100 zestawów. " +
        $"Usuń co najmniej {9 - availableSlots} zestawów."
    );
}
```

#### DbUpdateException (500)
```csharp
// W Handler.cs
try
{
    await _context.Tickets.AddRangeAsync(tickets);
    await _context.SaveChangesAsync();
    await transaction.CommitAsync();
}
catch (DbUpdateException ex)
{
    await transaction.RollbackAsync();
    _logger.LogError(ex, "Database error generating system tickets for UserId={UserId}", userId);
    throw new InvalidOperationException("Wystąpił błąd podczas zapisywania zestawów", ex);
}
```

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
_logger.LogInformation(
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
    "9 zestawów wygenerowanych i zapisanych pomyślnie",
    9,
    tickets.Select(t => new TicketDto(
        t.Id,
        t.Numbers.OrderBy(n => n.Position).Select(n => n.Number).ToList(),
        t.CreatedAt
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
| Algorytm generowania | <5 ms | In-memory, deterministyczny |
| Query COUNT limitu | <10 ms | Z indeksem |
| Bulk insert 63 rekordów | 20-50 ms | W zależności od obciążenia DB |
| **Całkowity czas** | **<100 ms** | Optymistyczny scenariusz |

**Rekomendacje:**
- Monitorowanie czasu transakcji (Serilog + Application Insights)
- Testowanie wydajności przy 10+ równoczesnych żądaniach
- Rozważenie connection pooling (domyślnie w EF Core)

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
    /// No parameters required - UserId extracted from JWT token.
    /// </summary>
    public record Request : IRequest<Response>;
    
    /// <summary>
    /// Response containing success message and details of generated tickets.
    /// </summary>
    /// <param name="Message">Success message</param>
    /// <param name="GeneratedCount">Number of tickets generated (always 9)</param>
    /// <param name="Tickets">List of generated ticket details</param>
    public record Response(
        string Message,
        int GeneratedCount,
        List<TicketDto> Tickets
    );
    
    /// <summary>
    /// DTO representing a single generated ticket.
    /// </summary>
    /// <param name="Id">Unique ticket identifier (int)</param>
    /// <param name="Numbers">6 lottery numbers (sorted by position)</param>
    /// <param name="CreatedAt">UTC timestamp of creation</param>
    public record TicketDto(
        int Id,
        List<int> Numbers,
        DateTime CreatedAt
    );
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
/// Validates that user has sufficient space for 9 new system tickets.
/// Maximum allowed tickets per user: 100
/// System generation requires: 9 slots available (≤91 existing tickets)
/// </summary>
public class Validator : AbstractValidator<Contracts.Request>
{
    private readonly AppDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public Validator(AppDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;

        RuleFor(x => x)
            .CustomAsync(async (request, context, cancellationToken) =>
            {
                // Extract UserId from JWT token
                var userIdClaim = _httpContextAccessor.HttpContext?.User
                    .FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    context.AddFailure("UserId", "Nie można zidentyfikować użytkownika z tokenu JWT");
                    return;
                }

                // Check current ticket count
                var currentCount = await _context.Tickets
                    .Where(t => t.UserId == userId)
                    .CountAsync(cancellationToken);

                // Validate: must have ≤91 tickets (space for 9 more)
                const int maxTickets = 100;
                const int systemTicketsCount = 9;
                const int maxAllowedBeforeGeneration = maxTickets - systemTicketsCount; // 91

                if (currentCount > maxAllowedBeforeGeneration)
                {
                    var availableSlots = maxTickets - currentCount;
                    var ticketsToDelete = systemTicketsCount - availableSlots;

                    context.AddFailure("limit", 
                        $"Brak miejsca na {systemTicketsCount} zestawów. " +
                        $"Masz {currentCount}/{maxTickets} zestawów. " +
                        $"Usuń co najmniej {ticketsToDelete} zestawów.");
                }
            });
    }
}
```

**Uwaga:** Wymaga rejestracji `IHttpContextAccessor` w `Program.cs`:
```csharp
builder.Services.AddHttpContextAccessor();
```

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
/// </summary>
public class GenerateSystemTicketsHandler : IRequestHandler<Contracts.Request, Contracts.Response>
{
    private readonly AppDbContext _context;
    private readonly IValidator<Contracts.Request> _validator;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<GenerateSystemTicketsHandler> _logger;

    public GenerateSystemTicketsHandler(
        AppDbContext context,
        IValidator<Contracts.Request> validator,
        IHttpContextAccessor httpContextAccessor,
        ILogger<GenerateSystemTicketsHandler> logger)
    {
        _context = context;
        _validator = validator;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<Contracts.Response> Handle(Contracts.Request request, CancellationToken cancellationToken)
    {
        // Validate request
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // Extract UserId from JWT
        var userIdClaim = _httpContextAccessor.HttpContext?.User
            .FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Nie można zidentyfikować użytkownika");
        }

        // Generate 9 system tickets
        var generatedNumbers = GenerateSystemTickets();

        // Validate coverage (all numbers 1-49 must appear)
        ValidateCoverage(generatedNumbers);

        // Create Ticket entities
        var tickets = new List<Ticket>();
        var createdAt = DateTime.UtcNow;

        foreach (var numbers in generatedNumbers)
        {
            var ticket = new Ticket
            {
                UserId = userId,
                CreatedAt = createdAt,
                Numbers = numbers.Select((num, index) => new TicketNumber
                {
                    Number = num,
                    Position = (byte)(index + 1) // Positions 1-6
                }).ToList()
            };

            tickets.Add(ticket);
        }

        // Save to database in transaction
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await _context.Tickets.AddRangeAsync(tickets, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Generated 9 system tickets for UserId={UserId}. TicketIds={TicketIds}",
                userId,
                string.Join(", ", tickets.Select(t => t.Id))
            );
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Database error generating system tickets for UserId={UserId}", userId);
            throw new InvalidOperationException("Wystąpił błąd podczas zapisywania zestawów", ex);
        }

        // Build response
        var response = new Contracts.Response(
            "9 zestawów wygenerowanych i zapisanych pomyślnie",
            9,
            tickets.Select(t => new Contracts.TicketDto(
                t.Id,
                t.Numbers.OrderBy(n => n.Position).Select(n => n.Number).ToList(),
                t.CreatedAt
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
/// Endpoint for generating 9 system lottery tickets covering all numbers 1-49.
/// Requires JWT authentication. User must have ≤91 existing tickets.
/// </summary>
public static class Endpoint
{
    public static void AddEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("api/tickets/generate-system", async (IMediator mediator) =>
        {
            var request = new Contracts.Request();
            var result = await mediator.Send(request);
            return Results.Created($"api/tickets", result);
        })
        .RequireAuthorization() // Requires JWT token
        .WithName("GenerateSystemTickets")
        .Produces<Contracts.Response>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
        .WithOpenApi(operation =>
        {
            operation.Summary = "Generowanie 9 zestawów systemowych";
            operation.Description = 
                "Generuje 9 zestawów liczb LOTTO pokrywających wszystkie liczby 1-49. " +
                "Każdy zestaw zawiera 6 unikalnych liczb. Wymaga maksymalnie 91 istniejących zestawów.";
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
// Rejestracja IHttpContextAccessor (jeśli nie istnieje)
builder.Services.AddHttpContextAccessor();

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
public async Task POST_GenerateSystem_WithValidToken_ReturnsCreated()
{
    // Test sukcesu generowania
}

[Fact]
public async Task POST_GenerateSystem_WithMoreThan91Tickets_ReturnsBadRequest()
{
    // Test walidacji limitu
}

[Fact]
public async Task POST_GenerateSystem_WithoutToken_ReturnsUnauthorized()
{
    // Test autoryzacji
}
```

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
5. Sprawdź odpowiedź 201 Created z 9 zestawami

#### Krok 7.2: Weryfikacja w bazie danych
```sql
-- Sprawdź wygenerowane zestawy
SELECT t.Id, t.UserId, t.CreatedAt, tn.Number, tn.Position
FROM Tickets t
INNER JOIN TicketNumbers tn ON t.Id = tn.TicketId
WHERE t.UserId = @userId
ORDER BY t.CreatedAt DESC, tn.Position;

-- Sprawdź pokrycie liczb 1-49
SELECT DISTINCT tn.Number
FROM Tickets t
INNER JOIN TicketNumbers tn ON t.Id = tn.TicketId
WHERE t.UserId = @userId
  AND t.CreatedAt > @lastGeneration
ORDER BY tn.Number;
-- Oczekiwany wynik: 49 rekordów (liczby 1-49)
```

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
- [ ] Zaimplementuj `Contracts.cs` (Request/Response/TicketDto)
- [ ] Zaimplementuj `Validator.cs` (walidacja limitu 91 zestawów)
- [ ] Zaimplementuj `Handler.cs` (algorytm + logika biznesowa)
- [ ] Zaimplementuj `Endpoint.cs` (routing + autoryzacja)
- [ ] Dodaj rejestrację `IHttpContextAccessor` w `Program.cs`
- [ ] Zarejestruj endpoint w `Program.cs`

### Testy
- [ ] Napisz testy jednostkowe algorytmu (pokrycie 1-49)
- [ ] Napisz testy jednostkowe walidatora (limit)
- [ ] Napisz testy integracyjne endpointu (sukces/błędy)
- [ ] Przetestuj manualnie przez Swagger UI
- [ ] Sprawdź dane w bazie SQL Server

### Optymalizacja i bezpieczeństwo
- [ ] Sprawdź wydajność bulk insert (≤100ms)
- [ ] Zweryfikuj transakcję (rollback przy błędzie)
- [ ] Przetestuj autoryzację JWT (brak tokenu = 401)
- [ ] Sprawdź logowanie Serilog (info/warning/error)
- [ ] Code review (algorytm, walidacja, obsługa błędów)

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
      "id": "a3bb189e-8bf9-3888-9912-ace4e6543002",
      "numbers": [7, 15, 23, 31, 39, 47],
      "createdAt": "2025-11-05T10:30:00Z"
    },
    {
      "id": "b4cc289f-9cf0-4999-0023-bdf5f7654113",
      "numbers": [2, 10, 18, 26, 34, 42],
      "createdAt": "2025-11-05T10:30:00Z"
    },
    {
      "id": "c5dd389g-0dg1-5000-1134-ceg6g8765224",
      "numbers": [1, 9, 17, 25, 33, 41],
      "createdAt": "2025-11-05T10:30:00Z"
    },
    {
      "id": "d6ee489h-1eh2-6111-2245-dfh7h9876335",
      "numbers": [4, 12, 20, 28, 36, 44],
      "createdAt": "2025-11-05T10:30:00Z"
    },
    {
      "id": "e7ff589i-2fi3-7222-3356-egi8i0987446",
      "numbers": [6, 14, 22, 30, 38, 3],
      "createdAt": "2025-11-05T10:30:00Z"
    },
    {
      "id": "f8gg689j-3gj4-8333-4467-fhj9j1098557",
      "numbers": [8, 16, 24, 32, 40, 11],
      "createdAt": "2025-11-05T10:30:00Z"
    },
    {
      "id": "g9hh789k-4hk5-9444-5578-gik0k2109668",
      "numbers": [5, 13, 21, 29, 37, 19],
      "createdAt": "2025-11-05T10:30:00Z"
    },
    {
      "id": "h0ii890l-5il6-0555-6689-hjl1l3210779",
      "numbers": [49, 48, 46, 45, 43, 27],
      "createdAt": "2025-11-05T10:30:00Z"
    },
    {
      "id": "i1jj901m-6jm7-1666-7790-ikm2m4321880",
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
