
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
*   **[x] Weryfikacja wyników (Verification):**
    *   [x] Mechanizm sprawdzania wygranych dla danego kuponu.
*   **[x] System XLotto:**
    *   [x] Pobieranie aktualnych wyników z XLotto przez Gemini API.
    *   [x] Obsługa różnych typów gier (LOTTO, LOTTO PLUS).
    *   [x] Integracja z zewnętrznym serwisem XLotto (włączanie/Wyłącznake funkcji jako Feature Flag).
    *   [x] Przetwarzanie i walidacja JSON z Gemini API.
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

### 4.4. System XLotto - Pobieranie Aktualnych Wyników

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
5.  Błąd zostaje zamknięty po pomyślnej weryfikacji.
