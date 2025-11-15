# LottoWorker - Automatyczne Pobieranie Wyników Losowań

**Wersja:** 1.0
**Data:** 2025-11-15
**Autor:** Tomasz Mularczyk

---

## 1. Wprowadzenie

### 1.1 Cel dokumentu
Ten dokument opisuje implementację i działanie automatycznego workera (`LottoWorker`), który w tle pobiera wyniki losowań LOTTO i LOTTO PLUS z zewnętrznego API XLotto.pl.

### 1.2 Cel funkcjonalności
Automatyzacja procesu pobierania wyników losowań eliminuje potrzebę ręcznego wprowadzania wyników przez administratorów i zapewnia, że dane są zawsze aktualne dla wszystkich użytkowników systemu.

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
│   └── LottoWorker.cs          // Główna implementacja workera
├── Options/
│   └── LottoWorkerOptions.cs   // Konfiguracja workera
```

### 2.3 Zależności
Worker wykorzystuje następujące serwisy:
- `IServiceScopeFactory` - tworzenie scope dla dostępu do scoped services (DbContext)
- `ILogger<LottoWorker>` - logowanie zdarzeń i błędów
- `IOptions<LottoWorkerOptions>` - konfiguracja parametrów działania
- `IConfiguration` - dostęp do ustawień aplikacji (ApiUrl)
- `AppDbContext` - dostęp do bazy danych
- `IXLottoService` - serwis do pobierania wyników z API XLotto
- `IHttpClientFactory` - tworzenie HTTP clientów do pingowania API

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
    "IntervalMinutes": 5
  },
  "ApiUrl": "https://your-api-url.com"
}
```

**Feature Flag - Enable:**
- `Enable: true` - Worker działa w skonfigurowanym oknie czasowym
- `Enable: false` - Worker jest całkowicie wyłączony (nie pobiera wyników)
- **Domyślnie: false** (produkcja), można włączyć w development
- Umożliwia łatwe włączanie/wyłączanie workera bez restartowania aplikacji

### 3.3 Logika czasowa
1. Worker uruchamia się wraz z aplikacją
2. Co 5 minut (domyślnie) sprawdza czy:
   - **Feature Flag `Enable` jest ustawiony na `true`**
   - Jest w aktywnym oknie czasowym (StartTime - EndTime)
3. Jeśli OBA warunki są spełnione → wykonuje ping API + sprawdza i pobiera wyniki
4. Jeśli NIE → czeka kolejne 5 minut i loguje odpowiedni komunikat

**Dlaczego 22:15-23:00?**
- Losowania LOTTO odbywają się wieczorem (zwykle około 22:00)
- Wyniki są publikowane na stronie XLotto.pl krótko po losowaniu
- Okno czasowe daje systemowi czas na pobranie i zapisanie wyników

---

## 4. Proces Pobierania Wyników

### 4.1 Algorytm główny (`ExecuteAsync`)
```
1. START pętli while (!stoppingToken.IsCancellationRequested)
2. Ping API dla utrzymania aktywności (PingForApiVersion)
3. Pobierz aktualny czas (DateTime.Now.TimeOfDay)
4. Sprawdź czy Enable=true ORAZ czas jest w przedziale [StartTime, EndTime]
5. JEŚLI TAK:
   a. Oblicz targetDate = Today - 2 dni
   b. Sprawdź czy wyniki na targetDate już istnieją w bazie (LOTTO i LOTTO PLUS)
   c. JEŚLI NIE ISTNIEJĄ:
      - Pobierz wyniki z XLotto API (GetLottoDrawsFromXLottoApi)
      - Zapisz do bazy danych
   d. JEŚLI ISTNIEJĄ:
      - Log informacji i pomiń pobieranie
6. Czekaj 5 minut (Task.Delay)
7. GOTO 1
```

### 4.2 Dlaczego targetDate = Today - 2 dni?
- **Powód testowy**: Umożliwia testowanie workera z historycznymi danymi
- **W produkcji**: Powinno być ustawione na `Today` lub `Today - 1` w zależności od potrzeb
- **Można skonfigurować**: Parametr może być przeniesiony do konfiguracji

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

### 5.1 Funkcja główna (`GetLottoDrawsFromXLottoApi`)
```csharp
private async Task GetLottoDrawsFromXLottoApi(CancellationToken stoppingToken)
```

**Działanie**:
1. Tworzy scope dla dostępu do scoped services (DbContext, XLottoService)
2. Oblicza targetDate (Today - 2 dni w testach)
3. Pobiera wyniki LOTTO z API XLotto
4. Przetwarza i zapisuje wyniki LOTTO
5. Pobiera wyniki LOTTO PLUS z API XLotto
6. Przetwarza i zapisuje wyniki LOTTO PLUS
7. Loguje sukces lub błędy

### 5.2 Przetwarzanie i zapis (`ProcessAndSaveXLottoApiDraw`)
```csharp
private async Task ProcessAndSaveXLottoApiDraw(string jsonResults, AppDbContext dbContext, CancellationToken stoppingToken)
```

**Działanie**:
1. Deserializuje JSON response do `DrawsResponse` (zawiera `List<DrawData>`)
2. Dla każdego `DrawData`:
   a. **Walidacja daty**: Parse DrawDate do DateOnly
   b. **Sprawdzenie duplikatów**: Czy losowanie na tę datę i typ już istnieje?
   c. **Walidacja liczb**:
      - Czy jest dokładnie 6 liczb?
      - Czy wszystkie są w zakresie 1-49?
      - Czy wszystkie są unikalne (bez duplikatów)?
   d. **Transakcja zapisu**:
      - INSERT do tabeli `Draws` (DrawDate, LottoType, CreatedAt, CreatedByUserId=NULL)
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

### 6.1 DrawsResponse
```csharp
private class DrawsResponse
{
    public List<DrawData>? Data { get; set; }
}
```

### 6.2 DrawData
```csharp
private class DrawData
{
    public string DrawDate { get; set; } = string.Empty;
    public string GameType { get; set; } = string.Empty;
    public int[] Numbers { get; set; } = Array.Empty<int>();
}
```

**Przykładowy JSON response z XLotto API**:
```json
{
  "Data": [
    {
      "DrawDate": "2025-11-13",
      "GameType": "LOTTO",
      "Numbers": [3, 12, 25, 31, 42, 48]
    }
  ]
}
```

---

## 7. Logowanie

### 7.1 Poziomy logowania
Worker używa różnych poziomów logowania:
- `LogInformation` - normalne zdarzenia (start, sukces)
- `LogWarning` - walidacja niepowodzenia (nieprawidłowe dane)
- `LogError` - krytyczne błędy (Exception)

### 7.2 Kluczowe logi
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

## 8. Obsługa Błędów

### 8.1 Poziomy obsługi błędów

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

### 8.2 Zasada działania
**Worker nigdy się nie zatrzymuje z powodu błędów biznesowych**
- Wszystkie błędy są logowane
- Worker zawsze próbuje ponownie w następnym cyklu
- Tylko `CancellationToken` może zatrzymać workera (shutdown aplikacji)

---

## 9. Testowanie Workera

### 9.1 Testy manualne

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

### 9.2 Testy jednostkowe (TODO)
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

## 10. Konfiguracja w Program.cs

### 10.1 Rejestracja workera
```csharp
// Konfiguracja opcji
builder.Services.Configure<LottoWorkerOptions>(
    builder.Configuration.GetSection("LottoWorker"));

// Rejestracja workera jako hosted service
builder.Services.AddHostedService<LottoWorker>();
```

### 10.2 Wymagane serwisy
Worker wymaga uprzedniej rejestracji:
- `IXLottoService` - musi być zarejestrowany w DI
- `AppDbContext` - musi być zarejestrowany jako scoped
- `IHttpClientFactory` - automatycznie dostępny po `AddHttpClient()`

---

## 11. Monitorowanie i Utrzymanie

### 11.1 Co monitorować?
- **Logi błędów** - czy są powtarzające się błędy?
- **Czas wykonania** - czy pobieranie wyników zajmuje rozsądny czas?
- **Brak danych** - czy wyniki są pobierane regularnie?
- **Duplikaty** - czy walidacja duplikatów działa poprawnie?

### 11.2 Metryki sukcesu
- ✅ Worker uruchamia się automatycznie przy starcie aplikacji
- ✅ Wyniki są pobierane codziennie w oknie czasowym
- ✅ Brak duplikatów w bazie danych
- ✅ Błędy są logowane, ale nie zatrzymują workera
- ✅ Wszystkie użytkownicy mają dostęp do aktualnych wyników

### 11.3 Zalecenia operacyjne
1. **Przegląd logów** - codzienne sprawdzanie logów z workera
2. **Alert na błędy** - konfiguracja alertów dla powtarzających się błędów
3. **Backup bazy** - regularne backupy przed wprowadzeniem zmian
4. **Testowanie** - testy na środowisku staging przed deploymentem

---

## 12. Przyszłe Usprawnienia (TODO)

### 12.1 Konfigurowalność
- [ ] Parametr `TargetDateOffset` w konfiguracji (zamiast hardcodowanych -2 dni)
- [ ] Konfiguracja retry policy dla HTTP requests
- [ ] Konfiguracja timeout dla operacji HTTP

### 12.2 Funkcjonalność
- [ ] Wysyłanie powiadomień email do administratorów po pobraniu wyników
- [ ] Dashboard z metrykami workera (last run, success rate, errors)
- [ ] Możliwość ręcznego triggera workera przez API endpoint (dla adminów)

### 12.3 Niezawodność
- [ ] Circuit breaker pattern dla XLotto API
- [ ] Exponential backoff przy błędach HTTP
- [ ] Health check endpoint dla workera
- [ ] Dead letter queue dla nieudanych prób

### 12.4 Testowanie
- [ ] Testy jednostkowe dla logiki biznesowej
- [ ] Testy integracyjne z mockiem XLotto API
- [ ] Testy wydajnościowe (czas pobierania i zapisu)

---

## 13. Rozwiązywanie Problemów

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
**Objawy**: Logi "Failed to deserialize draw results JSON"
**Przyczyny**:
- Zmiana formatu response API XLotto
- Błędne dane z API

**Rozwiązanie**:
1. Sprawdź surowy JSON w logach
2. Porównaj z klasami DTO (`DrawsResponse`, `DrawData`)
3. Dostosuj DTO do nowego formatu

---

## 14. Podsumowanie

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
