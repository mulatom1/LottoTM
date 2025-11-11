# Schemat Bazy Danych SQL Server - LottoTM MVP

**Wersja:** 2.2
**Data:** 2025-11-11
**Baza danych:** SQL Server 2022
**ORM:** Entity Framework Core 9

---

## 1. Tabele z kolumnami, typami danych i ograniczeniami

### 1.1 Tabela: Users

**Opis:** Przechowuje dane użytkowników systemu. Każdy użytkownik ma unikalny adres email, zahaszowane hasło oraz flagę uprawnień administratora.

```sql
CREATE TABLE Users (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Email NVARCHAR(255) NOT NULL,
    PasswordHash NVARCHAR(255) NOT NULL,
    IsAdmin BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT UQ_Users_Email UNIQUE (Email)
);

CREATE INDEX IX_Users_Email ON Users(Email);
```

**Kolumny:**
- `Id` - INT IDENTITY, klucz główny, autoincrement
- `Email` - NVARCHAR(255), unikalny, wymagany (login użytkownika)
- `PasswordHash` - NVARCHAR(255), wymagany (hash bcrypt, min. 10 rounds)
- `IsAdmin` - BIT, domyślnie 0 (FALSE), flaga uprawnień administratora
- `CreatedAt` - DATETIME2, domyślnie GETUTCDATE(), data rejestracji (UTC)

**Ograniczenia:**
- PRIMARY KEY na `Id`
- UNIQUE constraint na `Email`
- NOT NULL na wszystkich kolumnach oprócz IsAdmin (ma domyślną wartość)

**Zmiany vs. wersja 1.0:**
- Dodano kolumnę `IsAdmin` - flaga określająca uprawnienia administratora (dodawanie/edycja/usuwanie losowań)
- Zmieniono `Password` na `PasswordHash` dla jasności
- Zmieniono `GETDATE()` na `GETUTCDATE()` dla konsystencji czasowej (UTC)
- Zwiększono długość Email z 100 do 255 znaków

---

### 1.2 Tabela: Draws

**Opis:** Globalny rejestr wyników losowań LOTTO i LOTTO PLUS dostępny dla wszystkich użytkowników. Każda kombinacja (data losowania + typ gry) jest unikalna. Losowania są wprowadzane przez użytkowników z uprawnieniami administratora.

```sql
CREATE TABLE Draws (
    Id INT PRIMARY KEY IDENTITY(1,1),
    DrawDate DATE NOT NULL,
    LottoType NVARCHAR(20) NOT NULL CHECK (LottoType IN ('LOTTO', 'LOTTO PLUS')),
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CreatedByUserId INT NOT NULL,
    CONSTRAINT UQ_Draws_DrawDateLottoType UNIQUE (DrawDate, LottoType),
    CONSTRAINT FK_Draws_Users FOREIGN KEY (CreatedByUserId)
        REFERENCES Users(Id) ON DELETE CASCADE
);

CREATE INDEX IX_Draws_DrawDate ON Draws(DrawDate);
CREATE INDEX IX_Draws_CreatedByUserId ON Draws(CreatedByUserId);
CREATE INDEX IX_Draws_LottoType ON Draws(LottoType);
```

**Kolumny:**
- `Id` - INT IDENTITY, klucz główny
- `DrawDate` - DATE, data losowania (bez godziny)
- `LottoType` - NVARCHAR(20), typ gry ("LOTTO" lub "LOTTO PLUS")
- `CreatedAt` - DATETIME2, data wprowadzenia wyniku do systemu (UTC)
- `CreatedByUserId` - INT, klucz obcy do Users (kto wprowadził wynik)

**Ograniczenia:**
- PRIMARY KEY na `Id`
- UNIQUE constraint na kombinacji (DrawDate, LottoType) - jedno losowanie danego typu na datę
- CHECK constraint na LottoType - tylko wartości 'LOTTO' lub 'LOTTO PLUS'
- FOREIGN KEY `CreatedByUserId` → `Users.Id` z CASCADE DELETE
- NOT NULL na wszystkich kolumnach

**Zmiany vs. wersja 1.0:**
- **USUNIĘTO kolumny Number1-Number6** - zastąpione przez znormalizowaną tabelę `DrawNumbers`
- **DODANO kolumnę `CreatedByUserId`** - tracking który użytkownik (admin) wprowadził wynik losowania
- Zmieniono `GETDATE()` na `GETUTCDATE()` dla konsystencji czasowej (UTC)
- Dodano indeks `IX_Draws_CreatedByUserId` dla wydajności zapytań

**Zmiany vs. wersja 2.1:**
- **DODANO kolumnę `LottoType`** - obsługa różnych typów gier (LOTTO, LOTTO PLUS)
- **ZMIENIONO UNIQUE constraint** - z `DrawDate` na `(DrawDate, LottoType)` - w tym samym dniu może być losowanie LOTTO i LOTTO PLUS
- **DODANO CHECK constraint** - walidacja wartości LottoType na poziomie bazy danych
- **DODANO indeks `IX_Draws_LottoType`** - dla szybkiego filtrowania po typie gry

---

### 1.3 Tabela: DrawNumbers

**Opis:** Przechowuje pojedyncze wylosowane liczby dla każdego losowania. Każde losowanie ma dokładnie 6 liczb w pozycjach 1-6. Znormalizowana struktura danych.

```sql
CREATE TABLE DrawNumbers (
    Id INT PRIMARY KEY IDENTITY(1,1),
    DrawId INT NOT NULL,
    Number INT NOT NULL CHECK (Number BETWEEN 1 AND 49),
    Position TINYINT NOT NULL CHECK (Position BETWEEN 1 AND 6),
    CONSTRAINT FK_DrawNumbers_Draws FOREIGN KEY (DrawId)
        REFERENCES Draws(Id) ON DELETE CASCADE,
    CONSTRAINT UQ_DrawNumbers_DrawPosition UNIQUE (DrawId, Position)
);

CREATE INDEX IX_DrawNumbers_DrawId ON DrawNumbers(DrawId);
CREATE INDEX IX_DrawNumbers_Number ON DrawNumbers(Number);
```

**Kolumny:**
- `Id` - INT IDENTITY, klucz główny
- `DrawId` - INT, klucz obcy do Draws
- `Number` - INT, wylosowana liczba LOTTO (zakres 1-49)
- `Position` - TINYINT, pozycja liczby w losowaniu (1-6)

**Ograniczenia:**
- PRIMARY KEY na `Id`
- FOREIGN KEY `DrawId` → `Draws.Id` z CASCADE DELETE
- CHECK constraint: Number w zakresie 1-49
- CHECK constraint: Position w zakresie 1-6
- UNIQUE constraint na kombinacji (DrawId, Position) - każda pozycja unikalna dla losowania
- NOT NULL na wszystkich kolumnach

**Uwaga implementacyjna:**
- Każde losowanie wymaga utworzenia **dokładnie 6 rekordów** w DrawNumbers (Position 1-6)
- Przy dodawaniu losowania: transakcja 1 Draw + 6 DrawNumbers
- Przy pobieraniu losowania: EF Core `.Include(d => d.Numbers)` dla eager loading
- Walidacja unikalności liczb w losowaniu odbywa się w backendzie (Number1 ≠ Number2 etc.)
- Indeks na `Number` umożliwia szybkie wyszukiwanie losowań zawierających konkretną liczbę

**Korzyści znormalizowanej struktury:**
- Łatwiejsze zapytania typu "znajdź wszystkie losowania z liczbą 7"
- Możliwość rozszerzenia w przyszłości (np. dodanie typu gry: LOTTO, LOTTO PLUS)
- Spójna struktura z tabelą TicketNumbers

---

### 1.4 Tabela: Tickets

**Opis:** Metadane zestawów liczb LOTTO należących do użytkowników. Same liczby przechowywane są w tabeli TicketNumbers. Każdy użytkownik może mieć maksymalnie 100 zestawów (walidacja w backendzie).

```sql
CREATE TABLE Tickets (
    Id INT PRIMARY KEY IDENTITY(1,1),
    UserId INT NOT NULL,
    GroupName NVARCHAR(100) NOT NULL DEFAULT '',
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_Tickets_Users FOREIGN KEY (UserId)
        REFERENCES Users(Id) ON DELETE CASCADE
);

CREATE INDEX IX_Tickets_UserId ON Tickets(UserId);
CREATE INDEX IX_Tickets_GroupName ON Tickets(GroupName);
```

**Kolumny:**
- `Id` - INT IDENTITY, klucz główny, autoincrement
- `UserId` - INT, klucz obcy do Users
- `GroupName` - NVARCHAR(100), wymagane, nazwa grupy dla kilku zestawów (domyślnie pusty string)
- `CreatedAt` - DATETIME2, data utworzenia zestawu (UTC)

**Ograniczenia:**
- PRIMARY KEY na `Id`
- FOREIGN KEY `UserId` → `Users.Id` z CASCADE DELETE
- NOT NULL na wszystkich kolumnach
- DEFAULT '' (pusty string) dla GroupName

**Zmiany vs. wersja 1.0:**
- **USUNIĘTO kolumny Number1-Number6** - zastąpione przez znormalizowaną tabelę `TicketNumbers`
- Zmieniono `GETDATE()` na `GETUTCDATE()` dla konsystencji czasowej (UTC)

**Zmiany vs. wersja 2.1:**
- **DODANO kolumnę `GroupName`** - wymagane pole tekstowe do grupowania zestawów (z domyślną wartością pustego stringa)
- **DODANO indeks `IX_Tickets_GroupName`** - dla szybkiego filtrowania po grupach

**Decyzje projektowe:**
- Brak kolumny `Name` - nazwy generowane w UI (np. "Zestaw #1")
- Brak kolumny `UpdatedAt` - edycja nadpisuje bez historii (Opcja C)
- Limit 100 zestawów/użytkownik - walidacja w backendzie
- Duplikaty zestawów **BLOKOWANE** - walidacja unikalności w backendzie
- **GroupName DEFAULT ''** - zestaw bez grupy ma pusty string
- **GroupName wykorzystanie:** Użytkownik może przypisać tę samą nazwę grupy do wielu zestawów (np. "Rodzina", "Urodziny")

---

### 1.5 Tabela: TicketNumbers

**Opis:** Przechowuje pojedyncze liczby dla każdego zestawu użytkownika. Każdy zestaw ma dokładnie 6 liczb w pozycjach 1-6. Znormalizowana struktura danych.

```sql
CREATE TABLE TicketNumbers (
    Id INT PRIMARY KEY IDENTITY(1,1),
    TicketId INT NOT NULL,
    Number INT NOT NULL CHECK (Number BETWEEN 1 AND 49),
    Position TINYINT NOT NULL CHECK (Position BETWEEN 1 AND 6),
    CONSTRAINT FK_TicketNumbers_Tickets FOREIGN KEY (TicketId)
        REFERENCES Tickets(Id) ON DELETE CASCADE,
    CONSTRAINT UQ_TicketNumbers_TicketPosition UNIQUE (TicketId, Position)
);

CREATE INDEX IX_TicketNumbers_TicketId ON TicketNumbers(TicketId);
CREATE INDEX IX_TicketNumbers_Number ON TicketNumbers(Number);
```

**Kolumny:**
- `Id` - INT IDENTITY, klucz główny
- `TicketId` - INT, klucz obcy do Tickets
- `Number` - INT, liczba w zestawie (zakres 1-49)
- `Position` - TINYINT, pozycja liczby w zestawie (1-6)

**Ograniczenia:**
- PRIMARY KEY na `Id`
- FOREIGN KEY `TicketId` → `Tickets.Id` z CASCADE DELETE
- CHECK constraint: Number w zakresie 1-49
- CHECK constraint: Position w zakresie 1-6
- UNIQUE constraint na kombinacji (TicketId, Position) - każda pozycja unikalna dla zestawu
- NOT NULL na wszystkich kolumnach

**Uwaga implementacyjna:**
- Każdy zestaw wymaga utworzenia **dokładnie 6 rekordów** w TicketNumbers (Position 1-6)
- Przy dodawaniu zestawu: transakcja 1 Ticket + 6 TicketNumbers
- Przy pobieraniu zestawu: EF Core `.Include(t => t.Numbers)` dla eager loading
- Walidacja unikalności liczb w zestawie odbywa się w backendzie (Number1 ≠ Number2 etc.)
- **Walidacja unikalności CAŁEGO zestawu:** Algorytm porównuje 6 liczb niezależnie od kolejności
- Indeks na `Number` umożliwia szybkie wyszukiwanie zestawów zawierających konkretną liczbę

**Algorytm walidacji unikalności zestawu:**

```csharp
// Przed zapisem nowego zestawu
var newNumbersSorted = newNumbers.OrderBy(n => n).ToArray();

var existingTickets = await db.Tickets
    .Where(t => t.UserId == userId)
    .Include(t => t.Numbers)
    .ToListAsync();

foreach (var ticket in existingTickets)
{
    var existingNumbersSorted = ticket.Numbers
        .OrderBy(n => n.Number)
        .Select(n => n.Number)
        .ToArray();
    
    if (newNumbersSorted.SequenceEqual(existingNumbersSorted))
    {
        return BadRequest("Ten zestaw już istnieje");
    }
}
```

**Korzyści znormalizowanej struktury:**
- Łatwiejsze zapytania typu "znajdź wszystkie zestawy zawierające liczbę 7"
- Możliwość rozszerzenia w przyszłości (np. dodanie nazw liczb, statystyk)
- Spójna struktura z tabelą DrawNumbers
- Lepsze wsparcie dla algorytmów weryfikacji wygranych

---

## 2. Relacje między tabelami

### 2.1 Diagram relacji

```
┌──────────────────┐
│      Users       │
│------------------|
│ Id (PK)          │
│ Email            │
│ PasswordHash     │
│ IsAdmin          │
│ CreatedAt        │
└────────┬─────────┘
         │
         │ 1:N (Cascade Delete)
         │
         ▼
┌──────────────────────┐
│      Tickets         │
│----------------------|
│ Id (PK, INT)         │
│ UserId (FK) ────────►│
│ GroupName            │
│ CreatedAt            │
└────────┬─────────────┘
         │
         │ 1:6 (Cascade Delete)
         │
         ▼
┌──────────────────────┐
│   TicketNumbers      │
│----------------------|
│ Id (PK)              │
│ TicketId (FK) ──────►│
│ Number (1-49)        │
│ Position (1-6)       │
└──────────────────────┘

┌──────────────────┐
│      Draws       │
│------------------|
│ Id (PK)          │
│ DrawDate         │
│ LottoType        │
│ CreatedAt        │
│ CreatedByUserId  │──┐
└────────┬─────────┘  │
         │            │ FK (Cascade Delete)
         │ 1:6        │
         │            ▼
         ▼         [Users]
┌──────────────────────┐
│    DrawNumbers       │
│----------------------|
│ Id (PK)              │
│ DrawId (FK) ────────►│
│ Number (1-49)        │
│ Position (1-6)       │
└──────────────────────┘

UQ: (DrawDate, LottoType)
```

### 2.2 Opis relacji

#### Users → Tickets (1:N)

- **Kardynalność:** Jeden użytkownik ma wiele zestawów (0..100)
- **Foreign Key:** `Tickets.UserId` → `Users.Id`
- **Cascade:** ON DELETE CASCADE - usunięcie użytkownika usuwa wszystkie jego zestawy (oraz ich liczby przez drugą kaskadę)
- **Izolacja danych:** Każdy endpoint API filtruje zestawy po `UserId` z JWT tokenu

#### Tickets → TicketNumbers (1:6)

- **Kardynalność:** Jeden zestaw ma dokładnie 6 liczb
- **Foreign Key:** `TicketNumbers.TicketId` → `Tickets.Id`
- **Cascade:** ON DELETE CASCADE - usunięcie zestawu automatycznie usuwa wszystkie jego liczby
- **Walidacja:** Backend musi zapewnić utworzenie dokładnie 6 rekordów przy dodawaniu zestawu
- **Unikalność pozycji:** Constraint `UQ_TicketNumbers_TicketPosition` zapewnia unikalność (TicketId, Position)

#### Users → Draws (1:N) - Tracking

- **Kardynalność:** Jeden użytkownik (admin) może wprowadzić wiele losowań
- **Foreign Key:** `Draws.CreatedByUserId` → `Users.Id`
- **Cascade:** ON DELETE CASCADE - usunięcie użytkownika usuwa losowania przez niego wprowadzone
- **Uwaga:** To relacja **trackingowa**, nie własności. Losowania są globalne (dostępne dla wszystkich), ale system śledzi kto je wprowadził
- **Uprawnienia:** Tylko użytkownicy z `IsAdmin = TRUE` mogą dodawać/edytować/usuwać losowania

#### Draws → DrawNumbers (1:6)

- **Kardynalność:** Jedno losowanie ma dokładnie 6 liczb
- **Foreign Key:** `DrawNumbers.DrawId` → `Draws.Id`
- **Cascade:** ON DELETE CASCADE - usunięcie losowania automatycznie usuwa wszystkie jego liczby
- **Walidacja:** Backend musi zapewnić utworzenie dokładnie 6 rekordów przy dodawaniu losowania
- **Unikalność pozycji:** Constraint `UQ_DrawNumbers_DrawPosition` zapewnia unikalność (DrawId, Position)

#### Draws (Globalny rejestr)

- **Dostępność:** Wszystkie losowania dostępne dla wszystkich użytkowników (do weryfikacji)
- **Unikalność dat:** Constraint `UQ_Draws_DrawDate` zapewnia jedno losowanie na dzień
- **Korzyści:** Eliminacja duplikacji danych, jedna prawda o wyniku losowania
- **Weryfikacja:** Algorytm porównuje zestawy użytkownika (`TicketNumbers`) z wszystkimi losowaniami (`DrawNumbers`) w wybranym zakresie dat

### 2.3 Zmiany vs. wersja 1.0

**Nowe relacje:**
- Tickets → TicketNumbers (1:6) - znormalizowana struktura zestawów
- Draws → DrawNumbers (1:6) - znormalizowana struktura losowań
- Users → Draws (1:N) - tracking autora losowania przez `CreatedByUserId`

**Korzyści znormalizowanej struktury:**
- Elastyczność: łatwiejsze rozszerzenie (np. dodanie typów gier)
- Wydajność zapytań: indeksy na `Number` umożliwiają szybkie wyszukiwanie
- Spójność danych: transakcje zapewniają integralność (1 Ticket/Draw + 6 Numbers)
- Czytelność: separation of concerns (metadane vs. dane)

---

## 3. Indeksy

### 3.1 Indeksy wydajnościowe

```sql
-- Indeks na email (logowanie użytkownika)
CREATE UNIQUE INDEX IX_Users_Email
ON Users(Email);
```

**Cel:** Szybkie wyszukiwanie użytkownika po email (O(log n)). Unikalność email wymuszana przez indeks.

```sql
-- Indeks na UserId w Tickets (filtrowanie zestawów użytkownika)
CREATE INDEX IX_Tickets_UserId
ON Tickets(UserId);
```

**Cel:** Optymalizacja zapytań `WHERE UserId = @userId`. Krytyczny dla wydajności przy 100 zestawach/użytkownik.

```sql
-- Indeks na TicketId w TicketNumbers (JOIN z Tickets)
CREATE INDEX IX_TicketNumbers_TicketId
ON TicketNumbers(TicketId);
```

**Cel:**
- Optymalizacja JOIN między Tickets i TicketNumbers przy pobieraniu zestawów
- Szybkie pobieranie wszystkich liczb dla zestawu
- Wsparcie dla eager loading w EF Core (`.Include(t => t.Numbers)`)

```sql
-- Indeks na Number w TicketNumbers (wyszukiwanie zestawów z konkretną liczbą)
CREATE INDEX IX_TicketNumbers_Number
ON TicketNumbers(Number);
```

**Cel:**
- Opcjonalne zapytania typu "znajdź wszystkie zestawy zawierające liczbę 7"
- Analiza statystyk (najczęściej wybierane liczby)
- Post-MVP: sugestie liczb

```sql
-- Indeks na DrawDate (date range queries + unikalność)
CREATE UNIQUE INDEX IX_Draws_DrawDate
ON Draws(DrawDate);
```

**Cel:**
- Szybkie queries dla date range picker (WHERE DrawDate BETWEEN @from AND @to)
- Unikalność daty losowania (jedno losowanie na dzień)
- Sortowanie wyników po dacie

```sql
-- Indeks na CreatedByUserId w Draws (tracking autora)
CREATE INDEX IX_Draws_CreatedByUserId
ON Draws(CreatedByUserId);
```

**Cel:**
- Opcjonalne zapytanie "kto wprowadził to losowanie?"
- Audyt i raportowanie
- Wsparcie dla CASCADE DELETE przy usunięciu użytkownika

```sql
-- Indeks na DrawId w DrawNumbers (JOIN z Draws)
CREATE INDEX IX_DrawNumbers_DrawId
ON DrawNumbers(DrawId);
```

**Cel:**
- Optymalizacja JOIN między Draws i DrawNumbers przy pobieraniu losowań
- Szybkie pobieranie wszystkich liczb dla losowania
- Wsparcie dla eager loading w EF Core (`.Include(d => d.Numbers)`)

```sql
-- Indeks na Number w DrawNumbers (wyszukiwanie losowań z konkretną liczbą)
CREATE INDEX IX_DrawNumbers_Number
ON DrawNumbers(Number);
```

**Cel:**
- Opcjonalne zapytania typu "znajdź wszystkie losowania z liczbą 7"
- Analiza statystyk wyników losowań
- Post-MVP: rekomendacje na podstawie historii

### 3.2 Strategia wydajności weryfikacji (znormalizowana struktura)

**Pytanie:** Jak znormalizowana struktura wpływa na wydajność weryfikacji?

**Odpowiedź:** Weryfikacja wymaga JOIN, ale indeksy zapewniają optymalizację:

**Algorytm weryfikacji (2 fazy):**

1. **Faza 1: Pobranie danych z bazy**

```sql
-- Pobranie wszystkich zestawów użytkownika z liczbami (1 zapytanie z JOIN)
SELECT t.Id, t.UserId, tn.Number, tn.Position
FROM Tickets t
INNER JOIN TicketNumbers tn ON t.Id = tn.TicketId
WHERE t.UserId = @userId
ORDER BY t.Id, tn.Position;

-- Pobranie wszystkich losowań w zakresie dat z liczbami (1 zapytanie z JOIN)
SELECT d.Id, d.DrawDate, dn.Number, dn.Position
FROM Draws d
INNER JOIN DrawNumbers dn ON d.Id = dn.DrawId
WHERE d.DrawDate BETWEEN @dateFrom AND @dateTo
ORDER BY d.DrawDate DESC, dn.Position;
```

**Wydajność:**
- Indeks `IX_Tickets_UserId` + `IX_TicketNumbers_TicketId` → szybki JOIN
- Indeks `IX_Draws_DrawDate` + `IX_DrawNumbers_DrawId` → szybki JOIN
- EF Core eager loading: 2 zapytania zamiast N+1

2. **Faza 2: Weryfikacja w pamięci (LINQ)**

```csharp
// Grupowanie liczb per zestaw/losowanie w pamięci
var ticketsGrouped = ticketData
    .GroupBy(x => x.TicketId)
    .Select(g => new {
        TicketId = g.Key,
        Numbers = g.OrderBy(x => x.Position).Select(x => x.Number).ToArray()
    });

var drawsGrouped = drawData
    .GroupBy(x => x.DrawId)
    .Select(g => new {
        DrawId = g.Key,
        DrawDate = g.First().DrawDate,
        Numbers = g.OrderBy(x => x.Position).Select(x => x.Number).ToArray()
    });

// Weryfikacja (algorytm jak w wersji 1.0)
foreach (var ticket in ticketsGrouped)
{
    foreach (var draw in drawsGrouped)
    {
        var matches = ticket.Numbers.Intersect(draw.Numbers).ToArray();
        var matchCount = matches.Length;
        // ... rest of logic
    }
}
```

**Złożoność:**
- Faza 1 (SQL): O(n × log n) dla JOIN z indeksami
- Faza 2 (LINQ): O(tickets × draws × 6) = O(100 × 8 × 6) ≈ 4800 operacji
- **Total: < 2 sekundy** (wymaganie NFR-001 spełnione)

**Optymalizacje (jeśli potrzebne):**
- **Batch processing:** Parallel.ForEach dla dużych zbiorów
- **Caching:** Redis dla często weryfikowanych zakresów dat
- **Materialized view:** Pre-computed results dla popularnych zapytań

### 3.3 Porównanie: 6 kolumn vs. znormalizowana struktura

| Aspekt | 6 kolumn (Number1-6) | Znormalizowana (TicketNumbers) |
|--------|----------------------|--------------------------------|
| **Prostota zapytań** | ✅ Prostsze SELECT | ❌ Wymaga JOIN |
| **Wydajność READ** | ✅ 1 wiersz = 1 SELECT | ⚠️ 1 wiersz + 6 JOIN (z indeksami szybkie) |
| **Wydajność weryfikacji** | ✅ Bezpośrednie porównanie | ✅ Równoważna (z indeksami) |
| **Walidacja unikalności** | ❌ Skomplikowana (6 pól) | ✅ Łatwiejsza (WHERE Number = X) |
| **Rozszerzalność** | ❌ Trudna (dodanie pól) | ✅ Łatwa (dodanie kolumn/typów) |
| **Normalizacja** | ❌ Naruszenie 1NF | ✅ Pełna normalizacja |
| **Statystyki/analiza** | ❌ Skomplikowane zapytania | ✅ Proste GROUP BY Number |
| **Storage** | ✅ Mniej wierszy | ⚠️ Więcej wierszy (x6) |

**Decyzja:** Znormalizowana struktura wybrana dla **rozszerzalności** i **czytelności kodu**, mimo niewielkiej złożoności zapytań. Indeksy kompensują overhead JOIN.

---

## 4. Zasady SQL Server (Security)

### 4.1 Izolacja danych użytkowników (Row-Level Security w backendzie)

**Uwaga:** SQL Server ma funkcję Row-Level Security (RLS), ale w MVP implementujemy izolację **w backendzie**, nie w bazie danych.

**Dlaczego nie RLS w MVP?**
- Dodatkowa złożoność (funkcje security policies)
- EF Core + JWT filtry są prostsze i wystarczające
- RLS zalecane dla multi-tenant SaaS, nie dla prostego MVP

### 4.2 Implementacja bezpieczeństwa (Backend - MUST HAVE)

**Krytyczne zabezpieczenie w każdym endpoincie:**

```csharp
// ZAWSZE filtruj po UserId z JWT tokenu
var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

var tickets = await db.Tickets
    .Where(t => t.UserId == currentUserId)
    .ToListAsync();
```

**Przykład zabezpieczenia przed nieautoryzowanym dostępem:**

```csharp
// GET /api/tickets/{id}
var ticket = await db.Tickets
    .Where(t => t.Id == ticketId && t.UserId == currentUserId)
    .FirstOrDefaultAsync();

if (ticket == null)
    return Forbid(); // 403, nie 404 (security by obscurity)
```

**Dlaczego to jest krytyczne?**
- `Tickets.Id` to INT (sekwencyjne), użytkownik może zgadywać ID: `/api/tickets/123`
- Bez filtracji po `UserId`, użytkownik A mógłby zobaczyć zestawy użytkownika B
- ZAWSZE zwracaj 403 Forbidden (nie 404), aby nie ujawniać czy zasób istnieje

### 4.3 Dodatkowe zabezpieczenia

**Constraints w bazie:**
```sql
-- CHECK constraints dla zakresu liczb (1-49) w TicketNumbers i DrawNumbers
ALTER TABLE TicketNumbers ADD CONSTRAINT CK_TicketNumbers_Number 
    CHECK (Number BETWEEN 1 AND 49);
ALTER TABLE TicketNumbers ADD CONSTRAINT CK_TicketNumbers_Position 
    CHECK (Position BETWEEN 1 AND 6);

ALTER TABLE DrawNumbers ADD CONSTRAINT CK_DrawNumbers_Number 
    CHECK (Number BETWEEN 1 AND 49);
ALTER TABLE DrawNumbers ADD CONSTRAINT CK_DrawNumbers_Position 
    CHECK (Position BETWEEN 1 AND 6);
```

**Parametryzowane zapytania (Entity Framework Core):**
- EF Core automatycznie parametryzuje wszystkie zapytania
- Ochrona przed SQL Injection out-of-the-box
- NIE używać `FromSqlRaw()` z konkatenacją stringów

**Rate Limiting (middleware):**
- AspNetCoreRateLimit dla endpointów `/auth/login` i `/auth/register`
- Limit: 5 prób/minutę/IP (NFR-011)

---

## 5. Dodatkowe uwagi i wyjaśnienia

### 5.1 Strategie edycji zestawów (Opcja C)

**Decyzja:** Proste nadpisanie bez historii weryfikacji

**Implementacja:**
```csharp
// PUT /api/tickets/{id}
using var transaction = await db.Database.BeginTransactionAsync();

try
{
    // 1. Pobierz zestaw z liczbami
    var ticket = await db.Tickets
        .Include(t => t.Numbers)
        .FirstOrDefaultAsync(t => t.Id == ticketId && t.UserId == currentUserId);
    
    if (ticket == null)
        return Forbid();
    
    // 2. Usuń stare liczby
    db.TicketNumbers.RemoveRange(ticket.Numbers);
    
    // 3. Dodaj nowe liczby
    for (int i = 0; i < dto.Numbers.Length; i++)
    {
        db.TicketNumbers.Add(new TicketNumber
        {
            TicketId = ticket.Id,
            Number = dto.Numbers[i],
            Position = (byte)(i + 1)
        });
    }
    
    await db.SaveChangesAsync();
    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```

**Co się dzieje z historią weryfikacji?**
- Brak tabeli `TicketVerifications` w MVP - weryfikacja on-demand
- Edycja zestawu nie wpływa na historię (bo historia nie istnieje)
- Po edycji użytkownik może ponownie zweryfikować zestawy

**UI Warning (opcjonalne):**
```javascript
// Modal przy edycji
"Czy na pewno chcesz edytować ten zestaw?"
// Bez wspominania o weryfikacji
```

### 5.2 Strategia weryfikacji wygranych (NFR-001: <2s)

**Algorytm weryfikacji (C# LINQ) - znormalizowana struktura:**

```csharp
// Faza 1: Pobranie danych z bazy z eager loading
var tickets = await db.Tickets
    .Where(t => t.UserId == userId)
    .Include(t => t.Numbers)
    .ToListAsync(); // np. 100 zestawów

var draws = await db.Draws
    .Where(d => d.DrawDate >= dateFrom && d.DrawDate <= dateTo)
    .Include(d => d.Numbers)
    .ToListAsync(); // np. 8 losowań

// Faza 2: Weryfikacja w pamięci
var results = new List<VerificationResult>();

foreach (var ticket in tickets)
{
    // Konwersja TicketNumbers na tablicę liczb
    var ticketNumbers = ticket.Numbers
        .OrderBy(tn => tn.Position)
        .Select(tn => tn.Number)
        .ToArray();

    foreach (var draw in draws)
    {
        // Konwersja DrawNumbers na tablicę liczb
        var drawNumbers = draw.Numbers
            .OrderBy(dn => dn.Position)
            .Select(dn => dn.Number)
            .ToArray();

        // Intersect: liczby wspólne
        var matches = ticketNumbers.Intersect(drawNumbers).ToArray();
        var matchCount = matches.Length;

        if (matchCount >= 3) // Minimalna wygrana
        {
            results.Add(new VerificationResult
            {
                TicketId = ticket.Id,
                DrawId = draw.Id,
                DrawDate = draw.DrawDate,
                MatchCount = matchCount,
                MatchedNumbers = matches
            });
        }
    }
}

return results;
```

**Wydajność:**
- 100 zestawów × 8 losowań = 800 iteracji
- Każda iteracja: konwersja (O(6)) + Intersect (O(6)) = O(12) = stała
- Złożoność: O(tickets × draws × 12) ≈ O(n × m)
- Eager loading eliminuje N+1 problem (2 zapytania zamiast 100+8)
- Dla 100 × 8: ~5ms (znacznie poniżej 2s)

**Optymalizacja (jeśli potrzebna):**
```csharp
// Parallel.ForEach dla dużych zbiorów
Parallel.ForEach(tickets, ticket =>
{
    // ... weryfikacja
});
```

**Cache (post-MVP, jeśli potrzebny):**
- Redis cache dla wyników weryfikacji
- Klucz: `verification:{userId}:{dateFrom}:{dateTo}`
- TTL: 24 godziny

### 5.3 Generator systemowy (9 zestawów)

**Algorytm:**

```csharp
public List<int[]> GenerateSystemTickets()
{
    var tickets = new List<int[]>();
    var random = new Random();
    var pool = Enumerable.Range(1, 49).ToList(); // [1..49]

    // Krok 1: Rozdziel 49 liczb na 9 zestawów (każda liczba min. 1 raz)
    for (int i = 0; i < 9; i++)
    {
        tickets.Add(new int[6]);
    }

    int ticketIndex = 0;
    int positionIndex = 0;

    foreach (var number in pool.OrderBy(x => random.Next()))
    {
        tickets[ticketIndex][positionIndex] = number;

        positionIndex++;
        if (positionIndex == 6)
        {
            positionIndex = 0;
            ticketIndex++;
            if (ticketIndex == 9) ticketIndex = 0;
        }
    }

    // Krok 2: Dopełnij 5 pustych pozycji losowymi liczbami 1-49
    for (int i = 0; i < 9; i++)
    {
        for (int j = 0; j < 6; j++)
        {
            if (tickets[i][j] == 0)
            {
                tickets[i][j] = pool[random.Next(pool.Count)];
            }
        }

        // Sortowanie dla czytelności
        Array.Sort(tickets[i]);
    }

    return tickets;
}
```

**Walidacja algorytmu (testy jednostkowe):**
- Każdy zestaw ma dokładnie 6 liczb
- Każda liczba 1-49 pojawia się minimum raz w 9 zestawach
- Liczby w zestawie są unikalne
- 9 zestawów × 6 liczb = 54 pozycje (49 unikalnych + 5 powtórzeń)

### 5.4 Time zone strategy - UTC

**Decyzja:** UTC (GETUTCDATE()) - przyjęte w wersji 2.0

**Implementacja we wszystkich tabelach:**
```sql
CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
```

**Dlaczego UTC?**
- Przygotowanie na skalowanie międzynarodowe
- Brak problemów z Daylight Saving Time (DST)
- Standardowa praktyka w aplikacjach webowych
- Konwersja do local time odbywa się w UI (JavaScript)

**Frontend - konwersja do local time:**
```javascript
// JavaScript automatycznie konwertuje UTC na local time
const createdAt = new Date(ticket.createdAt); // UTC z backendu
console.log(createdAt.toLocaleString()); // Wyświetli w lokalnej strefie czasowej użytkownika
```

**Backend - zawsze używaj UTC:**
```csharp
// ZAWSZE używaj DateTime.UtcNow
ticket.CreatedAt = DateTime.UtcNow;

// NIE używaj DateTime.Now (local time serwera)
```

### 5.5 Brak tabeli TicketVerifications w MVP

**Decyzja:** Weryfikacja on-demand bez cache

**Dlaczego nie cache w MVP?**
- Dodatkowa złożoność (tabela, CRUD, invalidation logic)
- Wymaganie NFR-001 (<2s) jest osiągalne bez cache
- Cache potrzebny dopiero przy >1000 zestawów/użytkownik lub >100 losowań

**Jeśli w przyszłości potrzebny cache:**

```sql
CREATE TABLE TicketVerifications (
    Id INT PRIMARY KEY IDENTITY(1,1),
    TicketId INT NOT NULL,
    DrawId INT NOT NULL,
    MatchCount INT NOT NULL,
    MatchedNumbers NVARCHAR(50), -- JSON: "[3,12,25]"
    VerifiedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_TicketVerifications_Tickets FOREIGN KEY (TicketId)
        REFERENCES Tickets(Id) ON DELETE CASCADE,
    CONSTRAINT FK_TicketVerifications_Draws FOREIGN KEY (DrawId)
        REFERENCES Draws(Id) ON DELETE CASCADE,
    CONSTRAINT UQ_TicketVerifications_TicketDraw UNIQUE (TicketId, DrawId)
);

CREATE INDEX IX_TicketVerifications_TicketId ON TicketVerifications(TicketId);
CREATE INDEX IX_TicketVerifications_DrawId ON TicketVerifications(DrawId);
```

**Invalidation logic:**
- Przy edycji zestawu: `DELETE FROM TicketVerifications WHERE TicketId = @id`
- Przy dodaniu nowego losowania: wszystkie zestawy muszą być zweryfikowane ponownie

### 5.6 Wskaźnik "nowe losowania" (F-VERIFY-003)

**Wymaganie:** Wizualny wskaźnik o nowych/niesprawdzonych losowaniach

**Problem:** Tabela Draws jest wspólna (bez UserId), jak system wie które losowania są "nowe" dla użytkownika?

**Rozwiązania:**

#### Opcja A: Porównanie z ostatnim logowaniem
```sql
-- Dodaj kolumnę do Users
ALTER TABLE Users ADD LastLoginAt DATETIME2;

-- Query dla nowych losowań
SELECT COUNT(*)
FROM Draws
WHERE CreatedAt > @lastLoginAt;
```

**Zalety:** Proste
**Wady:** Użytkownik może być zalogowany cały czas (LastLoginAt nieaktualne)

#### Opcja B: Tabela UserDraws (tracking)
```sql
CREATE TABLE UserDraws (
    UserId INT NOT NULL,
    DrawId INT NOT NULL,
    ViewedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    PRIMARY KEY (UserId, DrawId),
    CONSTRAINT FK_UserDraws_Users FOREIGN KEY (UserId)
        REFERENCES Users(Id) ON DELETE CASCADE,
    CONSTRAINT FK_UserDraws_Draws FOREIGN KEY (DrawId)
        REFERENCES Draws(Id) ON DELETE CASCADE
);

-- Query dla nowych losowań
SELECT d.*
FROM Draws d
LEFT JOIN UserDraws ud ON d.Id = ud.DrawId AND ud.UserId = @userId
WHERE ud.DrawId IS NULL;
```

**Zalety:** Precyzyjne tracking per user
**Wady:** Dodatkowa tabela, więcej logiki

#### Opcja C: Po stronie aplikacji (localStorage)
```javascript
// Frontend przechowuje date ostatniej weryfikacji
localStorage.setItem('lastVerificationDate', '2025-11-02');

// Query dla nowych losowań
SELECT COUNT(*)
FROM Draws
WHERE CreatedAt > @lastVerificationDate;
```

**Zalety:** Zero zmian w bazie, proste
**Wady:** Dane tracone przy czyszczeniu przeglądarki

**Rekomendacja dla MVP:** Opcja C (localStorage) - najprostsza

### 5.7 Duplikaty zestawów

**Pytanie:** Czy użytkownik może zapisać ten sam zestaw liczb wielokrotnie?

**Decyzja MVP:** NIE, duplikaty **BLOKOWANE** (zmiana vs. wersja 1.0)

**Dlaczego?**
- Zgodnie z PRD: walidacja unikalności zestawów dla użytkownika
- Lepsze UX - system ostrzega przed przypadkowym dodaniem duplikatu
- Oszczędność miejsca w bazie (limit 100 zestawów)
- W UI można pokazać warning: "Ten zestaw już istnieje. Czy chcesz dodać duplikat?" (opcjonalnie)

**Implementacja (walidacja w backendzie - znormalizowana struktura):**

```csharp
// Przed zapisem sprawdź czy zestaw już istnieje
var newNumbersSorted = numbers.OrderBy(n => n).ToArray();

var existingTickets = await db.Tickets
    .Where(t => t.UserId == userId)
    .Include(t => t.Numbers)
    .ToListAsync();

foreach (var ticket in existingTickets)
{
    var existingNumbersSorted = ticket.Numbers
        .OrderBy(tn => tn.Number)
        .Select(tn => tn.Number)
        .ToArray();
    
    if (newNumbersSorted.SequenceEqual(existingNumbersSorted))
    {
        return BadRequest("Ten zestaw już istnieje");
    }
}

// Jeśli nie znaleziono duplikatu, zapisz zestaw
```

**Uwaga:** Ze względu na znormalizowaną strukturę (TicketNumbers), nie możemy użyć prostego UNIQUE constraint na bazie danych. Walidacja musi być po stronie aplikacji.

**Alternatywa (computed column z hash - bardziej złożona):**

```sql
-- Dodaj computed column z hashcode do Tickets
ALTER TABLE Tickets ADD NumbersHash AS
    (SELECT HASHBYTES('SHA2_256', 
        STRING_AGG(CAST(Number AS VARCHAR), ',') WITHIN GROUP (ORDER BY Number))
     FROM TicketNumbers 
     WHERE TicketId = Tickets.Id) PERSISTED;

-- UNIQUE constraint na kombinacji UserId + NumbersHash
CREATE UNIQUE INDEX UQ_Tickets_UserIdNumbersHash
ON Tickets(UserId, NumbersHash);
```

**Decyzja:** Walidacja w backendzie (prostsze dla MVP, wystarczające dla wydajności)

### 5.8 Nazwa zestawu w UI (brak kolumny Name)

**Decyzja:** Tabela Tickets bez pola `Name`

**Rozwiązania w UI:**

#### Opcja 1: Generowana nazwa w liście (pozycja)
```javascript
// Frontend
tickets.map((ticket, index) => (
  <div>Zestaw #{index + 1}: [{ticket.numbers.join(', ')}]</div>
))
```
**Przykład:** "Zestaw #1: [3, 12, 25, 31, 42, 48]"

#### Opcja 2: Tylko liczby (bez nazwy)
```javascript
tickets.map(ticket => (
  <div>[{ticket.numbers.join(', ')}]</div>
))
```
**Przykład:** "[3, 12, 25, 31, 42, 48]"

#### Opcja 3: Pierwsza liczba jako identyfikator
```javascript
tickets.map(ticket => (
  <div>Zestaw {ticket.numbers[0]}, {ticket.numbers[1]}, ...</div>
))
```
**Przykład:** "Zestaw 3, 12, ..."

**Rekomendacja:** Opcja 1 (generowana nazwa) - najczytelniejsza

**Jeśli w przyszłości dodać nazwę:**
```sql
ALTER TABLE Tickets ADD Name NVARCHAR(100) NULL;
```

### 5.9 Migracje Entity Framework Core

**Setup migracji:**

```bash
# Utworzenie pierwszej migracji
dotnet ef migrations add InitialCreate

# Aplikacja migracji do bazy
dotnet ef database update
```

**DbContext configuration (OnModelCreating) - znormalizowana struktura:**

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Users
    modelBuilder.Entity<User>(entity =>
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => e.Email).IsUnique();
        entity.Property(e => e.Email).HasMaxLength(255).IsRequired();
        entity.Property(e => e.PasswordHash).HasMaxLength(255).IsRequired();
        entity.Property(e => e.IsAdmin).HasDefaultValue(false).IsRequired();
        entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
    });

    // Tickets
    modelBuilder.Entity<Ticket>(entity =>
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => e.UserId);
        entity.HasIndex(e => e.GroupName);
        entity.Property(e => e.GroupName).HasMaxLength(100);
        entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

        // Relacja do Users z CASCADE DELETE
        entity.HasOne(e => e.User)
            .WithMany(u => u.Tickets)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    });

    // TicketNumbers
    modelBuilder.Entity<TicketNumber>(entity =>
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => e.TicketId);
        entity.HasIndex(e => e.Number);
        
        // Relacja do Tickets z CASCADE DELETE
        entity.HasOne(e => e.Ticket)
            .WithMany(t => t.Numbers)
            .HasForeignKey(e => e.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        // CHECK constraints
        entity.ToTable(t => t.HasCheckConstraint("CK_TicketNumbers_Number", "Number BETWEEN 1 AND 49"));
        entity.ToTable(t => t.HasCheckConstraint("CK_TicketNumbers_Position", "Position BETWEEN 1 AND 6"));
        
        // UNIQUE constraint na (TicketId, Position)
        entity.HasIndex(e => new { e.TicketId, e.Position }).IsUnique();
    });

    // Draws
    modelBuilder.Entity<Draw>(entity =>
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => e.DrawDate);
        entity.HasIndex(e => e.CreatedByUserId);
        entity.HasIndex(e => e.LottoType);
        entity.Property(e => e.DrawDate).IsRequired();
        entity.Property(e => e.LottoType).HasMaxLength(20).IsRequired();
        entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

        // UNIQUE constraint na (DrawDate, LottoType)
        entity.HasIndex(e => new { e.DrawDate, e.LottoType }).IsUnique();

        // CHECK constraint na LottoType
        entity.ToTable(t => t.HasCheckConstraint("CK_Draws_LottoType", "LottoType IN ('LOTTO', 'LOTTO PLUS')"));

        // Relacja do Users (tracking autora) z CASCADE DELETE
        entity.HasOne(e => e.CreatedByUser)
            .WithMany(u => u.CreatedDraws)
            .HasForeignKey(e => e.CreatedByUserId)
            .OnDelete(DeleteBehavior.Cascade);
    });

    // DrawNumbers
    modelBuilder.Entity<DrawNumber>(entity =>
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => e.DrawId);
        entity.HasIndex(e => e.Number);
        
        // Relacja do Draws z CASCADE DELETE
        entity.HasOne(e => e.Draw)
            .WithMany(d => d.Numbers)
            .HasForeignKey(e => e.DrawId)
            .OnDelete(DeleteBehavior.Cascade);

        // CHECK constraints
        entity.ToTable(t => t.HasCheckConstraint("CK_DrawNumbers_Number", "Number BETWEEN 1 AND 49"));
        entity.ToTable(t => t.HasCheckConstraint("CK_DrawNumbers_Position", "Position BETWEEN 1 AND 6"));
        
        // UNIQUE constraint na (DrawId, Position)
        entity.HasIndex(e => new { e.DrawId, e.Position }).IsUnique();
    });
}
```

**Encje (models):**

```csharp
// User.cs
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

// Ticket.cs
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

// TicketNumber.cs
public class TicketNumber
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public int Number { get; set; }
    public byte Position { get; set; }
    
    // Navigation property
    public Ticket Ticket { get; set; } = null!;
}

// Draw.cs
public class Draw
{
    public int Id { get; set; }
    public DateTime DrawDate { get; set; }
    public string LottoType { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public int CreatedByUserId { get; set; }
    
    // Navigation properties
    public User CreatedByUser { get; set; } = null!;
    public ICollection<DrawNumber> Numbers { get; set; } = new List<DrawNumber>();
}

// DrawNumber.cs
public class DrawNumber
{
    public int Id { get; set; }
    public int DrawId { get; set; }
    public int Number { get; set; }
    public byte Position { get; set; }
    
    // Navigation property
    public Draw Draw { get; set; } = null!;
}
```

**Przykłady użycia (Eager Loading):**

```csharp
// Pobranie zestawu z liczbami
var ticket = await db.Tickets
    .Include(t => t.Numbers.OrderBy(tn => tn.Position))
    .FirstOrDefaultAsync(t => t.Id == ticketId && t.UserId == userId);

// Pobranie wszystkich zestawów użytkownika z liczbami
var tickets = await db.Tickets
    .Where(t => t.UserId == userId)
    .Include(t => t.Numbers.OrderBy(tn => tn.Position))
    .OrderByDescending(t => t.CreatedAt)
    .ToListAsync();

// Pobranie losowania z liczbami
var draw = await db.Draws
    .Include(d => d.Numbers.OrderBy(dn => dn.Position))
    .FirstOrDefaultAsync(d => d.Id == drawId);

// Pobranie losowań w zakresie dat z liczbami
var draws = await db.Draws
    .Where(d => d.DrawDate >= dateFrom && d.DrawDate <= dateTo)
    .Include(d => d.Numbers.OrderBy(dn => dn.Position))
    .OrderByDescending(d => d.DrawDate)
    .ToListAsync();
```

**Dodawanie zestawu z liczbami (transakcja):**

```csharp
using var transaction = await db.Database.BeginTransactionAsync();

try
{
    // 1. Utworzenie zestawu
    var ticket = new Ticket
    {
        UserId = userId,
        GroupName = groupName, // pusty string jeśli brak grupy
        CreatedAt = DateTime.UtcNow
    };
    db.Tickets.Add(ticket);
    await db.SaveChangesAsync(); // Zapisz, aby otrzymać ticket.Id

    // 2. Dodanie 6 liczb
    for (int i = 0; i < numbers.Length; i++)
    {
        db.TicketNumbers.Add(new TicketNumber
        {
            TicketId = ticket.Id,
            Number = numbers[i],
            Position = (byte)(i + 1)
        });
    }
    await db.SaveChangesAsync();

    await transaction.CommitAsync();
    return ticket.Id;
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```

---

## 6. Podsumowanie kluczowych decyzji

### ✅ Zaakceptowane w planowaniu

| Decyzja | Uzasadnienie | Zmiana vs. v1.0 |
|---------|--------------|-----------------|
| **Znormalizowana struktura** | TicketNumbers/DrawNumbers zamiast Number1-6 | ✅ BEZ ZMIAN |
| **INT dla Tickets.Id** | Prostsza struktura z IDENTITY autoincrement | ✅ BEZ ZMIAN |
| **GroupName w Tickets** | Opcjonalne pole do grupowania zestawów | ✅ NOWE (v2.2) |
| **LottoType w Draws** | Obsługa LOTTO i LOTTO PLUS | ✅ NOWE (v2.2) |
| **IsAdmin w Users** | Flaga uprawnień administratora | ✅ BEZ ZMIAN |
| **CreatedByUserId w Draws** | Tracking autora losowania | ✅ NOWE |
| **UTC dla timestamps** | GETUTCDATE() zamiast GETDATE() | ✅ ZMIENIONE |
| **Draws globalny rejestr** | Wspólne losowania dla wszystkich, ale z tracking autora | ✅ ZMIENIONE |
| **Hard delete (CASCADE)** | Proste, bez komplikacji soft delete | ✅ BEZ ZMIAN |
| **Brak tabeli TicketVerifications** | Weryfikacja on-demand wystarczy dla MVP | ✅ BEZ ZMIAN |
| **Opcja C dla edycji** | Nadpisanie bez historii - najprostsze dla MVP | ✅ BEZ ZMIAN |
| **Walidacja unikalności w backendzie** | Porównanie 6 liczb niezależnie od kolejności | ✅ BEZ ZMIAN |
| **Duplikaty zestawów BLOKOWANE** | Walidacja unikalności dla użytkownika | ✅ ZMIENIONE |
| **Brak kolumny Name w Tickets** | Nazwy generowane w UI | ✅ BEZ ZMIAN |

### ⚠️ Do rozważenia w przyszłości

| Kwestia | Rozwiązanie post-MVP |
|---------|----------------------|
| **Materialized views** | Pre-computed results dla częstych weryfikacji |
| **Tabela TicketVerifications** | Cache wyników weryfikacji jeśli >2s |
| **Tabela UserDraws** | Tracking nowych losowań per user |
| **Kolumna Name w Tickets** | Własne nazwy zestawów |
| **Computed column dla unikalności** | Hash zestawu zamiast walidacji w backendzie |
| **Partitioning** | Dla bardzo dużych tabel (miliony rekordów) |

### 📊 Główne zmiany w wersji 2.0

**1. Normalizacja struktury danych:**
- **Było:** Tickets(Number1, Number2, ..., Number6)
- **Jest:** Tickets + TicketNumbers(Number, Position)
- **Powód:** Rozszerzalność, lepsze zapytania analityczne

**2. Tracking autora losowań:**
- **Dodano:** `Draws.CreatedByUserId` (FK do Users)
- **Powód:** Audyt, uprawnienia administratora

**3. Flaga administratora:**
- **Dodano:** `Users.IsAdmin` (BIT)
- **Powód:** Kontrola dostępu do dodawania/edycji losowań

**4. UTC zamiast Local time:**
- **Zmieniono:** GETDATE() → GETUTCDATE()
- **Powód:** Przygotowanie na skalowanie międzynarodowe

### 📊 Główne zmiany w wersji 2.2

**1. Grupowanie zestawów:**
- **Dodano:** `Tickets.GroupName` (NVARCHAR(100) NOT NULL DEFAULT '')
- **Powód:** Umożliwienie użytkownikom organizowania zestawów w grupy (np. "Rodzina", "Urodziny")
- **Indeks:** IX_Tickets_GroupName dla szybkiego filtrowania

**2. Obsługa różnych typów gier:**
- **Dodano:** `Draws.LottoType` (NVARCHAR(20) NOT NULL)
- **Wartości:** "LOTTO" lub "LOTTO PLUS" (CHECK constraint)
- **Powód:** Wsparcie dla różnych wariantów gry LOTTO
- **Indeks:** IX_Draws_LottoType dla szybkiego filtrowania

**3. Zmiana unikalności losowań:**
- **Było:** UNIQUE constraint na `DrawDate`
- **Jest:** UNIQUE constraint na `(DrawDate, LottoType)`
- **Powód:** W tym samym dniu może odbyć się losowanie LOTTO i LOTTO PLUS

---

## 7. Checklist wdrożenia

### Faza 1: Setup bazy danych

- [ ] Utworzenie bazy danych SQL Server
- [ ] Konfiguracja connection string w appsettings.json
- [ ] Utworzenie modeli Entity Framework:
  - [ ] User.cs (z kolumną IsAdmin)
  - [ ] Ticket.cs (z INT IDENTITY jako Id)
  - [ ] TicketNumber.cs (nowa encja)
  - [ ] Draw.cs (z CreatedByUserId)
  - [ ] DrawNumber.cs (nowa encja)
- [ ] Konfiguracja DbContext z OnModelCreating (relacje, indeksy, constraints)
- [ ] Utworzenie pierwszej migracji (`dotnet ef migrations add InitialCreate`)
- [ ] Aplikacja migracji (`dotnet ef database update`)
- [ ] Weryfikacja struktury tabel (SSMS lub Azure Data Studio):
  - [ ] Users (z IsAdmin)
  - [ ] Tickets (z INT IDENTITY Id)
  - [ ] TicketNumbers (z indeksami)
  - [ ] Draws (z CreatedByUserId)
  - [ ] DrawNumbers (z indeksami)

### Faza 2: Walidacja bezpieczeństwa

- [ ] Implementacja filtrów JWT w backendzie
- [ ] Testy izolacji danych (użytkownik A nie widzi zestawów użytkownika B)
- [ ] Walidacja hashowania haseł (bcrypt, min. 10 rounds)
- [ ] Implementacja uprawnień administratora (IsAdmin check)
- [ ] Walidacja: tylko admin może dodawać/edytować/usuwać losowania
- [ ] Rate limiting dla /auth/login i /auth/register
- [ ] HTTPS wymuszony na produkcji

### Faza 3: Testy integracji znormalizowanej struktury

- [ ] Testy dodawania zestawu (transakcja 1 Ticket + 6 TicketNumbers)
- [ ] Testy pobierania zestawu z liczbami (eager loading)
- [ ] Testy dodawania losowania (transakcja 1 Draw + 6 DrawNumbers)
- [ ] Testy pobierania losowania z liczbami (eager loading)
- [ ] Testy walidacji unikalności zestawów (porównanie 6 liczb)
- [ ] Testy CASCADE DELETE (usunięcie Ticket → automatyczne usunięcie TicketNumbers)
- [ ] Testy weryfikacji wygranych z JOIN (TicketNumbers ⋈ DrawNumbers)

### Faza 4: Performance testing

- [ ] Testy weryfikacji 100 zestawów × 1 losowanie (<2s)
- [ ] Testy zapytań z indeksami (EXPLAIN PLAN / Execution Plan)
- [ ] Optymalizacja JOIN (indeksy na TicketId, DrawId, Number)
- [ ] Optymalizacja algorytmu weryfikacji (parallel processing jeśli potrzebne)
- [ ] Dodanie Redis cache (jeśli weryfikacja >2s)

### Faza 5: Deployment produkcyjny

- [ ] Migracja bazy na środowisko produkcyjne
- [ ] Konfiguracja backupów (1x/dzień minimum)
- [ ] Monitoring błędów bazy danych
- [ ] Dokumentacja schematu dla zespołu
- [ ] Utworzenie pierwszego użytkownika admin (IsAdmin = TRUE)

---

## 8. Historia zmian dokumentu

| Wersja | Data | Autor | Opis zmian |
|--------|------|-------|------------|
| 1.0 | 2025-11-02 | Tomasz Mularczyk | Pierwsza wersja - struktura z kolumnami Number1-6 |
| 2.0 | 2025-11-05 | Tomasz Mularczyk | Normalizacja struktury danych: wprowadzenie TicketNumbers i DrawNumbers, dodanie IsAdmin do Users, dodanie CreatedByUserId do Draws, zmiana GETDATE() na GETUTCDATE(), aktualizacja strategii weryfikacji i indeksów, zmiana polityki duplikatów (blokowanie zamiast dozwolenia), aktualizacja przykładów EF Core |
| 2.1 | 2025-11-05 | Tomasz Mularczyk | Zmiana Tickets.Id z UNIQUEIDENTIFIER (GUID) na INT IDENTITY dla prostszej struktury |
| 2.2 | 2025-11-11 | Tomasz Mularczyk | Rozszerzenie modelu danych: dodanie pola GroupName (NVARCHAR(100) NOT NULL DEFAULT '') do tabeli Tickets dla grupowania zestawów; dodanie pola LottoType (NVARCHAR(20) NOT NULL) z CHECK constraint do tabeli Draws dla obsługi różnych typów gier (LOTTO, LOTTO PLUS); zmiana UNIQUE constraint na Draws z DrawDate na kombinację (DrawDate, LottoType); aktualizacja encji EF Core, DbContext configuration, przykładów użycia oraz diagramu relacji |

---

## Koniec dokumentu schematu bazy danych
