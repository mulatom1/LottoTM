# Product Requirements Document (PRD)
# LottoTM - System Zarządzania Kuponami Lotto MVP

**Wersja:** 1.0
**Data:** 2025-10-30
**Autor:** Tomasz Mularczyk

---

## 1. Wprowadzenie

### 1.1 Cel dokumentu
Niniejszy dokument definiuje wymagania produktowe dla systemu LottoTM w wersji MVP (Minimum Viable Product). Zawiera szczegółowe specyfikacje funkcjonalne, techniczne oraz kryteria sukcesu projektu.

### 1.2 Wizja produktu
Stworzenie intuicyjnego systemu dla graczy LOTTO, który ułatwi zarządzanie wieloma zestawami liczb i automatycznie wskaże wygrane, zwiększając komfort i efektywność gry.

### 1.3 Problem biznesowy
Wielu graczy LOTTO posiada liczne zestawy liczb do sprawdzenia. Ręczne weryfikowanie każdego kuponu po losowaniu jest procesem:
- Czasochłonnym dla wielu zestawów
- Monotonnym i męczącym
- Podatnym na błędy ludzkie
- Prowadzącym do ryzyka przeoczenia wygranej

**Rozwiązanie:** LottoTM automatyzuje proces weryfikacji, umożliwiając sprawdzenie do 100 zestawów w mniej niż 2 sekundy.

---

## 2. Zakres Projektu

### 2.1 Zakres MVP (Must Have)

**Zarządzanie kontem użytkownika:**
- Rejestracja z wykorzystaniem email i hasła
- Logowanie i wylogowywanie (JWT)
- Izolacja danych między użytkownikami

**Zarządzanie losowaniami:**
- Ręczne wprowadzanie wyników oficjalnych losowań LOTTO (6 z 49)
- Przechowywanie historii wyników losowań
- Walidacja danych wejściowych (6 unikalnych liczb z zakresu 1-49)

**Zarządzanie zestawami liczb (kuponami):**
- Przeglądanie wszystkich zapisanych zestawów użytkownika
- Dodawanie zestawu ręcznie
- Edycja istniejącego zestawu
- Usuwanie zestawu
- Generator pojedynczego losowego zestawu
- Generator "systemowy" - 9 zestawów pokrywających wszystkie liczby 1-49
- Limit 100 zestawów na użytkownika

**Weryfikacja wygranych:**
- Sprawdzanie zestawów względem wyników losowań w wybranym przedziale dat
- Wizualne oznaczenie wygranych liczb (pogrubienie)
- Wyświetlenie etykiety z liczbą trafień ("Wygrana 3 (trójka)", "Wygrana 4 (czwórka)" itd.)

### 2.2 Poza zakresem MVP (Out of Scope)

**Funkcjonalności odłożone na kolejne wersje:**
- Wsparcie dla innych gier losowych (MINI LOTTO, Multi Multi, EuroJackpot)
- Weryfikacja adresu email przy rejestracji
- Automatyczne pobieranie wyników losowań z zewnętrznych API
- Import/eksport zestawów z plików (CSV, PDF)
- Powiadomienia email lub push
- Funkcje społecznościowe (udostępnianie zestawów)
- Dedykowana aplikacja mobilna (natywna iOS/Android)
- Informacje o kategoriach wygranych i kwotach nagród
- Analiza AI/ML (wzorce w wyborze liczb, rekomendacje)
- Integracja z systemami płatności
- System zakładów grupowych

### 2.3 Kluczowe decyzje projektowe

1. **Zakres gier:** MVP wspiera wyłącznie LOTTO (6 z 49)
2. **Rejestracja:** Bez weryfikacji email - natychmiastowy dostęp po rejestracji, bez aktywacji linka z maila
3. **UI wprowadzania wyników:** 6 oddzielnych pól numerycznych z walidacją w czasie rzeczywistym + date picker
4. **Prezentacja wygranych:** Lista zbiorcza: data oraz zestawy z pogrubionymi wygranymi liczbami + etykiety z liczbą trafień
5. **Generator systemowy:** Algorytm losowo rozdziela liczby 1-49 w 9 zestawach + losowe dopełnienie pozostałych pozycji
6. **Limit zestawów:** Maksymalnie 100 zestawów na użytkownika
7. **Weryfikacja:** Elastyczny date range picker do wyboru zakresu dat
8. **Powiadomienia:** Tylko wizualny wskaźnik w UI (brak email/push)
9. **Edycja zestawów:** Użytkownik może edytować zapisane zestawy

---

## 3. Wymagania Funkcjonalne

### 3.1 Moduł Autoryzacji (Auth)

#### F-AUTH-001: Rejestracja użytkownika
**Priorytet:** Must Have
**Opis:** Użytkownik może utworzyć nowe konto w systemie.

**Kryteria akceptacji:**
- Formularz zawiera pola: email, hasło, potwierdzenie hasła
- Email jest unikalny w systemie
- Walidacja formatu email (regex)
- Wymagania dla hasła: przechowywane w bezpieczny sposób
- Po pomyślnej rejestracji użytkownik jest automatycznie zalogowany
- Zwracany jest token JWT
- Brak wymaganej weryfikacji adresu email w MVP

**Endpointy API:**
- `POST /api/auth/register`
  - Body: `{ "email": "string", "password": "string", "confirmPassword": "string" }`
  - Response: `{ "userId": "number", "email": "string" }`

#### F-AUTH-002: Logowanie użytkownika
**Priorytet:** Must Have
**Opis:** Zarejestrowany użytkownik może zalogować się do systemu.

**Kryteria akceptacji:**
- Formularz zawiera pola: email, hasło
- Walidacja poprawności credentials
- Po pomyślnym logowaniu zwracany jest token JWT z czasem wygaśnięcia
- Token jest przechowywany w local storage / session storage
- Nieprawidłowe dane → komunikat błędu

**Endpointy API:**
- `POST /api/auth/login`
  - Body: `{ "email": "string", "password": "string" }`
  - Response: `{ "token": "string", "userId": "number", "email": "string", "expiresAt": "datetime" }`

#### F-AUTH-003: Wylogowanie użytkownika
**Priorytet:** Must Have
**Opis:** Zalogowany użytkownik może się wylogować.

**Kryteria akceptacji:**
- Przycisk wylogowania dostępny w UI
- Po wylogowaniu token JWT jest usuwany z przeglądarki
- Przekierowanie na stronę logowania
- Opcjonalnie: blacklisting tokenu po stronie serwera

**Endpointy API:**
- `POST /api/auth/logout`
  - Response: `{ "message": "Logged out successfully" }`

#### F-AUTH-004: Izolacja danych użytkowników
**Priorytet:** Must Have (Security)
**Opis:** Każdy użytkownik ma dostęp wyłącznie do swoich danych.

**Kryteria akceptacji:**
- Wszystkie endpointy API wymagają uwierzytelnionego tokenu JWT
- Operacje na zestawach/weryfikacjach filtrują dane po `UserId` z tokenu
- Próba dostępu do cudzych danych → HTTP 403 Forbidden

---

### 3.2 Moduł Zarządzania Losowaniami (Draws)

#### F-DRAW-001: Dodawanie wyniku losowania
**Priorytet:** Must Have
**Opis:** Użytkownik może ręcznie wprowadzić oficjalne wyniki losowania LOTTO.

**Kryteria akceptacji:**
- Formularz zawiera 6 oddzielnych pól numerycznych (type="number")
- Zakres wartości: 1-49 dla każdego pola
- Walidacja w czasie rzeczywistym:
  - Liczby muszą być unikalne (brak duplikatów)
  - Wszystkie pola wymagane
  - Wyświetlanie komunikatów błędów pod polami
- Date picker do wyboru daty losowania
- Walidacja: data losowania nie może być w przyszłości
- Zapis wyniku do bazy danych
- Po zapisie: komunikat sukcesu + przekierowanie/odświeżenie listy losowań

**Endpointy API:**
- `POST /api/draws`
  - Body: `{ "drawDate": "2025-10-30", "numbers": [3, 12, 25, 31, 42, 48] }`
  - Response: `{ "id": "number", "drawDate": "2025-10-30", "numbers": [3, 12, 25, 31, 42, 48], "createdAt": "datetime" }`

#### F-DRAW-002: Przeglądanie historii losowań
**Priorytet:** Must Have
**Opis:** Użytkownik może zobaczyć listę wszystkich wprowadzonych wyników losowań.

**Kryteria akceptacji:**
- Lista zawiera: datę losowania, 6 liczb, datę dodania do systemu
- Sortowanie: domyślnie według daty losowania (najnowsze na górze)
- Paginacja dla dużej liczby wyników (opcjonalnie w MVP)

**Endpointy API:**
- `GET /api/draws?page=1&pageSize=20&sortBy=drawDate&sortOrder=desc`
  - Response: `{ "draws": [...], "totalCount": 123, "page": 1, "pageSize": 20 }`

#### F-DRAW-003: Pobranie szczegółów losowania
**Priorytet:** Should Have
**Opis:** Użytkownik może zobaczyć szczegóły pojedynczego losowania.

**Endpointy API:**
- `GET /api/draws/{id}`
  - Response: `{ "id": "number", "drawDate": "2025-10-30", "numbers": [3, 12, 25, 31, 42, 48], "createdAt": "datetime" }`

---

### 3.3 Moduł Zarządzania Zestawami (Tickets)

#### F-TICKET-001: Przeglądanie zestawów użytkownika
**Priorytet:** Must Have
**Opis:** Użytkownik może zobaczyć listę wszystkich swoich zapisanych zestawów liczb.

**Kryteria akceptacji:**
- Lista zawiera: nazwę zestawu (lub domyślną), 6 liczb, datę utworzenia, datę ostatniej modyfikacji
- Wyświetlenie licznika: "X / 100 zestawów"
- Sortowanie: [DO USTALENIA] według daty utworzenia? nazwy? (patrz sekcja 9.9)
- Paginacja lub infinite scroll dla wielu zestawów
- Przycisk akcji przy każdym zestawie: Edytuj, Usuń

**Endpointy API:**
- `GET /api/tickets?page=1&pageSize=50&sortBy=createdAt&sortOrder=desc`
  - Response: `{ "tickets": [...], "totalCount": 42, "page": 1, "pageSize": 50 }`

#### F-TICKET-002: Dodawanie zestawu ręcznie
**Priorytet:** Must Have
**Opis:** Użytkownik może ręcznie wprowadzić nowy zestaw 6 liczb.

**Kryteria akceptacji:**
- Formularz zawiera 6 oddzielnych pól numerycznych (type="number")
- Opcjonalne pole: nazwa zestawu [DO USTALENIA - patrz sekcja 9.2]
- Zakres wartości: 1-49 dla każdego pola
- Walidacja w czasie rzeczywistym:
  - Liczby muszą być unikalne
  - Wszystkie pola wymagane
  - Wyświetlanie komunikatów błędów
- Walidacja limitu: jeśli użytkownik ma już 100 zestawów → blokada + komunikat
- [DO USTALENIA] Czy system blokuje duplikaty zestawów? (patrz sekcja 9.3)
- Zapis zestawu do bazy danych z powiązaniem do `UserId`
- Po zapisie: komunikat sukcesu + powrót do listy zestawów

**Endpointy API:**
- `POST /api/tickets`
  - Body: `{ "name": "string (optional)", "numbers": [5, 14, 23, 29, 37, 41] }`
  - Response: `{ "id": "number", "userId": "guid", "name": "string", "numbers": [5, 14, 23, 29, 37, 41], "createdAt": "datetime", "updatedAt": "datetime" }`

#### F-TICKET-003: Edycja zestawu
**Priorytet:** Must Have
**Opis:** Użytkownik może edytować istniejący zestaw liczb.

**Kryteria akceptacji:**
- Przycisk "Edytuj" przy zestawie
- Formularz podobny do dodawania, ale wypełniony aktualnymi danymi
- Te same walidacje co przy dodawaniu
- [DO USTALENIA - KRYTYCZNE] Jak obsłużyć edycję zestawu, który był już weryfikowany? (patrz sekcja 9.1)
  - Opcja A: Wersjonowanie zestawów (zachowanie historii)
  - Opcja B: Zakaz edycji zestawów po weryfikacji
  - Opcja C: Nadpisanie z ostrzeżeniem + utrata historii weryfikacji
- Aktualizacja pola `updatedAt`
- Po zapisie: komunikat sukcesu + powrót do listy

**Endpointy API:**
- `PUT /api/tickets/{id}`
  - Body: `{ "name": "string (optional)", "numbers": [5, 14, 23, 29, 37, 41] }`
  - Response: `{ "id": "number", "userId": "guid", "name": "string", "numbers": [5, 14, 23, 29, 37, 41], "createdAt": "datetime", "updatedAt": "datetime" }`

#### F-TICKET-004: Usuwanie zestawu
**Priorytet:** Must Have
**Opis:** Użytkownik może usunąć wybrany zestaw liczb.

**Kryteria akceptacji:**
- Przycisk "Usuń" przy zestawie
- Modal potwierdzenia: "Czy na pewno chcesz usunąć ten zestaw?"
- Po potwierdzeniu: usunięcie zestawu z bazy danych
- Usunięcie powiązanych rekordów weryfikacji (cascade delete lub soft delete)
- Komunikat sukcesu + odświeżenie listy

**Endpointy API:**
- `DELETE /api/tickets/{id}`
  - Response: `{ "message": "Ticket deleted successfully" }`

#### F-TICKET-005: Generator losowego zestawu
**Priorytet:** Must Have
**Opis:** Użytkownik może wygenerować pojedynczy losowy zestaw 6 liczb z zakresu 1-49.

**Kryteria akceptacji:**
- Przycisk "Generuj losowy zestaw"
- Algorytm generuje 6 unikalnych liczb z zakresu 1-49
- Użytkownik widzi podgląd wygenerowanego zestawu
- Opcje:
  - "Zapisz" → zapis zestawu (z opcjonalną nazwą)
  - "Generuj ponownie" → nowy losowy zestaw
  - "Anuluj" → powrót bez zapisu
- Walidacja limitu 100 zestawów przed zapisem

**Endpointy API:**
- `POST /api/tickets/generate-random`
  - Body: `{ "name": "string (optional)" }`
  - Response: `{ "id": "number", "userId": "guid", "name": "string", "numbers": [7, 19, 28, 33, 40, 46], "createdAt": "datetime", "updatedAt": "datetime" }`

#### F-TICKET-006: Generator systemowy (9 zestawów)
**Priorytet:** Must Have
**Opis:** Użytkownik może wygenerować 9 zestawów pokrywających wszystkie liczby od 1 do 49.

**Algorytm:**
1. Każda liczba 1-49 pojawia się minimum raz w losowej pozycji w jednym z 9 zestawów
2. 9 zestawów × 6 liczb = 54 pozycje
3. 54 pozycje - 49 liczb = 5 wolnych pozycji
4. 5 wolnych pozycji jest losowo dopełnionych powtórzeniami liczb 1-49

**Kryteria akceptacji:**
- Przycisk "Generuj zestawy systemowe"
- Tooltip/wyjaśnienie funkcji (edukacyjny charakter - nie zwiększa matematycznie szans na wygraną, ale zapewnia pełne pokrycie)
- [DO USTALENIA] Czy użytkownik widzi podgląd 9 zestawów przed zapisem? (patrz sekcja 9.4)
- [DO USTALENIA] Czy można zapisać tylko wybrane zestawy z 9? (patrz sekcja 9.4)
- Walidacja limitu: czy użytkownik ma miejsce na 9 nowych zestawów (max 100 - aktualna liczba ≥ 9)
- Po zapisie: komunikat sukcesu + odświeżenie listy

**Endpointy API:**
- `POST /api/tickets/generate-system`
  - Body: `{ "namePrefix": "string (optional)" }` (np. "System #1", "System #2"...)
  - Response: `{ "tickets": [ {...}, {...}, ... ], "count": 9 }`

---

### 3.4 Moduł Weryfikacji Wygranych (Verification)

#### F-VERIFY-001: Weryfikacja wygranych w przedziale dat
**Priorytet:** Must Have
**Opis:** Użytkownik może sprawdzić swoje zestawy względem wyników losowań w wybranym zakresie dat.

**Kryteria akceptacji:**
- Date range picker do wyboru zakresu dat (od - do)
- [DO USTALENIA] Domyślna wartość zakresu dat (patrz sekcja 9.7)
- Przycisk "Sprawdź wygrane"
- System porównuje wszystkie zestawy użytkownika z wynikami losowań w wybranym zakresie
- Algorytm weryfikacji:
  - Dla każdego zestawu użytkownika
  - Dla każdego losowania w zakresie
  - Oblicz liczbę trafień (liczby wspólne)
- Wymagana wydajność: weryfikacja 100 zestawów względem 1 losowania ≤ 2 sekundy
- Prezentacja wyników (patrz F-VERIFY-002)

**Endpointy API:**
- `POST /api/verification/check`
  - Body: `{ "dateFrom": "2025-10-01", "dateTo": "2025-10-30" }`
  - Response: `{ "results": [ { "ticketId": "guid", "ticketName": "string", "numbers": [...], "draws": [ { "drawId": "guid", "drawDate": "date", "drawNumbers": [...], "matchCount": 3, "matchedNumbers": [5, 14, 29] } ] } ], "totalTickets": 42, "totalDraws": 8, "executionTimeMs": 1234 }`

#### F-VERIFY-002: Prezentacja wyników weryfikacji
**Priorytet:** Must Have
**Opis:** System prezentuje wyniki weryfikacji w formie listy zbiorczej.

**Kryteria akceptacji:**
- Lista zawiera wszystkie zestawy użytkownika
- [DO USTALENIA] Czy pokazujemy zestawy z 0 trafień? (patrz sekcja 9.8)
- Dla każdego zestawu:
  - Nazwa zestawu (lub domyślna)
  - 6 liczb zestawu
  - **Wygrane liczby są pogrubione (bold)**
  - Etykieta z liczbą trafień (widoczna tylko dla ≥3 trafień):
    - 3 trafienia: "Wygrana 3 (trójka)"
    - 4 trafienia: "Wygrana 4 (czwórka)"
    - 5 trafień: "Wygrana 5 (piątka)"
    - 6 trafień: "Wygrana 6 (szóstka)" - główna wygrana
- Brak informacji o kategorii wygranej i kwotach w MVP

**UI/UX:**
- Przykład wizualizacji:
  ```
  Zestaw: "Moje liczby"
  Liczby: [3, **12**, 18, **25**, **31**, 44]
  Wygrana 3 (trójka) - Losowanie z 2025-10-28
  ```

#### F-VERIFY-003: Wizualny wskaźnik o nowych losowaniach
**Priorytet:** Must Have
**Opis:** System informuje użytkownika o nowych/niesprawdzonych losowaniach.

**Kryteria akceptacji:**
- [DO USTALENIA - KRYTYCZNE] Szczegóły implementacji (patrz sekcja 9.5):
  - Gdzie w UI pojawia się wskaźnik?
  - Co oznacza "niesprawdzone" - nowe losowania wprowadzone przez użytkownika czy ogólnie nowe losowania?
  - Jak wskaźnik znika - po weryfikacji czy ręcznym oznaczeniu?
- Przykładowe rozwiązania:
  - Badge przy module weryfikacji: "2 nowe losowania"
  - Banner na dashboardzie: "Są nowe wyniki do sprawdzenia!"
  - Kolorowe oznaczenie nowych losowań w liście

---

## 4. Wymagania Niefunkcjonalne

### 4.1 Wydajność
- **NFR-001:** Weryfikacja 100 zestawów względem 1 losowania ≤ 2 sekundy
- **NFR-002:** Czas odpowiedzi API dla standardowych operacji CRUD ≤ 500ms (95 percentyl)
- **NFR-003:** Czas ładowania strony głównej ≤ 3 sekundy (First Contentful Paint)
- **NFR-004:** Generowanie 9 zestawów systemowych ≤ 1 sekunda

### 4.2 Bezpieczeństwo
- **NFR-005:** Wszystkie hasła przechowywane jako hash (bcrypt, min. 10 rounds)
- **NFR-006:** Tokeny JWT z czasem wygaśnięcia (np. 24 godziny)
- **NFR-007:** HTTPS wymagane dla wszystkich połączeń w środowisku produkcyjnym
- **NFR-008:** Ochrona przed SQL Injection (Entity Framework Core z parametryzowanymi zapytaniami)
- **NFR-009:** Ochrona przed XSS (React domyślnie escapuje dane, dodatkowa walidacja inputów)
- **NFR-010:** Ochrona przed CSRF (tokeny CSRF lub SameSite cookies)
- **NFR-011:** Rate limiting dla endpointów logowania/rejestracji (np. 5 prób/minutę/IP)

### 4.3 Niezawodność
- **NFR-012:** System dostępny 99% czasu (dopuszczalny downtime: ~7 godz./miesiąc)
- **NFR-013:** Mniej niż 1% sesji kończy się niespodziewanym błędem krytycznym
- **NFR-014:** Automatyczne backupy bazy danych (np. 1x/dzień)
- **NFR-015:** Graceful degradation przy awarii zewnętrznych zależności

### 4.4 Skalowalność
- **NFR-016:** System wspiera min. 100 jednoczesnych użytkowników w MVP
- **NFR-017:** Architektura umożliwia skalowanie horyzontalne (stateless backend)
- **NFR-018:** Baza danych obsługuje min. 10,000 użytkowników × 100 zestawów = 1M rekordów

### 4.5 Użyteczność (Usability)
- **NFR-019:** Interfejs responsywny (działa na desktop, tablet, mobile browser)
- **NFR-020:** Wsparcie dla najnowszych wersji przeglądarek: Chrome, Firefox, Safari, Edge
- **NFR-021:** Komunikaty błędów w języku polskim, jasne i zrozumiałe
- **NFR-022:** Formularz z walidacją w czasie rzeczywistym (inline errors)

### 4.6 Testowalność
- **NFR-023:** Pokrycie testami jednostkowymi: min. 70% kodu biznesowego
- **NFR-024:** Min. 1 test E2E dla każdego krytycznego przepływu użytkownika
- **NFR-025:** Testy wydajnościowe dla algorytmu weryfikacji

### 4.7 Deployment i Operacje
- **NFR-026:** Wsparcie dla wdrożenia do chmury (Azure/AWS/GCP) lub Docker
- **NFR-027:** CI/CD pipeline na GitHub Actions z automatycznym uruchamianiem testów
- **NFR-028:** [DO USTALENIA] Konkretna platforma cloud i strategia deployment (patrz sekcja 9.11)
- **NFR-029:** [DO USTALENIA] Narzędzia do monitoringu i loggingu (patrz sekcja 9.12)

---

## 5. Historie Użytkownika (User Stories)

### US-01: Rejestracja i pierwsze logowanie
**Jako** nowy użytkownik
**Chcę** szybko założyć konto podając email i hasło
**Aby** natychmiast rozpocząć korzystanie z systemu

**Kryteria akceptacji:**
- Formularz rejestracji z polami: email, hasło, potwierdzenie hasła
- Walidacja formatu email i siły hasła
- Natychmiastowe zalogowanie po rejestracji (bez weryfikacji email)
- Otrzymanie tokenu JWT
- Przekierowanie na dashboard/stronę główną aplikacji

**Priorytet:** Must Have
**Story Points:** 5

---

### US-02: Dodawanie zestawu ręcznie
**Jako** użytkownik
**Chcę** ręcznie wprowadzić moje ulubione liczby LOTTO
**Aby** móc je później weryfikować

**Kryteria akceptacji:**
- Formularz z 6 polami numerycznymi (zakres 1-49)
- Walidacja unikalności liczb w czasie rzeczywistym
- Opcjonalne pole nazwy zestawu
- Zapisanie zestawu do mojego konta
- Komunikat sukcesu po zapisie

**Priorytet:** Must Have
**Story Points:** 3

---

### US-03: Generowanie losowego zestawu
**Jako** użytkownik
**Chcę** wygenerować losowy zestaw 6 liczb
**Aby** szybko uzyskać nową kombinację do gry

**Kryteria akceptacji:**
- Przycisk "Generuj losowy zestaw"
- Wyświetlenie podglądu wygenerowanych liczb
- Opcja ponownego generowania lub zapisu
- Walidacja limitu 100 zestawów

**Priorytet:** Must Have
**Story Points:** 3

---

### US-04: Generowanie zestawów systemowych
**Jako** użytkownik
**Chcę** wygenerować 9 zestawów pokrywających wszystkie liczby od 1 do 49
**Aby** mieć pełne pokrycie zakresu

**Kryteria akceptacji:**
- Przycisk "Generuj zestawy systemowe"
- Tooltip wyjaśniający funkcję i algorytm
- Algorytm pokrycia 1-49 + losowe dopełnienie (5 pozycji)
- Zapis 9 zestawów do konta użytkownika
- Walidacja: czy użytkownik ma miejsce na 9 zestawów (limit 100)

**Priorytet:** Must Have
**Story Points:** 8

---

### US-05: Wprowadzanie wyników losowania
**Jako** użytkownik
**Chcę** wprowadzić oficjalne wyniki losowania LOTTO
**Aby** móc zweryfikować swoje zestawy

**Kryteria akceptacji:**
- Formularz z 6 polami numerycznymi (zakres 1-49)
- Walidacja unikalności liczb
- Date picker do wyboru daty losowania
- Walidacja: data nie może być w przyszłości
- Zapisanie wyniku losowania

**Priorytet:** Must Have
**Story Points:** 5

---

### US-06: Weryfikacja wygranych
**Jako** użytkownik
**Chcę** sprawdzić czy któryś z moich zestawów wygrał w określonym przedziale czasu
**Aby** poznać swoje wygrane

**Kryteria akceptacji:**
- Date range picker do wyboru zakresu dat (od - do)
- Przycisk "Sprawdź wygrane"
- Wykonanie weryfikacji wszystkich moich zestawów względem losowań w zakresie
- Lista zbiorcza wyników z pogrubionymi wygranymi liczbami
- Etykiety "Wygrana X" dla zestawów z ≥3 trafieniami
- Czas wykonania ≤ 2s dla 100 zestawów

**Priorytet:** Must Have
**Story Points:** 13

---

### US-07: Edycja zestawu
**Jako** użytkownik
**Chcę** poprawić liczby w swoim zestawie
**Aby** skorygować ewentualne błędy lub zmienić strategię

**Kryteria akceptacji:**
- Przycisk "Edytuj" przy zestawie
- Formularz edycji z walidacją (jak przy dodawaniu)
- Zapisanie zmian z aktualizacją pola `updatedAt`
- [Wymaga decyzji] Obsługa historii weryfikacji

**Priorytet:** Must Have
**Story Points:** 5

---

### US-08: Przeglądanie zestawów
**Jako** użytkownik
**Chcę** zobaczyć wszystkie moje zapisane zestawy w jednym miejscu
**Aby** mieć przegląd moich numerów

**Kryteria akceptacji:**
- Lista wszystkich zestawów użytkownika
- Wyświetlenie: nazwa, 6 liczb, data utworzenia
- Licznik: "X / 100 zestawów"
- Paginacja lub scroll dla wielu zestawów
- Przyciski akcji: Edytuj, Usuń

**Priorytet:** Must Have
**Story Points:** 3

---

### US-09: Usuwanie zestawu
**Jako** użytkownik
**Chcę** usunąć zestaw, który nie jest mi już potrzebny
**Aby** utrzymać porządek w moich numerach

**Kryteria akceptacji:**
- Przycisk "Usuń" przy zestawie
- Modal potwierdzenia akcji
- Usunięcie zestawu z bazy danych
- Komunikat sukcesu

**Priorytet:** Must Have
**Story Points:** 2

---

### US-10: Wizualny wskaźnik nowych losowań
**Jako** użytkownik
**Chcę** widzieć w interfejsie informację o nowych losowaniach
**Aby** wiedzieć, kiedy powinienem sprawdzić swoje zestawy

**Kryteria akceptacji:**
- Wskaźnik (badge/banner) w UI informujący o nowych losowaniach
- [Wymaga decyzji] Szczegóły implementacji i logika "nowych" losowań

**Priorytet:** Must Have
**Story Points:** 5

---

## 6. Architektura Systemu

### 6.1 Stos technologiczny

**Backend:**
- **.NET 8 (C#)**
- **ASP.NET Core Web API**
- **Minimal APIs** - lekki styl definiowania endpointów
- **Entity Framework Core 8** - ORM dla dostępu do bazy danych
- **Vertical Slice Architecture** - organizacja kodu wg funkcjonalności

**Frontend:**
- **React 19** - biblioteka UI
- **TypeScript**
- **Tailwind CSS** - utility-first CSS framework
- **React Router** - routing po stronie klienta
- **Axios / Fetch API** - komunikacja z backendem
- **React Hook Form** - zarządzanie formularzami
- **Date picker library** (np. react-datepicker lub shadcn/ui)

**Baza danych:**
- **SQL Server 2022**

**Autoryzacja:**
- **JWT (JSON Web Tokens)** - stateless authentication

**Deployment:**
- **Cloud:** - deploy na serwerze webio.pl

**CI/CD:**
- **GitHub Actions** - automatyczne testy, build

**Monitoring i Logging:**
- nie w MVP

### 6.2 Vertical Slice Architecture

Organizacja backendu wg funkcjonalności (nie wg warstw technicznych):

```
src/
├── Features/
│   ├── Auth/
│   │   ├── Register.cs          // Endpoint + logika + validator + DTO
│   │   ├── Login.cs
│   │   └── Logout.cs
│   ├── Draws/
│   │   ├── AddDrawResult.cs
│   │   ├── ListDrawResults.cs
│   │   └── GetDrawResult.cs
│   ├── Tickets/
│   │   ├── ListTickets.cs
│   │   ├── AddTicket.cs
│   │   ├── EditTicket.cs
│   │   ├── DeleteTicket.cs
│   │   ├── GenerateRandomTicket.cs
│   │   ├── GenerateSystemTickets.cs
│   │   └── VerifyWinnings.cs
│   └── Shared/
│       ├── Database/
│       │   └── AppDbContext.cs
│       ├── Entities/
│       │   ├── User.cs
│       │   ├── Ticket.cs
│       │   ├── Draw.cs
│       │   └── TicketVerification.cs (opcjonalnie)
│       └── Auth/
│           ├── JwtService.cs
│           └── PasswordHasher.cs
├── Program.cs                    // Entry point, konfiguracja Minimal APIs
└── appsettings.json
```

**Zalety Vertical Slice Architecture:**
- Każda funkcjonalność jest samodzielna (łatwe dodawanie nowych features)
- Mniej coupling między features
- Łatwiejsze testowanie i rozwijanie

### 6.3 Schemat Bazy Danych

#### Tabela: Users
```sql
CREATE TABLE Users (
    Id INT PRIMARY KEY IDENTITY,
    Email NVARCHAR(255) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

CREATE INDEX IX_Users_Email ON Users(Email);
```

#### Tabela: Tickets
```sql
CREATE TABLE Tickets (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId INT NOT NULL,
    Number1 INT NOT NULL CHECK (Number1 BETWEEN 1 AND 49),
    Number2 INT NOT NULL CHECK (Number2 BETWEEN 1 AND 49),
    Number3 INT NOT NULL CHECK (Number3 BETWEEN 1 AND 49),
    Number4 INT NOT NULL CHECK (Number4 BETWEEN 1 AND 49),
    Number5 INT NOT NULL CHECK (Number5 BETWEEN 1 AND 49),
    Number6 INT NOT NULL CHECK (Number6 BETWEEN 1 AND 49),
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),    
    CONSTRAINT FK_Tickets_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

CREATE INDEX IX_Tickets_UserId ON Tickets(UserId);
```

#### Tabela: Draws
```sql
CREATE TABLE Draws (
    Id INT PRIMARY KEY IDENTITY,
    UserId int,
    DrawDate DATE NOT NULL UNIQUE,
    Number1 INT NOT NULL CHECK (Number1 BETWEEN 1 AND 49),
    Number2 INT NOT NULL CHECK (Number2 BETWEEN 1 AND 49),
    Number3 INT NOT NULL CHECK (Number3 BETWEEN 1 AND 49),
    Number4 INT NOT NULL CHECK (Number4 BETWEEN 1 AND 49),
    Number5 INT NOT NULL CHECK (Number5 BETWEEN 1 AND 49),
    Number6 INT NOT NULL CHECK (Number6 BETWEEN 1 AND 49),
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_Draws_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

CREATE INDEX IX_Draws_DrawDate ON Draws(DrawDate);
```

### 6.4 Kluczowe API Endpointy

#### Auth Module
```
POST   /api/auth/register       - Rejestracja nowego użytkownika
POST   /api/auth/login          - Logowanie
POST   /api/auth/logout         - Wylogowanie
```

#### Draws Module
```
GET    /api/draws               - Lista wyników losowań (z paginacją)
GET    /api/draws/{id}          - Szczegóły pojedynczego losowania
POST   /api/draws               - Dodanie nowego wyniku losowania
DELETE /api/draws/{id}          - [DO USTALENIA] Usunięcie wyniku losowania
```

#### Tickets Module
```
GET    /api/tickets             - Lista zestawów użytkownika (z paginacją)
GET    /api/tickets/{id}        - Szczegóły pojedynczego zestawu
POST   /api/tickets             - Dodanie nowego zestawu ręcznie
PUT    /api/tickets/{id}        - Edycja zestawu
DELETE /api/tickets/{id}        - Usunięcie zestawu
POST   /api/tickets/generate-random   - Generowanie losowego zestawu
POST   /api/tickets/generate-system   - Generowanie 9 zestawów systemowych
```

#### Verification Module
```
POST   /api/verification/check  - Weryfikacja zestawów w zakresie dat
```

**Wszystkie endpointy (poza `/auth/register` i `/auth/login`) wymagają JWT w header:**
```
Authorization: Bearer <token>
```

### 6.5 Frontend - Struktura komponentów

```
src/
├── components/
│   ├── Auth/
│   │   ├── LoginForm.tsx
│   │   ├── RegisterForm.tsx
│   │   └── AuthLayout.tsx
│   ├── Tickets/
│   │   ├── TicketList.tsx
│   │   ├── TicketItem.tsx
│   │   ├── TicketForm.tsx          // Dodawanie/edycja
│   │   ├── GeneratorPanel.tsx      // Przyciski: losowy/systemowy
│   │   └── NumberInput.tsx         // Komponent dla pola numerycznego
│   ├── Draws/
│   │   ├── DrawList.tsx
│   │   ├── DrawForm.tsx            // Formularz wprowadzania wyniku
│   │   └── DrawItem.tsx
│   ├── Verification/
│   │   ├── VerificationPanel.tsx   // Date range picker + przycisk
│   │   ├── VerificationResults.tsx // Lista wyników
│   │   └── ResultItem.tsx          // Pojedynczy wynik z podświetleniem
│   ├── Shared/
│   │   ├── Navbar.tsx
│   │   ├── Layout.tsx
│   │   ├── Button.tsx
│   │   ├── Modal.tsx
│   │   └── DatePicker.tsx
│   └── Dashboard/
│       └── Dashboard.tsx           // Główny widok po zalogowaniu
├── services/
│   ├── api.js                      // Axios instance + interceptory
│   ├── authService.js
│   ├── ticketService.js
│   ├── drawService.js
│   └── verificationService.js
├── hooks/
│   ├── useAuth.js
│   ├── useTickets.js
│   └── useDraws.js
├── context/
│   └── AuthContext.tsx
├── utils/
│   ├── validators.js
│   └── helpers.js
├── App.tsx
└── main.tsx
```

---

## 7. Kryteria Sukcesu i Metryki

### 7.1 Kryteria Sukcesu MVP

#### KS-01: Akceptacja generatora
**Metryka:** ≥75% wygenerowanych zestawów jest zapisywanych przez użytkowników

**Sposób pomiaru:**
- Event tracking w aplikacji:
  - `ticket_generated_random` → liczba wygenerowanych zestawów losowych
  - `ticket_saved_random` → liczba zapisanych zestawów losowych
  - `ticket_generated_system` → liczba uruchomień generatora systemowego
  - `ticket_saved_system` → liczba faktycznie zapisanych zestawów systemowych
- **Wzór:** (Liczba zapisanych / Liczba wygenerowanych) × 100%

**Cel:** Jeśli użytkownicy akceptują i zapisują większość wygenerowanych zestawów, oznacza to że generator spełnia ich oczekiwania.

#### KS-02: Szybkość weryfikacji
**Metryka:** Sprawdzenie 100 zestawów względem 1 losowania ≤ 2 sekundy

**Sposób pomiaru:**
- Performance logging w backendzie:
  - Timestamp start i end operacji weryfikacji
  - `executionTimeMs` zwracany w response API
- **Testy wydajnościowe:** Automatyczne testy z 100 zestawami i 1 losowaniem

**Cel:** Użytkownik nie czeka długo na wyniki, co poprawia UX i spełnia wymagania projektowe.

#### KS-03: Retencja użytkowników
**Metryka:** 40% użytkowników wraca ≥1 raz/tydzień (w tygodniach z losowaniami)

**Sposób pomiaru:**
- Analytics śledząc logowania i aktywność użytkowników (np. Google Analytics, Mixpanel)
- **Wzór:** (Liczba aktywnych użytkowników w tygodniu / Liczba zarejestrowanych użytkowników) × 100%
- **Definicja "aktywny":** Min. 1 logowanie w tygodniu

**Cel:** Użytkownicy regularnie wracają do aplikacji, co wskazuje na wartość produktu.

#### KS-04: Satysfakcja użytkowników
**Metryka:** Średnia ocena ≥4/5 w ankietach satysfakcji

**Sposób pomiaru:**
- In-app survey lub email survey po 2 tygodniach użytkowania
- Pytanie: "Jak oceniasz swoją satysfakcję z aplikacji LottoTM?" (skala 1-5)
- **Wzór:** Średnia arytmetyczna ocen

**Cel:** Użytkownicy są zadowoleni z funkcjonalności i doświadczenia użytkownika.

#### KS-05: Niezawodność systemu
**Metryka:** <1% sesji kończy się niespodziewanym błędem krytycznym

**Sposób pomiaru:**
- Error tracking (np. Sentry, Application Insights)
- **Wzór:** (Liczba sesji z błędem krytycznym / Całkowita liczba sesji) × 100%
- **Definicja "błąd krytyczny":** Błąd uniemożliwiający dalsze korzystanie z aplikacji (500, crash, nieobsłużony wyjątek)

**Cel:** System jest stabilny i niezawodny, użytkownicy rzadko doświadczają problemów.

### 7.2 Dodatkowe Metryki Operacyjne

#### MO-01: Limit zestawów
**Metryka:** System umożliwia przechowywanie do 100 zestawów na użytkownika

**Sposób weryfikacji:**
- Walidacja biznesowa przy dodawaniu zestawu (backend)
- Testy jednostkowe weryfikujące blokadę po 100 zestawach

#### MO-02: Dostępność UI
**Metryka:** Wszystkie kluczowe komponenty działają poprawnie

**Sposób weryfikacji:**
- Testy E2E pokrywające wszystkie User Stories
- Manual QA przed release

#### MO-03: Pokrycie testami
**Metryka:** ≥70% pokrycia kodu testami jednostkowymi dla logiki biznesowej

**Sposób pomiaru:**
- Code coverage report z narzędzi testowych (.NET: Coverlet, JS: Jest coverage)

---

## 8. Strategia Testowania

### 8.1 Testy Jednostkowe (Unit Tests)

**Priorytet:** Logika biznesowa, algorytmy, walidacje

**Kluczowe obszary:**
1. **Algorytm weryfikacji wygranych:**
   - Test: 6 trafień (główna wygrana)
   - Test: 5 trafień
   - Test: 4 trafienia
   - Test: 3 trafienia
   - Test: 0 trafień
   - Test: Weryfikacja poprawności algorytmu (liczby wspólne)

2. **Algorytm generatora systemowego:**
   - Test: Pokrycie wszystkich liczb 1-49 w 9 zestawach
   - Test: Każda liczba pojawia się min. 1 raz
   - Test: Losowe dopełnienie 5 pozycji
   - Test: Każdy zestaw ma dokładnie 6 unikalnych liczb

3. **Walidacja danych wejściowych:**
   - Test: Walidacja zakresu liczb (1-49)
   - Test: Walidacja unikalności liczb w zestawie
   - Test: Walidacja formatu email
   - Test: Walidacja siły hasła

4. **Logika limitu zestawów:**
   - Test: Blokada dodawania po osiągnięciu 100 zestawów
   - Test: Możliwość dodania po usunięciu zestawów

5. **Serwisy autoryzacji:**
   - Test: Hashowanie hasła
   - Test: Weryfikacja hasła
   - Test: Generowanie tokenu JWT
   - Test: Walidacja tokenu JWT

**Narzędzia:**
- Backend: xUnit  (C# .NET)

**Target:** ≥70% pokrycia kodu biznesowego

## 9. Priorytetyzacja Funkcjonalności (MoSCoW)

### Must Have (MVP - Release 1.0)
- [x] Rejestracja i logowanie (JWT)
- [x] Dodawanie zestawu ręcznie
- [x] Edycja zestawu
- [x] Usuwanie zestawu
- [x] Przeglądanie zestawów
- [x] Wprowadzanie wyników losowania
- [x] Weryfikacja wygranych z date range picker
- [x] Generator losowy (pojedynczy zestaw)
- [x] Generator systemowy (9 zestawów)
- [x] Limit 100 zestawów/użytkownik
- [x] Wizualny wskaźnik w UI o nowych losowaniach
- [x] CI/CD na GitHub Actions (podstawowe workflow)
- [x] Min. 1 test jednostkowy

### Should Have (Post-MVP - Release 1.1-1.2)
- [ ] Weryfikacja email przy rejestracji
- [ ] Powiadomienia email o nowych losowaniach
- [ ] Eksport zestawów do CSV
- [ ] Kategorie wygranych i przybliżone kwoty
- [ ] Statystyki użytkownika (najczęstsze liczby, historia trafień)
- [ ] Reset hasła (Forgot Password)
- [ ] Paginacja/infinite scroll dla dużych list

### Could Have (Przyszłość - Release 2.0+)
- [ ] Wsparcie dla innych gier (MINI LOTTO, Multi Multi, EuroJackpot)
- [ ] Automatyczne pobieranie wyników z API (np. Totalizator Sportowy)
- [ ] Analiza AI/ML (rekomendacje liczb na podstawie historii)
- [ ] Funkcje społecznościowe (udostępnianie zestawów)
- [ ] Import zestawów z PDF
- [ ] Progressive Web App (PWA) - offline mode
- [ ] Dark mode
- [ ] Wielojęzyczność (obecnie tylko polski)

### Won't Have (Poza zakresem)
- [ ] Dedykowana aplikacja mobilna (natywna iOS/Android)
- [ ] Integracja z płatnościami (zakup kuponów)
- [ ] System zakładów grupowych (syndykaty)
- [ ] Live streaming losowań

---

## 10. Timeline i Kamienie Milowe

### Faza 0: Przygotowanie (2 dni)
- Wyjaśnienie nierozwiązanych kwestii
- Finalizacja PRD
- Setup środowiska deweloperskiego
- Inicjalizacja repozytorium i struktura projektu

### Faza 1: Fundament (3 dni)
- Setup bazy danych (SQL Server, migracje EF Core)
- Implementacja modułu Auth (rejestracja, logowanie, JWT)
- Podstawowy frontend (routing, layout, formularz logowania)
- Setup CI/CD (GitHub Actions - podstawowe workflow)

**Milestone M1:** Użytkownik może się zarejestrować i zalogować

### Faza 2: Zarządzanie zestawami (3 dni)
- CRUD dla zestawów (dodaj, edytuj, usuń, lista)
- Generator losowy
- Generator systemowy
- Walidacja limitu 100 zestawów
- Frontend dla zarządzania zestawami

**Milestone M2:** Użytkownik może zarządzać zestawami liczb

### Faza 3: Losowania (3 dni)
- CRUD dla wyników losowań (dodaj, lista)
- Walidacja danych wejściowych (6 unikalnych liczb 1-49)
- Frontend dla wprowadzania wyników

**Milestone M3:** Użytkownik może wprowadzać wyniki losowań

### Faza 4: Weryfikacja (3 dni)
- Algorytm weryfikacji wygranych
- API endpoint dla weryfikacji
- Frontend z date range picker i prezentacją wyników
- Optymalizacja wydajności (target: 100 zestawów w <2s)
- Wizualny wskaźnik o nowych losowaniach

**Milestone M4:** Użytkownik może weryfikować wygrane (core functionality)

### Faza 5: Testy i QA (1 dzień)
- Testy jednostkowe (algorytmy, walidacje)

**Milestone M5:** System przetestowany i gotowy do release

### Faza 6: Deployment (1 dzień)
- Setup infrastruktury cloud (WEBIO.PL)
- Deployment backendu i frontendu
- Konfiguracja bazy danych produkcyjnej
- Setup monitoringu i error trackingu
- Smoke tests w środowisku produkcyjnym

**Milestone M6 (MVP RELEASE):** System dostępny publicznie dla pierwszych użytkowników

### Faza 7: Post-Release (ciągłe)
- Monitoring błędów i wydajności
- Zbieranie feedbacku użytkowników
- Analiza metryk sukcesu (sekcja 7.1)
- Planowanie kolejnych iteracji (Should Have features)

---

**Sumarycznie: 2 tygodnie do MVP Release**

---

## 11. Ryzyka i Mitigacje

### R-01: Wolna wydajność weryfikacji
**Ryzyko:** Algorytm weryfikacji nie spełnia wymagania ≤2s dla 100 zestawów

**Prawdopodobieństwo:** Średnie
**Wpływ:** Wysoki (krytyczne wymaganie MVP)

**Mitigacja:**
- Wczesne testy wydajnościowe (Faza 4)
- Optymalizacja zapytań SQL (batch queries, indeksy)
- Cachowanie wyników weryfikacji (tabela TicketVerifications)
- Przeniesienie obliczeń do bazy danych (stored procedures / computed columns)
- Plan B: Asynchroniczna weryfikacja z progress bar

---

### R-02: Problemy z security (izolacja danych)
**Ryzyko:** Luka w zabezpieczeniach pozwalająca na dostęp do cudzych danych

**Prawdopodobieństwo:** Niskie
**Wpływ:** Krytyczny (GDPR, utrata zaufania użytkowników)

**Mitigacja:**
- Code review z fokusem na security
- Testy jednostkowe weryfikujące izolację danych
- Penetration testing przed release
- Rate limiting na endpointach
- Regular security audits

---

### R-03: Scope creep
**Ryzyko:** Rozszerzanie zakresu MVP poza zdefiniowane funkcjonalności

**Prawdopodobieństwo:** Średnie
**Wpływ:** Wysoki (opóźnienie release, wzrost kosztów)

**Mitigacja:**
- Jasna definicja MVP w PRD
- Priorytetyzacja MoSCoW (sekcja 10)
- Regularne review scope podczas sprintów
- Stakeholder buy-in na zakres MVP

---

### R-04: Brak adopcji przez użytkowników
**Ryzyko:** Niska retencja, użytkownicy nie wracają do aplikacji

**Prawdopodobieństwo:** Średnie
**Wpływ:** Wysoki (niepowodzenie produktu)

**Mitigacja:**
- Wczesne testy z pierwszymi użytkownikami (beta program)
- Zbieranie feedbacku i szybkie iteracje
- Analiza metryk sukcesu (sekcja 7.1)
- UX review i optymalizacja user flows
- Marketing i onboarding (komunikacja wartości produktu)

---

### R-05: Problemy z infrastructure / deployment
**Ryzyko:** Opóźnienia lub problemy z wdrożeniem do produkcji

**Prawdopodobieństwo:** Niskie-średnie
**Wpływ:** Średni (opóźnienie release)

**Mitigacja:**
- Wczesny setup CI/CD (Faza 1)
- Testowanie deployment na staging
- Dokumentacja procesu deployment
- Backup plan (rollback strategy)
- Współpraca z DevOps/infrastrukturą

---

### R-06: Edycja zestawów - niespójna logika biznesowa
**Ryzyko:** Nierozwiązane kwestie wokół edycji zestawów (sekcja 9.1) prowadzą do błędów lub złego UX

**Prawdopodobieństwo:** Średnie
**Wpływ:** Średni (konfuzja użytkowników, bugi)

**Mitigacja:**
- Szybka decyzja w kwestii edycji zestawów (przed Fazą 2)
- Jasna komunikacja w UI (ostrzeżenia, tooltips)
- Testy E2E pokrywające edge cases
- Możliwość zmiany logiki w kolejnej iteracji na podstawie feedbacku

---

## 12. Zależności i Założenia

### Zależności zewnętrzne:
- **Brak automatycznego pobierania wyników losowań** - użytkownik musi wprowadzać ręcznie (MVP)
- **Brak integracji z zewnętrznymi API** w MVP
- **Hosting/infrastruktura** - wymaga dostępu do środowiska cloud lub serwera

### Założenia:
- Użytkownicy mają podstawową znajomość obsługi aplikacji webowych
- Użytkownicy mają dostęp do oficjalnych wyników losowań LOTTO (strona Totalizatora Sportowego)
- System będzie używany głównie na desktop/laptop (responsywność na mobile to bonus, nie Must Have)
- Użytkownicy akceptują rejestrację bez weryfikacji email (MVP)
- Losowania LOTTO odbywają się regularnie (np. 3x w tygodniu) - użytkownicy mają motywację do powrotu

---

## 13. Załączniki

### Załącznik A: Przykładowy Request/Response API

**POST /api/verification/check**

Request:
```json
{
  "dateFrom": "2025-10-01",
  "dateTo": "2025-10-30"
}
```

Response:
```json
{
  "results": [
    {
      "ticketId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
      "ticketName": "Ulubione liczby",
      "numbers": [3, 12, 25, 31, 42, 48],
      "draws": [
        {
          "drawId": "a1b2c3d4-e5f6-4a5b-9c8d-1e2f3a4b5c6d",
          "drawDate": "2025-10-28",
          "drawNumbers": [12, 18, 25, 31, 40, 49],
          "matchCount": 3,
          "matchedNumbers": [12, 25, 31]
        }
      ]
    },
    {
      "ticketId": "b2c3d4e5-f6a7-4b5c-9d8e-2f3a4b5c6d7e",
      "ticketName": "Zestaw #2",
      "numbers": [1, 2, 3, 4, 5, 6],
      "draws": []
    }
  ],
  "totalTickets": 42,
  "totalDraws": 8,
  "executionTimeMs": 1234
}
```

### Załącznik B: Przykładowy Flow Diagram

```
[Użytkownik] → [Rejestracja] → [Login] → [Dashboard]
                                              ↓
                    ┌─────────────────────────┼─────────────────────────┐
                    ↓                         ↓                         ↓
            [Zarządzanie                [Wprowadź              [Weryfikuj
             Zestawami]                  Wyniki]                Wygrane]
                    ↓                         ↓                         ↓
        ┌──────────┼──────────┐              ↓                         ↓
        ↓          ↓          ↓              ↓                         ↓
    [Dodaj]   [Generator]  [Edytuj]    [Lista losowań]     [Date Range Picker]
                   ↓                                                    ↓
          ┌────────┼────────┐                              [Lista wyników
          ↓                 ↓                               z podświetleniem]
    [Losowy]         [Systemowy]
    [1 zestaw]       [9 zestawów]
```

### Załącznik C: Mockup UI (Opis tekstowy)

**Ekran: Dashboard**
- Navbar: Logo | Zestawy | Losowania | Weryfikacja | Wyloguj
- Sekcja powitalana: "Witaj, user@example.com! Masz 42/100 zestawów."
- Karta: "Nowe losowania (2)" → Badge czerwony
- Przyciski szybkiego dostępu:
  - "Dodaj zestaw"
  - "Wprowadź wynik losowania"
  - "Sprawdź wygrane"

**Ekran: Lista zestawów**
- Nagłówek: "Moje zestawy (42/100)"
- Przyciski: [+ Dodaj ręcznie] [🎲 Generuj losowy] [🔢 Generuj systemowy]
- Lista zestawów:
  - Zestaw #1: "Ulubione liczby" | [3, 12, 25, 31, 42, 48] | Utworzono: 2025-10-15 | [Edytuj] [Usuń]
  - Zestaw #2: "Urodziny" | [5, 14, 23, 29, 37, 41] | Utworzono: 2025-10-14 | [Edytuj] [Usuń]

**Ekran: Formularz dodawania zestawu**
- Pole: "Nazwa zestawu (opcjonalnie)"
- 6 pól numerycznych:
  - Liczba 1: [____] (placeholder: "1-49")
  - ...
  - Liczba 6: [____]
- Komunikaty błędów pod polami (czerwony tekst)
- Przycisk: [Zapisz zestaw]

**Ekran: Wyniki weryfikacji**
- Nagłówek: "Wyniki weryfikacji (01.10.2025 - 30.10.2025)"
- Lista wyników:
  - Zestaw: "Ulubione liczby"
    - Liczby: [3, **12**, 18, **25**, **31**, 44]
    - Badge zielony: "Wygrana 3 (trójka)"
    - Losowanie: 28.10.2025
  - Zestaw: "Zestaw #2"
    - Liczby: [1, 2, 3, 4, 5, 6]
    - Brak wygranej (0 trafień)

---

## 16. Aprobacja Dokumentu

**Dokument wymaga aprobaty następujących osób przed rozpoczęciem implementacji:**

| Rola | Imię i Nazwisko | Data | Podpis |
|------|-----------------|------|--------|
| Product Owner | [DO UZUPEŁNIENIA] | | |
| Tech Lead | [DO UZUPEŁNIENIA] | | |
| UX Designer | [DO UZUPEŁNIENIA] | | |
| QA Lead | [DO UZUPEŁNIENIA] | | |

**Wersja dokumentu:** 1.0
**Data ostatniej aktualizacji:** 2025-10-30
**Następny review:** Po wyjaśnieniu nierozwiązanych kwestii (sekcja 9)

---

**Koniec dokumentu PRD dla LottoTM MVP**
