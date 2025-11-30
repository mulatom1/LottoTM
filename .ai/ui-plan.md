# Architektura UI dla LottoTM MVP

**Wersja:** 1.0
**Data:** 2025-11-09
**Podstawa:** PRD v1.3, api-plan.md v1.0, ui-plan-analise-summary.md v2.0

---

## 1. Przegląd struktury UI

System LottoTM to aplikacja webowa do zarządzania kuponami loterii LOTTO, zbudowana w architekturze SPA (Single Page Application). Interfejs użytkownika oparty jest na React 19 z TypeScript i Tailwind CSS 4, wykorzystując React Router 7 do nawigacji.

### Kluczowe założenia architektoniczne:

- **Mobile-first CSS, desktop-first UX** - kod CSS używa breakpointów mobile-first (Tailwind default), ale decyzje projektowe i optymalizacje UX priorytetyzują użytkowników desktop
- **Język polski** - wszystkie elementy UI (labels, przyciski, komunikaty, placeholdery) w języku polskim
- **Context API dla autentykacji** - stan użytkownika (token JWT, email, isAdmin) zarządzany globalnie przez AppContext
- **Lokalny stan dla danych biznesowych** - dane takie jak lista zestawów, losowań pobierane i zarządzane na poziomie komponentów
- **Modalne interakcje** - formularze edycji, preview generatorów, potwierdzenia usunięcia w modalach (nie dedykowane routes)
- **Centralized error handling** - wszystkie błędy (walidacja inline + błędy biznesowe) wyświetlane w ErrorModal po submit
- **Paginacja selektywna** - tylko dla Draws (20/strona domyślnie, max 100), Tickets bez paginacji (max 100 zestawów, scrollowanie)
- **Filtrowanie opcjonalne** - Draws wspiera filtrowanie po zakresie dat (dateFrom/dateTo), Tickets wspiera częściowe filtrowanie po nazwie grupy (groupName, LIKE/Contains)

### Rezygnacja z Dashboard:

W MVP **całkowicie zrezygnowano z widoku Dashboard** (`/dashboard`). Po zalogowaniu użytkownik jest przekierowywany bezpośrednio na `/tickets`. Navbar nie zawiera zakładki "Dashboard".

---

## 2. Lista widoków

### 2.1 Landing Page

**Ścieżka widoku:** `/`

**Dostęp:** Publiczny (niezalogowani użytkownicy)

**Główny cel:** Zachęcić użytkowników do rejestracji lub logowania w systemie

**Kluczowe informacje do wyświetlenia:**
- Nazwa aplikacji: "LottoTM - System Zarządzania Kuponami LOTTO"
- Krótki opis (2-3 zdania): "Zarządzaj swoimi zestawami liczb LOTTO i automatycznie weryfikuj wygrane. Szybko, wygodnie, bezpiecznie."
- Call-to-action: Zachęta do rejestracji lub logowania

**Kluczowe komponenty widoku:**
- **Logo/Branding** (opcjonalnie)
- **Nagłówek h1** z tytułem aplikacji
- **Sekcja opisu** z krótkim tekstem wyjaśniającym wartość produktu
- **CTA Buttons:**
  - "Zaloguj się" (primary button) → redirect `/login`
  - "Zarejestruj się" (secondary button) → redirect `/register`

**UX, dostępność i względy bezpieczeństwa:**
- **UX:** Prosty, przyjazny layout centered na ekranie, minimalistyczny design skupiający uwagę na CTA
- **Dostępność:**
  - Semantic HTML (`<main>`, `<h1>`, `<p>`, `<button>`)
  - High contrast dla tekstu i buttonów (WCAG AA minimum 4.5:1)
  - Focus indicators na buttonach (Tailwind `focus:ring-2`)
  - Keyboard navigation (Tab order: Logo → Opis → Zaloguj się → Zarejestruj się)
- **Bezpieczeństwo:** Brak wrażliwych danych, publiczny widok statyczny (brak API calls)

**Responsywność:**
- Mobile: Single column, full-width buttons, padding dla czytelności
- Desktop: Centered content (max-width 600px), większe buttony

---

### 2.2 Login Page

**Ścieżka widoku:** `/login`

**Dostęp:** Publiczny

**Główny cel:** Uwierzytelnienie użytkownika i uzyskanie dostępu do systemu

**Kluczowe informacje do wyświetlenia:**
- Formularz logowania (email, hasło)
- Komunikaty błędów walidacji (inline + modal)
- Link do rejestracji

**Kluczowe komponenty widoku:**
- **Nagłówek h2:** "Zaloguj się"
- **Formularz logowania:**
  - **Input Email:**
    - Label: "Email"
    - Type: email
    - Required: true
    - Placeholder: "twoj@email.com"
    - Inline validation: format email (regex)
  - **Input Hasło:**
    - Label: "Hasło"
    - Type: password
    - Required: true
    - Placeholder: "••••••••"
  - **Button submit:** "Zaloguj się" (primary)
- **Link nawigacyjny:** "Nie masz konta? Zarejestruj się" → `/register`
- **ErrorModal** dla błędów API:
  - 401: "Nieprawidłowy email lub hasło"
  - 5xx/network: "Wystąpił problem. Spróbuj ponownie."

**UX, dostępność i względy bezpieczeństwa:**
- **UX:**
  - Centered form layout
  - Inline validation w czasie rzeczywistym (czerwony border + tekst błędu pod polem)
  - Po sukcesie: automatyczne logowanie + redirect `/tickets`
  - Auto-focus na pierwszym input (email) przy otwarciu strony
- **Dostępność:**
  - `<label for="email">` powiązane z `<input id="email">`
  - Aria-describedby dla błędów inline
  - Focus trap w ErrorModal
  - Keyboard navigation (Tab: Email → Hasło → Zaloguj się → Link rejestracji)
- **Bezpieczeństwo:**
  - Input type="password" (maskuje znaki)
  - HTTPS wymagane w produkcji (NFR-007)
  - Token JWT przechowywany w localStorage po sukcesie
  - Brak przechowywania hasła w pamięci po submit

**Responsywność:**
- Mobile: Single column, full-width inputs i buttony
- Desktop: Centered form (max-width 400px), większe touch targets

---

### 2.3 Register Page

**Ścieżka widoku:** `/register`

**Dostęp:** Publiczny

**Główny cel:** Utworzenie nowego konta użytkownika z automatycznym logowaniem

**Kluczowe informacje do wyświetlenia:**
- Formularz rejestracji (email, hasło, potwierdzenie hasła)
- Wymagania dla hasła (min. 8 znaków, wielka litera, cyfra, znak specjalny)
- Komunikaty błędów walidacji (inline + modal przy submit)
- Link do logowania

**Kluczowe komponenty widoku:**
- **Nagłówek h2:** "Zarejestruj się"
- **Formularz rejestracji:**
  - **Input Email:**
    - Label: "Email"
    - Type: email
    - Required: true
    - Inline validation: format email, unikalność (backend)
  - **Input Hasło:**
    - Label: "Hasło"
    - Type: password
    - Required: true
    - Inline validation: min. 8 znaków, wielka litera, cyfra, znak specjalny
    - Hint text: "Min. 8 znaków, 1 wielka litera, 1 cyfra, 1 znak specjalny"
  - **Input Potwierdzenie hasła:**
    - Label: "Potwierdź hasło"
    - Type: password
    - Required: true
    - Inline validation: identyczne z hasło
  - **Button submit:** "Zarejestruj się" (primary)
- **Link nawigacyjny:** "Masz już konto? Zaloguj się" → `/login`
- **ErrorModal** dla błędów przy submit:
  - Lista błędów walidacji (np. "Email jest już zajęty", "Hasła nie są identyczne")
  - Błędy biznesowe z backendu

**UX, dostępność i względy bezpieczeństwa:**
- **UX:**
  - Inline validation w czasie rzeczywistym (visual feedback natychmiastowy)
  - Po kliknięciu "Zarejestruj się": wszystkie błędy zbierane i wyświetlane w ErrorModal
  - Po sukcesie: automatyczne logowanie (backend zwraca token JWT) + redirect `/tickets`
  - Auto-focus na email przy otwarciu
- **Dostępność:**
  - Labels powiązane z inputs
  - Aria-describedby dla inline errors
  - Aria-live="polite" dla komunikatów walidacji
  - Keyboard navigation
- **Bezpieczeństwo:**
  - Hasło przechowywane jako bcrypt hash na backendzie (min. 10 rounds, NFR-005)
  - Walidacja siły hasła (frontend + backend)
  - HTTPS w produkcji
  - Brak weryfikacji email w MVP (decyzja PRD)

**Responsywność:**
- Mobile: Single column, vertical stack inputs
- Desktop: Centered form (max-width 400px)

---

### 2.4 Tickets Page

**Ścieżka widoku:** `/tickets`

**Dostęp:** Chroniony (wymaga autentykacji)

**Główny cel:** Zarządzanie zestawami liczb użytkownika (przeglądanie, dodawanie, edycja, usuwanie, generatory)

**Kluczowe informacje do wyświetlenia:**
- Pole filtrowania/wyszukiwania po nazwie grupy (textbox, częściowe dopasowanie)
- Lista zestawów użytkownika (6 liczb, nazwa grupy, data utworzenia)
- Licznik: "X/100 zestawów" z progresywną kolorystyką
- Przyciski akcji: Dodaj ręcznie, Generator losowy, Generator systemowy
- Przyciski CRUD przy każdym zestawie: Edytuj, Usuń

**Kluczowe komponenty widoku:**

**Header section:**
- **Nagłówek h1:** "Moje zestawy"
- **Licznik zestawów:** "[42/100]"
  - Kolorystyka progresywna:
    - 0-70: `text-green-600` (bezpiecznie)
    - 71-90: `text-yellow-600` (ostrzeżenie)
    - 91-100: `text-red-600` (limit bliski)
  - Toast ostrzegawczy jeśli >95: "Uwaga: Pozostało tylko X wolnych miejsc"

**Search/Filter section:**
- **Input tekstowy (textbox):**
  - Label: "Szukaj w grupach" lub "Filtruj po nazwie grupy"
  - Placeholder: "np. Ulubione, test, rodzina..."
  - Type: text, max 100 znaków
  - Ikona: 🔍 (po lewej stronie inputa)
  - **Zachowanie:**
    - Częściowe dopasowanie (LIKE/Contains, case-sensitive)
    - Debounced search (300ms opóźnienie przed wywołaniem API)
    - Przykład: wpisanie "test" znajduje: "test", "testing", "my test group"
  - **Wskazówka wizualna:**
    - Jeśli pole aktywne (focus): border podświetlony
    - Jeśli filtr aktywny (wartość niepusta): wyświetl przycisk "✕ Wyczyść" po prawej stronie
  - **API call:**
    - Endpoint: `GET /api/tickets?groupName={wartość}`
    - Jeśli pole puste/null: `GET /api/tickets` (wszystkie zestawy)
  - **Empty state po filtrowaniu:**
    - Jeśli brak wyników: "Nie znaleziono zestawów pasujących do '{wartość filtra}'. Spróbuj zmienić kryteria wyszukiwania."

**Action buttons (horizontal row):**
- **"+ Dodaj ręcznie"** (primary) → otwiera Modal dodawania
- **"🎲 Generuj losowy"** (secondary) → wywołuje generator losowy
- **"🔢 Generuj systemowy"** (secondary) → wywołuje generator systemowy (9 zestawów)
- **"📥 Importuj z CSV"** (secondary, warunkowo wyświetlany) → otwiera Modal importu (Feature Flag)
- **"📤 Eksportuj do CSV"** (secondary, warunkowo wyświetlany) → pobiera plik CSV (Feature Flag)

**Lista zestawów (scrollowalna, max 100):**
- Każdy zestaw jako card/row:
  - **Liczby:** [3, 12, 25, 31, 42, 48] (wyświetlone jako badges lub inline)
  - **Nazwa grupy:** "Ulubione" (wyświetlona jako tag/badge lub tekst, opcjonalnie)
  - **Data utworzenia:** "Utworzono: 2025-10-15 14:30"
  - **Przyciski akcji:** [Edytuj] [Usuń]
- Sortowanie: według daty utworzenia (najnowsze na górze, malejąco)
- **Empty state** (jeśli brak zestawów): "Nie masz jeszcze żadnych zestawów. Dodaj swój pierwszy zestaw używając przycisków powyżej."
- **Empty state po filtrowaniu**: "Nie znaleziono zestawów pasujących do '{wartość filtra}'. Spróbuj zmienić kryteria wyszukiwania."

**Modale:**

1. **Modal dodawania zestawu ręcznie:**
   - Tytuł: "Dodaj nowy zestaw"
   - **Pole tekstowe - Nazwa grupy (opcjonalne):**
     - Label: "Nazwa grupy (opcjonalnie)"
     - Placeholder: "np. Ulubione, Rodzina, Test..."
     - Type: text, max 100 znaków
     - Opcjonalne (może być puste)
   - **6 pól numerycznych:**
     - Labels: "Liczba 1" do "Liczba 6"
     - Type: number, min="1", max="49", required
     - Inline validation: zakres 1-49, unikalność w zestawie
   - Przyciski: [Wyczyść] (left, secondary) | [Anuluj] [Zapisz] (right, Zapisz=primary)
   - **Walidacja:**
     - Inline: błędy pod polami w czasie rzeczywistym (np. "Liczba musi być w zakresie 1-49")
     - Po kliknięciu "Zapisz": wszystkie błędy w ErrorModal (walidacja + biznesowe: limit 100, duplikat zestawu)
   - Po sukcesie: Toast "Zestaw zapisany pomyślnie" (zielony, auto-dismiss 3-4s), modal zamyka, lista odświeża

2. **Modal edycji zestawu:**
   - Identyczny jak dodawanie, ale tytuł: "Edytuj zestaw"
   - Pola pre-wypełnione aktualnymi wartościami (nazwa grupy + 6 liczb)
   - Pole nazwa grupy również edytowalne
   - Walidacja unikalności pomija edytowany zestaw
   - Po sukcesie: Toast "Zestaw zaktualizowany pomyślnie"

3. **Modal potwierdzenia usunięcia:**
   - Tytuł: "Usuń zestaw"
   - Treść: "Czy na pewno chcesz usunąć ten zestaw?\n[3, 12, 25, 31, 42, 48]"
   - Przyciski: [Anuluj] (focus default) [Usuń] (danger variant, czerwony)
   - Po sukcesie: Toast "Zestaw usunięty pomyślnie", lista odświeża

4. **Modal preview generatora losowego:**
   - Tytuł: "Generator losowy"
   - **Wygenerowane liczby:** [7, 19, 22, 33, 38, 45] (wyświetlone jako badges)
   - Przyciski: [Generuj ponownie] (secondary) | [Anuluj] [Zapisz] (primary)
   - Walidacja limitu przed zapisem: jeśli ≥100 → ErrorModal "Osiągnięto limit 100 zestawów. Usuń istniejące zestawy, aby dodać nowe."
   - Po sukcesie: Toast "Zestaw wygenerowany i zapisany", modal zamyka, lista odświeża

5. **Modal preview generatora systemowego:**
   - Tytuł: "Generator systemowy (9 zestawów)"
   - **Tooltip/wyjaśnienie:** "Generator tworzy 9 zestawów pokrywających wszystkie liczby od 1 do 49. Każda liczba pojawia się minimum raz."
   - **Grid zestawów:**
     - Desktop: Grid 3x3 (3 kolumny × 3 rzędy)
     - Mobile: Vertical list (9 cards stacked)
   - Każdy zestaw wyświetlony jako: "Zestaw X: [1, 5, 12, 23, 34, 45]"
   - Przyciski: [Generuj ponownie] | [Anuluj] [Zapisz wszystkie] (primary)
   - Walidacja limitu: sprawdzenie czy miejsce na 9 zestawów (100 - aktualna liczba ≥ 9)
   - Jeśli brak miejsca: ErrorModal "Brak miejsca na 9 zestawów. Dostępne: X zestawy. Usuń istniejące zestawy, aby kontynuować."
   - Po sukcesie: Toast "9 zestawów wygenerowanych i zapisanych", modal zamyka, lista odświeża

6. **Modal importu zestawów z CSV (Feature Flag):**
   - **Feature Flag:** Widoczny tylko gdy `Features:TicketImportExport:Enable = true` w konfiguracji backend
   - Frontend sprawdza dostępność przed wyświetleniem przycisku (opcjonalnie)
   - Tytuł: "Importuj zestawy z CSV"
   - **Wyjaśnienie formatu:**
     - Text: "Format pliku CSV: nagłówek + wiersze z danymi"
     - Przykład: `Number1,Number2,Number3,Number4,Number5,Number6,GroupName`
     - Link/tooltip z pełną dokumentacją formatu
   - **File input:**
     - Label: "Wybierz plik CSV"
     - Type: file, accept=".csv,text/csv"
     - Max size: 1MB (walidacja frontend + backend)
   - **Preview wybranego pliku (opcjonalnie):**
     - Nazwa pliku, rozmiar
   - Przyciski: [Anuluj] [Importuj] (primary)
   - **Walidacja:**
     - Sprawdzenie formatu pliku (CSV)
     - Backend sprawdza nagłówek, zakres liczb, limit dostępnych miejsc, unikalność
   - **Po sukcesie:**
     - ErrorModal z raportem importu:
       - "Zaimportowano: 15 zestawów"
       - "Odrzucono: 2 zestawy"
       - Lista błędów (jeśli są): "Wiersz 3: Duplicate ticket", "Wiersz 7: Invalid number range: 52"
     - Toast: "Import zakończony. Zaimportowano X zestawów." (jeśli imported > 0)
     - Modal zamyka, lista odświeża
   - **Po błędzie:**
     - ErrorModal: komunikaty błędów z backendu (np. "Osiągnięto limit 100 zestawów. Dostępne: X zestawów.")

7. **Modal/Direct download eksportu zestawów do CSV (Feature Flag):**
   - **Feature Flag:** Widoczny tylko gdy `Features:TicketImportExport:Enable = true`
   - **UX:** Nie modal - bezpośrednie pobranie pliku po kliknięciu przycisku "📤 Eksportuj do CSV"
   - API call: `GET /api/tickets/export-csv`
   - Response: Plik CSV z automatycznym pobraniem przez przeglądarkę
   - **Format pliku:**
     - Nazwa: `lotto-tickets-{userId}-{YYYY-MM-DD}.csv`
     - Nagłówek: `Number1,Number2,Number3,Number4,Number5,Number6,GroupName`
     - Wiersze: wszystkie zestawy użytkownika
   - **Po sukcesie:**
     - Toast: "Wyeksportowano X zestawów do pliku CSV"
     - Plik automatycznie pobierany przez przeglądarkę
   - **Po błędzie:**
     - ErrorModal: komunikat błędu (np. "Wystąpił problem. Spróbuj ponownie.")

**ErrorModal (używany wszędzie):**
- Tytuł: "Błąd"
- Treść: Lista błędów (• Błąd 1, • Błąd 2, ...)
- Przycisk: [Zamknij]

**UX, dostępność i względy bezpieczeństwa:**
- **UX:**
  - Licznik z kolorystyką daje natychmiastowy feedback o stanie limitu
  - Preview generatorów pozwala użytkownikowi na kontrolę przed zapisem (opcja "Generuj ponownie")
  - Inline validation redukuje friction (natychmiastowy feedback)
  - Toast notifications dla sukcesu (non-intrusive, auto-dismiss)
  - Modal errors dla błędów (wymaga acknowledge)
- **Dostępność:**
  - ARIA labels dla buttonów z ikonami (🎲 → aria-label="Generuj losowy zestaw")
  - Focus trap w modalach (Escape zamyka modal)
  - Keyboard navigation: Tab przez pola, Enter submit, Escape cancel
  - Screen reader announcements dla Toast (aria-live="polite")
- **Bezpieczeństwo:**
  - Protected route: redirect `/login` jeśli `!isLoggedIn`
  - Wszystkie API calls z JWT w header Authorization
  - Backend filtruje dane po UserId (izolacja danych, F-AUTH-004)
  - Walidacja limitu i unikalności na backendzie (frontend validation to tylko UX enhancement)

**Responsywność:**
- Mobile:
  - Lista zestawów: vertical stack, full-width cards
  - Action buttons: vertical stack lub 2 kolumny
  - Modale: full-screen lub centered z max-width
  - Formularze: 6 number inputs stacked vertically
- Desktop:
  - Lista zestawów: grid 2 kolumny (opcjonalnie) lub single column
  - Action buttons: horizontal row
  - Formularze: 2 kolumny (3 inputs per row)
  - Generator systemowy: grid 3x3

---

### 2.5 Draws Page

**Ścieżka widoku:** `/draws`

**Dostęp:** Chroniony (wszyscy zalogowani użytkownicy mają dostęp read-only, admini mają full access)

**Główny cel:** Przeglądanie historii losowań LOTTO (globalna tabela dostępna dla wszystkich użytkowników), zarządzanie losowaniami (admin only)

**Kluczowe informacje do wyświetlenia:**
- Lista wyników losowań (data losowania, 6 liczb, data wprowadzenia)
- Paginacja (100 elementów na stronę)
- Przyciski CRUD (tylko dla adminów)

**Kluczowe komponenty widoku:**

**Header section:**
- **Nagłówek h1:** "Historia losowań"
- **Przycisk "+ Dodaj wynik"** (primary, widoczny tylko dla adminów)
  - Conditional rendering: `{user.isAdmin && <Button>+ Dodaj wynik</Button>}`

**Filtr zakresu dat:**
- **Date range picker:**
  - **Input "Od:"**
    - Label: "Od:"
    - Type: date
    - Opcjonalny (domyślnie pusty - brak filtra)
    - Placeholder: "yyyy-mm-dd"
  - **Input "Do:"**
    - Label: "Do:"
    - Type: date
    - Opcjonalny (domyślnie pusty - brak filtra)
    - Placeholder: "yyyy-mm-dd"
  - Layout:
    - Mobile: stacked vertically (Od nad Do)
    - Desktop: inline (Od | Do obok siebie, compact)
- **Przyciski filtrowania:**
  - **"Filtruj"** (primary) - stosuje filtr, wywołuje API z parametrami dateFrom/dateTo
  - **"Wyczyść"** (secondary) - czyści pola filtra, resetuje do pełnej listy
- **Logika filtrowania:**
  - Jeśli oba pola puste: `GET /api/draws?page=1&pageSize=20` (bez filtra)
  - Jeśli wypełnione: `GET /api/draws?page=1&pageSize=20&dateFrom=YYYY-MM-DD&dateTo=YYYY-MM-DD`
  - Walidacja inline: data "Od" nie może być późniejsza niż data "Do"
  - Komunikat błędu: "Data 'Od' musi być wcześniejsza lub równa 'Do'"
  - Po zastosowaniu filtra: informacja "Filtr aktywny: 2025-10-01 - 2025-10-31" (nad listą)
  - Przycisk "Wyczyść" widoczny tylko gdy filtr jest aktywny
- **UX filtrowania:**
  - Filtr zachowuje się przy paginacji (parametry dateFrom/dateTo przekazywane w każdym page request)
  - Po kliknięciu "Filtruj": lista odświeża się do strony 1 z zastosowanym filtrem
  - Po kliknięciu "Wyczyść": lista odświeża się do strony 1 bez filtra
  - Stan filtra (dateFrom/dateTo) zarządzany w local state komponentu DrawsPage

**Lista losowań:**
- Każde losowanie jako card/row:
  - **Data losowania:** "2025-10-30"
  - **Typ losowania:** "LOTTO" lub "LOTTO PLUS"
  - **DrawSystemId:** 20250001 (wyświetlony obok daty jako "ID: 20250001")
  - **Wylosowane liczby:** [3, 12, 25, 31, 42, 48] (wyświetlone jako kolorowe badges)
  - **Cena biletu:** "Cena biletu: 3.00 zł" (jeśli dostępna)
  - **Informacje o wygranych** (jeśli dostępne):
    - Wyświetlone w ładnej ramce z gradientowym tłem (gray-50 do gray-100)
    - Nagłówek sekcji z kolorowym paskiem gradientowym (zielony → pomarańczowy)
    - 4 kolorowe karty (grid responsywny: 1 kolumna na mobile, 2 na tablet, 4 na desktop):
      - **Stopień 1 (6 trafionych):** Zielona karta z ikoną "6", ilość wygranych i kwota
      - **Stopień 2 (5 trafionych):** Niebieska karta z ikoną "5", ilość wygranych i kwota
      - **Stopień 3 (4 trafione):** Żółta karta z ikoną "4", ilość wygranych i kwota
      - **Stopień 4 (3 trafione):** Pomarańczowa karta z ikoną "3", ilość wygranych i kwota
  - **Przyciski akcji (admin only):** [Edytuj] [Usuń]
- Sortowanie: według daty losowania (najnowsze na górze, malejąco, domyślnie)
- **Empty state:** "Nie wprowadzono jeszcze żadnych wyników losowań."

**Pagination controls (na dole listy):**
- **Komponenty:**
  - Button "« Poprzednia" (disabled jeśli currentPage === 1)
  - Page numbers: 1 2 [3] 4 5 (max 5 widocznych numerów, current page highlighted)
  - Button "Następna »" (disabled jeśli currentPage === totalPages)
- **Info:** "Strona 3 z 5 (245 losowań)"
- **Logika:**
  - Zmiana strony: API call `GET /api/draws?page=X&pageSize=20`
  - Page size: 20 elementów (stały w MVP)

**Modale (admin only):**

1. **Modal dodawania wyniku losowania:**
   - Tytuł: "Dodaj wynik losowania"
   - **Date picker:**
     - Label: "Data losowania"
     - Type: date
     - Required: true
     - Validation: nie może być w przyszłości (date ≤ dzisiaj)
   - **Dropdown/Radio buttons dla typu losowania:**
     - Label: "Typ losowania"
     - Opcje: "LOTTO" lub "LOTTO PLUS"
     - Required: true
     - Default: "LOTTO"
   - **Przycisk "Pobierz z XLotto" (admin only, warunkowo wyświetlany):**
     - Secondary button, po lewej stronie
     - **Feature Flag:** Widoczny tylko gdy `GoogleGemini:Enable = true` w konfiguracji backend
     - Frontend sprawdza endpoint `GET /api/xlotto/is-enabled` przy otwarciu modalu
     - Funkcjonalność: Automatyczne pobieranie wyników z XLotto.pl przez Google Gemini API
     - Walidacja przed wywołaniem: data losowania i typ muszą być wybrane
     - API call: `GET /api/xlotto/actual-draws?Date={drawDate}&LottoType={lottoType}`
     - Loading state: disabled przyciski, spinner przy przycisku
     - Automatyczne wypełnienie wszystkich pól (liczby + DrawSystemId + opcjonalnie ticketPrice i win pools) wynikami z API
     - Obsługa błędów: Alert z komunikatem błędu (401, 400, 500)
   - **Input DrawSystemId:**
     - Label: "DrawSystemId"
     - Type: number
     - Required: true
     - Może być wypełnione automatycznie przez przycisk "Pobierz z XLotto"
   - **6 pól numerycznych:**
     - Labels: "Liczba 1" do "Liczba 6"
     - Type: number, min="1", max="49", required
     - Inline validation: zakres 1-49, unikalność
     - Mogą być wypełnione automatycznie przez przycisk "Pobierz z XLotto"
   - **Pola opcjonalne (win pool information):**
     - **Input "Cena biletu":**
       - Label: "Cena biletu (opcjonalnie)"
       - Type: number, step="0.01", min="0"
       - Format: DECIMAL(10,2)
     - **Sekcja "Wygrane 1. stopnia (6 trafionych)":**
       - Input "Ilość wygranych": Type: number, min="0" (winPoolCount1)
       - Input "Kwota wygranej": Type: number, step="0.01", min="0" (winPoolAmount1)
     - **Sekcja "Wygrane 2. stopnia (5 trafionych)":**
       - Input "Ilość wygranych": Type: number, min="0" (winPoolCount2)
       - Input "Kwota wygranej": Type: number, step="0.01", min="0" (winPoolAmount2)
     - **Sekcja "Wygrane 3. stopnia (4 trafione)":**
       - Input "Ilość wygranych": Type: number, min="0" (winPoolCount3)
       - Input "Kwota wygranej": Type: number, step="0.01", min="0" (winPoolAmount3)
     - **Sekcja "Wygrane 4. stopnia (3 trafione)":**
       - Input "Ilość wygranych": Type: number, min="0" (winPoolCount4)
       - Input "Kwota wygranej": Type: number, step="0.01", min="0" (winPoolAmount4)
   - Przyciski: [Pobierz z XLotto] [Wyczyść] | [Anuluj] [Zapisz]
   - **Logika backend:** Jeśli losowanie na daną datę i typ już istnieje, backend zwraca błąd z komunikatem "Wynik losowania na tę datę i typ już istnieje."
   - Po sukcesie: Toast "Wynik losowania zapisany pomyślnie", modal zamyka, aktualna strona odświeża

2. **Modal edycji wyniku:**
   - Identyczny jak dodawanie, tytuł: "Edytuj wynik losowania"
   - Pola pre-wypełnione aktualnymi wartościami (data + typ losowania + 6 liczb + drawSystemId + opcjonalnie ticketPrice i win pools)
   - Po sukcesie: Toast "Wynik zaktualizowany pomyślnie"

3. **Modal potwierdzenia usunięcia:**
   - Tytuł: "Usuń wynik losowania"
   - Treść: "Czy na pewno chcesz usunąć wynik losowania z dnia 2025-10-30?\n[3, 12, 25, 31, 42, 48]"
   - Przyciski: [Anuluj] [Usuń] (danger)
   - Po sukcesie: Toast "Wynik usunięty pomyślnie", aktualna strona odświeża (lub redirect do poprzedniej strony jeśli usunięto ostatni element)

**UX, dostępność i względy bezpieczeństwa:**
- **UX:**
  - Read-only access dla zwykłych użytkowników (transparentność danych: wszyscy widzą te same losowania)
  - Admin-only features wyraźnie oddzielone (conditional rendering, nie osobne route)
  - Paginacja zapobiega problemom wydajnościowym przy rosnącej liczbie losowań
  - Info o aktualnej stronie ("Strona X z Y") daje kontekst użytkownikowi
- **Dostępność:**
  - ARIA roles dla paginacji (role="navigation", aria-label="Paginacja losowań")
  - Disabled state dla buttonów Previous/Next (aria-disabled="true")
  - Current page highlighted wizualnie + aria-current="page"
  - Keyboard navigation: Tab przez pagination controls, Enter submit
- **Bezpieczeństwo:**
  - Backend sprawdza flagę `IsAdmin` przed wykonaniem operacji POST/PUT/DELETE (NFR-010)
  - Frontend conditional rendering to tylko UX enhancement (security on backend)
  - Draws jest globalną tabelą (brak filtrowania po UserId), ale CreatedByUserId tracking kto wprowadził
  - HTTPS w produkcji

**Responsywność:**
- Mobile:
  - Lista losowań: vertical stack, full-width cards
  - Pagination: simplified (tylko Previous/Next + current page number, bez page numbers)
  - Modale: full-screen lub centered
- Desktop:
  - Lista: single column lub grid 2 kolumny
  - Pagination: full (Previous + 5 page numbers + Next)

---

### 2.6 Checks Page

**Ścieżka widoku:** `/checks`

**Dostęp:** Chroniony

**Główny cel:** Weryfikacja wygranych w zestawach użytkownika względem wyników losowań w wybranym zakresie dat

**Kluczowe informacje do wyświetlenia:**
- Date range picker (zakres dat weryfikacji)
- Wyniki weryfikacji: losowania z pogrubionymi wygranymi liczbami w zestawach użytkownika
- Badges wygranych (dla ≥3 trafień)

**Kluczowe komponenty widoku:**

**Header section:**
- **Nagłówek h1:** "Sprawdź swoje wygrane"

**Sekcja podsumowania (CheckSummary):**

Wyświetlana po zakończeniu weryfikacji, przed szczegółową listą losowań.

- **Nagłówek z przyciskiem toggle:**
  - Ikona statystyk + "Podsumowanie wyników"
  - Przycisk rozwijania/zwijania (ikona ▼/▲)
  - Domyślnie: rozwinięta (isExpanded: true)

- **Zawartość (gdy rozwinięta):**
  - Grid ze statystykami (responsive: 1 kolumna mobile, 2 tablet, 3 desktop)

  **Układ kart (3 kolumny × 3 rzędy):**

  **Rząd 1:**
  1. **Liczba losowań** - niebieska karta
     - Ikona: 📅
     - Wartość: liczba unikalnych dat losowań (każda data = 1 losowanie z LOTTO + LOTTO PLUS)

  2. **Liczba kuponów** - zielona karta
     - Ikona: 🎫
     - Wartość: liczba unikalnych zestawów użytkownika

  3. **Suma nakładów** - pomarańczowa karta (kolumna 3)
     - Ikona: 💰
     - Formuła: **liczba losowań × liczba kuponów × (cena LOTTO + cena LOTTO PLUS)**
     - Logika:
       - Liczba losowań = liczba unikalnych dat losowań (każda data to 1 losowanie z LOTTO + LOTTO PLUS)
       - Cena za kupon na losowanie = suma cen LOTTO i LOTTO PLUS
       - Przykład: 5 dni × 10 kuponów × (3.00 + 1.50) zł = 5 × 10 × 4.50 = 225.00 zł
     - Format: "XX.XX zł"

  **Rząd 2:**
  4. **Wygrane 1° (trójki)** - żółta karta
     - Label: "Wygrane 1° (trójki)"
     - Format: "ilość | wartość zł"
     - Przykład: "5 | 25.00 zł"
     - Mapowanie: 3 trafienia → winPoolAmount4

  5. **Wygrane 2° (czwórki)** - jasnozielona karta
     - Label: "Wygrane 2° (czwórki)"
     - Format: "ilość | wartość zł"
     - Mapowanie: 4 trafienia → winPoolAmount3

  6. **Suma wygranych** - granatowa karta (kolumna 3)
     - Łączna liczba wszystkich wygranych i ich wartość
     - Format: "ilość | wartość zł"

  **Rząd 3:**
  7. **Wygrane 3° (piątki)** - ciemnozielona karta
     - Label: "Wygrane 3° (piątki)"
     - Format: "ilość | wartość zł"
     - Mapowanie: 5 trafień → winPoolAmount2

  8. **Wygrane 4° (szóstki)** - fioletowa karta
     - Label: "Wygrane 4° (szóstki)"
     - Format: "ilość | wartość zł"
     - Mapowanie: 6 trafień → winPoolAmount1

  9. **Bilans** - zielona (zysk) lub czerwona (strata) karta (kolumna 3)
     - Emoji: 😊 (zysk ≥0) lub 😠 (strata <0)
     - Wartość: suma wygranych - suma nakładów
     - Format: "+XX.XX zł" lub "-XX.XX zł"
     - Podpis: "Zysk" lub "Strata"

- **Responsywność:**
  - Mobile: 1 kolumna, karty pełnej szerokości
  - Tablet: 2 kolumny
  - Desktop: 3 kolumny, równomierne rozmieszczenie

**Formularz zakresu dat:**
- **Date range picker:**
  - **Input "Od:"**
    - Label: "Od:"
    - Type: date
    - Default: dzisiaj - 31 dni
    - Inline validation: data "Od" nie może być późniejsza niż "Do"
  - **Input "Do:"**
    - Label: "Do:"
    - Type: date
    - Default: dzisiaj
    - Inline validation: data "Do" musi być ≥ "Od", max zakres 31 dni
  - Layout:
    - Mobile: stacked vertically (Od nad Do)
    - Desktop: inline (Od | Do obok siebie)
- **Input "Grupa kuponów (opcjonalnie):"**
  - Label: "Grupa kuponów (opcjonalnie):"
  - Type: text
  - Placeholder: "np. Ulubione"
  - Default: puste
  - Opis pomocniczy: "Wyszukiwanie częściowe - wpisz fragment nazwy grupy (np. 'ulu' znajdzie 'Ulubione')"
  - Walidacja: brak (pole opcjonalne)
  - Layout: full-width na mobile i desktop
- **Button submit:** "Sprawdź wygrane" (primary, duży, prominent)

**Sekcja wyników:**

**Loading state (podczas weryfikacji):**
- Lokalny spinner w obszarze wyników (nie full-page overlay)
- Navbar i formularz zakresu dat pozostają aktywne
- Text: "Weryfikuję wygrane..." (opcjonalnie)

**Filtr wyników (po zakończeniu weryfikacji, przed listą losowań):**
- **Checkbox/Toggle:** "Pokaż tylko losowania z kuponami trafionymi"
  - Default: false (wyłączony - pokazuje wszystkie losowania)
  - Gdy włączony (true): ukrywa losowania bez trafień (drawsResults z pustą listą winningTicketsResult)
  - Filtrowanie lokalne (bez odpytywania backendu ponownie)
  - Layout: nad listą losowań, wyrównany do prawej lub lewej strony

**Lista Draws z rozwijalnymi sekcjami (po zakończeniu weryfikacji):**

Struktura - każde losowanie (Draw) jako card/rekord z dwoma rozwijalnymi sekcjami:

**Draw Card (dla każdego losowania w zakresie):**
- **Header główny (zawsze widoczny, nie klikany):**
  - **Data losowania:** "2025-10-28" (duża, pogrubiona czcionka)
  - **Typ losowania:** Badge "LOTTO" lub "LOTTO PLUS" (różne kolory: zielony dla LOTTO, niebieski dla LOTTO PLUS)
  - **DrawSystemId:** "ID: 20250001" (mniejsza czcionka, szary kolor)
  - **Wylosowane numery:** [12, 18, 25, 31, 40, 49] (niebieskie kółka, inline display)

- **Rozwijalna sekcja 1 (domyślnie ukryta):**
  - **Nagłówek sekcji (kliknąlny):** "Koszt kuponu" + ikona ▼/▶
  - **Zawartość (po rozwinięciu):**
    - **Cena kuponu:** "Cena biletu: 3.00 zł" (lub "Brak danych" jeśli null)
    - **Statystyki wygranych (stopień 1-4):**
      - Grid layout (4 kolumny na desktop, 2 na tablet, 1 na mobile)
      - Dla każdego stopnia (1-4):
        - **Stopień 1 (6 trafień):** Zielona karta z ikoną "6", ilość wygranych + kwota
          - "Ilość: 2 osoby" (lub "Brak danych")
          - "Kwota: 5,000,000.00 zł" (lub "Brak danych")
        - **Stopień 2 (5 trafień):** Niebieska karta z ikoną "5", ilość + kwota
        - **Stopień 3 (4 trafienia):** Żółta karta z ikoną "4", ilość + kwota
        - **Stopień 4 (3 trafienia):** Pomarańczowa karta z ikoną "3", ilość + kwota

- **Rozwijalna sekcja 2 (domyślnie ukryta):**
  - **Nagłówek sekcji (kliknąlny):** "Ilość wygranych zestawów (X)" + ikona ▼/▶
    - Gdzie X to liczba wygranych kuponów dla tego losowania (np. "Ilość wygranych zestawów (3)")
  - **Zawartość (po rozwinięciu):**
    - **Lista wygranych kuponów** (tylko kupony z ≥3 trafieniami):
      - Każdy kupon jako card/row:
        - **GroupName kuponu:** Badge szary z nazwą grupy (np. "Ulubione")
        - **Status wygranej:** Badge kolorowy z emoji i tekstem:
          - 3 trafienia: 🏆 "Wygrana 3 (trójka)" - zielony badge
          - 4 trafienia: 🏆 "Wygrana 4 (czwórka)" - niebieski badge
          - 5 trafień: 🏆 "Wygrana 5 (piątka)" - pomarańczowy badge
          - 6 trafień: 🎉 "Wygrana 6 (szóstka)" - czerwony/złoty badge
        - **Liczby z kuponu:**
          - Szare kółka dla nietrafionych liczb
          - Niebieskie kółka z pogrubionym tekstem dla trafionych liczb (matchingNumbers)
          - Przykład: [3, **12**, 19, **25**, **31**, 44] - gdzie 12, 25, 31 są trafione (niebieskie), a 3, 19, 44 są nietrafione (szare)

**Empty state (jeśli brak losowań z wygranymi w zakresie):**
- "Nie znaleziono wygranych w wybranym zakresie dat."
- Lub (jeśli są losowania, ale żaden kupon nie wygrał): każdy Draw Card wyświetla "Brak wygranych kuponów dla tego losowania" w sekcji 2

**UX, dostępność i względy bezpieczeństwa:**
- **UX:**
  - Domyślny zakres dat (-31 dni) redukuje friction (użytkownik może od razu kliknąć "Sprawdź wygrane")
  - Opcjonalny filtr grupy kuponów z wyszukiwaniem częściowym pozwala na elastyczną weryfikację (np. 'ulu' znajdzie 'Ulubione', 'Ulubione 2024')
  - Opis pomocniczy "Wyszukiwanie częściowe - wpisz fragment nazwy grupy" jasno komunikuje działanie filtra
  - Accordion pozwala na stopniowe odkrywanie wyników (czytelność przy wielu losowaniach)
  - Visual highlight wygranych liczb (pogrubienie) ułatwia szybkie skanowanie
  - Badges wygranych z emoji (🏆, 🎉) i kolorami przyciągają uwagę
  - Lokalny loading spinner (non-blocking) pozwala użytkownikowi pozostać w kontekście
- **Dostępność:**
  - ARIA expanded dla accordion items (`aria-expanded="true/false"`)
  - ARIA controls (`aria-controls="accordion-content-1"`)
  - Keyboard navigation: Tab przez accordion headers, Enter/Space toggle expand
  - Semantic colors + text dla badges (nie tylko kolor, ale także emoji i text "Wygrana X")
  - Screen reader friendly: pogrubione liczby czytane jako "12 wygrana, 25 wygrana, 31 wygrana"
- **Bezpieczeństwo:**
  - Backend filtruje zestawy po UserId (użytkownik widzi tylko swoje wygrane)
  - Weryfikacja wykonana na backendzie (frontend tylko rendering, brak manipulacji danych)
  - Walidacja zakresu dat na backendzie (max 31 dni, NFR-005 z api-plan.md)

**Performance:**
- Wymaganie NFR-001: weryfikacja 100 zestawów × 1 losowanie ≤ 2 sekundy
- Backend algorytm: LINQ Intersect w pamięci po eager loading z DrawNumbers/TicketNumbers
- Frontend: odbiór i renderowanie wyników bez dodatkowego przetwarzania

**Responsywność:**
- Mobile:
  - Date range picker: vertical stack (Od nad Do)
  - Accordion: full-width, touch-friendly tap targets (min 44x44px)
  - Zestawy: vertical stack, badges pod liczbami
- Desktop:
  - Date range picker: inline (Od | Do obok siebie)
  - Accordion: max-width dla czytelności
  - Zestawy: inline display, badges po prawej stronie

---

## 3. Mapa podróży użytkownika

### 3.1 Główny przepływ: Od rejestracji do weryfikacji wygranych

**Krok 1: Nowy użytkownik wchodzi na stronę**
- Punkt wejścia: Landing Page (`/`)
- Widzi: Tytuł aplikacji, opis, przyciski "Zaloguj się" i "Zarejestruj się"
- Akcja: Klika "Zarejestruj się"
- Rezultat: Redirect → `/register`

**Krok 2: Rejestracja**
- Widok: Register Page
- Użytkownik wypełnia formularz:
  - Email: "jan.kowalski@example.com"
  - Hasło: "SecurePass123!"
  - Potwierdzenie hasła: "SecurePass123!"
- Inline validation w czasie rzeczywistym (zielony check przy poprawnych polach)
- Klika "Zarejestruj się"
- Scenariusz sukcesu:
  - API call: `POST /api/auth/register`
  - Backend zwraca token JWT + dane użytkownika (automatyczne logowanie)
  - AppContext.login(userData) → token zapisany w localStorage
  - Redirect → `/tickets`
- Scenariusz błędu:
  - ErrorModal: "Email jest już zajęty"
  - Użytkownik poprawia email, ponownie submit

**Krok 3: Dodawanie zestawów liczb**
- Widok: Tickets Page (po pierwszym logowaniu)
- Użytkownik widzi: Empty state "Nie masz jeszcze żadnych zestawów..."
- Licznik: "0/100" (zielony)
- **Opcja A: Dodawanie ręczne**
  - Klika "+ Dodaj ręcznie"
  - Modal otwiera się, auto-focus na pierwszym polu
  - Wprowadza liczby: 3, 12, 25, 31, 42, 48
  - Inline validation: wszystko ok (zielone checki)
  - Klika "Zapisz"
  - API call: `POST /api/tickets` → sukces
  - Toast: "Zestaw zapisany pomyślnie" (zielony, auto-dismiss)
  - Modal zamyka się, lista odświeża → zestaw #1 widoczny
  - Licznik: "1/100"
- **Opcja B: Generator systemowy**
  - Klika "🔢 Generuj systemowy"
  - Modal preview otwiera się z 9 zestawami (grid 3x3 na desktop)
  - Tooltip: "Generator tworzy 9 zestawów pokrywających wszystkie liczby od 1 do 49..."
  - Użytkownik przegląda zestawy, klika "Zapisz wszystkie"
  - API call: `POST /api/tickets/generate-system` → sukces
  - Toast: "9 zestawów wygenerowanych i zapisanych"
  - Modal zamyka, lista odświeża → 9 nowych zestawów widocznych
  - Licznik: "9/100"

**Krok 4: Przeglądanie losowań**
- Użytkownik klika zakładkę "Losowania" w navbar
- Redirect → `/draws`
- Widzi listę losowań (sorted by drawDate desc):
  - 2025-11-08: [5, 12, 18, 25, 37, 44]
  - 2025-11-05: [3, 9, 15, 22, 31, 48]
  - ... (więcej wyników)
- Paginacja na dole: "Strona 1 z 12 (1156 losowań)"
- **Opcja A: Paginacja**
  - Użytkownik klika "2" (page 2)
  - API call: `GET /api/draws?page=2&pageSize=20`
  - Lista odświeża z wynikami strony 2
- **Opcja B: Filtrowanie po zakresie dat**
  - Użytkownik wypełnia date range picker:
    - Od: 2025-10-01
    - Do: 2025-10-31
  - Klika "Filtruj"
  - API call: `GET /api/draws?page=1&pageSize=20&dateFrom=2025-10-01&dateTo=2025-10-31`
  - Lista odświeża z wynikami tylko z października 2025
  - Info nad listą: "Filtr aktywny: 2025-10-01 - 2025-10-31"
  - Użytkownik może kliknąć "Wyczyść" aby wrócić do pełnej listy

**Krok 5: Weryfikacja wygranych**
- Użytkownik klika zakładkę "Sprawdź Wygrane" w navbar
- Redirect → `/checks`
- Date range picker pre-wypełniony:
  - Od: 2025-10-09 (dzisiaj - 31 dni)
  - Do: 2025-11-09 (dzisiaj)
- Użytkownik klika "Sprawdź wygrane"
- Lokalny spinner pojawia się w sekcji wyników
- API call: `POST /api/verification/check` → payload: { dateFrom: "2025-10-09", dateTo: "2025-11-09" }
- Backend przetwarza (≤2s dla 9 zestawów × ~10 losowań)
- Response: { results: [...], totalTickets: 9, totalDraws: 10, executionTimeMs: 1234 }
- Frontend renderuje accordion:
  - **Losowanie 2025-11-08: [5, 12, 18, 25, 37, 44]** (expanded default)
    - Zestaw #1: [3, **12**, **25**, 31, 42, 48] → 🏆 Wygrana 2 (nie pokazujemy badge, <3 trafienia) → "Brak trafień"
    - Zestaw #2: [**5**, **12**, 19, **25**, 31, 44] → 🏆 Wygrana 3 (trójka) - badge zielony
    - ... (wszystkie 9 zestawów)
  - **Losowanie 2025-11-05: [3, 9, 15, 22, 31, 48]** (collapsed)
    - ... (użytkownik może rozwinąć)
- Użytkownik widzi wygrany badge przy zestawie #2 → zadowolony, misja zakończona sukcesem 🎉

**Krok 6: Wylogowanie**
- Użytkownik klika "Wyloguj" w navbar
- AppContext.logout() → token usuwany z localStorage
- Redirect → `/login`
- Użytkownik widzi formularz logowania, może zalogować się ponownie

### 3.2 Przepływ alternatywny: Admin zarządza losowaniami

**Warunek:** Użytkownik zalogowany z flagą `isAdmin === true`

**Krok 1: Admin wchodzi na stronę `/draws`**
- Widzi listę losowań + dodatkowe przyciski (warunkowo renderowane):
  - Header: przycisk "+ Dodaj wynik"
  - Przy każdym losowaniu: [Edytuj] [Usuń]

**Krok 2: Admin dodaje nowy wynik losowania**
- Klika "+ Dodaj wynik"
- Modal otwiera się z formularzem:
  - Date picker: wybiera datę (np. 2025-11-09)
  - 6 pól numerycznych: wprowadza liczby (1, 7, 14, 21, 35, 42)
  - Inline validation: wszystko ok
- Klika "Zapisz"
- API call: `POST /api/draws` → sukces
- Toast: "Wynik losowania zapisany pomyślnie"
- Modal zamyka, aktualna strona odświeża → nowy wynik widoczny na górze listy (sorted by drawDate desc)

**Krok 3: Admin edytuje istniejący wynik**
- Klika [Edytuj] przy losowaniu z 2025-11-08
- Modal edycji otwiera się, pola pre-wypełnione:
  - Data: 2025-11-08
  - Liczby: 5, 12, 18, 25, 37, 44
- Admin zmienia jedną liczbę: 44 → 49
- Klika "Zapisz"
- API call: `PUT /api/draws/{id}` → sukces
- Toast: "Wynik zaktualizowany pomyślnie"
- Modal zamyka, strona odświeża → zaktualizowane liczby widoczne

**Krok 4: Admin usuwa błędny wynik**
- Klika [Usuń] przy losowaniu z 2025-11-01
- Modal potwierdzenia:
  - "Czy na pewno chcesz usunąć wynik losowania z dnia 2025-11-01?\n[3, 9, 15, 22, 31, 48]"
- Admin klika [Usuń]
- API call: `DELETE /api/draws/{id}` → sukces
- Toast: "Wynik usunięty pomyślnie"
- Modal zamyka, strona odświeża → losowanie zniknęło z listy

### 3.3 Kluczowe interakcje użytkownika (podsumowanie)

1. **Nawigacja między widokami:** Navbar (zakładki), links w Landing/Login/Register
2. **Autentykacja:** Formularze logowania/rejestracji z inline validation + ErrorModal
3. **CRUD na zestawach:** Przyciski → Modale (dodawanie/edycja/usuwanie) → API calls → Toast feedback
4. **Generatory:** Przyciski → Preview modale → Zapis → Toast feedback
5. **Paginacja losowań:** Pagination controls → API calls z page parameter → Odświeżenie listy
6. **Weryfikacja wygranych:** Date range picker → Button submit → Lokalny loading → Accordion rendering z highlighted wygranych
7. **Admin operations:** Conditional rendering buttonów → Modale CRUD → API calls → Toast feedback

---

## 4. Układ i struktura nawigacji

### 4.1 Routing (React Router 7)

**Publiczne routes (dostępne bez autentykacji):**
- `/` - Landing Page
- `/login` - Login Page
- `/register` - Register Page

**Chronione routes (wymagają autentykacji, Protected by ProtectedRoute component):**
- `/tickets` - Tickets Page (domyślny widok po zalogowaniu)
- `/draws` - Draws Page (read-only dla users, full access dla adminów)
- `/checks` - Checks Page

**Redirect logic:**
- Niezalogowany użytkownik próbuje wejść na chroniony route → redirect `/login`
- Zalogowany użytkownik wchodzi na `/` → redirect `/tickets` (opcjonalnie)
- Po pomyślnym logowaniu/rejestracji → redirect `/tickets`

**UWAGA:** Zrezygnowano całkowicie z `/dashboard` w MVP.

### 4.2 Navbar (widoczny po zalogowaniu)

**Layout:**
```
┌────────────────────────────────────────────────────────────┐
│ [Logo] LottoTM  │  Moje Zestawy  │  Losowania  │  Sprawdź Wygrane  │  [jan.kowalski@example.com]  │  [Wyloguj] │
└────────────────────────────────────────────────────────────┘
```

**Komponenty:**
- **Logo/Tytuł aplikacji** (po lewej):
  - Text: "LottoTM" (opcjonalnie z logo/ikoną)
  - Klikalny: redirect `/tickets` (home route dla zalogowanych)
- **Zakładki nawigacji** (center):
  - "Moje Zestawy" → `/tickets`
  - "Losowania" → `/draws`
  - "Sprawdź Wygrane" → `/checks`
  - Active state: zakładka odpowiadająca aktualnemu route highlighted (bold, underline, kolor primary)
- **User info + logout** (po prawej):
  - Email użytkownika: "jan.kowalski@example.com" (display only, opcjonalnie z dropdown menu w przyszłości)
  - Przycisk "Wyloguj" → onClick: logout() + redirect `/login`

**Responsywność:**
- **Desktop:** Horizontal navbar, wszystkie elementy widoczne inline
- **Mobile:**
  - Hamburger menu icon (☰) po prawej
  - Kliknięcie hamburger → drawer/slide-in menu z zakładkami vertically stacked
  - Logo/tytuł po lewej zawsze widoczny
  - Email i Wyloguj w drawer menu

**Accessibility:**
- Semantic HTML: `<nav>` z `<ul><li>` dla zakładek
- ARIA current: `aria-current="page"` dla aktywnej zakładki
- Keyboard navigation: Tab przez zakładki, Enter select
- Focus indicators na wszystkich klikanych elementach

### 4.3 Layout Component (Main Layout)

**Struktura:**
```jsx
<Layout>
  <Navbar /> {/* Widoczny tylko jeśli isLoggedIn */}
  <main className="container mx-auto px-4 py-8">
    {children} {/* Renderowane komponenty stron */}
  </main>
  <ToastContainer /> {/* Toast notifications overlay */}
</Layout>
```

**Main content area:**
- Container z max-width (np. 1200px) i auto-margin dla centrowania
- Padding responsywny: mniejszy na mobile, większy na desktop
- Background: neutralny (np. gray-50)

**Toast notifications:**
- Pozycjonowanie: top-right corner (fixed)
- Stack: multiple toasts stacked vertically
- Auto-dismiss: 3-4 sekundy
- Kolory: zielony (success), czerwony (error, opcjonalnie), żółty (warning, opcjonalnie)

---

## 5. Kluczowe komponenty

### 5.1 Shared Components (reusable w całej aplikacji)

**Lokalizacja:** `src/components/Shared/`

#### 5.1.1 Button

**Opis:** Standardowy button component z różnymi wariantami

**Warianty:**
- `primary` - niebieski, główne akcje (np. "Zapisz", "Zaloguj się")
- `secondary` - szary, drugorzędne akcje (np. "Anuluj")
- `danger` - czerwony, destrukcyjne akcje (np. "Usuń")

**Props:**
- `variant: 'primary' | 'secondary' | 'danger'`
- `onClick: () => void`
- `disabled?: boolean`
- `className?: string` (dla customizacji Tailwind)
- `children: ReactNode` (text lub ikony)

**Styling (Tailwind):**
```jsx
const variantClasses = {
  primary: 'bg-blue-600 hover:bg-blue-700 text-white',
  secondary: 'bg-gray-200 hover:bg-gray-300 text-gray-800',
  danger: 'bg-red-600 hover:bg-red-700 text-white'
}
```

**Accessibility:**
- Type="button" default (prevent form submit)
- Disabled state: `disabled={true}` + `aria-disabled="true"`
- Focus ring: `focus:ring-2 focus:ring-offset-2`

---

#### 5.1.2 Modal

**Opis:** Generyczny modal/dialog component z backdrop i focus trap

**Props:**
- `isOpen: boolean`
- `onClose: () => void`
- `title: string`
- `children: ReactNode` (modal content)
- `size?: 'sm' | 'md' | 'lg' | 'xl'` (max-width modalnego contentu)

**Funkcjonalności:**
- Backdrop: semi-transparent overlay, kliknięcie zamyka modal (onClick={onClose})
- Close button (X) w prawym górnym rogu
- Escape key zamyka modal (useEffect + event listener)
- Focus trap: Tab cyklicznie w obrębie modalnych elementów (nie wycieka na backdrop)
- Auto-focus na pierwszy input/button przy otwarciu

**Styling:**
- Overlay: fixed full-screen, `bg-black bg-opacity-50`, z-index 50
- Modal content: centered, white background, rounded corners, shadow-lg, padding
- Animation: fade-in przy otwarciu (Tailwind transition)

**Accessibility:**
- `role="dialog"`
- `aria-modal="true"`
- `aria-labelledby={titleId}` (title jako h2 z id)
- Focus management (focus na pierwszy element, return focus po zamknięciu)

---

#### 5.1.3 ErrorModal

**Opis:** Specjalizowany modal do wyświetlania błędów (lista błędów walidacji, błędy API)

**Props:**
- `isOpen: boolean`
- `onClose: () => void`
- `errors: string[] | string` (lista błędów lub pojedynczy string)

**Layout:**
- Tytuł: "Błąd"
- Content: Lista błędów (bullet points, czerwony tekst)
- Footer: Przycisk [Zamknij] (primary, onClick={onClose})

**Przykład użycia:**
```tsx
<ErrorModal
  isOpen={isErrorModalOpen}
  onClose={() => setIsErrorModalOpen(false)}
  errors={["Email jest już zajęty", "Hasła nie są identyczne"]}
/>
```

---

#### 5.1.4 Toast

**Opis:** Toast notification system dla komunikatów sukcesu (auto-dismiss)

**Props:**
- `message: string`
- `variant: 'success' | 'error' | 'warning'`
- `duration?: number` (domyślnie 3000ms)

**Funkcjonalności:**
- Auto-dismiss po `duration` ms
- Pozycjonowanie: top-right corner, fixed
- Stack: multiple toasts displayed vertically
- Animation: slide-in z prawej, fade-out przy zamknięciu

**Styling (Tailwind):**
```jsx
const variantClasses = {
  success: 'bg-green-600 text-white',
  error: 'bg-red-600 text-white',
  warning: 'bg-yellow-500 text-gray-900'
}
```

**Accessibility:**
- `role="alert"`
- `aria-live="polite"` (screen reader announces)

---

#### 5.1.5 NumberInput

**Opis:** Input type="number" z walidacją zakresu 1-49 i inline error display

**Props:**
- `label: string` (np. "Liczba 1")
- `value: number | ''`
- `onChange: (value: number | '') => void`
- `error?: string` (inline error message)
- `min?: number` (default 1)
- `max?: number` (default 49)
- `required?: boolean`

**Funkcjonalności:**
- Inline validation w czasie rzeczywistym (onChange):
  - Zakres 1-49 (lub custom min/max)
  - Wyświetlanie error message pod inputem (czerwony tekst)
- Visual feedback: border czerwony jeśli error, zielony jeśli valid (opcjonalnie)

**Styling:**
- Label: `text-sm font-medium text-gray-700`
- Input: `border border-gray-300 rounded px-3 py-2`
- Error state: `border-red-500`
- Valid state (opcjonalnie): `border-green-500`

**Accessibility:**
- `<label for={inputId}>`
- `aria-describedby={errorId}` jeśli error istnieje
- `aria-invalid={!!error}`

---

#### 5.1.6 DatePicker

**Opis:** Input type="date" z walidacją i labelem

**Props:**
- `label: string` (np. "Od:")
- `value: string` (format YYYY-MM-DD)
- `onChange: (value: string) => void`
- `error?: string`
- `min?: string` (min date)
- `max?: string` (max date, np. dzisiaj)
- `required?: boolean`

**Funkcjonalności:**
- Native HTML5 date picker
- Inline validation (onChange): sprawdzenie min/max constraints
- Error display pod inputem

**Accessibility:**
- Jak NumberInput (label, aria-describedby, aria-invalid)

---

#### 5.1.7 Pagination

**Opis:** Pagination controls dla Draws Page (Previous/Next + page numbers)

**Props:**
- `currentPage: number`
- `totalPages: number`
- `onPageChange: (page: number) => void`

**Layout:**
```
[« Poprzednia]  1  2  [3]  4  5  [Następna »]
```

**Funkcjonalności:**
- Previous button: disabled jeśli currentPage === 1
- Next button: disabled jeśli currentPage === totalPages
- Page numbers: max 5 widocznych (centered wokół currentPage)
- Current page: highlighted (bg-blue-600 text-white)
- Kliknięcie page number/button → wywołanie onPageChange(newPage)

**Responsywność:**
- Mobile: simplified (tylko Previous/Next + current page text, np. "3 / 12")
- Desktop: full (Previous + 5 page numbers + Next)

**Accessibility:**
- `role="navigation"`, `aria-label="Paginacja losowań"`
- Disabled buttons: `aria-disabled="true"`, `disabled={true}`
- Current page: `aria-current="page"`

---

#### 5.1.8 Spinner

**Opis:** Loading spinner (lokalny lub globalny)

**Props:**
- `size?: 'sm' | 'md' | 'lg'`
- `text?: string` (opcjonalny text pod spinnerem, np. "Weryfikuję wygrane...")

**Styling:**
- SVG spinner (animated rotate)
- Kolory: primary (niebieski)
- Sizes: sm (16px), md (32px), lg (48px)

**Accessibility:**
- `role="status"`
- `aria-live="polite"`
- `<span className="sr-only">Ładowanie...</span>` (screen reader only text)

---

#### 5.1.9 Layout

**Opis:** Main layout wrapper z Navbar i container

**Props:**
- `children: ReactNode`

**Struktura:**
```tsx
<div className="min-h-screen bg-gray-50">
  {isLoggedIn && <Navbar />}
  <main className="container mx-auto px-4 py-8">
    {children}
  </main>
  <ToastContainer />
</div>
```

---

### 5.2 Feature-Specific Components (specjalne dla poszczególnych widoków)

**Lokalizacja:** `src/components/Auth/`, `src/components/Tickets/`, `src/components/Draws/`, `src/components/Checks/`

#### 5.2.1 Tickets Components

**TicketList** - Lista zestawów użytkownika (wykorzystuje TicketItem)
- Props: `tickets: Ticket[]`, `onEdit: (id) => void`, `onDelete: (id) => void`

**TicketItem** - Pojedynczy zestaw w liście
- Props: `ticket: Ticket`, `onEdit: () => void`, `onDelete: () => void`
- Layout: Liczby | Data | [Edytuj] [Usuń]

**TicketForm** - Formularz dodawania/edycji zestawu (modal)
- Props: `mode: 'add' | 'edit'`, `initialValues?: number[]`, `onSubmit: (numbers) => void`, `onCancel: () => void`
- Wykorzystuje 6x NumberInput + inline validation

**GeneratorPreview** - Modal preview generatora (losowy lub systemowy)
- Props: `type: 'random' | 'system'`, `numbers: number[] | number[][]`, `onRegenerate: () => void`, `onSave: () => void`, `onCancel: () => void`
- Layout: Grid 3x3 dla systemowego (desktop), vertical list (mobile)

**TicketCounter** - Licznik zestawów z progresywną kolorystyką
- Props: `count: number`, `max: number` (default 100)
- Funkcja `getCounterColor(count)` dla kolorystyki

**ImportCsvModal** - Modal importu zestawów z pliku CSV (Feature Flag)
- Props: `isOpen: boolean`, `onClose: () => void`, `onImportSuccess: (report) => void`
- Wykorzystuje file input (accept=".csv,text/csv")
- Wyświetla raport importu po sukcesie (imported/rejected/errors)
- API call: `POST /api/tickets/import-csv` (multipart/form-data)

**ExportCsvButton** - Przycisk eksportu do CSV (Feature Flag)
- Props: `onExportSuccess: (count) => void`
- Bezpośrednie wywołanie API: `GET /api/tickets/export-csv`
- Automatyczne pobranie pliku przez przeglądarkę
- Toast notification po sukcesie

#### 5.2.2 Draws Components

**DrawsFilterPanel** - Panel filtrowania z date range picker
- Props: `onFilter: (dateFrom?, dateTo?) => void`, `onClearFilter: () => void`, `isFilterActive: boolean`
- Wykorzystuje 2x DateInput + Button ("Filtruj", "Wyczyść")
- Walidacja: dateFrom ≤ dateTo
- State: `dateFrom`, `dateTo` (local state)

**DrawList** - Lista losowań z paginacją (wykorzystuje DrawItem + Pagination)
- Props: `draws: Draw[]`, `isAdmin: boolean`, `onEdit: (id) => void`, `onDelete: (id) => void`, `pagination: { currentPage, totalPages, onPageChange }`, `filterInfo?: string`
- Wyświetla info o aktywnym filtrze jeśli `filterInfo` podane (np. "Filtr aktywny: 2025-10-01 - 2025-10-31")

**DrawItem** - Pojedyncze losowanie w liście
- Props: `draw: Draw`, `isAdmin: boolean`, `onEdit: () => void`, `onDelete: () => void`
- Layout:
  - **Header:** Data losowania | Typ losowania (badge) | DrawSystemId | Przyciski akcji [Edytuj] [Usuń] (admin)
  - **Liczby:** 6 kolorowych badges z liczbami
  - **Cena biletu:** "Cena biletu: X.XX zł" (jeśli dostępna)
  - **Sekcja wygranych** (jeśli dostępna):
    - Gradient tło (gray-50 do gray-100) z zaokrąglonymi rogami
    - Nagłówek "Informacje o wygranych" z kolorowym paskiem
    - Grid 4 kolorowych kart (responsywny):
      - Każda karta zawiera: kolorową ikonkę z liczbą trafień, ilość wygranych (duża czcionka), kwotę
      - Stopień 1: zielona karta (border green-300, bg-green-500 dla ikony)
      - Stopień 2: niebieska karta (border blue-300, bg-blue-500 dla ikony)
      - Stopień 3: żółta karta (border yellow-300, bg-yellow-500 dla ikony)
      - Stopień 4: pomarańczowa karta (border orange-300, bg-orange-500 dla ikony)

**DrawForm** - Formularz dodawania/edycji losowania (modal)
- Props: `mode: 'add' | 'edit'`, `initialValues?: { drawDate, lottoType, numbers, drawSystemId, ticketPrice?, winPoolCount1-4?, winPoolAmount1-4? }`, `onSubmit: (data) => void`, `onCancel: () => void`
- Wykorzystuje DatePicker + Dropdown (lottoType) + 6x NumberInput + NumberInput (drawSystemId) + opcjonalne pola win pool info

#### 5.2.3 Checks Components

**CheckPanel** - Panel weryfikacji z date range picker i przyciskiem submit
- Props: `onSubmit: (dateFrom, dateTo) => void`
- Wykorzystuje 2x DatePicker + Button

**CheckResults** - Accordion z wynikami weryfikacji
- Props: `results: VerificationResult[]`, `loading: boolean`
- Wykorzystuje AccordionItem dla każdego losowania

**AccordionItem** - Pojedyncze losowanie w accordion (rozwijane)
- Props: `draw: { drawDate, drawNumbers }`, `tickets: TicketMatch[]`, `defaultExpanded?: boolean`
- Layout: Header (kliknąlny) + Content (lista zestawów z highlighted liczbami + badges)

**ResultTicketItem** - Pojedynczy zestaw w wynikach weryfikacji
- Props: `ticket: { numbers, matchCount, matchedNumbers }`
- Funkcjonalność: Pogrubione matched numbers, badge wygranych dla ≥3

---

## 6. Integracja z API i Obsługa Błędów

### 6.1 ApiService Pattern

**Opis:** Centralized service class dla wszystkich API calls z automatycznym dodawaniem headers (Content-Type, X-TOKEN, Authorization)

**Lokalizacja:** `src/services/api-service.ts`

**Kluczowe metody:**
- `setAuthToken(token: string)` - ustawienie JWT tokenu
- `clearAuthToken()` - wyczyszczenie tokenu (przy wylogowaniu)
- `request(endpoint: string, options: RequestInit)` - prywatna metoda wykonująca fetch z error handling

**Error handling:**
- Rzucanie `ApiError` dla błędów API (status 4xx, 5xx)
- Rzucanie `NetworkError` dla problemów z połączeniem
- Try-catch w każdej metodzie publicznej

**Przykłady metod:**
- Auth: `register()`, `login()`
- Tickets: `getTickets()`, `createTicket()`, `updateTicket()`, `deleteTicket()`, `generateRandomTicket()`, `generateSystemTickets()`
- Tickets Import/Export: `importTicketsFromCsv(file)`, `exportTicketsToCsv()` (Feature Flag)
- Draws: `getDraws(page, pageSize, dateFrom?, dateTo?)`, `createDraw()`, `updateDraw()`, `deleteDraw()`
- XLotto: `xLottoActualDraws(date, lottoType)` - pobieranie wyników z XLotto.pl przez Gemini API
- XLotto: `xLottoIsEnabled()` - sprawdzenie Feature Flag (czy funkcja XLotto jest włączona)
- Verification: `checkWinnings(dateFrom, dateTo)`

### 6.2 Error Handling w Komponentach

**Pattern:**
```tsx
try {
  const response = await apiService.createTicket(numbers)
  showToast('Zestaw zapisany pomyślnie', 'success')
  refreshList()
} catch (error) {
  if (error instanceof ApiError) {
    if (error.status >= 400 && error.status < 500) {
      // 4xx: szczegółowy komunikat z backendu w ErrorModal
      showErrorModal(error.data.errors || error.data.error)
    } else {
      // 5xx: generyczny komunikat
      showErrorModal('Wystąpił problem z serwerem. Spróbuj ponownie za chwilę.')
    }
  } else if (error instanceof NetworkError) {
    // Network errors w ErrorModal
    showErrorModal(error.message)
  }

  // Specjalny przypadek: wygasły token (401)
  if (error.status === 401) {
    logout()
    navigate('/login')
    showErrorModal('Twoja sesja wygasła. Zaloguj się ponownie.')
  }
}
```

### 6.3 AppContext dla Autentykacji

**Opis:** React Context zarządzający stanem użytkownika (JWT token, email, isAdmin)

**Lokalizacja:** `src/context/app-context.tsx`

**Interface:**
```tsx
interface User {
  id: number
  email: string
  isAdmin: boolean
  token: string
  expiresAt: string
}

interface AppContextType {
  user: User | null
  isLoggedIn: boolean
  login: (userData: User) => void
  logout: () => void
  getApiService: () => ApiService
}
```

**Funkcjonalności:**
- Auto-restore user z localStorage przy mount
- Synchronizacja token z ApiService (setAuthToken/clearAuthToken)
- Metody login/logout aktualizują localStorage

**Użycie w komponentach:**
```tsx
const { user, isLoggedIn, login, logout, getApiService } = useAppContext()
const apiService = getApiService()

// Conditional rendering
{isLoggedIn ? <TicketsPage /> : <Navigate to="/login" />}
{user?.isAdmin && <Button>+ Dodaj wynik</Button>}
```

---

## 7. Responsywność, Dostępność i Bezpieczeństwo

### 7.1 Responsywność (NFR-019)

**Strategia:** Mobile-first CSS (Tailwind default breakpoints), desktop-first UX priorities

**Breakpoints (Tailwind CSS 4):**
- `sm: 640px` - Tablet portrait
- `md: 768px` - Tablet landscape
- `lg: 1024px` - Desktop
- `xl: 1280px` - Large desktop

**Adaptive Layouts (przykłady):**

**Navbar:**
- Mobile: Hamburger menu (☰), drawer slide-in z zakładkami vertical
- Desktop: Horizontal nav bar, wszystkie zakładki inline

**Formularze (6 number inputs):**
- Mobile: 1 kolumna (vertical stack)
- Desktop: 2 kolumny (3 inputs per row)

**Generator systemowy preview:**
- Mobile: Vertical list (9 cards stacked)
- Desktop: Grid 3x3

**Pagination:**
- Mobile: Simplified (tylko Previous/Next + current page text)
- Desktop: Full (Previous + 5 page numbers + Next)

**Touch targets:**
- Minimum 44x44px (WCAG 2.5.5)
- Buttons padding: `px-4 py-2` (minimum)

### 7.2 Dostępność (NFR-020, NFR-021, NFR-022)

**Semantic HTML:**
- `<nav>` dla nawigacji
- `<main>` dla głównej zawartości
- `<button>` dla interaktywnych elementów (nie `<div onClick>`)
- `<form>` z `<label for>` dla formularzy

**ARIA attributes:**
- `aria-label` dla buttonów z ikonami (np. 🎲 → "Generuj losowy zestaw")
- `aria-describedby` dla inline errors (połączenie error message z inputem)
- `aria-invalid={!!error}` dla inputs z błędami
- `aria-live="polite"` dla Toast notifications
- `aria-current="page"` dla aktywnej zakładki w navbar
- `aria-expanded` dla accordion items

**Keyboard Navigation:**
- Tab order logiczny (top to bottom, left to right)
- Enter/Space dla buttonów
- Escape zamyka modale
- Arrow keys w listach (opcjonalnie)

**Focus Management:**
- Widoczny focus indicator: `focus:ring-2 focus:ring-blue-500 focus:ring-offset-2`
- Focus trap w modalach (Tab cyklicznie w obrębie modal content)
- Auto-focus na pierwszy input w otwartym modalu
- Return focus do trigger element po zamknięciu modalu

**Komunikaty błędów (NFR-021):**
- **Język polski** (wszystkie komunikaty)
- Jasne i konkretne (np. "Liczby muszą być w zakresie 1-49", nie "Error 400")
- Powiązane z polami via `aria-describedby`

**Color Contrast:**
- WCAG AA minimum: 4.5:1 dla tekstu
- Nie polegać tylko na kolorze (ikony + text dla statusów, np. badges wygranych: emoji 🏆 + text "Wygrana 3")

### 7.3 Bezpieczeństwo na Poziomie UI

**Protected Routes:**
- `ProtectedRoute` component wrapper
- Redirect `/login` jeśli `!isLoggedIn`

**XSS Protection (NFR-009):**
- React domyślnie escapuje dane (safe by default)
- Unikać `dangerouslySetInnerHTML`

**Token Security:**
- JWT w localStorage (decyzja projektowa: prostsze dla MVP niż httpOnly cookies)
- Auto-restore przy odświeżeniu strony
- Clear token przy wylogowaniu
- Obsługa wygasłego tokenu (401): silent failure → ErrorModal + redirect `/login`

**HTTPS (NFR-007):**
- Wymagane w produkcji dla wszystkich połączeń
- Dev mode: HTTP acceptable

**Input Validation:**
- Frontend validation to tylko UX enhancement (główna walidacja na backendzie)
- Type attributes: `type="email"`, `type="number"`, `type="password"`
- Min/max constraints dla number inputs

---

## 8. Mapowanie Wymagań na Elementy UI (Szczegółowe)

### 8.1 Wymagania Funkcjonalne → UI Components

**F-AUTH-001: Rejestracja użytkownika**
- UI: Register Page (`/register`)
  - Input email (validation: format, unique)
  - Input hasło (validation: min 8 znaków, wielka litera, cyfra, znak specjalny)
  - Input potwierdzenie hasła (validation: identyczne z hasło)
  - Inline validation (real-time)
  - ErrorModal dla błędów przy submit
  - Auto-login po sukcesie + redirect `/tickets`

**F-AUTH-002: Logowanie użytkownika**
- UI: Login Page (`/login`)
  - Input email
  - Input hasło
  - ErrorModal dla 401 ("Nieprawidłowy email lub hasło")
  - Redirect `/tickets` po sukcesie

**F-AUTH-003: Wylogowanie użytkownika**
- UI: Navbar → Przycisk "Wyloguj"
  - onClick: logout() → clear localStorage → redirect `/login`

**F-AUTH-004: Izolacja danych użytkowników**
- UI: Protected Routes + Backend filtering
  - ProtectedRoute wrapper → redirect `/login` jeśli nie zalogowany
  - Backend filtruje dane po UserId z JWT

**F-DRAW-001: Dodawanie wyniku losowania**
- UI: Draws Page → Modal dodawania (admin only)
  - DatePicker (drawDate, walidacja: nie w przyszłości)
  - Dropdown/Radio buttons (lottoType: "LOTTO" lub "LOTTO PLUS")
  - **Przycisk "Pobierz z XLotto"** (warunkowo wyświetlany przez Feature Flag):
    - Sprawdzenie statusu: API call `GET /api/xlotto/is-enabled` przy otwarciu modalu
    - Widoczny tylko gdy `response.data === true`
    - Funkcjonalność: automatyczne pobieranie wyników z XLotto.pl przez Google Gemini API
  - NumberInput (drawSystemId, wymagane)
  - 6x NumberInput (1-49, unikalne, mogą być wypełnione automatycznie przez XLotto)
  - Opcjonalne pola: ticketPrice, winPoolCount1-4, winPoolAmount1-4
  - Inline validation + ErrorModal
  - Przyciski [Pobierz z XLotto (conditional)] [Wyczyść] + [Anuluj] [Zapisz]

**F-DRAW-002: Przeglądanie historii losowań**
- UI: Draws Page (`/draws`)
  - DrawsFilterPanel: Date range picker (Od/Do) + Button "Filtruj" + Button "Wyczyść"
  - Lista losowań: Data | Liczby | Data wprowadzenia
  - Info o aktywnym filtrze: "Filtr aktywny: 2025-10-01 - 2025-10-31" (jeśli filtr zastosowany)
  - Paginacja (20/strona domyślnie): Pagination component
  - Sortowanie: drawDate desc (default backend)
  - API: `GET /api/draws?page=X&pageSize=20&dateFrom=YYYY-MM-DD&dateTo=YYYY-MM-DD`

**F-DRAW-004: Usuwanie wyniku losowania**
- UI: Draws Page → Modal potwierdzenia (admin)
  - "Czy na pewno chcesz usunąć losowanie z dnia [data]? [liczby]"
  - [Anuluj] [Usuń]

**F-DRAW-005: Edycja wyniku losowania**
- UI: Draws Page → Modal edycji (admin)
  - DrawForm z pre-wypełnionymi wartościami (drawDate, lottoType, numbers, drawSystemId, ticketPrice?, winPoolCount1-4?, winPoolAmount1-4?)
  - Inline validation + ErrorModal

**F-TICKET-001: Przeglądanie zestawów użytkownika**
- UI: Tickets Page (`/tickets`)
  - TicketList: Liczby | Data | [Edytuj] [Usuń]
  - TicketCounter: "X/100" z kolorystyką
  - Sortowanie: createdAt desc

**F-TICKET-002: Dodawanie zestawu ręcznie**
- UI: Tickets Page → Modal dodawania
  - 6x NumberInput (1-49, unikalne)
  - Inline validation
  - Walidacja limitu 100 → ErrorModal
  - Walidacja unikalności zestawu → ErrorModal

**F-TICKET-003: Edycja zestawu**
- UI: Tickets Page → Modal edycji
  - TicketForm z pre-wypełnionymi wartościami
  - Walidacja jak przy dodawaniu

**F-TICKET-004: Usuwanie zestawu**
- UI: Tickets Page → Modal potwierdzenia
  - "Czy na pewno chcesz usunąć zestaw? [liczby]"
  - [Anuluj] [Usuń]

**F-TICKET-005: Generator losowego zestawu**
- UI: Tickets Page → Modal preview generatora
  - GeneratorPreview (type='random')
  - 6 wylosowanych liczb
  - [Generuj ponownie] [Anuluj] [Zapisz]
  - Walidacja limitu przed zapisem

**F-TICKET-006: Generator systemowy (9 zestawów)**
- UI: Tickets Page → Modal preview generatora
  - GeneratorPreview (type='system')
  - Grid 3x3 (desktop) / vertical (mobile)
  - Tooltip wyjaśniający algorytm
  - Walidacja limitu (miejsce na 9)

**F-TICKET-007: Import zestawów z pliku CSV (Feature Flag)**
- UI: Tickets Page → Przycisk "📥 Importuj z CSV" (warunkowo wyświetlany)
  - ImportCsvModal
  - File input (accept=".csv,text/csv", max 1MB)
  - Wyjaśnienie formatu CSV
  - API: `POST /api/tickets/import-csv` (multipart/form-data)
  - Raport importu: imported/rejected/errors
  - ErrorModal z raportem lub błędami
  - Toast "Import zakończony. Zaimportowano X zestawów."

**F-TICKET-008: Eksport zestawów do pliku CSV (Feature Flag)**
- UI: Tickets Page → Przycisk "📤 Eksportuj do CSV" (warunkowo wyświetlany)
  - ExportCsvButton
  - Bezpośrednie pobranie pliku (bez modalu)
  - API: `GET /api/tickets/export-csv`
  - Format: `lotto-tickets-{userId}-{YYYY-MM-DD}.csv`
  - Toast "Wyeksportowano X zestawów do pliku CSV"

**F-VERIFY-001: Weryfikacja wygranych w przedziale dat**
- UI: Checks Page (`/checks`)
  - CheckPanel: Date range picker (default -31 dni) + Button "Sprawdź wygrane"
  - Lokalny Spinner (loading state)
  - CheckResults: Accordion z wynikami

**F-VERIFY-002: Prezentacja wyników weryfikacji**
- UI: Checks Page → AccordionItem dla każdego losowania
  - Header: Data + wygrane liczby
  - Content: Lista zestawów z pogrubionymi matched numbers
  - Badges wygranych (≥3 trafień): 🏆 "Wygrana 3 (trójka)"

### 8.2 Wymagania Niefunkcjonalne → UI Design

**NFR-019: Interfejs responsywny**
- Tailwind CSS 4, mobile-first breakpoints
- Adaptive layouts (Navbar, formularze, generator systemowy, pagination)
- Touch targets min 44x44px

**NFR-020: Wsparcie przeglądarek**
- React 19, nowoczesne API (fetch, ES6+)
- Target: Chrome, Firefox, Safari, Edge (latest)

**NFR-021: Komunikaty błędów w języku polskim, jasne i zrozumiałe**
- ErrorModal z user-friendly komunikatami po polsku
- Przykłady: "Liczby muszą być w zakresie 1-49", "Email jest już zajęty"

**NFR-022: Formularz z walidacją w czasie rzeczywistym (inline errors)**
- NumberInput, DatePicker z inline validation
- Błędy wyświetlane pod polami (czerwony tekst)
- ErrorModal przy submit (wszystkie błędy zbierane)

---

## 9. Podsumowanie i Następne Kroki

### 9.1 Kluczowe Cechy Architektury UI

1. **6 widoków (routes):** Landing, Login, Register, Tickets, Draws, Checks (BRAK Dashboard)
2. **Język polski** we wszystkich elementach UI
3. **Context API** dla autentykacji, lokalny stan dla danych biznesowych
4. **Modalne interakcje** (edycja, preview, potwierdzenia)
5. **ErrorModal dla wszystkich błędów** (decyzja użytkownika)
6. **Paginacja selektywna:** tylko Draws (100/strona)
7. **Progresywna kolorystyka licznika** zestawów (zielony/żółty/czerwony)
8. **Accordion dla wyników weryfikacji** (czytelność przy wielu losowaniach)
9. **Conditional rendering** dla admin features (role-based UI)
10. **Automatyczne pobieranie wyników z XLotto.pl** (admin) - przycisk "Pobierz z XLotto" wykorzystujący Google Gemini API
11. **Import/eksport zestawów CSV** (Feature Flag) - przyciski "📥 Importuj z CSV" i "📤 Eksportuj do CSV" dla masowego zarządzania zestawami
12. **Mobile-first CSS, desktop-first UX**

### 9.2 Gotowość do Implementacji

Dokument zawiera wszystkie niezbędne informacje do rozpoczęcia implementacji:
- ✅ Szczegółowa specyfikacja wszystkich 6 widoków
- ✅ Mapowanie User Stories z PRD na elementy UI
- ✅ Komponenty shared i feature-specific
- ✅ Integracja z API (ApiService pattern)
- ✅ Error handling strategy
- ✅ Responsywność, dostępność, bezpieczeństwo
- ✅ Kluczowe interakcje użytkownika (user journeys)

### 9.3 Priorytety Implementacji (sugerowane fazy)

**Faza 1: Fundament (3 dni)**
- Setup React 19 + Vite 7 + TypeScript + Tailwind CSS 4
- Routing (React Router 7) - 6 routes
- AppContext + ApiService
- Layout + Navbar
- Shared components: Button, Modal, ErrorModal, Toast, Spinner

**Faza 2: Autentykacja (2 dni)**
- Landing Page, Login Page, Register Page
- Protected Route component
- Integracja z API Auth

**Faza 3: Moduł Zestawów (4 dni)**
- Tickets Page (lista, CRUD, generatory)
- NumberInput component
- TicketForm, GeneratorPreview, TicketCounter
- Integracja z API Tickets

**Faza 4: Moduł Losowań (3 dni)**
- Draws Page (lista z paginacją, CRUD admin)
- DatePicker, Pagination components
- DrawForm, DrawList, DrawItem
- Integracja z API Draws

**Faza 5: Moduł Weryfikacji (3 dni)**
- Checks Page (date range picker, accordion wyników)
- CheckPanel, CheckResults, AccordionItem
- Highlight wygranych liczb, badges
- Integracja z API Verification

**Faza 6: Finalizacja (2 dni)**
- Responsywność (testowanie mobile/tablet)
- Accessibility audit
- Error boundary
- Polish translation audit
- Final QA

**Łączny czas:** ~17 dni roboczych (3+ tygodnie)

---

**Koniec dokumentu Architektura UI dla LottoTM MVP**
