# Podsumowanie Rozmowy - Planowanie PRD dla LottoTM MVP

## Decyzje Podjęte Podczas Konsultacji

1. **Zakres gier losowych**: W wersji MVP system będzie wspierał wyłącznie klasyczne LOTTO (6 z 49), bez innych wariantów jak MINI LOTTO czy EuroJackpot.

2. **Proces rejestracji**: Użytkownik rejestruje się podając email (login) i hasło. System nie wymaga weryfikacji adresu email - użytkownik może natychmiast korzystać z aplikacji po rejestracji.

3. **Interfejs wprowadzania wyników losowania**: Formularz będzie zawierał 6 oddzielnych pól numerycznych z zakresem 1-49, walidacją w czasie rzeczywistym oraz date picker do wyboru daty losowania.

4. **Prezentacja wyników weryfikacji**: System wyświetla zestaw liczb, liczbę trafień oraz wizualnie podświetla (pogrubienie) wygrane liczby. Brak informacji o kategorii wygranej i potencjalnej kwocie w MVP.

5. **Algorytm generatora "systemowego"**: System losowo rozdysponuje wszystkie liczby od 1 do 49 w 9 zestawach (każda liczba pojawi się minimum raz na losowej pozycji). Pozostałe 5 pozycji (54 miejsca - 49 liczb) zostanie losowo dopełnionych.

6. **Prezentacja wielu wygranych**: Jedna lista zbiorcza zawierająca wszystkie zestawy użytkownika. Wygrane liczby będą pogrubione, a obok zestawu wyświetli się etykieta typu "Wygrana 3 (trójka)" dla 3 trafień, "Wygrana 4 (czwórka)" dla 4 trafień itd.

7. **Limit zestawów**: Maksymalnie 100 zestawów liczb na jednego użytkownika w systemie MVP.

8. **Mechanizm weryfikacji**: Elastyczny komponent date range picker umożliwiający użytkownikowi wybór zakresu dat, dla których chce zweryfikować swoje zestawy.

9. **System powiadomień**: W MVP brak powiadomień email/push. System oferuje wyłącznie wizualny wskaźnik w interfejsie aplikacji.

10. **Edycja zestawów**: Użytkownik ma możliwość edycji każdego swojego zestawu liczb (nie tylko usuwanie i dodawanie nowego).

---

## Dopasowane Rekomendacje

1. **Skupienie na jednym wariancie gry** - Decyzja o ograniczeniu MVP do LOTTO (6 z 49) uprości logikę biznesową, przyspieszy time-to-market i pozwoli na szybszą walidację produktu przez pierwszych użytkowników. Dodatkowe warianty można wprowadzić w kolejnych iteracjach.

2. **Minimalistyczny proces onboardingu** - Rejestracja bez wymaganej weryfikacji email zmniejsza friction w procesie wdrażania użytkowników i pozwala na natychmiastowy dostęp do funkcjonalności systemu, co jest kluczowe dla retencji.

3. **Walidacja danych wejściowych** - Komponent z oddzielnymi polami numerycznymi i walidacją real-time zminimalizuje ryzyko błędów wprowadzania danych i znacząco poprawi UX, zapewniając wysoką jakość danych w systemie.

4. **Uproszczona prezentacja wyników** - Skupienie się na podstawowych informacjach (zestaw + trafienia + podświetlenie) bez kategorii wygranej i kwot upraszcza MVP i eliminuje potrzebę integracji z zewnętrznymi źródłami danych o wysokości nagród.

5. **Edukacyjny charakter generatora systemowego** - Algorytm pełnego pokrycia 1-49 w 9 zestawach powinien być wyraźnie opisany w UI (tooltip/FAQ), aby użytkownicy rozumieli, że nie zwiększa to matematycznie szans na wygraną, ale zapewnia pełne pokrycie zakresu.

6. **Scentralizowana prezentacja wygranych** - Widok zbiorczy wszystkich zestawów z wizualnym oznaczeniem wygranych pozwala użytkownikowi szybko ocenić swój sukces w danym losowaniu bez konieczności nawigacji między wieloma ekranami.

7. **Ograniczenie techniczne dla wydajności** - Limit 100 zestawów na użytkownika zabezpiecza system przed nadużyciami, zapewnia zgodność z kryterium wydajności (weryfikacja 100 zestawów w <2s) i pozostawia przestrzeń na przyszłe plany subskrypcyjne.

8. **Elastyczność weryfikacji** - Date range picker daje użytkownikom pełną kontrolę nad zakresem weryfikacji, umożliwiając zarówno sprawdzanie pojedynczych losowań, jak i weryfikację historyczną bez wielokrotnego powtarzania procesu.

9. **Uproszczona architektura w MVP** - Brak systemu powiadomień email/push w pierwszej wersji znacząco upraszcza architekturę techniczną. Wizualny wskaźnik w aplikacji zapewnia podstawową informację zwrotną, a pełny system notyfikacji można zbudować po zebraniu feedbacku użytkowników.

10. **Funkcja edycji z uwzględnieniem przyszłych wymagań** - Umożliwienie edycji zestawów poprawia UX, ale wymaga przemyślanej implementacji z uwzględnieniem integralności danych historycznych (np. czy zestaw był już weryfikowany dla konkretnych losowań). Zaleca się implementację z odpowiednią logiką audytu/wersjonowania.

---

## Szczegółowe Podsumowanie Planowania PRD

### Główne Wymagania Funkcjonalne

**Moduł Autoryzacji i Konta Użytkownika:**
- Rejestracja z wykorzystaniem email jako login i hasła
- Natychmiastowy dostęp po rejestracji (bez weryfikacji email)
- Logowanie i wylogowywanie z wykorzystaniem JWT
- Pełna izolacja danych między użytkownikami

**Moduł Zarządzania Losowaniami:**
- Ręczne wprowadzanie wyników oficjalnych losowań LOTTO (6 z 49)
- Formularz z 6 oddzielnymi polami numerycznymi (zakres 1-49)
- Walidacja w czasie rzeczywistym (unikalne liczby, prawidłowy zakres)
- Date picker do wyboru daty losowania
- Przechowywanie historii wyników losowań

**Moduł Zarządzania Zestawami (Kuponami):**
- Przeglądanie wszystkich zapisanych zestawów użytkownika (limit: 100 zestawów)
- Dodawanie zestawu ręcznie (6 liczb z zakresu 1-49)
- Generator pojedynczego losowego zestawu
- Generator "systemowy" - 9 zestawów pokrywających wszystkie liczby 1-49
- Edycja istniejących zestawów
- Usuwanie zestawów

**Moduł Weryfikacji Wygranych:**
- Sprawdzanie zestawów użytkownika względem wyników losowań
- Elastyczny wybór zakresu dat za pomocą date range picker
- Prezentacja wyników w formie listy zbiorczej
- Wizualne oznaczenie (pogrubienie) wygranych liczb
- Wyświetlenie etykiety z liczbą trafień ("Wygrana 3 (trójka)", "Wygrana 4 (czwórka)" itd.)
- Wizualny wskaźnik w interfejsie o nowych/niesprawdzonych losowaniach

### Kluczowe Historie Użytkownika

**US-01: Rejestracja i pierwsze logowanie**
- Jako nowy użytkownik chcę szybko założyć konto podając email i hasło, aby natychmiast rozpocząć korzystanie z systemu
- Kryteria akceptacji: formularz rejestracji, walidacja email i hasła, natychmiastowe zalogowanie po rejestracji

**US-02: Dodawanie zestawu ręcznie**
- Jako użytkownik chcę ręcznie wprowadzić moje ulubione liczby LOTTO, aby móc je później weryfikować
- Kryteria akceptacji: formularz z 6 polami, walidacja zakresu 1-49, walidacja unikalności, zapisanie zestawu

**US-03: Generowanie losowego zestawu**
- Jako użytkownik chcę wygenerować losowy zestaw 6 liczb, aby szybko uzyskać nową kombinację do gry
- Kryteria akceptacji: przycisk generowania, losowanie 6 unikalnych liczb 1-49, opcja zapisu lub ponownego losowania

**US-04: Generowanie zestawów systemowych**
- Jako użytkownik chcę wygenerować 9 zestawów pokrywających wszystkie liczby od 1 do 49, aby mieć pełne pokrycie zakresu
- Kryteria akceptacji: przycisk generowania systemowego, algorytm pokrycia 1-49 + losowe dopełnienie, zapis 9 zestawów, tooltip wyjaśniający funkcję

**US-05: Wprowadzanie wyników losowania**
- Jako użytkownik chcę wprowadzić oficjalne wyniki losowania LOTTO, aby móc zweryfikować swoje zestawy
- Kryteria akceptacji: 6 pól numerycznych z walidacją, date picker, zapisanie wyniku losowania

**US-06: Weryfikacja wygranych**
- Jako użytkownik chcę sprawdzić czy któryś z moich zestawów wygrał w określonym przedziale czasu, aby poznać swoje wygrane
- Kryteria akceptacji: date range picker, wykonanie weryfikacji wszystkich zestawów, lista zbiorcza z pogrubionymi wygranymi, etykiety "Wygrana X"

**US-07: Edycja zestawu**
- Jako użytkownik chcę poprawić liczby w swoim zestawie, aby skorygować ewentualne błędy lub zmienić strategię
- Kryteria akceptacji: przycisk edycji przy zestawie, formularz edycji z walidacją, zapisanie zmian z zachowaniem historii weryfikacji

**US-08: Przeglądanie zestawów**
- Jako użytkownik chcę zobaczyć wszystkie moje zapisane zestawy w jednym miejscu, aby mieć przegląd moich numerów
- Kryteria akceptacji: lista wszystkich zestawów, paginacja/scroll dla wielu zestawów, licznik zapisanych zestawów

**US-09: Usuwanie zestawu**
- Jako użytkownik chcę usunąć zestaw, który nie jest mi już potrzebny, aby utrzymać porządek w moich numerach
- Kryteria akceptacji: przycisk usuń, potwierdzenie akcji, usunięcie z bazy danych

### Kryteria Sukcesu i Metryki

**Kryteria Sukcesu MVP (zgodnie z dokumentem projektu):**

1. **Akceptacja generatora**: ≥75% wygenerowanych zestawów jest akceptowanych i zapisywanych przez użytkowników
   - Metryka: (Liczba zapisanych wygenerowanych zestawów / Liczba wygenerowanych zestawów) × 100%
   - Sposób pomiaru: Tracking eventów w aplikacji (generate_ticket, save_generated_ticket)

2. **Szybkość weryfikacji**: Sprawdzenie 100 zestawów względem 1 losowania ≤2 sekundy
   - Metryka: Czas wykonania operacji weryfikacji
   - Sposób pomiaru: Performance logging w backendzie, testy wydajnościowe

3. **Retencja użytkowników**: 40% użytkowników wraca ≥1 raz/tydzień (w tygodniach z losowaniami)
   - Metryka: (Liczba aktywnych użytkowników w tygodniu / Liczba zarejestrowanych użytkowników) × 100%
   - Sposób pomiaru: Analytics z śledząc logowania i aktywność użytkowników

4. **Satysfakcja użytkowników**: Średnia ocena ≥4/5 w ankietach satysfakcji
   - Metryka: Średnia arytmetyczna ocen z ankiet
   - Sposób pomiaru: In-app survey lub email survey po 2 tygodniach użytkowania

5. **Niezawodność systemu**: <1% sesji kończy się niespodziewanym błędem krytycznym
   - Metryka: (Liczba sesji z błędem krytycznym / Całkowita liczba sesji) × 100%
   - Sposób pomiaru: Error tracking (np. Sentry), monitoring aplikacji

**Dodatkowe Metryki Operacyjne:**

6. **Limit zestawów**: System umożliwia przechowywanie do 100 zestawów na użytkownika
   - Sposób weryfikacji: Walidacja biznesowa przy dodawaniu zestawu, testy jednostkowe

7. **Dostępność UI**: Wszystkie kluczowe komponenty (pola numeryczne, date picker, date range picker) działają poprawnie
   - Sposób weryfikacji: Testy E2E, manual QA

### Ograniczenia Projektowe

**Funkcjonalne:**
- Brak wsparcia dla innych gier losowych poza LOTTO (6 z 49)
- Brak weryfikacji adresu email w procesie rejestracji
- Brak informacji o kategoriach wygranych i kwotach nagród
- Brak systemu powiadomień email/push
- Limit 100 zestawów na użytkownika
- Brak importu/eksportu zestawów z plików (CSV, PDF)
- Brak funkcji społecznościowych

**Techniczne:**
- Backend: .NET 8, ASP.NET Core Web API, Minimal APIs, Vertical Slice Architecture, EF Core
- Frontend: React 19, Tailwind CSS
- Baza danych: SQL Server 2022
- Autoryzacja: JWT
- Wymagana wydajność: weryfikacja 100 zestawów w <2s

**Poza zakresem MVP:**
- Automatyczne pobieranie wyników losowań z zewnętrznych API
- Dedykowana aplikacja mobilna (tylko web app)
- Analiza AI/ML (wzorce w wyborze liczb, rekomendacje)
- Funkcje społecznościowe (dzielenie się zestawami)

### Architektura i Struktura

**Backend - Vertical Slice Architecture:**
```
Features/
  ├── Auth/
  │   ├── Register.cs
  │   ├── Login.cs
  │   └── Logout.cs
  ├── Draws/
  │   ├── AddDrawResult.cs
  │   ├── ListDrawResults.cs
  │   └── GetDrawResult.cs
  ├── Tickets/
  │   ├── ListTickets.cs
  │   ├── AddTicket.cs
  │   ├── EditTicket.cs
  │   ├── DeleteTicket.cs
  │   ├── GenerateRandomTicket.cs
  │   ├── GenerateSystemTickets.cs
  │   └── VerifyWinnings.cs
```

**Schemat bazy danych (konceptualny):**
- `Users` - dane użytkowników (Id, Email, PasswordHash, CreatedAt)
- `Tickets` - zestawy użytkowników (Id, UserId, Numbers (JSON/6 kolumn), Name, CreatedAt, UpdatedAt)
- `Draws` - wyniki losowań (Id, DrawDate, Numbers (JSON/6 kolumn), CreatedAt)
- `TicketVerifications` - historia weryfikacji (Id, TicketId, DrawId, MatchCount, VerifiedAt) - opcjonalnie

**Frontend - Kluczowe komponenty:**
- `AuthForm` - rejestracja/logowanie
- `TicketList` - lista zestawów użytkownika
- `TicketForm` - dodawanie/edycja zestawu (6 pól numerycznych)
- `DrawForm` - wprowadzanie wyników (6 pól numerycznych + date picker)
- `DrawList` - lista wyników losowań
- `VerificationPanel` - weryfikacja z date range picker
- `VerificationResults` - lista wyników z podświetleniem wygranych
- `GeneratorPanel` - przyciski: losowy/systemowy

### Strategia Testowania

**Testy jednostkowe:**
- Algorytm weryfikacji wygranych (dopasowanie liczb)
- Algorytm generatora systemowego (pokrycie 1-49)
- Logika biznesowa walidacji (unikalne liczby, zakres 1-49)
- Ograniczenie limitu zestawów (max 100)

**Testy E2E:**
- Przepływ: rejestracja → dodanie zestawu → wprowadzenie wyniku → weryfikacja
- Przepływ: generowanie losowego zestawu → zapis
- Przepływ: generowanie zestawów systemowych → zapis 9 zestawów
- Przepływ: edycja zestawu → weryfikacja poprawności zmian
- Przepływ: date range picker → weryfikacja dla zakresu dat

**Testy wydajnościowe:**
- Weryfikacja 100 zestawów względem 1 losowania (target: <2s)
- Generowanie zestawów systemowych (target: <1s)

**Testy bezpieczeństwa:**
- Izolacja danych między użytkownikami
- Walidacja JWT
- SQL injection prevention (EF Core)
- XSS prevention (React)

### Priorytetyzacja Funkcjonalności (MoSCoW)

**Must Have (MVP):**
- Rejestracja i logowanie (JWT)
- Dodawanie zestawu ręcznie
- Edycja zestawu
- Usuwanie zestawu
- Przeglądanie zestawów
- Wprowadzanie wyników losowania
- Weryfikacja wygranych z date range picker
- Generator losowy (pojedynczy zestaw)
- Generator systemowy (9 zestawów)
- Limit 100 zestawów/użytkownik
- Wizualny wskaźnik w UI

**Should Have (po MVP):**
- Weryfikacja email przy rejestracji
- Powiadomienia email o nowych losowaniach
- Eksport zestawów do CSV
- Kategorie wygranych i przybliżone kwoty
- Statystyki użytkownika (najczęstsze liczby, historia trafień)

**Could Have (przyszłość):**
- Wsparcie dla innych gier (MINI LOTTO, Multi Multi, EuroJackpot)
- Automatyczne pobieranie wyników z API
- Analiza AI/ML (rekomendacje liczb)
- Funkcje społecznościowe
- Import zestawów z PDF

**Won't Have (poza zakresem):**
- Dedykowana aplikacja mobilna (natywna)
- Integracja z płatnościami
- System zakładów grupowych

---

## Nierozwiązane Kwestie

### Wymagające Wyjaśnienia przed Implementacją

1. **Edycja zestawów a historia weryfikacji:**
   - Jak system ma obsługiwać edycję zestawu, który był już weryfikowany dla wcześniejszych losowań?
   - Czy zachowujemy historię weryfikacji dla starej wersji zestawu, czy nadpisujemy?
   - Czy potrzebujemy audytu zmian (kto, kiedy, co zmienił)?
   - **Rekomendacja:** Rozważyć implementację wersjonowania lub zakazanie edycji zestawów, które były już weryfikowane.

2. **Nazewnictwo zestawów:**
   - Czy użytkownik może nazwać swoje zestawy (np. "Ulubione liczby", "Urodziny rodziny")?
   - Czy nazwa jest obowiązkowa, czy opcjonalna?
   - Jeśli opcjonalna - jak system generuje domyślną nazwę (np. "Zestaw #1", "Zestaw #2")?

3. **Duplikaty zestawów:**
   - Czy użytkownik może zapisać dwa identyczne zestawy liczb (np. te same 6 liczb)?
   - Czy system ma blokować duplikaty, ostrzegać użytkownika, czy pozwolić na zapisanie?

4. **Generator systemowy - prezentacja i UX:**
   - Czy tooltip/wyjaśnienie funkcji systemowej jest wystarczające, czy potrzebny jest osobny ekran edukacyjny/FAQ?
   - Czy użytkownik widzi podgląd 9 zestawów przed zapisem i może je odrzucić/regenerować?
   - Czy możliwe jest zapisanie tylko wybranych zestawów z wygenerowanych 9?

5. **Wizualny wskaźnik - szczegóły implementacji:**
   - Gdzie dokładnie w interfejsie pojawia się wskaźnik (pasek nawigacji, dashboard, przy module weryfikacji)?
   - Co dokładnie wskaźnik sygnalizuje: nowe losowania dodane przez użytkownika, czy niesprawdzone losowania (jak system to określa)?
   - Czy wskaźnik znika po wykonaniu weryfikacji, czy wymaga ręcznego oznaczenia jako "przeczytane"?

6. **Bezpieczeństwo hasła:**
   - Jakie są minimalne wymagania dla hasła (długość, znaki specjalne, cyfry)?
   - Czy system wymusza silne hasła, czy tylko sugeruje?

7. **Date range picker - zachowanie domyślne:**
   - Jakie są domyślne wartości w date range picker (wszystkie losowania, ostatni miesiąc, ostatnie 7 dni)?
   - Czy użytkownik może zapisać ulubiony zakres jako domyślny?

8. **Prezentacja zestawów bez wygranych:**
   - Czy w wynikach weryfikacji pokazujemy również zestawy bez trafień (0 trafień), czy tylko te z ≥3 trafieniami?
   - Jeśli pokazujemy wszystkie - jak wizualnie odróżniamy wygrane od niewygrane?

9. **Sortowanie i filtrowanie:**
   - Czy lista zestawów ma możliwość sortowania (data dodania, nazwa, ostatnia weryfikacja)?
   - Czy wyniki weryfikacji są sortowane (według liczby trafień malejąco, czy chronologicznie według daty losowania)?

10. **Usuwanie wyników losowań:**
    - Czy użytkownik może usuwać wprowadzone wyniki losowań, czy są one trwałe?
    - Czy istnieje wspólna baza wyników losowań dla wszystkich użytkowników, czy każdy użytkownik ma swoją prywatną listę?

### Kwestie Techniczne do Ustalenia

11. **Deployment i infrastruktura:**
    - Czy przewidziane jest wdrożenie do konkretnej chmury (Azure, AWS, GCP)?
    - Czy wymagany jest Docker w MVP?
    - Jakie są wymagania dotyczące CI/CD (GitHub Actions wspomniane w CLAUDE.md - jakie konkretnie workflow)?

12. **Monitoring i logging:**
    - Jakie narzędzia do monitoringu błędów (Sentry, Application Insights)?
    - Czy wymagany jest structured logging?
    - Jakie poziomy logowania są akceptowalne w produkcji?

13. **Analytics:**
    - Jakie narzędzie do zbierania metryk użytkowania (Google Analytics, Mixpanel, custom)?
    - Czy wymagana jest zgodność z RODO dla danych analytics?
    - Jakie eventy należy trackować w MVP?

---

## Następne Kroki

1. **Wyjaśnienie nierozwiązanych kwestii** - odpowiedzi na pytania z sekcji "Nierozwiązane Kwestie"
2. **Stworzenie pełnego PRD** - dokument zawierający wszystkie szczegóły techniczne i funkcjonalne
3. **Przygotowanie User Stories** - szczegółowe rozpisanie historii użytkownika z kryteriami akceptacji
4. **Zaprojektowanie schematu bazy danych** - finalna struktura tabel z relacjami
5. **Mockupy/Wireframes** - wizualizacja kluczowych ekranów aplikacji
6. **Planowanie sprintów** - podział prac na iteracje z priorytetyzacją
7. **Setup środowiska CI/CD** - konfiguracja GitHub Actions
8. **Inicjalizacja projektu** - setup repozytorium, struktura folderów, initial commit
