# Podsumowanie Planowania Architektury UI - LottoTM MVP

**Data:** 2025-11-09
**Wersja:** 2.0 (Finalna)
**Podstawa:** PRD v1.3, tech-stack.md, api-plan.md v1.0

---

## 1. Decyzje Podjęte Przez Użytkownika

### 1.1 Struktura Nawigacji i Routing

1. **Formularz logowania i rejestracji:** Osobne dedykowane podstrony (`/login`, `/register`)
2. **Strona główna (`/`):** Landing page dla niezalogowanych użytkowników zawierający:
   - Tytuł aplikacji
   - Logo
   - Krótki opis systemu
   - Przyciski "Zaloguj się" i "Zarejestruj się"
3. **Przekierowanie po logowaniu:** Automatyczne przekierowanie na `/tickets` (nie `/dashboard`)
4. **Nawigacja główna:** Globalny navbar widoczny po zalogowaniu z zakładkami:
   - Moje Zestawy
   - Losowania
   - Sprawdź Wygrane
   - [Email użytkownika]
   - Wyloguj
   - **UWAGA:** Brak zakładki "Dashboard" - zrezygnowano z tej funkcjonalności w MVP

### 1.2 Formularze i Interakcje

5. **Generator losowy/systemowy:** Tryb dwuetapowy z preview przed zapisem (użytkownik widzi wygenerowane zestawy, może "Generuj ponownie" lub "Zapisz")
6. **Edycja zestawu/losowania:** Modal z formularzem edycji (nie dedykowana podstrona)
7. **Przycisk "Wyczyść" w formularzu:** Dodać przycisk "Wyczyść wszystkie pola" po lewej stronie formularza
8. **Modal potwierdzenia usunięcia:** Pokazywać szczegóły usuwanego elementu (liczby lub data + liczby)

### 1.3 Walidacja i Komunikaty

9. **Walidacja inline:** Komunikaty błędów w czasie rzeczywistym pod polami numerycznymi
10. **Komunikaty walidacyjne:** **Wszystkie błędy (w tym inline validation) w modalu w MVP**
    - Walidacja w czasie rzeczywistym pokazuje błędy inline
    - Po kliknięciu "Zapisz" wszystkie błędy walidacji są wyświetlane w modalu
    - Dotyczy błędów pól (zakres 1-49, unikalność) oraz błędów biznesowych (limit, duplikat zestawu)

### 1.4 Prezentacja Danych

11. **Lista wyników weryfikacji:** Struktura accordion - każde losowanie jako rozwijalna sekcja
12. **Domyślny zakres dat weryfikacji:** Ostatnie 31 dni (pre-wypełniony date range picker)
13. **Przycisk "Dodaj losowanie" (admin):** Widoczny tylko na stronie `/draws`
14. **Loading state podczas weryfikacji:** Spinner tylko w obszarze wyników (lokalny loading state, nie overlay blokujący całą aplikację)
15. **Lista zestawów `/tickets`:** Tylko domyślne sortowanie po dacie utworzenia malejąco (bez dodatkowych opcji sortowania/filtrowania, bez paginacji - max 100 zestawów)
16. **Licznik zestawów "X/100":** Progresywna kolorystyka:
    - Zielony: 0-70 zestawów
    - Żółty/Pomarańczowy: 71-90 zestawów
    - Czerwony: 91-100 zestawów
    - Toast ostrzegawczy gdy limit >95

### 1.5 Dostęp i Uprawnienia

17. **Strona `/draws`:** Dostępna dla wszystkich użytkowników (read-only), przyciski "Dodaj", "Edytuj", "Usuń" widoczne tylko dla adminów (`user.isAdmin === true`)

### 1.6 Architektura Danych

18. **Generator systemowy preview:** Wszystkie 9 zestawów na jednym ekranie (grid 3x3 na desktop, vertical list na mobile)
19. **AppContext:** Przechowuje tylko dane autentykacji:
    - `user` (id, email, isAdmin, token, expiresAt)
    - `isLoggedIn`
    - `login()`, `logout()`, `getApiService()`
    - Dane biznesowe (ticketCount, lista zestawów) pobierane na poziomie komponentów

### 1.7 Obsługa Błędów i Feedback

20. **Błędy API:** Hybrydowy approach:
    - Błędy 4xx (400, 401, 403, 404): szczegółowy komunikat z backendu w modalu
    - Błędy 5xx i network errors: generyczny user-friendly komunikat w modalu
21. **Feedback po zapisie:** Toast notification (zielony, auto-dismiss 3-4s) + odświeżenie listy bez przekierowania

### 1.8 Rozwiązane Kwestie Dodatkowe (z sekcji 4)

22. **Dashboard:** **Zrezygnowano całkowicie** - brak strony `/dashboard` i brak zakładki w navbar. Po logowaniu redirect bezpośrednio na `/tickets`
23. **Paginacja:**
    - **Tickets (`/tickets`):** Bez paginacji (max 100 zestawów, scrollowanie)
    - **Draws (`/draws`):** Classic pagination po 100 elementów na stronę (Previous/Next + numery stron)
24. **Wygasły token JWT:** Silent failure - następne API call → 401 → modal "Sesja wygasła, zaloguj się ponownie" → redirect `/login`
25. **Offline/Network errors:** Modal z error message "Wystąpił problem z połączeniem. Sprawdź internet i spróbuj ponownie"
26. **Mobile vs Desktop:** Mobile-first CSS (Tailwind default breakpoints), desktop-first UX priorities (design decisions dla desktop use cases)
27. **Język aplikacji:** Cała aplikacja w języku polskim (UI labels, przyciski, komunikaty, placeholdery)
28. **Lazy loading:** Brak lazy loading w MVP - wszystkie komponenty importowane normalnie

---

## 2. Dopasowane Rekomendacje

### 2.1 Architektura Nawigacji
- Separate routes dla logowania i rejestracji zamiast modali
- Landing page jako publiczny entry point
- Globalny persistent navbar dla zalogowanych użytkowników
- Przekierowanie na `/tickets` jako główny widok roboczy
- **Uproszczenie:** Rezygnacja z dashboard zwiększa focus na core functionality

### 2.2 User Experience
- Preview przed zapisem dla generatorów (kontrola użytkownika)
- Modalne formularze edycji (zachowanie kontekstu)
- Szczegółowe potwierdzenia przed destrukcyjnymi akcjami
- Walidacja inline w czasie rzeczywistym + modalne podsumowanie błędów przy submit

### 2.3 Prezentacja Danych
- Accordion dla wyników weryfikacji (czytelność przy wielu losowaniach)
- Domyślne zakresy dat (redukcja friction)
- Progresywna kolorystyka licznika (proaktywna komunikacja)
- Lokalny loading state (non-blocking UX)
- **Paginacja dla Draws:** Zapobiega problemom wydajnościowym przy rosnącej liczbie losowań

### 2.4 Bezpieczeństwo i Dostęp
- Role-based UI (conditional rendering dla adminów)
- Transparentność danych (publiczny dostęp do listy losowań)
- Separation of concerns (auth w context, dane biznesowe w komponentach)
- **Silent failure tokenu:** Prosty mechanizm bez dodatkowej complexity w MVP

### 2.5 Obsługa Błędów
- Inteligentne rozróżnienie typów błędów (4xx vs 5xx)
- User-friendly komunikaty dla błędów technicznych
- Toast notifications dla success feedback
- **Modalne error messages** dla wszystkich błędów (decyzja użytkownika)

### 2.6 Internacjonalizacja i Lokalizacja
- **Język polski** dla całej aplikacji (target: polscy gracze LOTTO)
- Przyszłość: i18n jako post-MVP enhancement

---

## 3. Szczegółowe Podsumowanie Architektury UI

### 3.1 Struktura Widoków i Routing

```
/                           - Landing page (public)
├── /login                  - Formularz logowania (public)
├── /register               - Formularz rejestracji (public)
│
└── (authenticated routes)
    ├── /tickets            - Lista zestawów użytkownika (domyślny po loginie, bez paginacji)
    ├── /draws              - Lista losowań (read-only dla users, full access dla adminów, Z PAGINACJĄ)
    └── /checks             - Weryfikacja wygranych
```

**UWAGA:** Zrezygnowano z `/dashboard` w MVP. Navbar nie zawiera zakładki "Dashboard".

### 3.2 Kluczowe Komponenty i Przepływy Użytkownika

#### 3.2.1 Moduł Autentykacji

**Landing Page (`/`)**
- Layout: Centered content z logo, tytuł, krótki opis (2-3 zdania) po polsku
- CTA: Dwa prominent buttony "Zaloguj się" (primary) i "Zarejestruj się" (secondary)
- Responsywność: Single column na mobile, centered na desktop

**Login Page (`/login`)**
- Formularz:
  - Email input (type="email", required, label "Email")
  - Password input (type="password", required, label "Hasło")
  - Inline validation errors pod każdym polem
  - Submit button "Zaloguj się"
  - Link "Nie masz konta? Zarejestruj się" → `/register`
- Po sukcesie: Redirect → `/tickets`
- Błędy: Modal z error message dla 401 (np. "Nieprawidłowy email lub hasło")

**Register Page (`/register`)**
- Formularz:
  - Email input (walidacja formatu, unikalność, label "Email")
  - Password input (min. 8 znaków, wymagania: wielka litera, cyfra, znak specjalny, label "Hasło")
  - Confirm Password input (musi być identyczne z password, label "Potwierdź hasło")
  - Inline validation w czasie rzeczywistym
  - Submit button "Zarejestruj się"
  - Link "Masz już konto? Zaloguj się" → `/login`
- Po sukcesie: Automatyczne logowanie + redirect → `/tickets`
- Błędy przy submit: Modal z listą wszystkich błędów (np. "Email jest już zajęty", "Hasła nie są identyczne")

#### 3.2.2 Moduł Zestawów (`/tickets`)

**Layout:**
```
┌─────────────────────────────────────────────────────┐
│ Navbar: Moje Zestawy | Losowania | ... | Wyloguj    │
├─────────────────────────────────────────────────────┤
│ Nagłówek: "Moje zestawy"                            │
│ Licznik: [42/100] (z progresywną kolorystyką)       │
│                                                      │
│ [+ Dodaj ręcznie] [🎲 Generuj losowy] [🔢 System]   │
│                                                      │
│ ┌───────────────────────────────────────────────┐   │
│ │ Zestaw #1: [3, 12, 25, 31, 42, 48]           │   │
│ │ Utworzono: 2025-10-15 14:30                  │   │
│ │                         [Edytuj] [Usuń]       │   │
│ └───────────────────────────────────────────────┘   │
│ │ ... (scrollowanie, max 100 zestawów)         │   │
│ └───────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────┘
```

**Funkcjonalności:**

1. **Dodawanie zestawu ręcznie:**
   - Przycisk "+ Dodaj ręcznie" otwiera modal
   - Modal zawiera:
     - 6 pól numerycznych (type="number", min="1", max="49", labels "Liczba 1" do "Liczba 6")
     - Inline validation errors pod każdym polem w czasie rzeczywistym
     - Przyciski: [Wyczyść] (left) | [Anuluj] [Zapisz] (right)
   - Walidacja:
     - Inline: Zakres 1-49, unikalność liczb w zestawie
     - Po kliknięciu "Zapisz": Modal z listą błędów jeśli walidacja nie przeszła
   - Po zapisie: Toast "Zestaw zapisany pomyślnie" + modal zamyka się + lista odświeża

2. **Generator losowy:**
   - Przycisk "🎲 Generuj losowy" wywołuje `POST /api/tickets/generate-random`
   - Preview w modalu: wyświetlenie 6 wylosowanych liczb
   - Przyciski: [Generuj ponownie] [Anuluj] [Zapisz]
   - Po zapisie: jak wyżej

3. **Generator systemowy (9 zestawów):**
   - Przycisk "🔢 Generuj systemowy" wywołuje `POST /api/tickets/generate-system`
   - Preview w modalu (fullscreen):
     - Grid 3x3 (desktop) lub vertical list (mobile)
     - Wszystkie 9 zestawów widoczne jednocześnie
     - Tooltip/wyjaśnienie algorytmu po polsku
   - Przyciski: [Generuj ponownie] [Anuluj] [Zapisz wszystkie]
   - Walidacja limitu: sprawdzenie czy user ma miejsce na 9 zestawów
   - Błąd limitu: Modal z error message "Brak miejsca na 9 zestawów. Dostępne: X zestawy."

4. **Edycja zestawu:**
   - Przycisk "Edytuj" otwiera modal identyczny jak "Dodaj ręcznie"
   - Pola pre-wypełnione aktualnymi wartościami
   - Walidacja unikalności pomija edytowany zestaw
   - Po zapisie: Toast "Zestaw zaktualizowany pomyślnie" + odświeżenie listy

5. **Usuwanie zestawu:**
   - Przycisk "Usuń" otwiera modal potwierdzenia
   - Modal: "Czy na pewno chcesz usunąć ten zestaw? [3, 12, 25, 31, 42, 48]"
   - Przyciski: [Anuluj] [Usuń] (danger variant)
   - Po usunięciu: Toast "Zestaw usunięty pomyślnie" + odświeżenie listy

**Licznik zestawów - progresywna kolorystyka:**
```javascript
const getCounterColor = (count) => {
  if (count <= 70) return 'text-green-600'
  if (count <= 90) return 'text-yellow-600'
  return 'text-red-600'
}

// Toast ostrzegawczy przy count > 95:
if (count > 95) {
  showToast(`Uwaga: Pozostało tylko ${100 - count} wolnych miejsc`, 'warning')
}
```

**Brak paginacji:** Lista scrollowalna, max 100 zestawów (limit backendu).

#### 3.2.3 Moduł Losowań (`/draws`)

**Layout:**
```
┌─────────────────────────────────────────────────────┐
│ Navbar: Moje Zestawy | Losowania | ...              │
├─────────────────────────────────────────────────────┤
│ Nagłówek: "Historia losowań"                        │
│                           [+ Dodaj wynik] (admin)    │
│                                                      │
│ ┌───────────────────────────────────────────────┐   │
│ │ 2025-10-30: [3, 12, 25, 31, 42, 48]          │   │
│ │ Wprowadzono: 2025-10-30 18:35                │   │
│ │                 [Edytuj] [Usuń] (admin only)  │   │
│ └───────────────────────────────────────────────┘   │
│ │ ... (więcej losowań)                          │   │
│ └───────────────────────────────────────────────┘   │
│                                                      │
│ ┌─────────────────────────────────────────────┐     │
│ │  [<< Poprzednia]  1 2 [3] 4 5  [Następna >>] │     │
│ │  Strona 3 z 5 (245 losowań)                  │     │
│ └─────────────────────────────────────────────┘     │
└─────────────────────────────────────────────────────┘
```

**Paginacja:**
- Classic pagination: 100 elementów na stronę
- Kontrolki: Previous/Next buttons + numery stron (max 5 widocznych numerów)
- Informacja: "Strona X z Y (Z losowań)"
- API call: `GET /api/draws?page=3&pageSize=100&sortBy=drawDate&sortOrder=desc`

**Dostęp:**
- Strona dostępna dla wszystkich użytkowników (read-only)
- Przyciski "+ Dodaj wynik", "Edytuj", "Usuń" renderowane warunkowo:
  ```javascript
  {user.isAdmin && <Button>+ Dodaj wynik</Button>}
  {user.isAdmin && <Button>Edytuj</Button>}
  {user.isAdmin && <Button>Usuń</Button>}
  ```

**Funkcjonalności (admin only):**

1. **Dodawanie wyniku losowania:**
   - Przycisk "+ Dodaj wynik" otwiera modal
   - Formularz:
     - Date picker (drawDate, label "Data losowania", nie może być w przyszłości)
     - 6 pól numerycznych (1-49, unikalne, labels "Liczba 1" do "Liczba 6")
     - Inline validation
     - Przyciski: [Wyczyść] | [Anuluj] [Zapisz]
   - Logika backend: jeśli losowanie na daną datę istnieje → nadpisanie
   - Po zapisie: Toast "Wynik losowania zapisany pomyślnie" + odświeżenie listy

2. **Edycja wyniku:**
   - Modal jak przy dodawaniu, pola pre-wypełnione
   - Po zapisie: Toast "Wynik zaktualizowany pomyślnie"

3. **Usuwanie wyniku:**
   - Modal potwierdzenia: "Czy na pewno chcesz usunąć wynik losowania z dnia 2025-10-30? [3, 12, 25, 31, 42, 48]"
   - Po usunięciu: Toast "Wynik usunięty pomyślnie" + odświeżenie aktualnej strony

#### 3.2.4 Moduł Weryfikacji (`/checks`)

**Layout:**
```
┌─────────────────────────────────────────────────────┐
│ Navbar: ... | Sprawdź Wygrane | ...                 │
├─────────────────────────────────────────────────────┤
│ Nagłówek: "Sprawdź swoje wygrane"                   │
│                                                      │
│ Zakres dat:                                          │
│ Od: [2025-10-09] Do: [2025-11-09] (default: -31d)  │
│                                                      │
│                          [Sprawdź wygrane] (primary) │
│                                                      │
│ ─────────── WYNIKI ─────────────                    │
│ (Loading spinner tutaj podczas weryfikacji)         │
│                                                      │
│ ▼ Losowanie 2025-10-28: [12, 18, 25, 31, 40, 49]   │
│   ┌───────────────────────────────────────────┐     │
│   │ Zestaw #1: [3, 12, 19, 25, 31, 44]       │     │
│   │            [  **12**    **25** **31**  ]  │     │
│   │ 🏆 Wygrana 3 (trójka)                     │     │
│   └───────────────────────────────────────────┘     │
│   ┌───────────────────────────────────────────┐     │
│   │ Zestaw #2: [1, 2, 5, 6, 7, 8]            │     │
│   │ Brak trafień                              │     │
│   └───────────────────────────────────────────┘     │
│                                                      │
│ ▶ Losowanie 2025-10-15: [5, 11, 22, 33, 44, 49]    │
│   (collapsed)                                        │
└─────────────────────────────────────────────────────┘
```

**Funkcjonalności:**

1. **Date Range Picker:**
   - Domyślnie pre-wypełniony: dzisiaj - 31 dni → dzisiaj
   - Labels po polsku: "Od:", "Do:"
   - Walidacja: dateTo >= dateFrom, max 31 dni zakresu
   - Inline validation errors

2. **Przycisk "Sprawdź wygrane":**
   - Wywołuje `POST /api/verification/check`
   - Lokalny loading state: spinner w obszarze wyników
   - Pozostała część UI (navbar, date picker) pozostaje aktywna

3. **Prezentacja wyników - Accordion:**
   - Każde losowanie jako rozwijalna sekcja
   - Header: Data + wylosowane liczby
   - Expandable content: lista wszystkich zestawów użytkownika
   - Wygrane liczby: pogrubione (bold)
   - Etykiety wygranych (tylko dla ≥3 trafień):
     - 3 trafienia: 🏆 "Wygrana 3 (trójka)" - badge zielony
     - 4 trafienia: 🏆 "Wygrana 4 (czwórka)" - badge niebieski
     - 5 trafień: 🏆 "Wygrana 5 (piątka)" - badge pomarańczowy
     - 6 trafień: 🎉 "Wygrana 6 (szóstka)" - badge czerwony/złoty (główna wygrana)

4. **Performance:**
   - Wymaganie: ≤2s dla 100 zestawów × 1 losowanie
   - Backend: algorytm weryfikacji z LINQ Intersect (patrz api-plan.md linia 853-915)
   - Frontend: odbiór i renderowanie wyników bez dodatkowego przetwarzania

### 3.3 Integracja z API i Zarządzanie Stanem

#### 3.3.1 ApiService Pattern

**Konfiguracja (`src/services/api-service.ts`):**
```typescript
class ApiService {
  private baseUrl: string
  private appToken: string
  private authToken: string | null

  constructor(baseUrl: string, appToken: string) {
    this.baseUrl = baseUrl
    this.appToken = appToken
    this.authToken = null
  }

  setAuthToken(token: string) {
    this.authToken = token
  }

  clearAuthToken() {
    this.authToken = null
  }

  private async request(endpoint: string, options: RequestInit = {}) {
    const headers = {
      'Content-Type': 'application/json',
      'X-TOKEN': this.appToken,
      ...(this.authToken && { 'Authorization': `Bearer ${this.authToken}` }),
      ...options.headers,
    }

    try {
      const response = await fetch(`${this.baseUrl}${endpoint}`, {
        ...options,
        headers,
      })

      if (!response.ok) {
        const error = await response.json()
        throw new ApiError(response.status, error)
      }

      return await response.json()
    } catch (error) {
      if (error instanceof ApiError) throw error
      // Network error lub inne
      throw new NetworkError('Wystąpił problem z połączeniem. Sprawdź internet i spróbuj ponownie.')
    }
  }

  // Auth endpoints
  async register(email: string, password: string, confirmPassword: string) {
    return this.request('/api/auth/register', {
      method: 'POST',
      body: JSON.stringify({ email, password, confirmPassword }),
    })
  }

  async login(email: string, password: string) {
    return this.request('/api/auth/login', {
      method: 'POST',
      body: JSON.stringify({ email, password }),
    })
  }

  // Tickets endpoints
  async getTickets() {
    return this.request('/api/tickets')
  }

  async createTicket(numbers: number[]) {
    return this.request('/api/tickets', {
      method: 'POST',
      body: JSON.stringify({ numbers }),
    })
  }

  async updateTicket(id: string, numbers: number[]) {
    return this.request(`/api/tickets/${id}`, {
      method: 'PUT',
      body: JSON.stringify({ numbers }),
    })
  }

  async deleteTicket(id: string) {
    return this.request(`/api/tickets/${id}`, {
      method: 'DELETE',
    })
  }

  async generateRandomTicket() {
    return this.request('/api/tickets/generate-random', {
      method: 'POST',
    })
  }

  async generateSystemTickets() {
    return this.request('/api/tickets/generate-system', {
      method: 'POST',
    })
  }

  // Draws endpoints (Z PAGINACJĄ)
  async getDraws(page: number = 1, pageSize: number = 100) {
    return this.request(`/api/draws?page=${page}&pageSize=${pageSize}&sortBy=drawDate&sortOrder=desc`)
  }

  async createDraw(drawDate: string, numbers: number[]) {
    return this.request('/api/draws', {
      method: 'POST',
      body: JSON.stringify({ drawDate, numbers }),
    })
  }

  async updateDraw(id: number, drawDate: string, numbers: number[]) {
    return this.request(`/api/draws/${id}`, {
      method: 'PUT',
      body: JSON.stringify({ drawDate, numbers }),
    })
  }

  async deleteDraw(id: number) {
    return this.request(`/api/draws/${id}`, {
      method: 'DELETE',
    })
  }

  // Verification endpoint
  async checkWinnings(dateFrom: string, dateTo: string) {
    return this.request('/api/verification/check', {
      method: 'POST',
      body: JSON.stringify({ dateFrom, dateTo }),
    })
  }
}
```

**Error Handling:**
```typescript
class ApiError extends Error {
  constructor(public status: number, public data: any) {
    super(`API Error: ${status}`)
  }
}

class NetworkError extends Error {
  constructor(message: string) {
    super(message)
  }
}

// W komponentach:
try {
  const response = await apiService.createTicket(numbers)
  showToast('Zestaw zapisany pomyślnie', 'success')
} catch (error) {
  if (error instanceof ApiError) {
    // Wszystkie błędy API w modalu (decyzja użytkownika)
    if (error.status >= 400 && error.status < 500) {
      // 4xx: szczegółowy komunikat z backendu
      showErrorModal(error.data.errors || error.data.error)
    } else {
      // 5xx: generyczny komunikat
      showErrorModal('Wystąpił problem z serwerem. Spróbuj ponownie za chwilę.')
    }
  } else if (error instanceof NetworkError) {
    // Network errors również w modalu (decyzja 4.5)
    showErrorModal(error.message)
  }
}

// Obsługa wygasłego tokenu (401) - decyzja 4.4: Silent failure
if (error.status === 401) {
  logout()
  navigate('/login')
  showErrorModal('Twoja sesja wygasła. Zaloguj się ponownie.')
}
```

#### 3.3.2 AppContext - Zarządzanie Autentykacją

**Definicja (`src/context/app-context.ts`):**
```typescript
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

**Provider (`src/context/app-context-provider.tsx`):**
```typescript
export const AppContextProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<User | null>(() => {
    // Restore from localStorage on mount
    const stored = localStorage.getItem('user')
    return stored ? JSON.parse(stored) : null
  })

  const apiService = useMemo(() => {
    const service = new ApiService(
      import.meta.env.VITE_API_URL,
      import.meta.env.VITE_APP_TOKEN
    )
    if (user?.token) {
      service.setAuthToken(user.token)
    }
    return service
  }, [user?.token])

  const login = useCallback((userData: User) => {
    setUser(userData)
    localStorage.setItem('user', JSON.stringify(userData))
    apiService.setAuthToken(userData.token)
  }, [apiService])

  const logout = useCallback(() => {
    setUser(null)
    localStorage.removeItem('user')
    apiService.clearAuthToken()
  }, [apiService])

  const value = {
    user,
    isLoggedIn: !!user,
    login,
    logout,
    getApiService: () => apiService,
  }

  return <AppContext.Provider value={value}>{children}</AppContext.Provider>
}
```

**Użycie w komponentach:**
```typescript
const { user, isLoggedIn, login, logout, getApiService } = useAppContext()
const apiService = getApiService()

// Conditional rendering based on auth
{isLoggedIn ? <TicketsPage /> : <Navigate to="/login" />}

// Admin-only features
{user?.isAdmin && <Button>+ Dodaj wynik</Button>}
```

#### 3.3.3 Zarządzanie Stanem Komponentów

**Dane biznesowe pobierane lokalnie w komponentach:**

```typescript
// Example: DrawsPage (z paginacją)
const DrawsPage = () => {
  const { getApiService } = useAppContext()
  const apiService = getApiService()

  const [draws, setDraws] = useState<Draw[]>([])
  const [currentPage, setCurrentPage] = useState(1)
  const [totalPages, setTotalPages] = useState(1)
  const [totalCount, setTotalCount] = useState(0)
  const [loading, setLoading] = useState(true)

  const pageSize = 100

  useEffect(() => {
    const fetchDraws = async () => {
      try {
        setLoading(true)
        const response = await apiService.getDraws(currentPage, pageSize)
        setDraws(response.draws)
        setTotalCount(response.totalCount)
        setTotalPages(Math.ceil(response.totalCount / pageSize))
      } catch (error) {
        showErrorModal('Nie udało się pobrać listy losowań')
      } finally {
        setLoading(false)
      }
    }

    fetchDraws()
  }, [apiService, currentPage])

  const handlePageChange = (newPage: number) => {
    setCurrentPage(newPage)
  }

  // Render logic with pagination controls...
}
```

**Pattern: Optimistic Updates (opcjonalnie dla lepszego UX):**
```typescript
const handleDeleteTicket = async (id: string) => {
  // Optimistic update
  const previousTickets = [...tickets]
  setTickets(tickets.filter(t => t.id !== id))

  try {
    await apiService.deleteTicket(id)
    showToast('Zestaw usunięty pomyślnie', 'success')
  } catch (error) {
    // Rollback on error
    setTickets(previousTickets)
    showErrorModal('Nie udało się usunąć zestawu')
  }
}
```

### 3.4 Responsywność i Dostępność

#### 3.4.1 Responsywność (NFR-019)

**Breakpoints (Tailwind CSS 4):**
```css
/* Mobile first approach - CSS */
sm: 640px   /* Tablet portrait */
md: 768px   /* Tablet landscape */
lg: 1024px  /* Desktop */
xl: 1280px  /* Large desktop */
```

**Strategia (decyzja 4.6):**
- **CSS:** Mobile-first (Tailwind default) - zaczynamy od mobile, dodajemy media queries dla większych ekranów
- **UX Priorities:** Desktop-first - design decisions i optymalizacja dla desktop use cases (główny target wg PRD)

**Adaptive Layouts:**

1. **Navbar:**
   - Mobile: Hamburger menu (collapse links)
   - Desktop: Horizontal navigation bar

2. **Tickets List:**
   - Mobile: Vertical stack, full-width cards
   - Desktop: Grid 2 columns (opcjonalnie)

3. **Generator Systemowy Preview:**
   - Mobile: Vertical list (9 cards stacked)
   - Desktop: Grid 3x3

4. **Forms:**
   - Mobile: Single column (6 number inputs stacked)
   - Desktop: 2 columns (3 inputs per row)

5. **Date Range Picker:**
   - Mobile: Stacked inputs (Od i Do jeden pod drugim)
   - Desktop: Inline (Od | Do obok siebie)

6. **Pagination (Draws):**
   - Mobile: Simplified (tylko Previous/Next + current page)
   - Desktop: Full (Previous/Next + 5 page numbers)

**Touch targets:** Minimum 44x44px (zgodnie z WCAG 2.5.5)

#### 3.4.2 Dostępność (NFR-020, NFR-021)

**Kluczowe wymagania:**

1. **Semantic HTML:**
   - `<nav>` dla nawigacji
   - `<main>` dla głównej zawartości
   - `<button>` dla interaktywnych elementów
   - `<form>` z `<label>` dla formularzy

2. **ARIA attributes:**
   ```jsx
   <button aria-label="Usuń zestaw" onClick={handleDelete}>
     🗑️
   </button>

   <div role="alert" aria-live="assertive">
     {errorMessage}
   </div>
   ```

3. **Keyboard Navigation:**
   - Tab order logiczny
   - Enter/Space dla buttonów
   - Escape zamyka modale
   - Arrow keys w listach (opcjonalnie)

4. **Focus management:**
   - Widoczny focus indicator (Tailwind: `focus:ring-2 focus:ring-blue-500`)
   - Focus trap w modalach
   - Auto-focus na pierwszy input w otwartym modalu

5. **Komunikaty błędów (NFR-021, decyzja 4.7):**
   - Język polski
   - Jasne i konkretne (nie "Error 400", ale "Liczby muszą być w zakresie 1-49")
   - Powiązane z polami via `aria-describedby`

6. **Color contrast:**
   - WCAG AA minimum (4.5:1 dla tekstu)
   - Nie polegać tylko na kolorze (ikony + tekst dla statusów)

### 3.5 Bezpieczeństwo na Poziomie UI

#### 3.5.1 Ochrona Route'ów

**Protected Routes Pattern:**
```typescript
// src/components/ProtectedRoute.tsx
const ProtectedRoute: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const { isLoggedIn } = useAppContext()

  if (!isLoggedIn) {
    return <Navigate to="/login" replace />
  }

  return <>{children}</>
}

// W main.tsx:
<Route path="/tickets" element={
  <ProtectedRoute>
    <TicketsPage />
  </ProtectedRoute>
} />
```

**Brak AdminRoute:** Wszystkie funkcje admin są renderowane warunkowo w standardowych route'ach (np. `/draws`).

#### 3.5.2 XSS Protection (NFR-009)

**React domyślnie escapuje dane:**
```jsx
{/* Safe - React escapuje user input */}
<p>{userEmail}</p>

{/* UNSAFE - unikać dangerouslySetInnerHTML */}
<div dangerouslySetInnerHTML={{ __html: userInput }} />
```

**Walidacja inputów na frontendzie:**
- Type="number" dla pól numerycznych (1-49)
- Type="email" dla adresu email
- Type="password" dla hasła
- Inline validation + modal errors zapobiegają wysłaniu nieprawidłowych danych

#### 3.5.3 Token Security

**JWT w localStorage:**
- Token przechowywany w `localStorage.setItem('user', JSON.stringify(userData))`
- Auto-restore przy odświeżeniu strony
- Clear token przy wylogowaniu

**Obsługa wygasłego tokenu (decyzja 4.4 - Silent failure):**
```typescript
// W ApiService error handling:
if (error.status === 401) {
  // Token wygasł lub jest nieprawidłowy
  logout() // Wyczyść localStorage
  navigate('/login')
  showErrorModal('Twoja sesja wygasła. Zaloguj się ponownie.')
}
```

**BRAK proactive check w MVP:** Nie sprawdzamy `expiresAt` w useEffect. Token wygasa po 24h, następny API call zwraca 401, pokazujemy modal i redirect.

**HTTPS (NFR-007):**
- Wymagane w produkcji dla wszystkich połączeń
- Dev mode: HTTP acceptable

### 3.6 UI Component Library i Styling

#### 3.6.1 Tailwind CSS 4 Utility Classes

**Design System (przykładowe wartości):**

**Kolory:**
```css
/* Primary (brand) */
bg-blue-600, hover:bg-blue-700, text-blue-600

/* Success */
bg-green-600, text-green-600

/* Warning */
bg-yellow-500, text-yellow-600

/* Danger */
bg-red-600, hover:bg-red-700, text-red-600

/* Neutral */
bg-gray-100, text-gray-700, border-gray-300
```

**Typografia:**
```css
/* Headings */
text-3xl font-bold (h1)
text-2xl font-semibold (h2)
text-xl font-medium (h3)

/* Body */
text-base (16px default)
text-sm (14px secondary)
```

**Spacing:**
```css
/* Padding/Margin scale */
p-2 (8px), p-4 (16px), p-6 (24px), p-8 (32px)
```

**Shadows:**
```css
shadow-sm (subtle)
shadow-md (cards)
shadow-lg (modals)
```

#### 3.6.2 Reusable Components

**Shared Components Structure:**
```
src/components/
├── Shared/
│   ├── Button.tsx              // <Button variant="primary|secondary|danger" />
│   ├── Modal.tsx               // <Modal isOpen={} onClose={} title="" />
│   ├── ErrorModal.tsx          // Modal specjalny dla błędów (decyzja 4.1)
│   ├── Toast.tsx               // Toast notification system (success feedback)
│   ├── NumberInput.tsx         // Input type="number" 1-49 z walidacją
│   ├── DatePicker.tsx          // Date picker z walidacją
│   ├── Pagination.tsx          // Pagination controls (dla Draws)
│   ├── Spinner.tsx             // Loading spinner
│   ├── ErrorMessage.tsx        // Inline error display (deprecated w MVP - wszystko w modalu)
│   └── Layout.tsx              // Main layout z navbar
```

**Przykład: ErrorModal Component:**
```typescript
interface ErrorModalProps {
  isOpen: boolean
  onClose: () => void
  errors: string[] | string // Lista błędów lub pojedynczy string
}

const ErrorModal: React.FC<ErrorModalProps> = ({ isOpen, onClose, errors }) => {
  const errorList = Array.isArray(errors) ? errors : [errors]

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Błąd">
      <div className="space-y-2">
        {errorList.map((error, index) => (
          <p key={index} className="text-red-600">• {error}</p>
        ))}
      </div>
      <div className="mt-4 flex justify-end">
        <Button onClick={onClose}>Zamknij</Button>
      </div>
    </Modal>
  )
}
```

**Przykład: Pagination Component:**
```typescript
interface PaginationProps {
  currentPage: number
  totalPages: number
  onPageChange: (page: number) => void
}

const Pagination: React.FC<PaginationProps> = ({ currentPage, totalPages, onPageChange }) => {
  const renderPageNumbers = () => {
    const pages = []
    const maxVisible = 5

    let start = Math.max(1, currentPage - Math.floor(maxVisible / 2))
    let end = Math.min(totalPages, start + maxVisible - 1)

    if (end - start < maxVisible - 1) {
      start = Math.max(1, end - maxVisible + 1)
    }

    for (let i = start; i <= end; i++) {
      pages.push(
        <button
          key={i}
          onClick={() => onPageChange(i)}
          className={`px-3 py-1 rounded ${
            i === currentPage
              ? 'bg-blue-600 text-white'
              : 'bg-gray-200 hover:bg-gray-300'
          }`}
        >
          {i}
        </button>
      )
    }

    return pages
  }

  return (
    <div className="flex items-center justify-center gap-2">
      <Button
        disabled={currentPage === 1}
        onClick={() => onPageChange(currentPage - 1)}
      >
        &lt;&lt; Poprzednia
      </Button>

      {renderPageNumbers()}

      <Button
        disabled={currentPage === totalPages}
        onClick={() => onPageChange(currentPage + 1)}
      >
        Następna &gt;&gt;
      </Button>
    </div>
  )
}
```

### 3.7 Testing Strategy dla UI

#### 3.7.1 Testowanie Komponentów (opcjonalnie post-MVP)

**Narzędzia:**
- Jest + React Testing Library
- Vitest (alternatywa dla Vite)

**Priority test cases:**
1. **Formularze:** Walidacja inline, submit logic, modal errors
2. **Protected Routes:** Redirect dla niezalogowanych
3. **Conditional Rendering:** Admin vs user views
4. **API Integration:** Mock ApiService responses
5. **Pagination:** Page change logic

#### 3.7.2 E2E Testing (opcjonalnie post-MVP)

**Narzędzia:**
- Playwright lub Cypress

**Critical flows:**
1. Rejestracja → Logowanie → Dodanie zestawu → Weryfikacja
2. Generator systemowy → Preview → Zapis 9 zestawów
3. Admin: Dodanie losowania → Weryfikacja wygranych
4. Pagination: Nawigacja przez strony losowań

---

## 4. Wszystkie Kwestie Rozwiązane ✅

Wszystkie nierozwiązane kwestie z wersji 1.0 zostały uzgodnione:

### ✅ 4.1 Komunikaty Walidacyjne
**Decyzja:** Wszystkie błędy (w tym inline validation pól) wyświetlane w **modalu** po kliknięciu submit/zapisz.
- Inline validation pokazuje błędy w czasie rzeczywistym pod polami (visual feedback)
- Po kliknięciu "Zapisz" wszystkie błędy są zbierane i wyświetlane w ErrorModal

### ✅ 4.2 Dashboard
**Decyzja:** **Zrezygnowano całkowicie** z `/dashboard` w MVP.
- Brak route `/dashboard`
- Brak zakładki "Dashboard" w navbar
- Po logowaniu redirect bezpośrednio na `/tickets`

### ✅ 4.3 Paginacja
**Decyzja:**
- **Tickets:** Bez paginacji (max 100 zestawów, scrollowanie)
- **Draws:** Classic pagination po 100 elementów na stronę (Previous/Next + numery stron)

### ✅ 4.4 Wygasły Token JWT
**Decyzja:** Silent failure
- Brak proactive check `expiresAt`
- Następny API call po wygaśnięciu → 401 → modal "Sesja wygasła" → redirect `/login`

### ✅ 4.5 Offline/Network Errors
**Decyzja:** Modal z error message
- "Wystąpił problem z połączeniem. Sprawdź internet i spróbuj ponownie."
- Brak offline indicator w navbar
- Brak retry logic w MVP

### ✅ 4.6 Mobile vs Desktop
**Decyzja:** Mobile-first CSS (Tailwind), desktop-first UX priorities
- CSS używa mobile-first breakpoints (domyślne w Tailwind)
- Design decisions i UX optymalizacje dla desktop (główny target)

### ✅ 4.7 Język Aplikacji
**Decyzja:** Całość w języku polskim
- UI labels: "Moje zestawy", "Losowania", "Sprawdź wygrane"
- Przyciski: "Zapisz", "Anuluj", "Usuń", "Wyczyść"
- Komunikaty błędów: "Liczby muszą być w zakresie 1-49"
- Placeholdery: "1-49", "Email", "Hasło"

### ✅ 4.8 Lazy Loading
**Decyzja:** Brak lazy loading w MVP
- Wszystkie komponenty importowane normalnie (nie `React.lazy()`)
- Brak `Suspense` wrappers dla route'ów
- Prostsze do implementacji i debugowania
- Performance audit post-MVP

---

## 5. Next Steps - Implementacja

### 5.1 Priorytetyzacja Implementacji (Fazy)

**Faza 1: Fundament (3 dni)**
1. Setup projektu React 19 + Vite 7 + TypeScript
2. Konfiguracja Tailwind CSS 4
3. Implementacja AppContext (autentykacja)
4. Implementacja ApiService (base class z paginacją dla draws)
5. Routing setup (React Router 7) - TYLKO: /, /login, /register, /tickets, /draws, /checks
6. Layout component + Navbar (bez "Dashboard")

**Faza 2: Autentykacja (2 dni)**
7. Landing Page (`/`) - tytuł, logo, opis, przyciski po polsku
8. Login Page (`/login`) - labels po polsku
9. Register Page (`/register`) - labels po polsku, modal errors
10. Protected Route component
11. Integracja z API: `POST /api/auth/login`, `POST /api/auth/register`
12. ErrorModal component

**Faza 3: Moduł Zestawów (4 dni)**
13. Tickets Page (`/tickets`) - lista zestawów (bez paginacji)
14. Modal dodawania zestawu ręcznie (labels po polsku, ErrorModal dla błędów)
15. Modal edycji zestawu
16. Modal usuwania zestawu (confirmation z szczegółami)
17. Generator losowy (preview + zapis, ErrorModal dla błędu limitu)
18. Generator systemowy (9 zestawów, preview grid, ErrorModal)
19. NumberInput component z inline validation
20. Licznik zestawów z progresywną kolorystyką
21. Integracja z API: CRUD endpoints dla tickets

**Faza 4: Moduł Losowań (3 dni)**
22. Draws Page (`/draws`) - lista losowań Z PAGINACJĄ (100/strona)
23. Pagination component (Previous/Next + page numbers)
24. Admin-only: Modal dodawania wyniku losowania (labels po polsku)
25. Admin-only: Modal edycji wyniku
26. Admin-only: Modal usuwania wyniku (confirmation)
27. Conditional rendering (admin vs user)
28. Integracja z API: CRUD endpoints dla draws + pagination

**Faza 5: Moduł Weryfikacji (3 dni)**
29. Checks Page (`/checks`) - weryfikacja wygranych
30. Date Range Picker component (labels "Od:", "Do:" po polsku)
31. Accordion component dla wyników
32. Rendering wyników z pogrubionymi liczbami
33. Loading state (spinner w obszarze wyników)
34. Etykiety wygranych (badges po polsku: "Wygrana 3 (trójka)")
35. Integracja z API: `POST /api/verification/check`

**Faza 6: Finalizacja (2 dni)**
36. Toast notification system (tylko success feedback)
37. Shared components finalization (Button, Modal, ErrorModal, Pagination, Spinner, etc.)
38. Responsywność - testowanie na mobile/tablet
39. Accessibility audit (keyboard navigation, ARIA labels, polski język)
40. Error boundary implementation
41. 401 handling w ApiService (modal + redirect `/login`)

**Faza 7: Testing i Polish (2 dni)**
42. Manual QA dla wszystkich user flows
43. Bug fixing
44. Polish translation audit (wszystkie teksty po polsku)
45. Final deployment setup

**Łączny czas: ~17 dni roboczych (3+ tygodnie)**

### 5.2 Kluczowe Milestones

**M1 (Faza 1-2):** Użytkownik może się zarejestrować i zalogować (polski język) ✅
**M2 (Faza 3):** Użytkownik może zarządzać zestawami liczb (bez dashboard, ErrorModal) ✅
**M3 (Faza 4):** Admin może wprowadzać wyniki losowań z paginacją, użytkownicy je przeglądają ✅
**M4 (Faza 5):** Użytkownik może weryfikować wygrane (core functionality, polski język) ✅
**M5 (Faza 6-7):** Aplikacja gotowa do release (MVP complete) 🎯

---

## 6. Podsumowanie Kluczowych Decyzji Architektonicznych

1. **Routing:** React Router 7 - tylko 6 routes (/, /login, /register, /tickets, /draws, /checks) - BRAK /dashboard
2. **State Management:** Context API dla autentykacji, lokalny stan komponentów dla danych biznesowych
3. **API Integration:** Centralized ApiService class z error handling + paginacja dla draws
4. **UI Framework:** Tailwind CSS 4 utility-first, mobile-first CSS, desktop-first UX
5. **Modalne interakcje:** Edycja, usuwanie, preview w modalach (nie dedykowane routes)
6. **Walidacja:** Inline real-time validation + **ErrorModal dla WSZYSTKICH błędów po submit** (decyzja użytkownika)
7. **Język:** **Cała aplikacja w języku polskim** (labels, przyciski, komunikaty, placeholdery)
8. **Bezpieczeństwo:** Protected routes, conditional rendering dla adminów, token w localStorage, silent failure dla wygasłego tokenu
9. **Performance:** Brak lazy loading, lokalny loading state, paginacja tylko dla draws
10. **Accessibility:** Semantic HTML, ARIA attributes, keyboard navigation, WCAG AA contrast, polski język

---

**Dokument przygotowany:** 2025-11-09
**Ostatnia aktualizacja:** 2025-11-09
**Wersja:** 2.0 (Finalna)
**Status:** ✅ **GOTOWY DO IMPLEMENTACJI** - wszystkie kwestie rozwiązane
