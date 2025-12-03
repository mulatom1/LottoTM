# API Endpoint Implementation Plan: POST /api/verification/check

## 1. Przegląd punktu końcowego

Ten punkt końcowy umożliwia zalogowanemu użytkownikowi weryfikację swoich zapisanych zestawów liczb (kuponów) względem oficjalnych wyników losowań w zadanym zakresie dat. System porównuje każdy kupon z każdym losowaniem i zwraca listę trafień (minimum 3 zgodne liczby). Opcjonalnie użytkownik może przefiltrować kupony według nazwy grupy (groupName) - w takim przypadku weryfikowane będą tylko kupony należące do wybranej grupy.

## 2. Szczegóły żądania

- **Metoda HTTP**: `POST`
- **Struktura URL**: `/api/verification/check`
- **Parametry**:
  - **Wymagane**: Brak parametrów URL.
  - **Opcjonalne**: Brak.
- **Request Body**: Wymagany jest obiekt JSON z następującymi polami:

  ```json
  {
    "dateFrom": "2025-10-01",
    "dateTo": "2025-10-31",
    "groupName": "Ulubione"
  }
  ```

  - `dateFrom` (string, "YYYY-MM-DD"): Data początkowa okresu weryfikacji.
  - `dateTo` (string, "YYYY-MM-DD"): Data końcowa okresu weryfikacji.
  - `groupName` (string, opcjonalne): Nazwa grupy kuponów do filtrowania (wyszukiwanie częściowe, case-insensitive). Jeśli podane, zwracane są tylko kupony, których nazwa grupy zawiera podany tekst (używając `.ToLower().Contains()`). Jeśli puste lub nie podane, weryfikowane są wszystkie kupony użytkownika.

## 3. Wykorzystywane typy

Wszystkie typy zostały zdefiniowane w folderze `Features/Verification/Check`.

- **`Contracts.cs`**:
  - `Request(DateOnly DateFrom, DateOnly DateTo, string? GroupName)`: Model żądania.
  - `Response(long ExecutionTimeMs, List<DrawsResult> DrawsResults, List<TicketsResult> TicketsResults)`: Model odpowiedzi.
  - `DrawsResult(int DrawId, DateOnly DrawDate, int DrawSystemId, string LottoType, List<int> DrawNumbers, decimal? TicketPrice, int? WinPoolCount1, decimal? WinPoolAmount1, int? WinPoolCount2, decimal? WinPoolAmount2, int? WinPoolCount3, decimal? WinPoolAmount3, int? WinPoolCount4, decimal? WinPoolAmount4, List<WinningTicketResult> WinningTicketsResult)`: Dane losowania wraz z listą wygranych kuponów (≥3 trafienia).
  - `WinningTicketResult(int TicketId, List<int> MatchingNumbers)`: Informacje o wygranym kuponie w kontekście konkretnego losowania.
  - `TicketsResult(int TicketId, string GroupName, List<int> TicketNumbers)`: Dane kuponu użytkownika.

## 4. Szczegóły odpowiedzi

- **Odpowiedź sukcesu (200 OK)**:

  ```json
  {
    "executionTimeMs": 123,
    "drawsResults": [
      {
        "drawId": 123,
        "drawDate": "2025-10-15",
        "drawSystemId": 20250001,
        "lottoType": "LOTTO",
        "drawNumbers": [3, 14, 23, 31, 37, 48],
        "ticketPrice": 3.00,
        "winPoolCount1": 2,
        "winPoolAmount1": 5000000.00,
        "winPoolCount2": 15,
        "winPoolAmount2": 50000.00,
        "winPoolCount3": 120,
        "winPoolAmount3": 500.00,
        "winPoolCount4": 850,
        "winPoolAmount4": 20.00,
        "winningTicketsResult": [
          {
            "ticketId": 1001,
            "matchingNumbers": [14, 23, 37]
          }
        ]
      }
    ],
    "ticketsResults": [
      {
        "ticketId": 1001,
        "groupName": "Ulubione",
        "ticketNumbers": [5, 14, 23, 29, 37, 41]
      }
    ]
  }
  ```

  **Uwaga o strukturze odpowiedzi:**
  - Response zawiera dwie osobne listy zamiast zagnieżdżonej struktury
  - `drawsResults[]` - wszystkie losowania z zakresu dat wraz z listą wygranych kuponów (`winningTicketsResult[]` zawiera tylko kupony z ≥3 trafieniami dla danego losowania)
  - `ticketsResults[]` - wszystkie kupony użytkownika (lub filtrowane po groupName jeśli podane w request)
  - Frontend łączy obie listy używając `ticketId` jako klucza, aby wyświetlić pełne informacje (groupName i ticketNumbers z `ticketsResults`, matchingNumbers z `drawsResults.winningTicketsResult`)

- **Odpowiedź błędu (400 Bad Request)**:

  ```json
  {
    "errors": {
      "DateTo": ["'Date To' must be greater than or equal to 'Date From'."],
      "DateRange": ["Zakres dat nie może przekraczać 3 lat."]
    }
  }
  ```

## 5. Przepływ danych

1. Użytkownik wysyła żądanie `POST` na `/api/verification/check` z zakresem dat i opcjonalnym filtrem `groupName`.
2. Endpoint `Check` odbiera żądanie.
3. `Mediator` przekazuje żądanie do `CheckVerificationHandler`.
4. Handler waliduje żądanie za pomocą `FluentValidation` (zakres dat, poprawność).
5. Handler pobiera `UserId` z `ClaimsPrincipal` (z `IHttpContextAccessor`).
6. Handler uruchamia `Stopwatch` do pomiaru czasu.
7. Handler wykonuje dwa równoległe zapytania do bazy danych (`AppDbContext`):
   a. Pobiera kupony (`Tickets`) wraz z ich numerami (`TicketNumbers`) i nazwami grup (`GroupName`) dla danego `UserId`. Jeśli w żądaniu podano `GroupName`, filtruje kupony tylko do tych, których `GroupName` zawiera podany tekst (wyszukiwanie częściowe, case-insensitive używając `.ToLower().Contains()`).
   b. Pobiera wszystkie losowania (`Draws`) wraz z ich numerami (`DrawNumbers`), typami gry (`LottoType`) i danymi o wygranych (`TicketPrice`, `WinPoolCount1-4`, `WinPoolAmount1-4`) w zadanym zakresie dat.
8. **Faza 2: Budowanie listy `ticketsResults`**:
   - Handler iteruje po pobranych kuponach i tworzy `TicketsResult` dla każdego kuponu (zawierające `TicketId`, `GroupName`, `TicketNumbers`).
9. **Faza 3: Budowanie listy `drawsResults`**:
   - Handler iteruje po każdym pobranym losowaniu.
   - Dla każdego losowania, weryfikuje wszystkie kupony z `ticketsResults`:
     - Za pomocą `Enumerable.Intersect`, znajduje listę pasujących numerów między kuponem a losowaniem.
     - Jeśli liczba trafień jest większa niż 2 (≥3), dodaje `WinningTicketResult` (zawierające `TicketId` i `MatchingNumbers`) do listy wygranych kuponów dla danego losowania.
   - Tworzy `DrawsResult` dla każdego losowania (zawierające dane losowania, informacje o wygranych i listę `WinningTicketsResult`).
10. Po sprawdzeniu wszystkich kombinacji, handler zatrzymuje `Stopwatch`.
11. Konstruuje finalny obiekt `Response(ExecutionTimeMs, DrawsResults, TicketsResults)`.
12. Zwraca odpowiedź `200 OK`.

## 6. Konfiguracja

System wykorzystuje parametr konfiguracyjny określający maksymalny zakres dat dla weryfikacji:

- **`Features:Verification:Days`** (appsettings.json): Określa maksymalną liczbę dni wstecz, które mogą być uwzględnione w weryfikacji kuponów użytkownika z losowaniami. Domyślna wartość: 31 dni.

Przykład konfiguracji w `appsettings.json`:
```json
{
  "Features": {
    "Verification": {
      "Days": 31
    }
  }
}
```

Handler odczytuje wartość z `IConfiguration` i waliduje, że zakres dat `(dateTo - dateFrom).Days` nie przekracza wartości `Features:Verification:Days`.

## 7. Względy bezpieczeństwa

- **Uwierzytelnianie**: Endpoint będzie chroniony za pomocą `[Authorize]`, co gwarantuje, że tylko zalogowani użytkownicy mogą z niego korzystać.
- **Autoryzacja**: Logika w handlerze musi ściśle filtrować dane (`Tickets`) na podstawie `UserId` pobranego z tokenu JWT. Zapobiega to możliwości odpytania o dane innego użytkownika.
- **Walidacja danych wejściowych**: `FluentValidation` zabezpiecza przed niepoprawnymi danymi (np. nieprawidłowy format daty, odwrócony zakres). Ograniczenie zakresu dat (zgodnie z parametrem `Features:Verification:Days`) chroni przed atakami typu DoS polegającymi na żądaniu weryfikacji dla ogromnego przedziału czasowego.

## 8. Obsługa błędów

- **400 Bad Request**: Zwracany, gdy walidacja `FluentValidation` nie powiodła się. Globalny middleware `ExceptionHandlingMiddleware` przechwyci `ValidationException` i sformatuje odpowiedź z listą błędów.
- **401 Unauthorized**: Zwracany przez middleware autoryzacji ASP.NET Core, jeśli token JWT jest nieprawidłowy, brak go lub wygasł.
- **500 Internal Server Error**: Zwracany przez `ExceptionHandlingMiddleware` w przypadku nieoczekiwanych błędów (np. problem z połączeniem z bazą danych). Błąd zostanie zarejestrowany przez `Serilog`.

## 9. Rozważania dotyczące wydajności

- **Dostęp do bazy danych**: Dane (kupony i losowania) są pobierane w dwóch osobnych, zoptymalizowanych zapytaniach z użyciem `AsNoTracking()` i `Include()`, aby zminimalizować liczbę zapytań (2 zapytania na żądanie). Wykorzystane zostaną indeksy na `Tickets.UserId` i `Draws.DrawDate`.
- **Przetwarzanie w pamięci**: Główna logika porównawcza (pętle i `Intersect`) odbywa się w pamięci. Dla maksymalnych wartości (100 kuponów, ~600 losowań w 3 latach dla obu typów gier LOTTO + LOTTO PLUS) operacja jest zoptymalizowana. Złożoność obliczeniowa to O(liczba_kuponów * liczba_losowań).
- **Asynchroniczność**: Wszystkie operacje I/O (dostęp do bazy danych) są w pełni asynchroniczne (`async/await`), co zapewnia, że wątek nie jest blokowany podczas oczekiwania na dane.

## 10. Etapy wdrożenia

1. Utworzenie nowego folderu w projekcie `LottoTM.Server.Api`: `src/server/LottoTM.Server.Api/Features/Verification/Check`.
2. W folderze `Check` utwórz plik `Contracts.cs` i zdefiniuj w nim rekordy: `Request` (z opcjonalnym parametrem `GroupName`), `Response`, `TicketVerificationResult`, `DrawVerificationResult`.
3. Dodaj sekcję `Features:Verification:Days` do pliku `appsettings.json` z wartością domyślną (np. 31 dni).
4. Utwórz plik `Validator.cs` i zaimplementuj logikę walidacji dla `Request` przy użyciu `FluentValidation`, sprawdzając poprawność i zakres dat. Walidator powinien odczytywać wartość `Features:Verification:Days` z `IConfiguration` i sprawdzać, czy `(dateTo - dateFrom).Days <= Features:Verification:Days`.
5. Utwórz plik `Handler.cs` i zaimplementuj klasę `CheckVerificationHandler` dziedziczącą po `IRequestHandler<Contracts.Request, Contracts.Response>`.
6. Wstrzyknij `AppDbContext`, `IHttpContextAccessor`, `IConfiguration` i `IValidator<Contracts.Request>` do konstruktora handlera.
7. Zaimplementuj metodę `Handle`:
   a. Uruchom walidację.
   b. Pobierz `UserId` z `HttpContext`.
   c. Uruchom `Stopwatch`.
   d. Asynchronicznie pobierz dane kuponów z bazy. Jeśli w żądaniu podano `GroupName`, zastosuj filtr `.Where(t => t.GroupName != null && t.GroupName.ToLower().Contains(request.GroupName.ToLower()))` do zapytania o kupony (wyszukiwanie częściowe, case-insensitive).
   e. Asynchronicznie pobierz dane losowań z bazy.
   f. Zaimplementuj logikę porównywania w pętlach.
   g. Zatrzymaj `Stopwatch` i zbuduj obiekt odpowiedzi.
8. Utwórz plik `Endpoint.cs` i zdefiniuj w nim metodę rozszerzającą `AddEndpoint`, która mapuje `POST /api/verification/check` do logiki handlera. Zabezpiecz endpoint za pomocą `.RequireAuthorization()`.
9. W głównym pliku `Program.cs` zarejestruj nowo utworzony endpoint, wywołując `LottoTM.Server.Api.Features.Verification.Check.Endpoint.AddEndpoint(app);`.
10. Zarejestruj walidator w kontenerze DI w `Program.cs`: `builder.Services.AddScoped<IValidator<Features.Verification.Check.Contracts.Request>, Features.Verification.Check.Validator>();`.
11. Upewnij się, że wszystkie nowe pliki używają poprawnej przestrzeni nazw `LottoTM.Server.Api.Features.Verification.Check`.
