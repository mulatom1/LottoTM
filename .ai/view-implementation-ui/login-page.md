# Plan implementacji widoku Login Page

## 1. Przegląd

Login Page to publiczny widok dostępny pod ścieżką `/login`, którego głównym celem jest uwierzytelnienie użytkownika w systemie LottoTM. Widok umożliwia użytkownikowi wprowadzenie danych logowania (email i hasło) oraz zalogowanie się do aplikacji. Po pomyślnym logowaniu użytkownik otrzymuje token JWT, który jest przechowywany w localStorage, a następnie zostaje przekierowany na stronę `/tickets`.

Widok jest zbudowany jako formularz z walidacją w czasie rzeczywistym (inline validation) oraz obsługą błędów API (wyświetlanych w ErrorModal). Całość jest responsywna, zaprojektowana z uwzględnieniem dostępności (WCAG AA) oraz bezpieczeństwa (HTTPS, maskowanie hasła).

## 2. Routing widoku

**Ścieżka:** `/login`

**Dostęp:** Publiczny (niezalogowani użytkownicy)

**Logika przekierowań:**
- Po pomyślnym logowaniu → redirect na `/tickets`
- Jeśli użytkownik już jest zalogowany (sprawdzenie `isLoggedIn` z AppContext) → redirect na `/tickets`
- Link do rejestracji → redirect na `/register`

## 3. Struktura komponentów

```
LoginPage (główny komponent strony)
├── Layout (opcjonalnie, jeśli używany globalnie)
│   └── main (container)
│       └── LoginForm (komponent formularza)
│           ├── Nagłówek h2: "Zaloguj się"
│           ├── EmailInput (komponent input z walidacją)
│           ├── PasswordInput (komponent input z walidacją)
│           ├── Button: "Zaloguj się" (submit)
│           └── Link: "Nie masz konta? Zarejestruj się"
└── ErrorModal (modal błędów API)
```

**Hierarchia komponentów:**
- **LoginPage** (kontener strony, zarządza stanem formularza i API calls)
  - **LoginForm** (formularz z inputami i przyciskiem submit)
    - **EmailInput** (pole email z inline validation)
    - **PasswordInput** (pole hasła z inline validation)
    - **Button** (przycisk submit)
    - **Link** (nawigacja do rejestracji)
  - **ErrorModal** (modal do wyświetlania błędów)

## 4. Szczegóły komponentów

### 4.1 LoginPage (główny komponent)

**Opis komponentu:**
Główny komponent strony logowania, który zarządza stanem formularza (email, hasło), walidacją, komunikacją z API oraz logiką przekierowań.

**Główne elementy HTML i komponenty dzieci:**
- `<main>` - kontener semantyczny dla treści strony
  - `<div>` - centered wrapper (max-width 400px, mx-auto)
    - `LoginForm` - komponent formularza logowania
    - `ErrorModal` - modal błędów (conditional rendering)

**Obsługiwane zdarzenia:**
- **onSubmit (handleSubmit):**
  - Walidacja danych (email format)
  - API call `POST /api/auth/login`
  - Obsługa sukcesu (login do AppContext, redirect `/tickets`)
  - Obsługa błędów (wyświetlenie ErrorModal z komunikatem)

- **onEmailChange (handleEmailChange):**
  - Aktualizacja stanu `email`
  - Inline validation formatu email (regex)

- **onPasswordChange (handlePasswordChange):**
  - Aktualizacja stanu `password`

**Warunki walidacji:**
- **Email:**
  - Wymagane: tak
  - Format: regex email `/^[^\s@]+@[^\s@]+\.[^\s@]+$/`
  - Komunikat błędu inline: "Nieprawidłowy format email"

- **Hasło:**
  - Wymagane: tak
  - Brak walidacji długości/siły przy logowaniu (tylko przy rejestracji)

- **Submit (API validation):**
  - 401 Unauthorized → ErrorModal: "Nieprawidłowy email lub hasło"
  - 5xx/Network Error → ErrorModal: "Wystąpił problem. Spróbuj ponownie."

**Typy wymagane przez komponent:**

```typescript
// Local state
interface LoginFormState {
  email: string
  password: string
  emailError: string
  isSubmitting: boolean
  isErrorModalOpen: boolean
  errorMessage: string
}

// API Request (z api-plan.md, linia 84-89)
interface LoginRequest {
  email: string
  password: string
}

// API Response (z api-plan.md, linia 94-102)
interface LoginResponse {
  token: string
  userId: number
  email: string
  isAdmin: boolean
  expiresAt: string
}
```

**Propsy przyjmowane od rodzica:**
Brak - komponent top-level (strona)

---

### 4.2 LoginForm (komponent formularza)

**Opis komponentu:**
Formularz zawierający pola email, hasło oraz przycisk submit. Wykorzystuje reusable komponenty (EmailInput, PasswordInput, Button).

**Główne elementy HTML i komponenty dzieci:**
- `<form>` (onSubmit={onSubmit})
  - `<h2>` - nagłówek "Zaloguj się"
  - `EmailInput` - pole email z inline validation
  - `PasswordInput` - pole hasła
  - `Button` - przycisk submit "Zaloguj się" (variant="primary")
  - `<p>` + `<Link>` - "Nie masz konta? Zarejestruj się"

**Obsługiwane zdarzenia:**
- **onSubmit:** wywołanie callbacku z LoginPage (handleSubmit)
- **onEmailChange:** wywołanie callbacku (handleEmailChange)
- **onPasswordChange:** wywołanie callbacku (handlePasswordChange)

**Warunki walidacji:**
Walidacja delegowana do komponentów EmailInput i PasswordInput (inline validation)

**Typy:**

```typescript
interface LoginFormProps {
  email: string
  password: string
  emailError: string
  isSubmitting: boolean
  onEmailChange: (value: string) => void
  onPasswordChange: (value: string) => void
  onSubmit: (e: React.FormEvent) => void
}
```

**Propsy:**
- `email` - wartość pola email
- `password` - wartość pola hasła
- `emailError` - komunikat błędu dla email (inline)
- `isSubmitting` - flaga blokująca submit (pokazuje loading state)
- `onEmailChange` - callback zmiany email
- `onPasswordChange` - callback zmiany hasła
- `onSubmit` - callback submitu formularza

---

### 4.3 EmailInput (reusable komponent)

**Opis komponentu:**
Pole input typu email z labelką, walidacją inline (format email) oraz wyświetlaniem błędów pod polem.

**Główne elementy:**
- `<div>` - wrapper
  - `<label for="email">` - "Email"
  - `<input id="email" type="email" />` - pole email
  - `<span className="error">` - komunikat błędu (conditional rendering jeśli `error` nie jest pusty)

**Obsługiwane zdarzenia:**
- **onChange:** wywołanie `onChange(e.target.value)`
- **onBlur:** inline validation (format email)

**Warunki walidacji:**
- Format email: `/^[^\s@]+@[^\s@]+\.[^\s@]+$/`
- Komunikat błędu: "Nieprawidłowy format email"
- Pole wymagane (HTML5 `required` attribute)

**Typy:**

```typescript
interface EmailInputProps {
  label: string
  value: string
  error?: string
  onChange: (value: string) => void
  required?: boolean
  placeholder?: string
  autoFocus?: boolean
}
```

**Propsy:**
- `label` - tekst labelki ("Email")
- `value` - aktualna wartość
- `error` - komunikat błędu (opcjonalny)
- `onChange` - callback zmiany wartości
- `required` - czy pole wymagane (default: true)
- `placeholder` - placeholder ("twoj@email.com")
- `autoFocus` - auto-focus przy otwarciu strony (default: true dla login page)

---

### 4.4 PasswordInput (reusable komponent)

**Opis komponentu:**
Pole input typu password z labelką, maskowaniem znaków oraz opcjonalnym wyświetlaniem błędów.

**Główne elementy:**
- `<div>` - wrapper
  - `<label for="password">` - "Hasło"
  - `<input id="password" type="password" />` - pole hasła (maskowane)
  - `<span className="error">` - komunikat błędu (conditional rendering)

**Obsługiwane zdarzenia:**
- **onChange:** wywołanie `onChange(e.target.value)`

**Warunki walidacji:**
Brak walidacji dla login page (tylko wymagane). Walidacja siły hasła tylko przy rejestracji.

**Typy:**

```typescript
interface PasswordInputProps {
  label: string
  value: string
  error?: string
  onChange: (value: string) => void
  required?: boolean
  placeholder?: string
}
```

**Propsy:**
- `label` - tekst labelki ("Hasło")
- `value` - aktualna wartość
- `error` - komunikat błędu (opcjonalny)
- `onChange` - callback zmiany wartości
- `required` - czy pole wymagane (default: true)
- `placeholder` - placeholder ("••••••••")

---

### 4.5 Button (reusable komponent)

**Opis komponentu:**
Standardowy przycisk z różnymi wariantami (primary, secondary, danger) i obsługą stanu disabled.

**Główne elementy:**
- `<button type="button|submit">` - element przycisku

**Obsługiwane zdarzenia:**
- **onClick:** wywołanie callbacku (jeśli `type="button"`)

**Typy:**

```typescript
interface ButtonProps {
  variant?: 'primary' | 'secondary' | 'danger'
  type?: 'button' | 'submit'
  onClick?: () => void
  disabled?: boolean
  className?: string
  children: React.ReactNode
}
```

**Propsy:**
- `variant` - wariant stylu (default: 'primary')
- `type` - typ przycisku (default: 'button')
- `onClick` - callback kliknięcia
- `disabled` - czy przycisk zablokowany
- `className` - dodatkowe klasy Tailwind
- `children` - zawartość przycisku (tekst lub ikony)

**Styling (Tailwind CSS):**
```typescript
const variantClasses = {
  primary: 'bg-blue-600 hover:bg-blue-700 text-white focus:ring-blue-500',
  secondary: 'bg-gray-200 hover:bg-gray-300 text-gray-800 focus:ring-gray-400',
  danger: 'bg-red-600 hover:bg-red-700 text-white focus:ring-red-500'
}

// Base classes dla wszystkich wariantów
const baseClasses = 'px-4 py-2 rounded font-medium focus:outline-none focus:ring-2 focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed transition-colors'
```

---

### 4.6 ErrorModal (reusable komponent)

**Opis komponentu:**
Modal wyświetlający błędy API lub walidacji. Zawiera tytuł "Błąd", listę błędów oraz przycisk "Zamknij".

**Główne elementy:**
- `<div>` - backdrop overlay (kliknięcie zamyka modal)
  - `<div>` - modal content (centered)
    - `<div>` - header
      - `<h2>` - "Błąd"
      - `<button>` - close button (X)
    - `<div>` - body
      - Lista błędów (bullet points)
    - `<div>` - footer
      - `Button` - "Zamknij" (onClick={onClose})

**Obsługiwane zdarzenia:**
- **onClose:** zamknięcie modalu
- **onBackdropClick:** zamknięcie modalu (kliknięcie w backdrop)
- **onEscape:** zamknięcie modalu (klawisz Escape)

**Typy:**

```typescript
interface ErrorModalProps {
  isOpen: boolean
  onClose: () => void
  errors: string[] | string
}
```

**Propsy:**
- `isOpen` - czy modal otwarty
- `onClose` - callback zamknięcia
- `errors` - lista błędów (string[] lub pojedynczy string)

---

## 5. Typy

### 5.1 DTO i Request/Response (z api-plan.md)

```typescript
// Request (linia 84-89)
interface LoginRequest {
  email: string
  password: string
}

// Response sukces (linia 94-102)
interface LoginResponse {
  token: string
  userId: number
  email: string
  isAdmin: boolean
  expiresAt: string // ISO 8601 datetime string
}

// Response błąd 401 (linia 109-113)
interface LoginErrorResponse {
  error: string // "Nieprawidłowy email lub hasło"
}
```

### 5.2 ViewModel i Local State

```typescript
// Stan formularza w LoginPage
interface LoginFormState {
  email: string
  password: string
  emailError: string
  isSubmitting: boolean
  isErrorModalOpen: boolean
  errorMessage: string
}

// User model (z AppContext)
interface User {
  id: number
  email: string
  isAdmin: boolean
  token: string
  expiresAt: string
}

// AppContext type (z ui-plan.md, linia 1146-1162)
interface AppContextType {
  user: User | null
  isLoggedIn: boolean
  login: (userData: User) => void
  logout: () => void
  getApiService: () => ApiService
}
```

### 5.3 Props interfaces (komponenty)

```typescript
// LoginPage - brak props (top-level component)

// LoginForm
interface LoginFormProps {
  email: string
  password: string
  emailError: string
  isSubmitting: boolean
  onEmailChange: (value: string) => void
  onPasswordChange: (value: string) => void
  onSubmit: (e: React.FormEvent) => void
}

// EmailInput
interface EmailInputProps {
  label: string
  value: string
  error?: string
  onChange: (value: string) => void
  required?: boolean
  placeholder?: string
  autoFocus?: boolean
}

// PasswordInput
interface PasswordInputProps {
  label: string
  value: string
  error?: string
  onChange: (value: string) => void
  required?: boolean
  placeholder?: string
}

// Button
interface ButtonProps {
  variant?: 'primary' | 'secondary' | 'danger'
  type?: 'button' | 'submit'
  onClick?: () => void
  disabled?: boolean
  className?: string
  children: React.ReactNode
}

// ErrorModal
interface ErrorModalProps {
  isOpen: boolean
  onClose: () => void
  errors: string[] | string
}
```

## 6. Zarządzanie stanem

### 6.1 Local State (LoginPage component)

**Stan zarządzany w komponencie LoginPage:**

```typescript
const [email, setEmail] = useState<string>('')
const [password, setPassword] = useState<string>('')
const [emailError, setEmailError] = useState<string>('')
const [isSubmitting, setIsSubmitting] = useState<boolean>(false)
const [isErrorModalOpen, setIsErrorModalOpen] = useState<boolean>(false)
const [errorMessage, setErrorMessage] = useState<string>('')
```

**Opis stanu:**
- `email` - wartość pola email (kontrolowany input)
- `password` - wartość pola hasła (kontrolowany input)
- `emailError` - komunikat błędu walidacji email (inline, wyświetlany pod polem)
- `isSubmitting` - flaga blokująca submit podczas API call (loading state)
- `isErrorModalOpen` - czy ErrorModal jest otwarty
- `errorMessage` - treść błędu wyświetlana w ErrorModal

**Mutacje stanu:**
- `setEmail` - aktualizacja email (onChange EmailInput)
- `setPassword` - aktualizacja hasła (onChange PasswordInput)
- `setEmailError` - ustawienie błędu email (inline validation)
- `setIsSubmitting` - blokada/odblokowanie submit
- `setIsErrorModalOpen` - otwarcie/zamknięcie ErrorModal
- `setErrorMessage` - ustawienie komunikatu błędu

### 6.2 Global State (AppContext)

**Dostęp do AppContext:**

```typescript
const { login, isLoggedIn, getApiService } = useAppContext()
const apiService = getApiService()
```

**Wykorzystywane metody:**
- `login(userData: User)` - zapisanie użytkownika w AppContext + localStorage po pomyślnym logowaniu
- `isLoggedIn` - sprawdzenie czy użytkownik jest zalogowany (redirect jeśli true)
- `getApiService()` - pobranie instancji ApiService z tokenem JWT

### 6.3 Czy wymagany custom hook?

**Nie.** Zarządzanie stanem formularza jest proste (kilka pól + flagi), więc wystarczy local state w komponencie LoginPage. Custom hook `useLoginForm` byłby overkill dla MVP.

W przyszłości (post-MVP) można rozważyć custom hook, jeśli logika formularza stanie się bardziej złożona (np. dodatkowe pola, wieloetapowa walidacja, retry logic).

## 7. Integracja API

### 7.1 Endpoint

**URL:** `POST /api/auth/login`

**Request payload:**
```json
{
  "email": "user@example.com",
  "password": "SecurePass123!"
}
```

**Response sukces (200 OK):**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "userId": 123,
  "email": "user@example.com",
  "isAdmin": false,
  "expiresAt": "2025-11-04T10:30:00Z"
}
```

**Response błąd (401 Unauthorized):**
```json
{
  "error": "Nieprawidłowy email lub hasło"
}
```

**Response błąd (5xx/Network):**
Generyczny komunikat: "Wystąpił problem. Spróbuj ponownie."

### 7.2 Typy Request/Response

**TypeScript interfaces (zgodne z api-plan.md):**

```typescript
// Request (linia 84-89)
interface LoginRequest {
  email: string
  password: string
}

// Response (linia 94-102)
interface LoginResponse {
  token: string
  userId: number
  email: string
  isAdmin: boolean
  expiresAt: string
}

// Error Response (linia 109-113)
interface LoginErrorResponse {
  error: string
}
```

### 7.3 Implementacja API call

**ApiService method:**

```typescript
class ApiService {
  // ... existing code

  async login(request: LoginRequest): Promise<LoginResponse> {
    const response = await fetch(`${this.baseUrl}/api/auth/login`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-TOKEN': this.appToken
      },
      body: JSON.stringify(request)
    })

    if (!response.ok) {
      if (response.status === 401) {
        const errorData: LoginErrorResponse = await response.json()
        throw new ApiError(401, errorData.error)
      }
      throw new ApiError(response.status, 'Wystąpił problem. Spróbuj ponownie.')
    }

    return await response.json()
  }
}
```

**Użycie w LoginPage:**

```typescript
const handleSubmit = async (e: React.FormEvent) => {
  e.preventDefault()

  // Inline validation
  if (!validateEmail(email)) {
    setEmailError('Nieprawidłowy format email')
    return
  }

  setIsSubmitting(true)
  setEmailError('')

  try {
    const response = await apiService.login({ email, password })

    // Sukces - login do AppContext
    login({
      id: response.userId,
      email: response.email,
      isAdmin: response.isAdmin,
      token: response.token,
      expiresAt: response.expiresAt
    })

    // Redirect do /tickets
    navigate('/tickets')

  } catch (error) {
    if (error instanceof ApiError) {
      setErrorMessage(error.message)
      setIsErrorModalOpen(true)
    } else {
      setErrorMessage('Wystąpił nieoczekiwany problem. Spróbuj ponownie.')
      setIsErrorModalOpen(true)
    }
  } finally {
    setIsSubmitting(false)
  }
}
```

## 8. Interakcje użytkownika

### 8.1 Szczegółowy opis interakcji

**1. Otwarcie strony `/login`**
- **Akcja:** Użytkownik wchodzi na stronę `/login`
- **Rezultat:**
  - Renderuje się LoginPage
  - Pole email ma auto-focus (EmailInput `autoFocus={true}`)
  - Jeśli użytkownik jest już zalogowany (`isLoggedIn === true`), następuje redirect na `/tickets`

**2. Wprowadzanie email**
- **Akcja:** Użytkownik wpisuje email w pole "Email"
- **Rezultat:**
  - Stan `email` aktualizuje się (onChange)
  - Po opuszczeniu pola (onBlur): walidacja formatu email
    - Jeśli nieprawidłowy format: `emailError = "Nieprawidłowy format email"`, border czerwony, komunikat pod polem
    - Jeśli prawidłowy: `emailError = ""`, border normalny/zielony

**3. Wprowadzanie hasła**
- **Akcja:** Użytkownik wpisuje hasło w pole "Hasło"
- **Rezultat:**
  - Stan `password` aktualizuje się (onChange)
  - Znaki maskowane (`type="password"`)
  - Brak inline validation (tylko wymagane pole)

**4. Kliknięcie "Zaloguj się" (submit)**
- **Akcja:** Użytkownik klika przycisk "Zaloguj się" (lub Enter w formularzu)
- **Rezultat:**
  - **Pre-validation:** Sprawdzenie formatu email
    - Jeśli błąd: `emailError` ustawiony, brak API call
  - **API call:** `POST /api/auth/login`
    - `isSubmitting = true` (przycisk disabled, opcjonalnie spinner)
  - **Scenariusz sukcesu (200 OK):**
    - Token JWT zapisany do localStorage (via AppContext.login())
    - Stan `user` w AppContext zaktualizowany
    - Redirect na `/tickets` (React Router `navigate('/tickets')`)
  - **Scenariusz błędu (401):**
    - ErrorModal otwiera się (`isErrorModalOpen = true`)
    - Komunikat: "Nieprawidłowy email lub hasło"
    - `isSubmitting = false` (przycisk odblokowany)
  - **Scenariusz błędu (5xx/Network):**
    - ErrorModal otwiera się
    - Komunikat: "Wystąpił problem. Spróbuj ponownie."
    - `isSubmitting = false`

**5. Zamknięcie ErrorModal**
- **Akcja:** Użytkownik klika "Zamknij" w ErrorModal (lub Escape, lub kliknięcie backdrop)
- **Rezultat:**
  - `isErrorModalOpen = false`
  - Modal znika
  - Użytkownik wraca do formularza (pola zachowują wartości)

**6. Kliknięcie "Nie masz konta? Zarejestruj się"**
- **Akcja:** Użytkownik klika link nawigacyjny
- **Rezultat:**
  - Redirect na `/register` (React Router Link)

### 8.2 Keyboard navigation

**Tab order:**
1. Pole email (auto-focus)
2. Pole hasło
3. Przycisk "Zaloguj się"
4. Link "Nie masz konta? Zarejestruj się"

**Enter key:**
- W polach email/hasło: submit formularza (domyślne zachowanie `<form>`)
- Na przycisku "Zaloguj się": submit formularza

**Escape key:**
- W ErrorModal: zamknięcie modalu

## 9. Warunki i walidacja

### 9.1 Walidacja inline (frontend, real-time)

**Pole Email:**
- **Warunek:** Format email
- **Regex:** `/^[^\s@]+@[^\s@]+\.[^\s@]+$/`
- **Trigger:** onBlur (po opuszczeniu pola)
- **Komunikat błędu:** "Nieprawidłowy format email" (pod polem, czerwony tekst)
- **Visual feedback:** Border czerwony (`border-red-500`) jeśli błąd, zielony (`border-green-500`) jeśli valid
- **Impact:** Blokuje submit (pre-validation w handleSubmit)

**Pole Hasło:**
- **Warunek:** Wymagane
- **Trigger:** HTML5 `required` attribute (native validation)
- **Brak inline validation** przy logowaniu (tylko przy rejestracji)

### 9.2 Walidacja API (backend)

**Endpoint:** `POST /api/auth/login`

**Walidacja backend (z api-plan.md, linia 90-93):**
- `email`: wymagane, format email
- `password`: wymagane

**Response błędów:**
- **401 Unauthorized:** "Nieprawidłowy email lub hasło"
  - Trigger: email nie istnieje w bazie LUB hasło nieprawidłowe (bcrypt verify failed)
  - Obsługa frontend: ErrorModal z komunikatem
- **5xx Server Error:** "Wystąpił problem. Spróbuj ponownie."
  - Trigger: błąd serwera, problem z bazą danych, timeout
  - Obsługa frontend: ErrorModal z generycznym komunikatem

### 9.3 Wpływ walidacji na stan UI

**Email validation:**
- Pole valid (zielony border + brak error message):
  - Email spełnia regex
  - `emailError = ""`
- Pole invalid (czerwony border + error message):
  - Email nie spełnia regex
  - `emailError = "Nieprawidłowy format email"`
  - Przycisk submit zablokowany (pre-validation w handleSubmit return early)

**Submit validation:**
- **isSubmitting = true:**
  - Przycisk "Zaloguj się" disabled (`disabled={isSubmitting}`)
  - Opcjonalnie: spinner w przycisku lub zmiana tekstu na "Logowanie..."
  - Pola email i hasło disabled (opcjonalnie, zapobiega zmianom podczas API call)
- **isSubmitting = false:**
  - Przycisk odblokowany
  - Pola aktywne

**ErrorModal state:**
- **isErrorModalOpen = true:**
  - Modal wyświetlany (fixed overlay)
  - Backdrop semi-transparent (`bg-black bg-opacity-50`)
  - Focus trap (Tab cyklicznie w modalu)
  - Escape/Backdrop click zamyka modal
- **isErrorModalOpen = false:**
  - Modal ukryty (unmount lub `display: none`)

### 9.4 Szczegółowe warunki (tabela)

| Komponent | Warunek | Weryfikacja | Komunikat błędu | Wpływ na UI |
|-----------|---------|-------------|-----------------|-------------|
| **EmailInput** | Format email | Regex: `/^[^\s@]+@[^\s@]+\.[^\s@]+$/` | "Nieprawidłowy format email" | Border czerwony, text pod polem, blokada submit |
| **EmailInput** | Wymagane | HTML5 `required` | Native browser message | Brak submit (native validation) |
| **PasswordInput** | Wymagane | HTML5 `required` | Native browser message | Brak submit (native validation) |
| **LoginPage** | API 401 | Backend: email/hasło incorrect | "Nieprawidłowy email lub hasło" | ErrorModal otwarty |
| **LoginPage** | API 5xx/Network | Backend: server error | "Wystąpił problem. Spróbuj ponownie." | ErrorModal otwarty |

## 10. Obsługa błędów

### 10.1 Błędy walidacji (inline)

**Błąd:** Nieprawidłowy format email

**Trigger:** onBlur w EmailInput, regex validation failed

**Obsługa:**
```typescript
const validateEmail = (email: string): boolean => {
  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
  return emailRegex.test(email)
}

const handleEmailBlur = () => {
  if (!validateEmail(email)) {
    setEmailError('Nieprawidłowy format email')
  } else {
    setEmailError('')
  }
}
```

**UI:**
- Border czerwony: `className={emailError ? 'border-red-500' : 'border-gray-300'}`
- Komunikat pod polem: `{emailError && <span className="text-red-500 text-sm">{emailError}</span>}`
- ARIA: `aria-invalid={!!emailError}`, `aria-describedby="email-error"`

---

### 10.2 Błędy API

**Błąd 401: Nieprawidłowy email lub hasło**

**Trigger:** `POST /api/auth/login` zwraca 401

**Response (z api-plan.md, linia 109-113):**
```json
{
  "error": "Nieprawidłowy email lub hasło"
}
```

**Obsługa:**
```typescript
try {
  const response = await apiService.login({ email, password })
  // sukces
} catch (error) {
  if (error instanceof ApiError && error.status === 401) {
    setErrorMessage('Nieprawidłowy email lub hasło')
    setIsErrorModalOpen(true)
  }
}
```

**UI:**
- ErrorModal otwiera się
- Tytuł: "Błąd"
- Treść: "Nieprawidłowy email lub hasło"
- Przycisk: "Zamknij"

---

**Błąd 5xx / Network Error**

**Trigger:**
- Backend zwraca 500, 502, 503, 504
- Network timeout
- CORS error
- Brak połączenia

**Obsługa:**
```typescript
catch (error) {
  if (error instanceof ApiError && error.status >= 500) {
    setErrorMessage('Wystąpił problem z serwerem. Spróbuj ponownie.')
    setIsErrorModalOpen(true)
  } else if (error instanceof NetworkError) {
    setErrorMessage('Brak połączenia z serwerem. Sprawdź swoje połączenie internetowe.')
    setIsErrorModalOpen(true)
  } else {
    // Nieoczekiwany błąd
    setErrorMessage('Wystąpił nieoczekiwany problem. Spróbuj ponownie.')
    setIsErrorModalOpen(true)
  }
}
```

**UI:**
- ErrorModal z generycznym komunikatem
- Użytkownik może zamknąć modal i spróbować ponownie

---

### 10.3 Edge cases

**Edge case 1: Użytkownik już zalogowany**

**Scenariusz:** Zalogowany użytkownik próbuje wejść na `/login`

**Obsługa:**
```typescript
// W LoginPage, useEffect
useEffect(() => {
  if (isLoggedIn) {
    navigate('/tickets')
  }
}, [isLoggedIn, navigate])
```

**Rezultat:** Automatyczny redirect na `/tickets`

---

**Edge case 2: Token wygasł podczas sesji**

**Scenariusz:** Użytkownik zalogowany, ale token JWT wygasł (sprawdzenie `expiresAt`)

**Obsługa (post-MVP):**
- Middleware sprawdzający ważność tokenu przy każdym API call
- Jeśli wygasły: automatyczne wylogowanie + redirect `/login` + Toast "Twoja sesja wygasła"

**MVP:** Brak automatycznego sprawdzania expiry. Token sprawdzany tylko przy API calls (backend zwróci 401).

---

**Edge case 3: Wielokrotne kliknięcie submit**

**Scenariusz:** Użytkownik kilkakrotnie klika "Zaloguj się" przed otrzymaniem odpowiedzi

**Obsługa:**
- Flaga `isSubmitting` blokuje przycisk (`disabled={isSubmitting}`)
- Pierwsze kliknięcie: `isSubmitting = true`
- Kolejne kliknięcia: ignorowane (przycisk disabled)

---

**Edge case 4: Puste pola**

**Scenariusz:** Użytkownik próbuje zalogować się z pustymi polami

**Obsługa:**
- HTML5 `required` attribute na polach email i hasło
- Native browser validation zapobiega submitowi
- Komunikat: native browser message ("Please fill out this field")

**Opcjonalnie (custom validation):**
```typescript
const handleSubmit = (e: React.FormEvent) => {
  e.preventDefault()

  if (!email || !password) {
    setErrorMessage('Wypełnij wszystkie pola')
    setIsErrorModalOpen(true)
    return
  }

  // ... rest of submit logic
}
```

## 11. Kroki implementacji

### Krok 1: Setup struktur katalogów i plików

**Struktura katalogów:**
```
src/
├── pages/
│   └── auth/
│       └── login-page.tsx          (główny komponent strony)
├── components/
│   ├── Auth/
│   │   └── LoginForm.tsx           (komponent formularza)
│   └── Shared/
│       ├── EmailInput.tsx          (reusable)
│       ├── PasswordInput.tsx       (reusable)
│       ├── Button.tsx              (reusable)
│       └── ErrorModal.tsx          (reusable)
├── services/
│   └── api-service.ts              (ApiService z metodą login)
├── context/
│   └── app-context.tsx             (AppContext z metodą login)
├── types/
│   ├── api.ts                      (LoginRequest, LoginResponse)
│   └── models.ts                   (User interface)
└── utils/
    └── validation.ts               (funkcje walidacji: validateEmail)
```

**Zadania:**
1. Utworzenie plików komponentów (login-page.tsx, LoginForm.tsx)
2. Utworzenie plików shared components (EmailInput.tsx, PasswordInput.tsx, Button.tsx, ErrorModal.tsx)
3. Utworzenie/aktualizacja types (api.ts, models.ts)
4. Utworzenie utils/validation.ts z funkcją validateEmail

---

### Krok 2: Implementacja shared components (bottom-up approach)

**Kolejność (od najbardziej atomowych do złożonych):**

1. **Button.tsx**
   - Props interface: `ButtonProps`
   - Logika wariantów (primary/secondary/danger)
   - Tailwind styling: baseClasses + variantClasses
   - Accessibility: focus ring, disabled state, aria-disabled
   - Testy jednostkowe (opcjonalnie): renderowanie, kliknięcie, disabled

2. **EmailInput.tsx**
   - Props interface: `EmailInputProps`
   - Kontrolowany input (value + onChange)
   - Inline validation (onBlur + validateEmail)
   - Error display (conditional rendering)
   - Accessibility: label for, aria-describedby, aria-invalid
   - Styling: border czerwony/zielony w zależności od error state

3. **PasswordInput.tsx**
   - Props interface: `PasswordInputProps`
   - Kontrolowany input type="password"
   - Maskowanie znaków
   - Error display (opcjonalne dla login page)
   - Accessibility: label for, aria-describedby

4. **ErrorModal.tsx**
   - Props interface: `ErrorModalProps`
   - Backdrop overlay (semi-transparent, onClick zamyka)
   - Modal content (tytuł, lista błędów, przycisk)
   - Focus trap (Tab cyklicznie w modalu)
   - Escape key listener (useEffect)
   - Auto-focus na "Zamknij" przy otwarciu
   - ARIA: role="dialog", aria-modal, aria-labelledby

---

### Krok 3: Implementacja ApiService.login()

**Plik:** `src/services/api-service.ts`

**Zadania:**
1. Dodanie interfejsów `LoginRequest`, `LoginResponse`, `LoginErrorResponse` (lub import z types/api.ts)
2. Implementacja metody `login(request: LoginRequest): Promise<LoginResponse>`
3. Error handling:
   - Sprawdzenie `response.ok`
   - Rzucanie `ApiError` dla 401 (z message z response.error)
   - Rzucanie `ApiError` dla 5xx (generyczny message)
   - Rzucanie `NetworkError` dla network failures
4. Testy jednostkowe (opcjonalnie): mock fetch, sprawdzenie request payload, obsługa responses

**Przykładowa implementacja:**
```typescript
async login(request: LoginRequest): Promise<LoginResponse> {
  try {
    const response = await fetch(`${this.baseUrl}/api/auth/login`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-TOKEN': this.appToken
      },
      body: JSON.stringify(request)
    })

    if (!response.ok) {
      if (response.status === 401) {
        const errorData: LoginErrorResponse = await response.json()
        throw new ApiError(401, errorData.error)
      }
      throw new ApiError(response.status, 'Wystąpił problem. Spróbuj ponownie.')
    }

    return await response.json()
  } catch (error) {
    if (error instanceof ApiError) {
      throw error
    }
    throw new NetworkError('Brak połączenia z serwerem')
  }
}
```

---

### Krok 4: Implementacja AppContext (jeśli nie istnieje)

**Plik:** `src/context/app-context.tsx`

**Zadania:**
1. Utworzenie `AppContext` z interfejsem `AppContextType`
2. Implementacja `AppContextProvider`:
   - Stan `user: User | null`
   - Metoda `login(userData: User)`:
     - Zapisanie `user` do stanu
     - Zapisanie tokenu do localStorage (`localStorage.setItem('token', userData.token)`)
     - Synchronizacja z ApiService (`apiService.setAuthToken(userData.token)`)
   - Metoda `logout()`:
     - Wyczyszczenie `user` (null)
     - Usunięcie tokenu z localStorage
     - Wyczyszczenie tokenu w ApiService
   - Computed property `isLoggedIn: boolean` (return `user !== null`)
   - Auto-restore user z localStorage przy mount (useEffect)
3. Export `useAppContext()` hook dla łatwego dostępu

**Przykładowa implementacja (fragment):**
```typescript
const AppContext = createContext<AppContextType | undefined>(undefined)

export const AppContextProvider = ({ children }: { children: ReactNode }) => {
  const [user, setUser] = useState<User | null>(null)
  const apiService = new ApiService(VITE_API_URL, VITE_APP_TOKEN)

  // Auto-restore z localStorage
  useEffect(() => {
    const token = localStorage.getItem('token')
    const userData = localStorage.getItem('user')
    if (token && userData) {
      const parsedUser = JSON.parse(userData)
      setUser(parsedUser)
      apiService.setAuthToken(token)
    }
  }, [])

  const login = (userData: User) => {
    setUser(userData)
    localStorage.setItem('token', userData.token)
    localStorage.setItem('user', JSON.stringify(userData))
    apiService.setAuthToken(userData.token)
  }

  const logout = () => {
    setUser(null)
    localStorage.removeItem('token')
    localStorage.removeItem('user')
    apiService.clearAuthToken()
  }

  const value: AppContextType = {
    user,
    isLoggedIn: user !== null,
    login,
    logout,
    getApiService: () => apiService
  }

  return <AppContext.Provider value={value}>{children}</AppContext.Provider>
}

export const useAppContext = () => {
  const context = useContext(AppContext)
  if (!context) throw new Error('useAppContext must be used within AppContextProvider')
  return context
}
```

---

### Krok 5: Implementacja utils/validation.ts

**Plik:** `src/utils/validation.ts`

**Zadania:**
1. Funkcja `validateEmail(email: string): boolean`
   - Regex: `/^[^\s@]+@[^\s@]+\.[^\s@]+$/`
   - Return true jeśli valid, false jeśli invalid

**Implementacja:**
```typescript
export const validateEmail = (email: string): boolean => {
  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
  return emailRegex.test(email)
}
```

---

### Krok 6: Implementacja LoginForm component

**Plik:** `src/components/Auth/LoginForm.tsx`

**Zadania:**
1. Interfejs `LoginFormProps`
2. Struktura JSX:
   - `<form onSubmit={onSubmit}>`
   - Nagłówek h2: "Zaloguj się"
   - `<EmailInput />` z propsami (value, onChange, error, autoFocus)
   - `<PasswordInput />` z propsami (value, onChange)
   - `<Button type="submit" disabled={isSubmitting}>Zaloguj się</Button>`
   - Link nawigacyjny (React Router Link): "Nie masz konta? Zarejestruj się" → `/register`
3. Styling Tailwind:
   - Centered layout: `max-w-md mx-auto`
   - Padding/margins dla czytelności
   - Responsywność (mobile: full-width, desktop: max-width 400px)

**Przykładowa implementacja (fragment):**
```tsx
interface LoginFormProps {
  email: string
  password: string
  emailError: string
  isSubmitting: boolean
  onEmailChange: (value: string) => void
  onPasswordChange: (value: string) => void
  onSubmit: (e: React.FormEvent) => void
}

export const LoginForm = ({
  email,
  password,
  emailError,
  isSubmitting,
  onEmailChange,
  onPasswordChange,
  onSubmit
}: LoginFormProps) => {
  return (
    <form onSubmit={onSubmit} className="max-w-md mx-auto p-6 bg-white rounded-lg shadow">
      <h2 className="text-2xl font-bold mb-6 text-center">Zaloguj się</h2>

      <EmailInput
        label="Email"
        value={email}
        error={emailError}
        onChange={onEmailChange}
        placeholder="twoj@email.com"
        autoFocus
        required
      />

      <PasswordInput
        label="Hasło"
        value={password}
        onChange={onPasswordChange}
        placeholder="••••••••"
        required
      />

      <Button
        type="submit"
        variant="primary"
        disabled={isSubmitting}
        className="w-full mt-4"
      >
        {isSubmitting ? 'Logowanie...' : 'Zaloguj się'}
      </Button>

      <p className="mt-4 text-center text-sm text-gray-600">
        Nie masz konta?{' '}
        <Link to="/register" className="text-blue-600 hover:underline">
          Zarejestruj się
        </Link>
      </p>
    </form>
  )
}
```

---

### Krok 7: Implementacja LoginPage (główny komponent)

**Plik:** `src/pages/auth/login-page.tsx`

**Zadania:**
1. Import dependencies:
   - `useState`, `useEffect` z React
   - `useNavigate` z react-router-dom
   - `useAppContext` z context/app-context
   - `LoginForm` z components/Auth/LoginForm
   - `ErrorModal` z components/Shared/ErrorModal
   - `validateEmail` z utils/validation

2. Local state:
   ```typescript
   const [email, setEmail] = useState('')
   const [password, setPassword] = useState('')
   const [emailError, setEmailError] = useState('')
   const [isSubmitting, setIsSubmitting] = useState(false)
   const [isErrorModalOpen, setIsErrorModalOpen] = useState(false)
   const [errorMessage, setErrorMessage] = useState('')
   ```

3. AppContext hooks:
   ```typescript
   const { login, isLoggedIn, getApiService } = useAppContext()
   const apiService = getApiService()
   const navigate = useNavigate()
   ```

4. Redirect logic (jeśli już zalogowany):
   ```typescript
   useEffect(() => {
     if (isLoggedIn) {
       navigate('/tickets')
     }
   }, [isLoggedIn, navigate])
   ```

5. Event handlers:
   - `handleEmailChange(value: string)`:
     - `setEmail(value)`
     - Inline validation: `if (!validateEmail(value)) { setEmailError(...) } else { setEmailError('') }`
   - `handlePasswordChange(value: string)`:
     - `setPassword(value)`
   - `handleSubmit(e: React.FormEvent)`:
     - `e.preventDefault()`
     - Pre-validation email
     - `setIsSubmitting(true)`
     - Try-catch API call:
       - Sukces: `login(userData)`, `navigate('/tickets')`
       - Błąd: `setErrorMessage(...)`, `setIsErrorModalOpen(true)`
     - Finally: `setIsSubmitting(false)`

6. JSX structure:
   ```tsx
   <main className="min-h-screen flex items-center justify-center bg-gray-50">
     <LoginForm
       email={email}
       password={password}
       emailError={emailError}
       isSubmitting={isSubmitting}
       onEmailChange={handleEmailChange}
       onPasswordChange={handlePasswordChange}
       onSubmit={handleSubmit}
     />

     <ErrorModal
       isOpen={isErrorModalOpen}
       onClose={() => setIsErrorModalOpen(false)}
       errors={errorMessage}
     />
   </main>
   ```

**Przykładowa implementacja (fragment):**
```tsx
export const LoginPage = () => {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [emailError, setEmailError] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [isErrorModalOpen, setIsErrorModalOpen] = useState(false)
  const [errorMessage, setErrorMessage] = useState('')

  const { login, isLoggedIn, getApiService } = useAppContext()
  const apiService = getApiService()
  const navigate = useNavigate()

  // Redirect jeśli już zalogowany
  useEffect(() => {
    if (isLoggedIn) {
      navigate('/tickets')
    }
  }, [isLoggedIn, navigate])

  const handleEmailChange = (value: string) => {
    setEmail(value)
    if (value && !validateEmail(value)) {
      setEmailError('Nieprawidłowy format email')
    } else {
      setEmailError('')
    }
  }

  const handlePasswordChange = (value: string) => {
    setPassword(value)
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    // Pre-validation
    if (!validateEmail(email)) {
      setEmailError('Nieprawidłowy format email')
      return
    }

    setIsSubmitting(true)
    setEmailError('')

    try {
      const response = await apiService.login({ email, password })

      // Sukces - login do AppContext
      login({
        id: response.userId,
        email: response.email,
        isAdmin: response.isAdmin,
        token: response.token,
        expiresAt: response.expiresAt
      })

      // Redirect
      navigate('/tickets')

    } catch (error) {
      if (error instanceof ApiError) {
        setErrorMessage(error.message)
      } else {
        setErrorMessage('Wystąpił nieoczekiwany problem. Spróbuj ponownie.')
      }
      setIsErrorModalOpen(true)
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <main className="min-h-screen flex items-center justify-center bg-gray-50 px-4">
      <LoginForm
        email={email}
        password={password}
        emailError={emailError}
        isSubmitting={isSubmitting}
        onEmailChange={handleEmailChange}
        onPasswordChange={handlePasswordChange}
        onSubmit={handleSubmit}
      />

      <ErrorModal
        isOpen={isErrorModalOpen}
        onClose={() => setIsErrorModalOpen(false)}
        errors={errorMessage}
      />
    </main>
  )
}

export default LoginPage
```

---

### Krok 8: Dodanie route w React Router

**Plik:** `src/main.tsx` (lub gdzie konfigurowany jest routing)

**Zadania:**
1. Import `LoginPage` z `pages/auth/login-page`
2. Dodanie route w konfiguracji React Router 7:
   ```tsx
   {
     path: '/login',
     element: <LoginPage />
   }
   ```
3. Sprawdzenie że route nie jest chroniony (PublicRoute lub brak ProtectedRoute wrapper)

**Przykład (React Router 7):**
```tsx
import { createBrowserRouter, RouterProvider } from 'react-router-dom'
import LoginPage from './pages/auth/login-page'

const router = createBrowserRouter([
  {
    path: '/',
    element: <LandingPage />
  },
  {
    path: '/login',
    element: <LoginPage />
  },
  {
    path: '/register',
    element: <RegisterPage />
  },
  {
    path: '/tickets',
    element: <ProtectedRoute><TicketsPage /></ProtectedRoute>
  },
  // ... other routes
])
```

---

### Krok 9: Styling Tailwind CSS

**Zadania:**
1. Sprawdzenie konfiguracji Tailwind CSS 4 (tailwind.config.js, postcss.config.js)
2. Dodanie custom klasów w `@layer components` (opcjonalnie):
   ```css
   @layer components {
     .input-field {
       @apply px-3 py-2 border rounded focus:outline-none focus:ring-2 focus:ring-blue-500;
     }

     .input-error {
       @apply border-red-500;
     }

     .input-valid {
       @apply border-green-500;
     }
   }
   ```
3. Styling komponentów:
   - **LoginForm:** centered layout, max-width 400px, padding, shadow
   - **EmailInput/PasswordInput:** border dynamiczny (gray/red/green), focus ring, error text
   - **Button:** variant classes (primary/secondary/danger), hover effects, disabled state
   - **ErrorModal:** backdrop overlay (bg-black bg-opacity-50), centered content, rounded corners

**Responsywność:**
- Mobile (default): full-width inputs, vertical stack, padding px-4
- Desktop (md breakpoint): max-width 400px, centered, większe touch targets

---

### Krok 10: Accessibility (WCAG AA)

**Zadania:**
1. **Semantic HTML:**
   - `<main>` dla głównej zawartości
   - `<form>` dla formularza
   - `<label for="id">` powiązane z `<input id="id">`
   - `<button type="submit">` dla submitu

2. **ARIA attributes:**
   - EmailInput: `aria-describedby="email-error"` jeśli error istnieje
   - EmailInput: `aria-invalid={!!emailError}`
   - PasswordInput: analogicznie
   - ErrorModal: `role="dialog"`, `aria-modal="true"`, `aria-labelledby="error-modal-title"`

3. **Focus management:**
   - Auto-focus na email przy otwarciu strony (`autoFocus` prop)
   - Focus trap w ErrorModal (Tab cyklicznie w modalu)
   - Return focus do formularza po zamknięciu modalu
   - Visible focus indicators: `focus:ring-2 focus:ring-blue-500`

4. **Keyboard navigation:**
   - Tab order: email → hasło → submit → link rejestracji
   - Enter submit formularza (native `<form>` behavior)
   - Escape zamyka ErrorModal

5. **Color contrast:**
   - WCAG AA minimum 4.5:1
   - Sprawdzenie kontrastów: tekst na tle, border colors, button colors
   - Narzędzie: WebAIM Contrast Checker

6. **Screen reader testing:**
   - Test z NVDA/JAWS (Windows) lub VoiceOver (Mac)
   - Sprawdzenie czy błędy są poprawnie ogłaszane
   - Sprawdzenie czy labels są powiązane z inputami

---

### Krok 11: Testy

**1. Testy jednostkowe (opcjonalnie dla MVP, rekomendowane post-MVP):**

**Narzędzia:** Vitest + React Testing Library

**Testy komponentów:**
- **Button.tsx:**
  - Renderowanie z różnymi wariantami
  - Obsługa onClick
  - Disabled state

- **EmailInput.tsx:**
  - Renderowanie z label i placeholder
  - onChange callback
  - Walidacja email (onBlur)
  - Wyświetlanie error message

- **PasswordInput.tsx:**
  - Analogicznie

- **ErrorModal.tsx:**
  - Renderowanie (isOpen=true/false)
  - onClose callback (Escape, backdrop click, przycisk)
  - Focus trap

- **LoginForm.tsx:**
  - Renderowanie wszystkich elementów
  - Submit callback
  - Integration z EmailInput/PasswordInput

- **LoginPage.tsx:**
  - Renderowanie LoginForm i ErrorModal
  - Redirect jeśli już zalogowany (mock useAppContext)
  - handleSubmit logic (mock apiService.login)
  - Error handling (API errors)

**2. Testy E2E (opcjonalnie dla MVP, rekomendowane post-MVP):**

**Narzędzia:** Playwright lub Cypress

**Scenariusze testowe:**
- **Happy path:**
  1. Otwarcie `/login`
  2. Wprowadzenie poprawnego email i hasła
  3. Kliknięcie "Zaloguj się"
  4. Sprawdzenie redirectu na `/tickets`
  5. Sprawdzenie że token zapisany w localStorage

- **Nieprawidłowy email format:**
  1. Otwarcie `/login`
  2. Wprowadzenie email bez @
  3. Opuszczenie pola (blur)
  4. Sprawdzenie komunikatu błędu "Nieprawidłowy format email"
  5. Sprawdzenie że submit zablokowany

- **Nieprawidłowe credentials (401):**
  1. Otwarcie `/login`
  2. Wprowadzenie nieprawidłowego email/hasła
  3. Kliknięcie "Zaloguj się"
  4. Sprawdzenie ErrorModal z komunikatem "Nieprawidłowy email lub hasło"
  5. Zamknięcie modalu
  6. Sprawdzenie że użytkownik pozostał na `/login`

- **Redirect dla zalogowanego użytkownika:**
  1. Zalogowanie (mock token w localStorage)
  2. Próba wejścia na `/login`
  3. Sprawdzenie automatycznego redirectu na `/tickets`

**3. Manual QA:**
- Testowanie na różnych przeglądarkach (Chrome, Firefox, Safari, Edge)
- Testowanie na różnych urządzeniach (desktop, tablet, mobile)
- Testowanie keyboard navigation (Tab, Enter, Escape)
- Testowanie z screen readerem (NVDA, VoiceOver)
- Testowanie edge cases (puste pola, wielokrotne kliknięcie submit, network timeout)

---

### Krok 12: Finalizacja i dokumentacja

**Zadania:**
1. Code review:
   - Sprawdzenie zgodności z PRD i ui-plan.md
   - Sprawdzenie naming conventions (camelCase dla zmiennych, PascalCase dla komponentów)
   - Sprawdzenie TypeScript types (brak `any`, strict mode)
   - Sprawdzenie error handling (wszystkie błędy obsłużone)

2. Dokumentacja (komentarze w kodzie):
   - JSDoc dla komponentów i funkcji publicznych
   - Komentarze dla złożonej logiki biznesowej
   - TODO/FIXME dla znanych issues (jeśli istnieją)

3. Performance audit:
   - Sprawdzenie bundle size (Vite build output)
   - Sprawdzenie re-renderów (React DevTools Profiler)
   - Optymalizacja (useMemo, useCallback jeśli potrzebne - ale unikać premature optimization)

4. Security audit:
   - Sprawdzenie że hasło nie jest logowane (console.log)
   - Sprawdzenie że token JWT nie jest wystawiany w URL
   - Sprawdzenie HTTPS w produkcji (environment variable)
   - Sprawdzenie że localStorage zawiera tylko niezbędne dane

5. Deployment checklist:
   - Environment variables (.env.prod) skonfigurowane (VITE_API_URL, VITE_APP_TOKEN)
   - Build production (`npm run build`)
   - Smoke test na środowisku produkcyjnym

---

## Podsumowanie kroków (checklist):

- [ ] **Krok 1:** Setup struktur katalogów i plików
- [ ] **Krok 2:** Implementacja shared components (Button, EmailInput, PasswordInput, ErrorModal)
- [ ] **Krok 3:** Implementacja ApiService.login()
- [ ] **Krok 4:** Implementacja AppContext (login, logout, isLoggedIn)
- [ ] **Krok 5:** Implementacja utils/validation.ts (validateEmail)
- [ ] **Krok 6:** Implementacja LoginForm component
- [ ] **Krok 7:** Implementacja LoginPage (główny komponent)
- [ ] **Krok 8:** Dodanie route w React Router
- [ ] **Krok 9:** Styling Tailwind CSS + responsywność
- [ ] **Krok 10:** Accessibility (ARIA, focus management, keyboard navigation)
- [ ] **Krok 11:** Testy (jednostkowe, E2E, manual QA)
- [ ] **Krok 12:** Finalizacja (code review, dokumentacja, performance, security, deployment)

---

**Szacowany czas implementacji:** 1-2 dni robocze (w ramach Fazy 2: Autentykacja, zgodnie z ui-plan.md linia 1433-1443)

---

**Koniec planu implementacji widoku Login Page**
