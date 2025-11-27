# LottoWorker - Automatyczne Pobieranie Wyników Losowań

**Wersja:** 1.0
**Data:** 2025-11-15
**Autor:** Tomasz Mularczyk

---

## 1. Wprowadzenie

### 1.1 Cel dokumentu
Ten dokument opisuje implementację i działanie automatycznego workera (`LottoWorker`), który w tle pobiera wyniki losowań LOTTO i LOTTO PLUS z oficjalnego Lotto OpenApi (https://www.lotto.pl).

### 1.2 Cel funkcjonalności
Automatyzacja procesu pobierania wyników losowań eliminuje potrzebę ręcznego wprowadzania wyników przez administratorów i zapewnia, że dane są zawsze aktualne dla wszystkich użytkowników systemu. Worker wykorzystuje dedykowany serwis **LottoOpenApiService**, który komunikuje się z oficjalnym **Lotto OpenApi** (https://www.lotto.pl) do pobierania wyników LOTTO i LOTTO PLUS.

---

## 2. Architektura Workera

### 2.1 Typ workera
`LottoWorker` jest implementacją `BackgroundService` z .NET, co oznacza:
- Uruchamia się automatycznie przy starcie aplikacji
- Działa w tle niezależnie od żądań HTTP
- Jest zarządzany przez hosting .NET (automatyczne restarty w przypadku błędów)

### 2.2 Lokalizacja w projekcie
```
src/server/LottoTM.Server.Api/
├── Services/
│   ├── LottoWorker.cs                             // Główna implementacja workera
│   └── LottoOpenApi/                              // Folder serwisu Lotto OpenApi
│       ├── ILottoOpenApiService.cs                // Interface serwisu
│       ├── LottoOpenApiService.cs                 // Implementacja serwisu
│       └── DTOs/                                  // Data Transfer Objects
│           ├── GetDrawsLottoByDateResponse.cs
│           ├── GetDrawsLottoByDateResponseItem.cs
│           └── GetDrawsLottoByDateResponseResult.cs
├── Options/
│   └── LottoWorkerOptions.cs                      // Konfiguracja workera
```

### 2.3 Zależności
Worker wykorzystuje następujące serwisy:
- `IServiceScopeFactory` - tworzenie scope dla dostępu do scoped services (DbContext, LottoOpenApiService)
- `ILogger<LottoWorker>` - logowanie zdarzeń i błędów
- `IOptions<LottoWorkerOptions>` - konfiguracja parametrów działania
- `IConfiguration` - dostęp do ustawień aplikacji (ApiUrl)
- `AppDbContext` - dostęp do bazy danych
- `ILottoOpenApiService` - dedykowany serwis do pobierania wyników z Lotto OpenApi
- `IHttpClientFactory` - tworzenie HTTP clientów dla pingowania API

**LottoOpenApiService** (wykorzystywany przez Worker):
- `IHttpClientFactory` - tworzenie HTTP clientów dla Lotto OpenApi
- `IConfiguration` - dostęp do ustawień (LottoOpenApi:Url, LottoOpenApi:ApiKey)
- `ILogger<LottoOpenApiService>` - logowanie operacji

---

## 3. Harmonogram Działania

### 3.1 Okno czasowe
Worker działa w **konfigurowalnym oknie czasowym**:
- **Domyślny czas rozpoczęcia**: 22:00 (22:15 w produkcji)
- **Domyślny czas zakończenia**: 23:00
- **Interwał sprawdzania**: co 5 minut

### 3.2 Konfiguracja w appsettings.json
```json
{
  "LottoWorker": {
    "Enable": false,
    "StartTime": "22:15:00",
    "EndTime": "23:00:00",
    "IntervalMinutes": 5,
    "InWeek": ["PN", "WT", "SR", "CZ", "PT", "SO", "ND"]
  },
  "LottoOpenApi": {
    "Url": "https://www.lotto.pl",
    "ApiKey": "your-secret-api-key"
  },
  "ApiUrl": "https://your-api-url.com"
}
```

**Wymagane parametry:**
- `LottoWorker:Enable` - włącza/wyłącza workera
- `LottoWorker:StartTime` - czas rozpoczęcia okna czasowego (format: HH:mm:ss)
- `LottoWorker:EndTime` - czas zakończenia okna czasowego (format: HH:mm:ss)
- `LottoWorker:IntervalMinutes` - interwał sprawdzania w minutach
- `LottoWorker:InWeek` - lista dni tygodnia (skróty: PN, WT, SR, CZ, PT, SO, ND) w których worker ma działać
- `LottoOpenApi:Url` - URL bazowy Lotto OpenApi (https://www.lotto.pl)
- `LottoOpenApi:ApiKey` - klucz API do autoryzacji (header "secret")
- `ApiUrl` - URL własnego API dla keep-alive ping

**Feature Flag - Enable:**
- `Enable: true` - Worker działa w skonfigurowanym oknie czasowym
- `Enable: false` - Worker jest całkowicie wyłączony (nie pobiera wyników)
- **Domyślnie: false** (produkcja), można włączyć w development
- Umożliwia łatwe włączanie/wyłączanie workera bez restartowania aplikacji

### 3.3 Logika czasowa
1. Worker uruchamia się wraz z aplikacją
2. Co 5 minut (domyślnie) sprawdza czy:
   - **Feature Flag `Enable` jest ustawiony na `true`**
   - **Obecny dzień tygodnia jest w liście `InWeek`** (np. ["WT", "PT", "SO"] dla Wtorek, Piątek, Sobota)
   - Jest w aktywnym oknie czasowym (StartTime - EndTime)
3. Jeśli WSZYSTKIE warunki są spełnione → wykonuje ping API + sprawdza i pobiera wyniki (bieżące + archiwalne)
4. Jeśli NIE → czeka kolejne 5 minut i loguje odpowiedni komunikat

**Mapowanie dni tygodnia:**
- PN (Poniedziałek), WT (Wtorek), SR (Środa), CZ (Czwartek), PT (Piątek), SO (Sobota), ND (Niedziela)

**Dlaczego 22:15-23:00?**
- Losowania LOTTO odbywają się wieczorem (zwykle około 22:00)
- Wyniki są publikowane na Lotto.pl krótko po losowaniu
- Okno czasowe daje systemowi czas na pobranie i zapisanie wyników

---

## 4. Proces Pobierania Wyników

### 4.1 Algorytm główny (`ExecuteAsync`)
```
1. START - Pobierz ostatnią datę losowania z bazy (GetLastArchRunTime)
2. START pętli while (!stoppingToken.IsCancellationRequested)
3. Ping API dla utrzymania aktywności (PingForApiVersion)
4. Pobierz aktualny czas (DateTime.Now.TimeOfDay) i dzień tygodnia
5. Sprawdź czy Enable=true ORAZ dzień jest w InWeek ORAZ czas jest w przedziale [StartTime, EndTime]
6. JEŚLI TAK:
   a. Sprawdź czy wyniki na Today już istnieją w bazie (LOTTO i LOTTO PLUS)
   b. JEŚLI NIE ISTNIEJĄ:
      - Pobierz bieżące wyniki z Lotto OpenApi (FetchAndSaveDrawsFromLottoOpenApi dla Today)
      - Zapisz do bazy danych
   c. JEŚLI ISTNIEJĄ:
      - Log informacji i pomiń pobieranie
7. Pobieranie archiwum (jeśli Enable=true):
   a. Zmniejsz lastArchRunTime o 1 dzień
   b. Sprawdź czy dzień jest w InWeek
   c. JEŚLI TAK:
      - Pobierz archiwalne wyniki dla lastArchRunTime (FetchAndSaveArchDrawsFromLottoOpenApi)
      - Zapisz do bazy danych
8. Czekaj IntervalMinutes minut (Task.Delay)
9. GOTO 2
```

### 4.2 Pobieranie bieżące vs archiwalne
- **Pobieranie bieżące**: Wykonywane w oknie czasowym dla daty Today, sprawdza czy wyniki na dzisiejszą datę już istnieją
- **Pobieranie archiwalne**: Stopniowe wypełnianie historycznych danych wstecz od najstarszej daty w bazie do roku 1957
- **Filtrowanie dni tygodnia**: Oba tryby respektują konfigurację `InWeek` - pobierają tylko dni zgodne z harmonogramem losowań

### 4.3 Funkcja pingowania API (`PingForApiVersion`)
```csharp
private async Task PingForApiVersion(CancellationToken stoppingToken)
```

**Cel**: Utrzymanie API "przy życiu" (zapobieganie uśpieniu przez hosting)

**Działanie**:
1. Pobiera `ApiUrl` z konfiguracji
2. Wysyła GET request do `{ApiUrl}/api/version`
3. Loguje sukces lub błąd
4. Obsługuje wyjątki HTTP i inne błędy

---

## 5. Pobieranie i Zapisywanie Wyników

### 5.1 Funkcja pobierania bieżących wyników (`FetchAndSaveDrawsFromLottoOpenApi`)
```csharp
private async Task FetchAndSaveDrawsFromLottoOpenApi(CancellationToken stoppingToken)
```

**Działanie**:
1. Tworzy scope dla dostępu do scoped services (DbContext, ILottoOpenApiService)
2. Wywołuje `ILottoOpenApiService.GetDrawsLottoByDate(DateOnly.FromDateTime(DateTime.Today))` do pobrania wyników
3. Serwis LottoOpenApiService:
   - Konfiguruje HTTP client z wymaganymi headers:
     - `User-Agent: LottoTM.Server.Api.LottoOpenApiService/1.0`
     - `Accept: application/json`
     - `secret: {ApiKey}` (z konfiguracji LottoOpenApi:ApiKey)
   - Buduje URL do Lotto OpenApi:
     - Endpoint: `/api/open/v1/lotteries/draw-results/by-date-per-game`
     - Query params: `drawDate={yyyy-MM-dd}&gameType=Lotto&size=10&sort=drawDate&order=DESC&index=1`
   - Wysyła GET request do Lotto OpenApi
   - Deserializuje JSON response do `GetDrawsLottoByDateResponse` (DTO)
   - Zwraca typowany obiekt `GetDrawsLottoByDateResponse`
4. Worker przetwarza i zapisuje wyniki (LOTTO i LOTTO PLUS) wywołując `SaveInDatabase`
5. Loguje sukces lub błędy

### 5.1b Funkcja pobierania archiwalnych wyników (`FetchAndSaveArchDrawsFromLottoOpenApi`)
```csharp
private async Task FetchAndSaveArchDrawsFromLottoOpenApi(DateOnly date, CancellationToken stoppingToken)
```

**Działanie**:
Identyczne jak `FetchAndSaveDrawsFromLottoOpenApi`, ale dla dowolnej daty historycznej przekazanej jako parametr.

### 5.1c Funkcja określania ostatniej daty archiwum (`GetLastArchRunTime`)
```csharp
private async Task<DateOnly> GetLastArchRunTime(CancellationToken stoppingToken)
```

**Działanie**:
1. Pobiera najstarszą datę losowania z bazy danych (`MinAsync(d => d.DrawDate)`)
2. Zwraca tę datę jako punkt wyjścia dla pobierania archiwum wstecz
3. W przypadku błędu zwraca `DateTime.Today`

**Endpoint Lotto OpenApi (wywoływany przez LottoOpenApiService):**
```
GET https://www.lotto.pl/api/open/v1/lotteries/draw-results/by-date-per-game?drawDate=2025-11-15&gameType=Lotto&size=10&sort=drawDate&order=DESC&index=1
```

**Headers wymagane (ustawiane przez LottoOpenApiService):**
```
User-Agent: LottoTM.Server.Api.LottoOpenApiService/1.0
Accept: application/json
secret: your-api-key
```

### 5.2 Przetwarzanie i zapis (`SaveInDatabase`)
```csharp
private async Task SaveInDatabase(GetDrawsLottoByDateResponse response, AppDbContext dbContext, CancellationToken stoppingToken)
```

**Działanie**:
1. Przyjmuje typowany obiekt `GetDrawsLottoByDateResponse` z serwisu LottoOpenApiService
2. Iteruje przez `response.Items` → `item.Results`:
   a. **Walidacja daty**: Parse DrawDate do DateOnly
   b. **Sprawdzenie duplikatów**: Czy losowanie na tę datę i typ już istnieje?
   c. **Walidacja liczb**:
      - Czy jest dokładnie 6 liczb?
      - Czy wszystkie są w zakresie 1-49?
      - Czy wszystkie są unikalne (bez duplikatów)?
   d. **Transakcja zapisu**:
      - INSERT do tabeli `Draws`:
        - `DrawSystemId` (z result.DrawSystemId)
        - `DrawDate`, `LottoType`
        - `CreatedAt`, `CreatedByUserId=NULL`
        - `TicketPrice` (domyślnie: 3.0m dla "Lotto", 1.0m dla pozostałych)
        - Pola WinPool* pozostają NULL (nie są pobierane z API)
      - INSERT do tabeli `DrawNumbers` (6 rekordów: Number, Position 1-6)
      - COMMIT transakcji
   e. **Obsługa błędów**: Rollback transakcji w przypadku błędu

### 5.3 Kluczowe decyzje implementacyjne

#### CreatedByUserId = null
- Wyniki generowane przez workera nie mają przypisanego użytkownika
- `CreatedByUserId` jest ustawione na `NULL` (dozwolone przez schemat bazy)
- Umożliwia odróżnienie wyników wprowadzonych ręcznie od automatycznych

#### Duplikaty
- System sprawdza czy losowanie na daną datę i typ już istnieje
- Jeśli TAK → pomija zapis i loguje informację
- Zapobiega duplikowaniu danych przy wielokrotnym uruchomieniu workera

#### Walidacja
- Dokładnie 6 liczb (zgodnie z zasadami LOTTO)
- Zakres 1-49 (polski LOTTO)
- Brak duplikatów (każda liczba unikalna)
- Nieprawidłowe dane są logowane i pomijane

#### Transakcyjność
- Każde losowanie zapisywane w transakcji (Draw + 6 × DrawNumber)
- Gwarantuje spójność danych (wszystko albo nic)
- Rollback w przypadku jakiegokolwiek błędu

---

## 6. Struktura DTO

### 6.1 DTOs dla Lotto OpenApi Response

Serwis LottoOpenApiService używa dedykowanych klas DTO w przestrzeni nazw `LottoTM.Server.Api.Services.LottoOpenApi.DTOs`:

#### GetDrawsLottoByDateResponse
```csharp
public class GetDrawsLottoByDateResponse
{
    [JsonPropertyName("totalRows")]
    public int TotalRows { get; set; }

    [JsonPropertyName("items")]
    public List<GetDrawsLottoByDateResponseItem>? Items { get; set; }

    [JsonPropertyName("code")]
    public int Code { get; set; }
}
```

#### GetDrawsLottoByDateResponseItem
```csharp
public class GetDrawsLottoByDateResponseItem
{
    [JsonPropertyName("results")]
    public List<GetDrawsLottoByDateResponseResult>? Results { get; set; }
}
```

#### GetDrawsLottoByDateResponseResult
```csharp
public class GetDrawsLottoByDateResponseResult
{
    [JsonPropertyName("drawDate")]
    public DateTime? DrawDate { get; set; }

    [JsonPropertyName("drawSystemId")]
    public int DrawSystemId { get; set; }

    [JsonPropertyName("gameType")]
    public string? GameType { get; set; } // "Lotto" lub "LottoPlus"

    [JsonPropertyName("resultsJson")]
    public int[]? ResultsJson { get; set; } // 6 liczb
}
```

**Przykładowy JSON response z Lotto OpenApi**:
```json
{
  "totalRows": 2,
  "items": [
    {
      "results": [
        {
          "drawDate": "2025-11-15T21:00:00",
          "drawSystemId": 7273,
          "gameType": "Lotto",
          "resultsJson": [3, 12, 25, 31, 42, 48]
        }
      ]
    },
    {
      "results": [
        {
          "drawDate": "2025-11-15T21:00:00",
          "drawSystemId": 7274,
          "gameType": "LottoPlus",
          "resultsJson": [5, 14, 23, 29, 37, 41]
        }
      ]
    }
  ],
  "code": 200
}
```

### 6.2 Typy gier i mapowanie

**Typy gier z Lotto OpenApi:**
- `"Lotto"` - zapisywane jako `LottoType = "Lotto"` w bazie danych
- `"LottoPlus"` - zapisywane jako `LottoType = "LottoPlus"` w bazie danych

**Uwaga:** Nie ma już transformacji nazw typów - nazwy są zachowywane w oryginalnej formie z API.

### 6.3 LottoOpenApiService - Dedykowany Serwis

**Lokalizacja:**
```
src/server/LottoTM.Server.Api/Services/LottoOpenApi/
├── ILottoOpenApiService.cs                    // Interface
├── LottoOpenApiService.cs                     // Implementacja
└── DTOs/                                      // Data Transfer Objects
    ├── GetDrawsLottoByDateResponse.cs
    ├── GetDrawsLottoByDateResponseItem.cs
    └── GetDrawsLottoByDateResponseResult.cs
```

**Główne metody:**
```csharp
Task<GetDrawsLottoByDateResponse> GetDrawsLottoByDate(DateOnly? date = null);
Task<string> GetActualInfo();
```

**Odpowiedzialności:**
1. Komunikacja z Lotto OpenApi (https://www.lotto.pl)
2. Konfiguracja HTTP client z odpowiednimi headers
3. Deserializacja odpowiedzi API do typowanych DTOs (`GetDrawsLottoByDateResponse`)
4. Zwrot wyników jako typowany obiekt (nie JSON string)
5. Obsługa błędów i walidacja konfiguracji

**Zalety wydzielenia serwisu:**
- Separacja odpowiedzialności (Single Responsibility Principle)
- Możliwość ponownego użycia w innych miejscach (Worker, endpointy API)
- Łatwiejsze testowanie (mockowanie ILottoOpenApiService)
- Centralizacja logiki pobierania z Lotto OpenApi
- Jednolita konfiguracja (LottoOpenApi:Url, LottoOpenApi:ApiKey)
- Typowane DTOs zapewniają type-safety

**Różnica między LottoOpenApiService a XLottoService:**
- **LottoOpenApiService**: Bezpośrednie wywołanie oficjalnego Lotto.pl OpenApi, używane przez Worker do automatycznego pobierania
- **XLottoService**: Pobieranie HTML z XLotto.pl + ekstrakcja przez Google Gemini AI, używane przez endpoint on-demand (alternatywne źródło danych)

---

## 7. Encja Draw - Struktura i Nowe Właściwości

### 7.1 Podstawowe właściwości Draw

#### Klucze i identyfikatory
- **`Id`** (int) - klucz główny, auto-increment
- **`DrawSystemId`** (int) - identyfikator losowania z systemu Lotto.pl OpenApi (nowa właściwość)
- **`DrawDate`** (DateOnly) - data losowania (bez czasu), unique z LottoType
- **`LottoType`** (string) - typ gry ("Lotto", "LottoPlus"), unique z DrawDate
- **`CreatedAt`** (DateTime) - timestamp UTC utworzenia rekordu
- **`CreatedByUserId`** (int?, nullable) - ID użytkownika admin (NULL dla workera)

#### Nawigacja i relacje
- **`CreatedByUser`** (User?, nullable) - nawigacja do użytkownika
- **`Numbers`** (ICollection<DrawNumber>) - kolekcja 6 liczb (1-49)

### 7.2 Nowe właściwości - Informacje o pulach wygranych

Wszystkie właściwości poniżej są **nullable** (nie są obecnie pobierane z Lotto OpenApi):

#### Cena kuponu
- **`TicketPrice`** (decimal?, precision 18,2) - cena pojedynczego zakładu
  - Domyślnie ustawiana przez Worker: 3.0m dla "Lotto", 1.0m dla pozostałych

#### Pula wygranych - Tier 1 (6 trafień)
- **`WinPoolCount1`** (int?) - liczba wygranych w 1. kategorii
- **`WinPoolAmount1`** (decimal?, precision 18,2) - kwota wygranej w 1. kategorii

#### Pula wygranych - Tier 2 (5 trafień)
- **`WinPoolCount2`** (int?) - liczba wygranych w 2. kategorii
- **`WinPoolAmount2`** (decimal?, precision 18,2) - kwota wygranej w 2. kategorii

#### Pula wygranych - Tier 3 (4 trafienia)
- **`WinPoolCount3`** (int?) - liczba wygranych w 3. kategorii
- **`WinPoolAmount3`** (decimal?, precision 18,2) - kwota wygranej w 3. kategorii

#### Pula wygranych - Tier 4 (3 trafienia)
- **`WinPoolCount4`** (int?) - liczba wygranych w 4. kategorii
- **`WinPoolAmount4`** (decimal?, precision 18,2) - kwota wygranej w 4. kategorii

### 7.3 Konfiguracja w AppDbContext

```csharp
entity.Property(e => e.DrawSystemId)
    .IsRequired()
    .HasColumnType("int");

entity.Property(e => e.TicketPrice)
    .IsRequired(false)
    .HasPrecision(18, 2)
    .HasColumnType("numeric(18, 2)");

entity.Property(e => e.WinPoolCount1)
    .IsRequired(false)
    .HasColumnType("int");

entity.Property(e => e.WinPoolAmount1)
    .IsRequired(false)
    .HasPrecision(18, 2)
    .HasColumnType("numeric(18, 2)");

// ... analogicznie dla WinPoolCount2-4 i WinPoolAmount2-4
```

### 7.4 Uwagi implementacyjne

- **DrawSystemId**: Pobierany z Lotto OpenApi response i zapisywany podczas tworzenia Draw przez Worker
- **TicketPrice**: Ustawiana przez Worker na stałe wartości (3.0m/1.0m), można rozszerzyć o pobieranie z API
- **WinPool* properties**: Przygotowane na przyszłość, obecnie zawsze NULL
- **Precision decimal**: Wszystkie kwoty mają precision (18, 2) dla poprawnego przechowywania wartości pieniężnych
- **Nullable**: Wszystkie nowe pola są nullable, co zapewnia kompatybilność wsteczną z istniejącymi rekordami

---

## 8. Logowanie

### 8.1 Poziomy logowania
Worker używa różnych poziomów logowania:
- `LogInformation` - normalne zdarzenia (start, sukces)
- `LogWarning` - walidacja niepowodzenia (nieprawidłowe dane)
- `LogError` - krytyczne błędy (Exception)

### 8.2 Kluczowe logi
```csharp
// Start workera
"LottoWorker started. Enabled: {Enabled}, Time window: {StartTime} - {EndTime}, Interval: {Interval} minutes"

// Worker wyłączony
"LottoWorker is disabled (LottoWorker:Enable = false)"

// Aktywne okno czasowe
"LottoWorker is in active time window at: {time}"

// Wyniki już istnieją
"Draws for date {Date} already exist (LOTTO and LOTTO PLUS), skipping fetch"

// Brak wyników
"Draws for date {Date} do not exist or are incomplete. LOTTO exists: {LottoExists}, LOTTO PLUS exists: {LottoPlusExists}"

// Sukces zapisu
"Draw {DrawId} saved successfully: Date={DrawDate}, Type={GameType}"

// Błędy walidacji
"Invalid number count for draw {DrawDate}: expected 6, got {Count}"
"Invalid numbers for draw {DrawDate}: numbers must be between 1-49"
"Duplicate numbers found in draw {DrawDate}"
```

---

## 9. Obsługa Błędów

### 9.1 Poziomy obsługi błędów

#### Poziom 1: Ping API
```csharp
catch (HttpRequestException ex)
catch (Exception ex)
```
- Loguje błąd, ale nie przerywa działania workera
- Worker kontynuuje działanie

#### Poziom 2: Pobieranie wyników
```csharp
catch (Exception ex) // w GetLottoDrawsFromXLottoApi
```
- Loguje błąd całego procesu pobierania
- Worker kontynuuje działanie (następna iteracja po 5 min)

#### Poziom 3: Przetwarzanie pojedynczego losowania
```csharp
catch (Exception ex) // w transakcji
await transaction.RollbackAsync(stoppingToken);
```
- Rollback transakcji dla tego losowania
- Kontynuacja przetwarzania pozostałych losowań

#### Poziom 4: Deserializacja JSON
```csharp
catch (JsonException ex)
```
- Loguje błąd parsowania JSON
- Przerywa przetwarzanie tego response

### 9.2 Zasada działania
**Worker nigdy się nie zatrzymuje z powodu błędów biznesowych**
- Wszystkie błędy są logowane
- Worker zawsze próbuje ponownie w następnym cyklu
- Tylko `CancellationToken` może zatrzymać workera (shutdown aplikacji)

---

## 10. Testowanie Workera

### 10.1 Testy manualne

#### Test 1: Sprawdzenie działania w oknie czasowym
1. Ustaw `LottoWorker:StartTime` na aktualny czas + 1 minuta
2. Ustaw `LottoWorker:EndTime` na aktualny czas + 10 minut
3. Uruchom aplikację
4. Sprawdź logi - powinny pojawić się komunikaty o pobieraniu wyników

#### Test 2: Sprawdzenie poza oknem czasowym
1. Ustaw `LottoWorker:StartTime` i `EndTime` na przeszłość
2. Uruchom aplikację
3. Sprawdź logi - powinien być komunikat "LottoWorker outside active window"

#### Test 3: Duplikaty
1. Uruchom workera dwukrotnie w tym samym oknie czasowym
2. Sprawdź bazę danych - nie powinny pojawić się duplikaty
3. Sprawdź logi - powinien być komunikat "already exist, skipping"

### 10.2 Testy jednostkowe (TODO)
```csharp
// Przykładowe scenariusze testowe
- Test_Worker_RunsInTimeWindow
- Test_Worker_SkipsOutsideTimeWindow
- Test_Worker_SkipsDuplicateDraws
- Test_Worker_ValidatesNumbersCorrectly
- Test_Worker_HandlesAPIErrors
- Test_Worker_RollbacksOnDatabaseError
```

---

## 11. Konfiguracja w Program.cs

### 11.1 Rejestracja workera
```csharp
// Konfiguracja opcji
builder.Services.Configure<LottoWorkerOptions>(
    builder.Configuration.GetSection("LottoWorker"));

// Rejestracja workera jako hosted service
builder.Services.AddHostedService<LottoWorker>();
```

### 11.2 Wymagane serwisy
Worker wymaga uprzedniej rejestracji:
- `AppDbContext` - musi być zarejestrowany jako scoped
- `IHttpClientFactory` - automatycznie dostępny po `AddHttpClient()`
- `IConfiguration` - automatycznie dostępny (dostęp do LottoOpenApi:Url i LottoOpenApi:ApiKey)

---

## 12. Monitorowanie i Utrzymanie

### 12.1 Co monitorować?
- **Logi błędów** - czy są powtarzające się błędy?
- **Czas wykonania** - czy pobieranie wyników zajmuje rozsądny czas?
- **Brak danych** - czy wyniki są pobierane regularnie?
- **Duplikaty** - czy walidacja duplikatów działa poprawnie?

### 12.2 Metryki sukcesu
- ✅ Worker uruchamia się automatycznie przy starcie aplikacji
- ✅ Wyniki są pobierane codziennie w oknie czasowym
- ✅ Brak duplikatów w bazie danych
- ✅ Błędy są logowane, ale nie zatrzymują workera
- ✅ Wszystkie użytkownicy mają dostęp do aktualnych wyników

### 12.3 Zalecenia operacyjne
1. **Przegląd logów** - codzienne sprawdzanie logów z workera
2. **Alert na błędy** - konfiguracja alertów dla powtarzających się błędów
3. **Backup bazy** - regularne backupy przed wprowadzeniem zmian
4. **Testowanie** - testy na środowisku staging przed deploymentem

---

## 13. Przyszłe Usprawnienia (TODO)

### 13.1 Konfigurowalność
- [ ] Parametr `TargetDateOffset` w konfiguracji (zamiast hardcodowanych -2 dni)
- [ ] Konfiguracja retry policy dla HTTP requests
- [ ] Konfiguracja timeout dla operacji HTTP

### 13.2 Funkcjonalność
- [ ] Wysyłanie powiadomień email do administratorów po pobraniu wyników
- [ ] Dashboard z metrykami workera (last run, success rate, errors)
- [ ] Możliwość ręcznego triggera workera przez API endpoint (dla adminów)

### 13.3 Niezawodność
- [ ] Circuit breaker pattern dla XLotto API
- [ ] Exponential backoff przy błędach HTTP
- [ ] Health check endpoint dla workera
- [ ] Dead letter queue dla nieudanych prób

### 13.4 Testowanie
- [ ] Testy jednostkowe dla logiki biznesowej
- [ ] Testy integracyjne z mockiem XLotto API
- [ ] Testy wydajnościowe (czas pobierania i zapisu)

---

## 14. Rozwiązywanie Problemów

### Problem 1: Worker się nie uruchamia
**Objawy**: Brak logów "LottoWorker started"
**Przyczyny**:
- Błąd w konstruktorze workera
- Brak rejestracji `AddHostedService<LottoWorker>()`
- Błąd w konfiguracji `LottoWorkerOptions`

**Rozwiązanie**:
1. Sprawdź logi aplikacji przy starcie
2. Zweryfikuj `Program.cs` - czy `AddHostedService` jest wywołane
3. Sprawdź `appsettings.json` - czy sekcja `LottoWorker` istnieje

### Problem 2: Worker nie pobiera wyników
**Objawy**: Logi "outside active window" lub "LottoWorker is disabled"
**Przyczyny**:
- **Feature Flag `Enable` jest ustawiony na `false`** (najczęstszy powód)
- Nieprawidłowa konfiguracja czasu w `appsettings.json`
- Czas serwera inny niż oczekiwany (timezone)

**Rozwiązanie**:
1. **Sprawdź `appsettings.json` → Ustaw `LottoWorker:Enable: true`**
2. Sprawdź aktualny czas serwera: `DateTime.Now`
3. Dostosuj `StartTime` i `EndTime` w konfiguracji
4. Rozważ użycie UTC zamiast czasu lokalnego

### Problem 3: Duplikaty w bazie
**Objawy**: Wiele rekordów dla tej samej daty i typu
**Przyczyny**:
- Błąd w logice sprawdzania duplikatów
- Brak constraint UNIQUE w bazie danych

**Rozwiązanie**:
1. Dodaj UNIQUE constraint na (DrawDate, LottoType) w migracji
2. Zweryfikuj logikę `AnyAsync(d => d.DrawDate == drawDate && d.LottoType == drawData.GameType)`

### Problem 4: Błędy deserializacji JSON
**Objawy**: Logi "Failed to deserialize draw results JSON" lub "JSON deserialization error"
**Przyczyny**:
- Zmiana formatu response z Lotto OpenApi
- Błędne dane z API
- Brak wymaganego klucza API (401 Unauthorized)
- Nieprawidłowy ApiKey w konfiguracji

**Rozwiązanie**:
1. Sprawdź surowy JSON w logach
2. Porównaj z klasami DTO (`LottoOpenApiResponse`, `LottoOpenApiItem`, `LottoOpenApiResult`)
3. Sprawdź czy ApiKey jest poprawnie skonfigurowany w appsettings.json
4. Zweryfikuj header `secret` w HTTP request
5. Dostosuj DTO do nowego formatu jeśli API się zmieniło

---

## 15. Podsumowanie

`LottoWorker` to kluczowy komponent systemu LottoTM, który:
- ✅ **Automatyzuje** pobieranie wyników losowań
- ✅ **Eliminuje** potrzebę ręcznego wprowadzania danych
- ✅ **Zapewnia** aktualność danych dla wszystkich użytkowników
- ✅ **Działa niezawodnie** dzięki obsłudze błędów i transakcjom
- ✅ **Jest konfigurowalny** poprzez `appsettings.json`
- ✅ **Loguje** wszystkie istotne zdarzenia dla monitoringu

Worker uruchamia się automatycznie z aplikacją i działa w tle, pobierając wyniki w oknie czasowym 22:15-23:00 (konfigurowalnym). Wszystkie wyniki są walidowane, zapisywane transakcyjnie i zabezpieczone przed duplikatami.

---

**Koniec dokumentu Worker Plan**
