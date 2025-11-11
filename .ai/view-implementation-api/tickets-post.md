# API Endpoint Implementation Plan: POST /api/tickets

## 1. Przegląd punktu końcowego

Endpoint **POST /api/tickets** umożliwia użytkownikom dodawanie nowych zestawów liczb LOTTO do swojego konta. Użytkownik przesyła 6 unikalnych liczb z zakresu 1-49, które są walidowane i zapisywane w bazie danych jako zestaw powiązany z kontem użytkownika. System automatycznie sprawdza:
- Poprawność formatu danych
- Unikalność liczb w zestawie
- Limit 100 zestawów na użytkownika
- Unikalność zestawu (niezależnie od kolejności liczb)

Endpoint zwraca ID utworzonego zestawu oraz komunikat o sukcesie.

---

## 2. Szczegóły żądania

- **Metoda HTTP:** POST
- **Struktura URL:** `/api/tickets`
- **Autoryzacja:** Wymagany JWT token w header `Authorization: Bearer <token>`

### Parametry:
- **Wymagane:**
  - Header: `Authorization: Bearer <token>` (JWT)
  - Request Body: JSON z polem `numbers`

- **Opcjonalne:** Brak

### Request Body:
```json
{
  "groupName": "Ulubione",
  "numbers": [5, 14, 23, 29, 37, 41]
}
```

**Struktura:**
- `groupName` (string, opcjonalne): Nazwa grupy do grupowania zestawów (max 100 znaków, domyślnie pusty string)
- `numbers` (int[]): Tablica dokładnie 6 unikalnych liczb całkowitych w zakresie 1-49

---

## 3. Wykorzystywane typy

### 3.1 Contracts (DTO)

**Plik:** `Features/Tickets/Contracts.cs`

```csharp
using MediatR;

namespace LottoTM.Server.Api.Features.Tickets;

public class Contracts
{
    /// <summary>
    /// Request do utworzenia nowego zestawu liczb LOTTO
    /// </summary>
    /// <param name="GroupName">Nazwa grupy (opcjonalne, max 100 znaków)</param>
    /// <param name="Numbers">Tablica 6 unikalnych liczb w zakresie 1-49</param>
    public record CreateTicketRequest(string? GroupName, int[] Numbers) : IRequest<CreateTicketResponse>;

    /// <summary>
    /// Response po utworzeniu zestawu
    /// </summary>
    /// <param name="Id">ID utworzonego zestawu</param>
    /// <param name="Message">Komunikat o sukcesie</param>
    public record CreateTicketResponse(int Id, string Message);
}
```

### 3.2 Encje bazodanowe (już istniejące)

- `Ticket` - metadane zestawu (Id, UserId, GroupName, CreatedAt)
- `TicketNumber` - pojedyncza liczba w zestawie (Id, TicketId, Number, Position)

### 3.3 Modele pomocnicze (opcjonalnie w Handler)

```csharp
// Wewnętrzny model do przechowywania liczby i pozycji
private record TicketNumberData(int Number, byte Position);
```

---

## 4. Szczegóły odpowiedzi

### Sukces (201 Created):
```json
{
  "id": 1,
  "message": "Zestaw utworzony pomyślnie"
}
```

### Błąd walidacji (400 Bad Request):
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Błąd walidacji",
  "status": 400,
  "errors": [
    "Zestaw musi zawierać dokładnie 6 liczb",
    "Wszystkie liczby muszą być w zakresie 1-49"
  ]
}
```

### Błąd limitu (400 Bad Request):
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Błąd walidacji",
  "status": 400,
  "errors": [
    "Osiągnięto limit 100 zestawów. Usuń istniejące zestawy, aby dodać nowe."
  ]
}
```

### Błąd duplikatu (400 Bad Request):
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Błąd walidacji",
  "status": 400,
  "errors": [
    "Taki zestaw już istnieje w Twoim koncie"
  ]
}
```

### Brak autoryzacji (401 Unauthorized):
```json
{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
  "title": "Unauthorized",
  "status": 401
}
```

---

## 5. Przepływ danych

### 5.1 Diagram przepływu

```
[Klient] 
   ↓ POST /api/tickets + JWT
[Endpoint.cs] 
   ↓ Wyodrębnienie UserId z JWT Claims
   ↓ Utworzenie CreateTicketRequest
[MediatR]
   ↓ Send(request)
[Validator.cs]
   ↓ Walidacja FluentValidation (format, zakres, unikalność liczb)
[Handler.cs]
   ↓ 1. Sprawdzenie limitu (COUNT)
   ↓ 2. Sprawdzenie unikalności zestawu (SELECT + algorytm porównania)
   ↓ 3. Utworzenie transakcji
   ↓    - INSERT Ticket (ID auto-generated)
   ↓    - INSERT 6x TicketNumber (Position 1-6)
   ↓ 4. Commit transakcji
   ↓ CreateTicketResponse(Id, Message)
[Endpoint.cs]
   ↓ Results.Created($"/api/tickets/{id}", response)
[Klient]
   ↓ 201 Created + Response Body
```

### 5.2 Interakcje z bazą danych

**Tabele:**
- `Tickets` - INSERT 1 rekord (Id, UserId, CreatedAt)
- `TicketNumbers` - INSERT 6 rekordów (Id, TicketId, Number, Position)

**Zapytania:**

1. **Sprawdzenie limitu:**
```sql
SELECT COUNT(*) FROM Tickets WHERE UserId = @userId
```

2. **Pobranie istniejących zestawów dla walidacji unikalności:**
```sql
SELECT t.Id, tn.Number 
FROM Tickets t
INNER JOIN TicketNumbers tn ON t.Id = tn.TicketId
WHERE t.UserId = @userId
ORDER BY t.Id, tn.Position
```

3. **Insert zestawu (transakcja):**
```sql
BEGIN TRANSACTION;

-- 1. Insert Ticket (Id auto-increment)
INSERT INTO Tickets (UserId, CreatedAt) 
VALUES (@userId, GETUTCDATE());

-- 2. Insert TicketNumbers (6x)
INSERT INTO TicketNumbers (TicketId, Number, Position) 
VALUES (@ticketId, @number1, 1),
       (@ticketId, @number2, 2),
       (@ticketId, @number3, 3),
       (@ticketId, @number4, 4),
       (@ticketId, @number5, 5),
       (@ticketId, @number6, 6);

COMMIT;
```

### 5.3 Algorytm sprawdzania unikalności zestawu

```csharp
// 1. Sortowanie nowego zestawu
var newNumbersSorted = request.Numbers.OrderBy(n => n).ToArray();

// 2. Pobranie wszystkich zestawów użytkownika
var existingTickets = await dbContext.Tickets
    .Where(t => t.UserId == userId)
    .Include(t => t.Numbers)
    .ToListAsync(cancellationToken);

// 3. Porównanie z każdym istniejącym zestawem
foreach (var ticket in existingTickets)
{
    var existingNumbersSorted = ticket.Numbers
        .OrderBy(n => n.Number)
        .Select(n => n.Number)
        .ToArray();
    
    // 4. Jeśli posortowane zestawy są identyczne → duplikat
    if (newNumbersSorted.SequenceEqual(existingNumbersSorted))
    {
        throw new ValidationException("Taki zestaw już istnieje w Twoim koncie");
    }
}
```

---

## 6. Względy bezpieczeństwa

### 6.1 Autoryzacja i uwierzytelnianie

- **JWT Token:** Wymagany w header `Authorization: Bearer <token>`
- **Walidacja tokenu:** Automatyczna przez middleware ASP.NET Core (`app.UseAuthentication()`)
- **Ekstrakcja UserId:** Z JWT Claims (`ClaimTypes.NameIdentifier`)
  ```csharp
  var userId = int.Parse(httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
      ?? throw new UnauthorizedAccessException());
  ```
- **Izolacja danych:** UserId z tokenu zapewnia, że użytkownik modyfikuje tylko swoje dane

### 6.2 Walidacja danych

- **FluentValidation:** Walidacja formatu i zakresu wartości przed przejściem do Handler
- **CHECK Constraints:** Baza danych wymusza `Number` ∈ [1, 49] i `Position` ∈ [1, 6]
- **Unique Constraint:** `UQ_TicketNumbers_TicketPosition` zapewnia unikalność (TicketId, Position)

### 6.3 Ochrona przed atakami

| Atak | Ochrona |
|------|---------|
| **SQL Injection** | Entity Framework Core (parametryzowane zapytania) |
| **Mass Assignment** | Użycie `record` z explicite zdefiniowanymi polami |
| **CSRF** | Nie dotyczy API (brak cookies dla JWT) |
| **Rate Limiting** | Limit 100 zestawów/użytkownik (logika biznesowa) |
| **Manipulation UserId** | UserId ZAWSZE z JWT, nigdy z request body |
| **JWT Token Theft** | HTTPS (wymuszane w produkcji), krótki czas ważności tokenu |

### 6.4 Bezpieczeństwo transakcji

- **Atomowość:** Insert Ticket + Insert TicketNumbers w jednej transakcji
- **Izolacja:** Default isolation level SQL Server (READ COMMITTED)
- **Rollback:** Automatyczny w przypadku błędu w transakcji EF Core

---

## 7. Obsługa błędów

### 7.1 Rodzaje błędów

| Typ błędu | Kod HTTP | Obsługa | Przykład |
|-----------|----------|---------|----------|
| **Brak autoryzacji** | 401 | ASP.NET Middleware | Brak/nieprawidłowy token JWT |
| **Błąd walidacji (format)** | 400 | FluentValidation → ValidationException | `numbers.length != 6` |
| **Błąd walidacji (biznes)** | 400 | Custom ValidationException w Handler | Limit 100 osiągnięty |
| **Duplikat zestawu** | 400 | Custom ValidationException w Handler | Zestaw już istnieje |
| **Błąd bazy danych** | 500 | ExceptionHandlingMiddleware | Connection timeout, constraint violation |
| **Nieoczekiwany błąd** | 500 | ExceptionHandlingMiddleware + Serilog | Null reference, etc. |

### 7.2 Szczegółowe scenariusze

**1. Walidacja FluentValidation (Validator.cs):**
```csharp
// Jeśli walidacja nie przejdzie, rzucany jest ValidationException
// Middleware przekształca to na 400 Bad Request z listą błędów
throw new ValidationException(validationResult.Errors);
```

**2. Limit zestawów (Handler.cs):**
```csharp
var ticketCount = await dbContext.Tickets.CountAsync(t => t.UserId == userId, cancellationToken);
if (ticketCount >= 100)
{
    throw new ValidationException("Osiągnięto limit 100 zestawów. Usuń istniejące zestawy, aby dodać nowe.");
}
```

**3. Duplikat zestawu (Handler.cs):**
```csharp
if (IsDuplicate(request.Numbers, existingTickets))
{
    throw new ValidationException("Taki zestaw już istnieje w Twoim koncie");
}
```

**4. Błąd bazy danych:**
```csharp
// Middleware łapie DbUpdateException i zwraca 500 + log do Serilog
catch (DbUpdateException ex)
{
    logger.LogError(ex, "Database error while creating ticket for user {UserId}", userId);
    throw; // Middleware obsłuży
}
```

### 7.3 Struktura odpowiedzi błędu

Zgodna z RFC 7807 (Problem Details):
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Błąd walidacji",
  "status": 400,
  "errors": [
    "Szczegóły błędu 1",
    "Szczegóły błędu 2"
  ],
  "traceId": "00-abc123..."
}
```

---

## 8. Rozważania dotyczące wydajności

### 8.1 Potencjalne wąskie gardła

| Problem | Impact | Rozwiązanie |
|---------|--------|-------------|
| **Sprawdzanie unikalności zestawu** | O(n*m) gdzie n=liczba zestawów użytkownika, m=6 | Maksymalnie 100 zestawów → akceptowalne (600 porównań) |
| **Multiple INSERT dla TicketNumbers** | 6 operacji INSERT | Bulk insert w transakcji (EF Core optymalizuje) |
| **Brak indeksu na UserId** | Wolne COUNT i SELECT | ✅ Indeks `IX_Tickets_UserId` już istnieje |
| **Eager loading Numbers** | N+1 problem | ✅ `.Include(t => t.Numbers)` zapobiega |

### 8.2 Optymalizacje

**1. Wykorzystanie istniejących indeksów:**
- `IX_Tickets_UserId` - szybkie filtrowanie zestawów użytkownika
- `IX_TicketNumbers_TicketId` - szybkie JOIN z TicketNumbers

**2. Eager Loading:**
```csharp
var existingTickets = await dbContext.Tickets
    .Where(t => t.UserId == userId)
    .Include(t => t.Numbers) // Zapobiega N+1 query problem
    .ToListAsync(cancellationToken);
```

**3. Bulk Insert (EF Core):**
```csharp
// EF Core automatycznie optymalizuje do jednego batch INSERT
var ticketNumbers = numbers.Select((num, idx) => new TicketNumber
{
    TicketId = ticket.Id,
    Number = num,
    Position = (byte)(idx + 1)
}).ToList();

dbContext.TicketNumbers.AddRange(ticketNumbers);
await dbContext.SaveChangesAsync(cancellationToken);
```

**4. Asynchroniczne operacje:**
- Wszystkie operacje DB z `async/await` (CountAsync, ToListAsync, SaveChangesAsync)
- Nie blokuje wątku podczas oczekiwania na bazę danych

### 8.3 Metryki wydajności (szacowane)

| Operacja | Czas | Uzasadnienie |
|----------|------|--------------|
| Walidacja FluentValidation | <1ms | W pamięci, proste reguły |
| COUNT zestawów | 5-10ms | Indeks `IX_Tickets_UserId` |
| SELECT zestawów + Numbers | 10-30ms | Eager loading, max 100 rekordów |
| Algorytm unikalności | 1-5ms | 100 zestawów * 6 liczb = 600 porównań |
| INSERT Ticket + Numbers | 10-20ms | Transakcja, batch insert |
| **TOTAL** | **~50-70ms** | Akceptowalne dla MVP |

### 8.4 Skalowanie w przyszłości

Jeśli wydajność stanie się problemem (>1000 użytkowników równocześnie):
- **Caching:** Redis dla często używanych zestawów
- **Database:** Dodanie computed column z hash zestawu dla szybszego porównania
- **Sharding:** Partycjonowanie tabeli Tickets po UserId
- **Async Processing:** Queue (RabbitMQ) dla operacji nie-krytycznych

---

## 9. Etapy wdrożenia

### 9.1 Przygotowanie struktury (Features/Tickets)

**Krok 1:** Utworzenie folderu i plików
```
src/server/LottoTM.Server.Api/Features/Tickets/
├── Endpoint.cs      (definicja endpointu)
├── Handler.cs       (logika biznesowa)
├── Contracts.cs     (DTO: Request + Response)
└── Validator.cs     (walidacja FluentValidation)
```

### 9.2 Implementacja Contracts.cs

**Krok 2:** Definicja typów DTO
```csharp
using MediatR;

namespace LottoTM.Server.Api.Features.Tickets;

public class Contracts
{
    public record CreateTicketRequest(int[] Numbers) : IRequest<CreateTicketResponse>;
    public record CreateTicketResponse(int Id, string Message);
}
```

### 9.3 Implementacja Validator.cs

**Krok 3:** Walidacja FluentValidation
```csharp
using FluentValidation;

namespace LottoTM.Server.Api.Features.Tickets;

public class CreateTicketValidator : AbstractValidator<Contracts.CreateTicketRequest>
{
    public CreateTicketValidator()
    {
        RuleFor(x => x.Numbers)
            .NotNull()
            .WithMessage("Pole 'numbers' jest wymagane");

        RuleFor(x => x.Numbers)
            .Must(numbers => numbers != null && numbers.Length == 6)
            .WithMessage("Zestaw musi zawierać dokładnie 6 liczb");

        RuleFor(x => x.Numbers)
            .Must(numbers => numbers != null && numbers.All(n => n >= 1 && n <= 49))
            .WithMessage("Wszystkie liczby muszą być w zakresie 1-49");

        RuleFor(x => x.Numbers)
            .Must(numbers => numbers != null && numbers.Distinct().Count() == numbers.Length)
            .WithMessage("Liczby w zestawie muszą być unikalne");
    }
}
```

### 9.4 Implementacja Handler.cs

**Krok 4:** Logika biznesowa
```csharp
using FluentValidation;
using LottoTM.Server.Api.Entities;
using LottoTM.Server.Api.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LottoTM.Server.Api.Features.Tickets;

public class CreateTicketHandler : IRequestHandler<Contracts.CreateTicketRequest, Contracts.CreateTicketResponse>
{
    private readonly AppDbContext _dbContext;
    private readonly IValidator<Contracts.CreateTicketRequest> _validator;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<CreateTicketHandler> _logger;

    public CreateTicketHandler(
        AppDbContext dbContext,
        IValidator<Contracts.CreateTicketRequest> validator,
        IHttpContextAccessor httpContextAccessor,
        ILogger<CreateTicketHandler> logger)
    {
        _dbContext = dbContext;
        _validator = validator;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<Contracts.CreateTicketResponse> Handle(
        Contracts.CreateTicketRequest request,
        CancellationToken cancellationToken)
    {
        // 1. Walidacja FluentValidation
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // 2. Pobranie UserId z JWT
        var userId = GetUserIdFromJwt();

        // 3. Sprawdzenie limitu 100 zestawów
        await ValidateTicketLimitAsync(userId, cancellationToken);

        // 4. Sprawdzenie unikalności zestawu
        await ValidateTicketUniquenessAsync(userId, request.Numbers, cancellationToken);

        // 5. Utworzenie zestawu w transakcji
        var ticketId = await CreateTicketAsync(userId, request.Numbers, cancellationToken);

        _logger.LogInformation("Ticket {TicketId} created successfully for user {UserId}", ticketId, userId);

        return new Contracts.CreateTicketResponse(ticketId, "Zestaw utworzony pomyślnie");
    }

    private int GetUserIdFromJwt()
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Nie można zidentyfikować użytkownika");
        }
        return userId;
    }

    private async Task ValidateTicketLimitAsync(int userId, CancellationToken cancellationToken)
    {
        var ticketCount = await _dbContext.Tickets
            .CountAsync(t => t.UserId == userId, cancellationToken);

        if (ticketCount >= 100)
        {
            throw new ValidationException("Osiągnięto limit 100 zestawów. Usuń istniejące zestawy, aby dodać nowe.");
        }
    }

    private async Task ValidateTicketUniquenessAsync(int userId, int[] newNumbers, CancellationToken cancellationToken)
    {
        var newNumbersSorted = newNumbers.OrderBy(n => n).ToArray();

        var existingTickets = await _dbContext.Tickets
            .Where(t => t.UserId == userId)
            .Include(t => t.Numbers)
            .ToListAsync(cancellationToken);

        foreach (var ticket in existingTickets)
        {
            var existingNumbersSorted = ticket.Numbers
                .OrderBy(n => n.Number)
                .Select(n => n.Number)
                .ToArray();

            if (newNumbersSorted.SequenceEqual(existingNumbersSorted))
            {
                throw new ValidationException("Taki zestaw już istnieje w Twoim koncie");
            }
        }
    }

    private async Task<int> CreateTicketAsync(int userId, int[] numbers, CancellationToken cancellationToken)
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // 1. Utworzenie Ticket
            var ticket = new Ticket
            {
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Tickets.Add(ticket);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // 2. Utworzenie TicketNumbers (6 liczb)
            var ticketNumbers = numbers.Select((number, index) => new TicketNumber
            {
                TicketId = ticket.Id,
                Number = number,
                Position = (byte)(index + 1)
            }).ToList();

            _dbContext.TicketNumbers.AddRange(ticketNumbers);
            await _dbContext.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return ticket.Id;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
```

### 9.5 Implementacja Endpoint.cs

**Krok 5:** Definicja endpointu
```csharp
using MediatR;
using Microsoft.AspNetCore.Authorization;

namespace LottoTM.Server.Api.Features.Tickets;

public static class Endpoint
{
    public static void AddEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("api/tickets", [Authorize] async (
            Contracts.CreateTicketRequest request,
            IMediator mediator) =>
        {
            var result = await mediator.Send(request);
            return Results.Created($"/api/tickets/{result.Id}", result);
        })
        .WithName("CreateTicket")
        .Produces<Contracts.CreateTicketResponse>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .WithOpenApi();
    }
}
```

### 9.6 Rejestracja endpointu w Program.cs

**Krok 6:** Dodanie do pipeline
```csharp
// W pliku Program.cs, po linii z app.UseAuthorization();
// i przed app.Run();

// Dodaj:
using LottoTM.Server.Api.Features.Tickets;

// W sekcji rejestracji endpointów:
Endpoint.AddEndpoint(app);
```

**Dokładna lokalizacja w Program.cs:**
```csharp
app.UseAuthorization();

// === ENDPOINTS ===
LottoTM.Server.Api.Features.ApiVersion.Endpoint.AddEndpoint(app);
LottoTM.Server.Api.Features.Tickets.Endpoint.AddEndpoint(app); // ← DODAĆ

app.Run();
```

### 9.7 Rejestracja IHttpContextAccessor

**Krok 7:** Dodanie serwisu w Program.cs
```csharp
// W sekcji konfiguracji serwisów (przed var app = builder.Build();)
builder.Services.AddHttpContextAccessor();
```

### 9.8 Testowanie

**Krok 8:** Testy manualne (Swagger/Postman)

**Test 1: Sukces (201 Created)**
```http
POST /api/tickets
Authorization: Bearer eyJhbGc...
Content-Type: application/json

{
  "numbers": [5, 14, 23, 29, 37, 41]
}

Expected: 201 Created
{
  "id": "a3bb189e-8bf9-3888-9912-ace4e6543002",
  "message": "Zestaw utworzony pomyślnie"
}
```

**Test 2: Brak autoryzacji (401)**
```http
POST /api/tickets
Content-Type: application/json

{
  "numbers": [5, 14, 23, 29, 37, 41]
}

Expected: 401 Unauthorized
```

**Test 3: Nieprawidłowa liczba elementów (400)**
```http
POST /api/tickets
Authorization: Bearer eyJhbGc...
Content-Type: application/json

{
  "numbers": [5, 14, 23, 29]
}

Expected: 400 Bad Request
{
  "errors": ["Zestaw musi zawierać dokładnie 6 liczb"]
}
```

**Test 4: Liczba poza zakresem (400)**
```http
POST /api/tickets
Authorization: Bearer eyJhbGc...
Content-Type: application/json

{
  "numbers": [5, 14, 23, 29, 37, 50]
}

Expected: 400 Bad Request
{
  "errors": ["Wszystkie liczby muszą być w zakresie 1-49"]
}
```

**Test 5: Duplikat liczby w zestawie (400)**
```http
POST /api/tickets
Authorization: Bearer eyJhbGc...
Content-Type: application/json

{
  "numbers": [5, 5, 23, 29, 37, 41]
}

Expected: 400 Bad Request
{
  "errors": ["Liczby w zestawie muszą być unikalne"]
}
```

**Test 6: Duplikat zestawu (400)**
```http
// Pierwszy request
POST /api/tickets
{
  "numbers": [5, 14, 23, 29, 37, 41]
}
→ 201 Created

// Drugi request (te same liczby w innej kolejności)
POST /api/tickets
{
  "numbers": [41, 5, 29, 14, 37, 23]
}

Expected: 400 Bad Request
{
  "errors": ["Taki zestaw już istnieje w Twoim koncie"]
}
```

**Test 7: Limit 100 zestawów (400)**
```http
// Po utworzeniu 100 zestawów
POST /api/tickets
{
  "numbers": [1, 2, 3, 4, 5, 6]
}

Expected: 400 Bad Request
{
  "errors": ["Osiągnięto limit 100 zestawów. Usuń istniejące zestawy, aby dodać nowe."]
}
```

### 9.9 Weryfikacja w bazie danych

**Krok 9:** Sprawdzenie danych w SQL Server
```sql
-- 1. Sprawdzenie czy Ticket został utworzony
SELECT * FROM Tickets WHERE UserId = @userId ORDER BY CreatedAt DESC;

-- 2. Sprawdzenie czy TicketNumbers zostały utworzone
SELECT tn.* 
FROM TicketNumbers tn
INNER JOIN Tickets t ON tn.TicketId = t.Id
WHERE t.UserId = @userId
ORDER BY t.CreatedAt DESC, tn.Position;

-- 3. Sprawdzenie liczby zestawów użytkownika
SELECT COUNT(*) as TotalTickets FROM Tickets WHERE UserId = @userId;
```

### 9.10 Checklist implementacji

- [ ] **Utworzono folder** `Features/Tickets`
- [ ] **Utworzono plik** `Contracts.cs` z typami DTO
- [ ] **Utworzono plik** `Validator.cs` z regułami walidacji
- [ ] **Utworzono plik** `Handler.cs` z logiką biznesową
- [ ] **Utworzono plik** `Endpoint.cs` z definicją endpointu
- [ ] **Zarejestrowano** `IHttpContextAccessor` w `Program.cs`
- [ ] **Zarejestrowano** endpoint w `Program.cs`
- [ ] **Przetestowano** scenariusz sukcesu (201)
- [ ] **Przetestowano** brak autoryzacji (401)
- [ ] **Przetestowano** walidację formatu (400)
- [ ] **Przetestowano** duplikat zestawu (400)
- [ ] **Przetestowano** limit 100 zestawów (400)
- [ ] **Zweryfikowano** dane w bazie SQL Server
- [ ] **Sprawdzono** logi w Serilog (brak błędów)

---

## 10. Podsumowanie

Endpoint **POST /api/tickets** został zaprojektowany zgodnie z:
- ✅ Architekturą Vertical Slice Architecture (VSA)
- ✅ Wzorcem MediatR + FluentValidation
- ✅ Specyfikacją z `api-plan.md` i `db-plan.md`
- ✅ Stackiem technologicznym (.NET 8, EF Core, JWT, Serilog)
- ✅ Najlepszymi praktykami bezpieczeństwa i wydajności

**Kluczowe cechy implementacji:**
- Walidacja na 3 poziomach (FluentValidation, logika biznesowa, CHECK constraints)
- Izolacja danych (UserId z JWT)
- Atomowość operacji (transakcje EF Core)
- Sprawdzanie unikalności zestawów niezależnie od kolejności liczb
- Limit 100 zestawów/użytkownik
- Obsługa błędów zgodna z RFC 7807
- Logowanie przez Serilog
- Optymalizacja wydajności (indeksy, eager loading, async/await)

Endpoint jest gotowy do implementacji i testów!
