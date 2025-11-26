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
*   **[x] System XLotto (On-Demand Fetching):**
    *   [x] XLottoService - Pobieranie aktualnych wyników z XLotto przez Gemini API.
    *   [x] Obsługa różnych typów gier (LOTTO, LOTTO PLUS).
    *   [x] Integracja z zewnętrznym serwisem XLotto (włączanie/wyłączanie funkcji jako Feature Flag).
    *   [x] Przetwarzanie i walidacja JSON z Gemini API.
*   **[ ] LottoOpenApiService (Background Worker Fetching):**
    *   [ ] Pobieranie wyników bezpośrednio z oficjalnego Lotto.pl OpenApi.
    *   [ ] Transformacja danych (Lotto → LOTTO, LottoPlus → LOTTO PLUS).
    *   [ ] Walidacja JSON response z Lotto OpenApi.
    *   [ ] Obsługa błędów i brakującej konfiguracji.
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

*   **[ ] TC27:** Pomyślne pobranie wyników z Lotto OpenApi dla daty.
*   **[ ] TC28:** Weryfikacja poprawności transformacji danych (Lotto → LOTTO, LottoPlus → LOTTO PLUS).
*   **[ ] TC29:** Obsługa pustych wyników gdy brak danych w Lotto OpenApi.
*   **[ ] TC30:** Obsługa błędów HTTP przy niedostępności Lotto OpenApi.
*   **[ ] TC31:** Walidacja braku URL w konfiguracji (LottoOpenApi:Url).
*   **[ ] TC32:** Walidacja braku klucza API w konfiguracji (LottoOpenApi:ApiKey).
*   **[ ] TC33:** Weryfikacja poprawności deserializacji JSON z Lotto OpenApi.
*   **[ ] TC34:** Obsługa błędnych formatów danych w odpowiedzi API.
*   **[ ] TC35:** Poprawne ustawienie headers (User-Agent, Accept, secret).
*   **[ ] TC36:** Poprawne logowanie informacji podczas operacji LottoOpenApiService.

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
