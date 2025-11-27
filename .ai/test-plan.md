# Plan Testów Aplikacji LottoTM

---

## 1. Wprowadzenie i Cele Testowania

### 1.1. Wprowadzenie

Niniejszy dokument opisuje kompleksowy plan testów dla aplikacji LottoTM, systemu do zarządzania i uczestnictwa w grach losowych. Plan obejmuje strategię, zakres, zasoby i harmonogram działań testowych mających na celu zapewnienie najwyższej jakości, stabilności i bezpieczeństwa aplikacji przed jej wdrożeniem produkcyjnym. Testy zostaną przeprowadzone w oparciu o analizę stosu technologicznego (.NET 8, React, MS SQL Server) oraz architektury oprogramowania.

### 1.2. Cele Testowania

Główne cele procesu testowego to:

*   **Weryfikacja zgodności funkcjonalnej:** Upewnienie się, że wszystkie funkcje aplikacji działają zgodnie z założeniami dokumentacji wymagań (PRD).
*   **Zapewnienie bezpieczeństwa:** Identyfikacja i eliminacja potencjalnych luk w zabezpieczeniach, w szczególności w modułach uwierzytelniania, autoryzacji i przetwarzania danych.
*   **Ocena wydajności i skalowalności:** Sprawdzenie, jak system zachowuje się pod obciążeniem, oraz czy jest gotowy na obsługę rosnącej liczby użytkowników i transakcji.
*   **Zapewnienie integralności danych:** Weryfikacja poprawności operacji na bazie danych, w tym mechanizmów migracji i transakcji.
*   **Gwarancja użyteczności (UX/UI):** Potwierdzenie, że interfejs użytkownika jest intuicyjny, responsywny i spójny na różnych urządzeniach i przeglądarkach.

---

## 2. Zakres Testów

### 2.1. Funkcjonalności objęte testami

*   **[x] Moduł uwierzytelniania i autoryzacji (Auth):**
    *   [x] Rejestracja nowych użytkowników.
    *   [x] Logowanie i wylogowywanie.
    *   [x] Zarządzanie sesją użytkownika (tokeny JWT).
    *   [x] Ochrona punktów końcowych API w oparciu o role.
*   **[x] Zarządzanie losowaniami (Draws):**
    *   [x] Tworzenie, odczytywanie, aktualizowanie i usuwanie losowań.
    *   [x] Automatyczne i manualne zamykanie losowań.
    *   [x] Pobieranie wyników historycznych losowań.
*   **[x] Zarządzanie kuponami (Tickets):**
    *   [x] Składanie nowych kuponów przez użytkowników.
    *   [x] Walidacja numerów na kuponach.
    *   [x] Pobieranie kuponów użytkownika.
    *   [x] Filtrowanie kuponów po nazwie grupy (częściowe dopasowanie LIKE/Contains).
    *   [x] Import kuponów z pliku CSV (Feature Flag).
    *   [x] Eksport kuponów do pliku CSV (Feature Flag).
    *   [x] Usuwanie wszystkich kuponów użytkownika.
*   **[x] Weryfikacja wyników (Verification):**
    *   [x] Mechanizm sprawdzania wygranych dla danego kuponu.
    *   [x] Filtrowanie weryfikacji po nazwie grupy (opcjonalne).
*   **[x] System XLotto (On-Demand Fetching):**
    *   [x] XLottoService - Pobieranie aktualnych wyników z XLotto przez Gemini API.
    *   [x] Obsługa różnych typów gier (LOTTO, LOTTO PLUS).
    *   [x] Integracja z zewnętrznym serwisem XLotto (włączanie/wyłączanie funkcji jako Feature Flag).
    *   [x] Przetwarzanie i walidacja JSON z Gemini API.
*   **[x] LottoOpenApiService (Background Worker Fetching):**
    *   [x] Pobieranie wyników bezpośrednio z oficjalnego Lotto.pl OpenApi.
    *   [x] Deserializacja JSON do typowanych DTOs (GetDrawsLottoByDateResponse, GetDrawsLottoByDateResponseItem, GetDrawsLottoByDateResponseResult).
    *   [x] Zachowanie oryginalnych nazw typów gier z API (Lotto, LottoPlus).
    *   [x] Obsługa błędów i brakującej konfiguracji (URL, ApiKey).
    *   [x] Poprawne ustawienie HTTP headers (User-Agent, Accept, secret).
*   **[ ] LottoWorker - Background Service:**
    *   [ ] Weryfikacja harmonogramu działania (Enable, StartTime, EndTime, InWeek).
    *   [ ] Filtrowanie dni tygodnia (InWeek configuration).
    *   [ ] Pobieranie bieżących wyników (FetchAndSaveDrawsFromLottoOpenApi).
    *   [ ] Pobieranie archiwalnych wyników (FetchAndSaveArchDrawsFromLottoOpenApi).
    *   [ ] Określanie ostatniej daty archiwum (GetLastArchRunTime).
    *   [ ] Zapisywanie DrawSystemId do encji Draw.
    *   [ ] Ustawianie domyślnej TicketPrice (3.0m dla Lotto, 1.0m dla LottoPlus).
    *   [ ] Sprawdzanie duplikatów przed zapisem.
    *   [ ] Ping API dla keep-alive (PingForApiVersion).
*   **[ ] Draw Entity - Nowe właściwości:**
    *   [ ] DrawSystemId (int) - poprawne zapisywanie i odczyt.
    *   [ ] TicketPrice (decimal?) - nullable, precision 18,2.
    *   [ ] WinPoolCount1-4 (int?) - nullable fields.
    *   [ ] WinPoolAmount1-4 (decimal?) - nullable, precision 18,2.
*   **[ ] Interfejs użytkownika (Frontend):**
    *   [ ] Nawigacja i routing.
    *   [ ] Responsywność widoków.
    *   [ ] Interakcja z komponentami UI.

### 2.2. Funkcjonalności wyłączone z testów

*   Testy modułów zewnętrznych (np. bramki płatności), dla których będą stosowane mocki lub staby.
*   Testy wydajnościowe infrastruktury firm trzecich.

---

## 3. Typy Testów

Proces testowy zostanie podzielony na następujące poziomy i typy:

| Typ Testu | Opis | Odpowiedzialność | Narzędzia |
| :--- | :--- | :--- | :--- |
| **Testy Jednostkowe** | Weryfikacja pojedynczych komponentów/metod w izolacji. Głównie w logice biznesowej (serwisy, endpointy) oraz komponentach React. | Deweloperzy | xUnit, Moq, Jest, React Testing Library |
| **Testy Integracyjne** | Sprawdzanie poprawności współpracy między modułami, np. endpoint API -> serwis -> repozytorium -> baza danych (in-memory lub testowa). | Deweloperzy, QA | WebApplicationFactory, Entity Framework InMemory Provider |
| **Testy E2E (End-to-End)** | Symulacja pełnych scenariuszy użytkownika w działającej aplikacji (frontend + backend). | Inżynier QA | Playwright / Cypress |
| **Testy API** | Bezpośrednia weryfikacja kontraktu i logiki punktów końcowych API (żądania/odpowiedzi, kody statusu, obsługa błędów). | Inżynier QA | Postman, testy automatyczne w .NET |
| **Testy Bezpieczeństwa** | Testy penetracyjne, weryfikacja podatności na ataki (SQL Injection, XSS), sprawdzanie poprawności implementacji JWT. | Inżynier QA / Specjalista ds. Bezpieczeństwa | ZAP (OWASP), narzędzia do skanowania JWT |
| **Testy Wydajnościowe** | Testy obciążeniowe i stress-testy kluczowych endpointów (np. składanie kuponów, losowanie wyników). | Inżynier QA | k6, JMeter |
| **Testy Użyteczności (UAT)** | Testy akceptacyjne wykonywane przez użytkowników końcowych w środowisku zbliżonym do produkcyjnego. | Użytkownicy biznesowi, Product Owner | - |

---

## 4. Scenariusze Testowe dla Kluczowych Funkcjonalności

### 4.1. Rejestracja i Logowanie

*   **[x] TC1:** Pomyślna rejestracja z poprawnymi danymi.
*   **[x] TC2:** Próba rejestracji z zajętym adresem e-mail.
*   **[x] TC3:** Pomyślne logowanie i otrzymanie tokenu JWT.
*   **[x] TC4:** Próba logowania z błędnym hasłem.
*   **[x] TC5:** Próba dostępu do chronionego zasobu bez tokenu (oczekiwany błąd 401).
*   **[x] TC6:** Próba dostępu do chronionego zasobu z wygasłym tokenem.

### 4.2. Składanie Kuponu

*   **[x] TC7:** Zalogowany użytkownik pomyślnie składa kupon na aktywne losowanie.
*   **[x] TC8:** Próba złożenia kuponu na losowanie, które już się zakończyło.
*   **[x] TC9:** Próba złożenia kuponu z niepoprawną liczbą numerów.
*   **[x] TC10:** Niezalogowany użytkownik próbuje złożyć kupon (oczekiwany błąd 401).

### 4.3. Weryfikacja Wyników

*   **[x] TC11:** Sprawdzenie kuponu, który wygrał nagrodę główną.
*   **[x] TC12:** Sprawdzenie kuponu z częściową wygraną.
*   **[x] TC13:** Sprawdzenie kuponu, który nie wygrał.
*   **[x] TC14:** Próba sprawdzenia wyników dla losowania, które jeszcze się nie odbyło.

#### Weryfikacja z filtrowaniem po GroupName (POST /api/verification/check) - wyszukiwanie częściowe (Contains, case-insensitive)

*   **[x] TC14a:** Pomyślna weryfikacja z trafionymi kuponami bez filtrowania (groupName=null).
*   **[x] TC14b:** Weryfikacja z filtrowaniem po fragmencie nazwy grupy - zwracane tylko kupony, których GroupName zawiera podany tekst (np. "ulu" znajdzie "Ulubione", "Ulubione 2024").
*   **[x] TC14b2:** Weryfikacja z filtrowaniem case-insensitive - "ULU" znajdzie "Ulubione" i "ulubione".
*   **[x] TC14c:** Weryfikacja z filtrowaniem po nieistniejącym fragmencie - zwraca puste wyniki (totalTickets=0).
*   **[x] TC14d:** Weryfikacja z pustym groupName ("") - sprawdza wszystkie kupony użytkownika.
*   **[x] TC14e:** Weryfikacja z wieloma kuponami z różnych grup - filtr częściowy działa poprawnie (np. "2024" znajduje "Ulubione 2024" i "Losowe 2024").
*   **[x] TC14f:** Weryfikacja zwraca tylko kupony z minimum 3 trafieniami.
*   **[x] TC14g:** Weryfikacja z kuponami mającymi dokładnie 3 trafienia - są uwzględniane.
*   **[x] TC14h:** Weryfikacja z kuponami mającymi tylko 2 trafienia - nie są uwzględniane.
*   **[x] TC14i:** Weryfikacja zakresu dat - DateTo przed DateFrom zwraca błąd 400.
*   **[x] TC14j:** Weryfikacja zakresu dat przekraczającego 31 dni - zwraca błąd 400.
*   **[x] TC14k:** Weryfikacja bez autentykacji - zwraca błąd 401.
*   **[x] TC14l:** Weryfikacja izolacji użytkowników - zwracane tylko kupony zalogowanego użytkownika.
*   **[x] TC14m:** Weryfikacja z różnymi typami losowań (LOTTO, LOTTO PLUS) - poprawne rozpoznanie typów.
*   **[x] TC14n:** Weryfikacja zwraca czas wykonania (ExecutionTimeMs >= 0).
*   **[x] TC14o:** Weryfikacja z pustą listą kuponów - totalTickets=0, puste wyniki.
*   **[x] TC14p:** Weryfikacja z pustą listą losowań w zakresie dat - totalDraws=0, puste wyniki.


### 4.4. System XLotto - Pobieranie Aktualnych Wyników (On-Demand via XLottoService)

*   **[x] TC15:** Pomyślne pobranie wyników z XLotto dla typu LOTTO z prawidłową autoryzacją.
*   **[x] TC16:** Pomyślne pobranie wyników dla typu LOTTO PLUS.
*   **[x] TC17:** Pobranie wyników z domyślnymi parametrami (bieżąca data, typ LOTTO).
*   **[x] TC18:** Próba pobrania wyników bez autoryzacji (oczekiwany błąd 401).
*   **[x] TC19:** Obsługa pustych wyników gdy brak danych dla danej daty.
*   **[x] TC20:** Obsługa błędów serwisu XLotto (oczekiwany błąd 500).
*   **[x] TC21:** Weryfikacja poprawności przetwarzania JSON z Gemini API.
*   **[x] TC22:** Obsługa wyjątków HttpRequestException przy niedostępności serwisu.
*   **[x] TC23:** Walidacja braku klucza API w konfiguracji.
*   **[x] TC24:** Czyszczenie bloków markdown z odpowiedzi Gemini API.
*   **[x] TC25:** Obsługa różnych kodowań znaków (UTF-8, ISO-8859-2).
*   **[x] TC26:** Poprawne logowanie informacji podczas operacji.

### 4.5. LottoOpenApiService - Automatyczne Pobieranie Wyników (Background Worker)

*   **[x] TC27:** Pomyślne pobranie wyników z Lotto OpenApi dla daty.
*   **[x] TC28:** Weryfikacja poprawności deserializacji danych do typowanych DTOs (GetDrawsLottoByDateResponse).
*   **[x] TC29:** Obsługa pustych wyników gdy brak danych w Lotto OpenApi (totalRows=0).
*   **[x] TC30:** Obsługa błędów HTTP przy niedostępności Lotto OpenApi (HttpRequestException).
*   **[x] TC31:** Walidacja braku URL w konfiguracji (LottoOpenApi:Url) - InvalidOperationException.
*   **[x] TC32:** Weryfikacja poprawności deserializacji JSON z Lotto OpenApi do GetDrawsLottoByDateResponse.
*   **[x] TC33:** Obsługa błędnych formatów danych w odpowiedzi API (JsonException).
*   **[x] TC34:** Poprawne ustawienie headers (User-Agent, Accept, secret).
*   **[x] TC35:** Poprawne logowanie informacji podczas operacji LottoOpenApiService (Debug level).
*   **[x] TC36:** Obsługa API zwracającego null - zwraca domyślną pustą odpowiedź.
*   **[x] TC37:** Weryfikacja konstruowania poprawnego URL do Lotto OpenApi.
*   **[x] TC38:** Obsługa wielu typów gier w jednym response (Lotto + LottoPlus).
*   **[x] TC39:** Weryfikacja użycia domyślnej daty (Today) gdy parametr date=null.

### 4.6. Pobieranie Listy Kuponów (GET /api/tickets)

*   **[x] TC61:** Pomyślne pobranie wszystkich kuponów zalogowanego użytkownika.
*   **[x] TC62:** Pobranie listy gdy użytkownik nie ma kuponów (pusta lista, totalCount=0).
*   **[x] TC63:** Weryfikacja sortowania kuponów według CreatedAt DESC (najnowsze pierwsze).
*   **[x] TC64:** Weryfikacja izolacji danych - zwracane są tylko kupony zalogowanego użytkownika.
*   **[x] TC65:** Weryfikacja poprawnej kolejności liczb według Position (1-6).
*   **[x] TC66:** Pobranie maksymalnej liczby kuponów (100) - weryfikacja poprawności.
*   **[x] TC67:** Próba pobrania listy bez autentykacji (oczekiwany błąd 401).
*   **[x] TC68:** Próba pobrania listy z niepoprawnym tokenem JWT (oczekiwany błąd 401).

#### Filtrowanie po nazwie grupy (groupName)

*   **[x] TC69:** Filtrowanie częściowe - wyszukiwanie "test" znajduje "test", "testing", "my test group".
*   **[x] TC70:** Filtrowanie częściowe - wyszukiwanie case-sensitive ("Test" vs "test").
*   **[x] TC71:** Filtrowanie z parametrem null/pustym - zwraca wszystkie kupony użytkownika.
*   **[x] TC72:** Filtrowanie po nazwie grupy która nie istnieje - zwraca pustą listę.
*   **[x] TC73:** Walidacja parametru groupName - zbyt długi (>100 znaków) zwraca błąd 400.
*   **[x] TC74:** Filtrowanie ze znakami specjalnymi w groupName (ĄĘŚ, spacje).
*   **[x] TC75:** Weryfikacja izolacji przy filtrowaniu - użytkownik widzi tylko swoje kupony pasujące do filtra.

### 4.7. Import i Eksport Kuponów CSV (Feature Flag)

#### Import Kuponów (POST /api/tickets/import-csv)

*   **[x] TC76:** Pomyślny import kuponów z poprawnym plikiem CSV.
*   **[x] TC77:** Import częściowo niepoprawnych danych (niektóre wiersze odrzucone).
*   **[x] TC78:** Próba importu z niepoprawnym nagłówkiem CSV (oczekiwany błąd 400).
*   **[x] TC79:** Próba importu pliku nie-CSV (oczekiwany błąd 400).
*   **[x] TC80:** Próba importu pliku przekraczającego 1MB (oczekiwany błąd 400).
*   **[x] TC81:** Próba importu gdy osiągnięto limit 100 kuponów (oczekiwany błąd 400).
*   **[x] TC82:** Import z duplikującymi się kuponami w pliku CSV (duplikaty odrzucone).
*   **[x] TC83:** Import z duplikatami istniejących kuponów w bazie (duplikaty odrzucone, różna kolejność liczb).
*   **[x] TC84:** Próba importu bez autentykacji (oczekiwany błąd 401).
*   **[x] TC85:** Próba importu gdy Feature Flag wyłączony (oczekiwany błąd 404).
*   **[x] TC86:** Weryfikacja izolacji danych między użytkownikami podczas importu.

#### Eksport Kuponów (GET /api/tickets/export-csv)

*   **[x] TC87:** Pomyślny eksport wielu kuponów do CSV.
*   **[x] TC88:** Eksport gdy użytkownik nie ma kuponów (tylko nagłówek CSV).
*   **[x] TC89:** Eksport kuponów ze znakami specjalnymi w GroupName (ĄĘŚ).
*   **[x] TC90:** Weryfikacja izolacji danych - eksport tylko kuponów bieżącego użytkownika.
*   **[x] TC91:** Weryfikacja zachowania kolejności liczb w eksportowanym CSV.
*   **[x] TC92:** Próba eksportu bez autentykacji (oczekiwany błąd 401).
*   **[x] TC93:** Próba eksportu gdy Feature Flag wyłączony (oczekiwany błąd 404).
*   **[x] TC94:** Eksport dużej liczby kuponów (50+) - weryfikacja poprawności.

#### Walidacja Danych CSV

*   **[x] TC95:** Walidacja formatu CSV (Number1,Number2,Number3,Number4,Number5,Number6,GroupName).
*   **[x] TC96:** Walidacja zakresu liczb (1-49) podczas importu.
*   **[x] TC97:** Walidacja unikalności liczb w zestawie (6 unikalnych liczb).
*   **[x] TC98:** Walidacja liczby kolumn w wierszu CSV (minimum 6).
*   **[x] TC99:** Weryfikacja poprawności kodowania UTF-8 w importowanych/eksportowanych plikach.

### 4.8. Usuwanie Wszystkich Kuponów (DELETE /api/tickets/all)

*   **[x] TC100:** Pomyślne usunięcie wszystkich kuponów zalogowanego użytkownika (zwraca liczbę usuniętych kuponów).
*   **[x] TC101:** Usunięcie gdy użytkownik nie ma żadnych kuponów (zwraca 0 i odpowiedni komunikat).
*   **[x] TC102:** Weryfikacja izolacji danych - usuwane są tylko kupony zalogowanego użytkownika, kupony innych pozostają nienaruszone.
*   **[x] TC103:** Usunięcie dużej liczby kuponów (20+) - weryfikacja poprawności operacji.
*   **[x] TC104:** Próba usunięcia bez autentykacji (oczekiwany błąd 401).
*   **[x] TC105:** Próba usunięcia z niepoprawnym tokenem JWT (oczekiwany błąd 401).
*   **[x] TC106:** Weryfikacja kaskadowego usuwania powiązanych TicketNumbers (integralność danych).

### 4.9. LottoWorker - Background Service

*   **[ ] TC107:** Weryfikacja uruchomienia workera przy starcie aplikacji.
*   **[ ] TC108:** Weryfikacja działania workera w oknie czasowym (StartTime - EndTime).
*   **[ ] TC109:** Weryfikacja braku działania poza oknem czasowym.
*   **[ ] TC110:** Weryfikacja filtrowania dni tygodnia według InWeek configuration (np. tylko WT, PT, SO).
*   **[ ] TC111:** Weryfikacja działania workera tylko gdy Enable=true.
*   **[ ] TC112:** Weryfikacja braku działania gdy Enable=false.
*   **[ ] TC113:** Pobieranie bieżących wyników dla Today (FetchAndSaveDrawsFromLottoOpenApi).
*   **[ ] TC114:** Pobieranie archiwalnych wyników wstecz (FetchAndSaveArchDrawsFromLottoOpenApi).
*   **[ ] TC115:** Określanie ostatniej daty archiwum (GetLastArchRunTime) - zwraca najstarszą datę z bazy.
*   **[ ] TC116:** Zapisywanie DrawSystemId przy tworzeniu Draw.
*   **[ ] TC117:** Ustawianie domyślnej TicketPrice (3.0m dla Lotto, 1.0m dla LottoPlus).
*   **[ ] TC118:** Sprawdzanie duplikatów przed zapisem (pomijanie gdy Draw już istnieje).
*   **[ ] TC119:** Ping API dla keep-alive (PingForApiVersion) - nie przerywa działania przy błędzie.
*   **[ ] TC120:** Obsługa błędów pobierania - worker kontynuuje działanie w następnym cyklu.
*   **[ ] TC121:** Weryfikacja interwału sprawdzania (IntervalMinutes).
*   **[ ] TC122:** Walidacja liczb przed zapisem (6 liczb, zakres 1-49, unikalne).
*   **[ ] TC123:** Transakcyjność zapisu (Draw + DrawNumbers) - rollback przy błędzie.
*   **[ ] TC124:** Logowanie wszystkich kluczowych zdarzeń (start, sukces, błędy).
*   **[ ] TC125:** Weryfikacja mapowania dni tygodnia (PN, WT, SR, CZ, PT, SO, ND).

### 4.10. Draw Entity - Nowe Właściwości

*   **[ ] TC126:** Zapisywanie i odczyt DrawSystemId (int).
*   **[ ] TC127:** Zapisywanie i odczyt TicketPrice (decimal?, precision 18,2).
*   **[ ] TC128:** Zapisywanie i odczyt WinPoolCount1 (int?, nullable).
*   **[ ] TC129:** Zapisywanie i odczyt WinPoolAmount1 (decimal?, precision 18,2, nullable).
*   **[ ] TC130:** Zapisywanie i odczyt WinPoolCount2-4 (int?, nullable).
*   **[ ] TC131:** Zapisywanie i odczyt WinPoolAmount2-4 (decimal?, precision 18,2, nullable).
*   **[ ] TC132:** Weryfikacja nullable fields - akceptuje NULL jako wartość.
*   **[ ] TC133:** Weryfikacja precision dla decimal fields (18,2).
*   **[ ] TC134:** Migracja bazy danych - nowe kolumny utworzone poprawnie.
*   **[ ] TC135:** Kompatybilność wsteczna - istniejące rekordy nie zostały uszkodzone.
*   **[ ] TC136:** Unique constraint na (DrawDate, LottoType) nadal działa poprawnie.

---

## 5. Środowisko Testowe

Zostaną przygotowane trzy główne środowiska:

1.  **Środowisko deweloperskie (Lokalne):**
    *   Używane przez deweloperów do tworzenia i uruchamiania testów jednostkowych oraz integracyjnych.
    *   Backend uruchamiany w Visual Studio/Rider, frontend przez Vite.
    *   Baza danych: Lokalna instancja MS SQL lub baza danych w kontenerze Docker.
2.  **Środowisko testowe (Staging):**
    *   W pełni zintegrowane środowisko odzwierciedlające architekturę produkcyjną.
    *   Aplikacja wdrożona na serwerze (np. z użyciem Dockera).
    *   Dedykowana baza danych MS SQL Server.
    *   Na tym środowisku będą przeprowadzane testy E2E, API, wydajnościowe i UAT.
3.  **Środowisko produkcyjne:**
    *   Środowisko docelowe. Po pomyślnym zakończeniu testów na środowisku Staging, aplikacja zostanie wdrożona tutaj.

---

## 6. Narzędzia do Testowania

*   **Frameworki testowe:** xUnit (backend), Jest/RTL (frontend)
*   **Mockowanie:** Moq (backend)
*   **Testy E2E:** Playwright
*   **Testy API:** Postman, REST Client (VS Code)
*   **Testy wydajnościowe:** k6
*   **CI/CD:** GitHub Actions (do automatycznego uruchamiania testów po każdym pushu)
*   **Zarządzanie kodem:** Git, GitHub
*   **Zarządzanie zadaniami i błędami:** GitHub Issues / Jira

---

## 7. Harmonogram Testów

*Harmonogram jest przykładowy i wymaga dostosowania do faktycznego planu projektu.*

| Faza | Zadania | Przewidywany Czas |
| :--- | :--- | :--- |
| **Sprint 1-2** | Implementacja i testy jednostkowe/integracyjne modułu Auth i User. | 2 tygodnie |
| **Sprint 3-4** | Implementacja i testy jednostkowe/integracyjne modułów Draws i Tickets. | 2 tygodnie |
| **Sprint 5** | Przygotowanie środowiska Staging. Pierwsze testy E2E i API. | 1 tydzień |
| **Sprint 6** | Testy wydajnościowe i bezpieczeństwa. | 1 tydzień |
| **Sprint 7** | Faza stabilizacji, poprawa błędów, re-testy. | 1 tydzień |
| **Sprint 8** | Testy UAT. Finalne przygotowanie do wdrożenia. | 1 tydzień |

---

## 8. Kryteria Akceptacji Testów

### 8.1. Kryteria Wejścia

*   Ukończona implementacja funkcjonalności przewidzianej do testów.
*   Pomyślne przejście wszystkich testów jednostkowych i integracyjnych w pipeline CI/CD.
*   Dostępna i stabilna wersja aplikacji na środowisku testowym.

### 8.2. Kryteria Wyjścia ( zakończenia testów)

*   **100%** wykonanych scenariuszy testowych.
*   **95%** scenariuszy testowych zakończonych sukcesem.
*   **Brak błędów krytycznych (blocker/critical)** otwartych.
*   Wszystkie błędy o wysokim priorytecie (high) zostały rozwiązane.
*   Pokrycie kodu testami jednostkowymi na poziomie min. **80%**.

---

## 9. Role i Odpowiedzialności

*   **Deweloperzy:**
    *   Pisanie testów jednostkowych i integracyjnych.
    *   Poprawianie błędów zgłoszonych przez QA.
    *   Utrzymanie pipeline'u CI/CD.
*   **Inżynier QA:**
    *   Tworzenie i utrzymanie planu testów.
    *   Projektowanie i automatyzacja scenariuszy E2E, API, wydajnościowych.
    *   Raportowanie i weryfikacja błędów.
    *   Koordynacja testów UAT.
*   **Product Owner:**
    *   Dostarczenie kryteriów akceptacji dla funkcjonalności.
    *   Udział w testach UAT.
    *   Priorytetyzacja błędów.
*   **DevOps:**
    *   Zarządzanie i utrzymanie środowisk testowych.

---

## 10. Procedury Raportowania Błędów

1.  Każdy znaleziony błąd musi zostać zaraportowany w systemie do śledzenia błędów (np. GitHub Issues).
2.  Raport o błędzie musi zawierać:
    *   **Tytuł:** Zwięzły opis problemu.
    *   **Środowisko:** Gdzie błąd wystąpił (np. Staging, `v1.2.3`).
    *   **Kroki do odtworzenia:** Szczegółowa, numerowana lista kroków.
    *   **Wynik oczekiwany:** Co powinno się wydarzyć.
    *   **Wynik aktualny:** Co faktycznie się wydarzyło.
    *   **Priorytet:** (Krytyczny, Wysoki, Średni, Niski).
    *   **Załączniki:** Zrzuty ekranu, logi, nagrania wideo.
3.  Błąd jest przypisywany do odpowiedniego dewelopera.
4.  Po naprawieniu błędu, Inżynier QA przeprowadza re-testy w celu weryfikacji poprawki.
5.  Błąd zostaje zamknięty po pomyślnym weryfikacji.
