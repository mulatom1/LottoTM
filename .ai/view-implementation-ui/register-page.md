# Plan implementacji widoku Rejestracji Użytkownika

## 1. Przegląd

Widok rejestracji umożliwia nowym użytkownikom utworzenie konta w systemie LottoTM. Formularz rejestracyjny zbiera podstawowe dane (email, hasło, potwierdzenie hasła) i po pomyślnej walidacji tworzy nowe konto użytkownika. W MVP nie ma wymaganej weryfikacji adresu email - użytkownik po rejestracji zostaje automatycznie przekierowany na stronę logowania.

## 2. Routing widoku

**Ścieżka:** `/register`

**Dostęp:** Publiczny (niezalogowani użytkownicy)

**Redirect po sukcesie:** `/login` (użytkownik musi się zalogować po rejestracji)

## 3. Struktura komponentów

```
RegisterPage
├── Nagłówek (h2): "Zarejestruj się"
├── RegisterForm
│   ├── InputField (Email)
│   ├── InputField (Hasło)
│   ├── InputField (Potwierdź hasło)
│   ├── HintText (wymagania hasła)
│   └── SubmitButton: "Zarejestruj się"
├── LinkNavigation: "Masz już konto? Zaloguj się"
└── ErrorModal (komunikaty błędów)
```

## 4. Szczegóły komponentów

### 4.1 RegisterPage (komponent główny)

**Opis:** Kontener strony rejestracji zawierający formularz i elementy nawigacyjne.

**Główne elementy HTML:**
- `<main>` - główny kontener semantyczny
- `<div>` z `className="container mx-auto px-4 py-8"` - centrowanie i padding
- `<h2>` - nagłówek "Zarejestruj się"
- `<RegisterForm>` - komponent formularza
- `<p>` z linkiem do `/login` - zachęta do logowania

**Obsługiwane zdarzenia:**
- Brak (delegacja do dzieci)

**Warunki walidacji:**
- Brak (delegacja do RegisterForm)

**Typy:**
- Brak propsów (samodzielna strona)

**Propsy:**
- Brak

### 4.2 RegisterForm (komponent formularza)

**Opis:** Główny formularz rejestracyjny z trzema polami input i przyciskiem submit. Zarządza lokalnym stanem formularza i wykonuje walidację inline oraz wysyła dane do API.

**Główne elementy HTML:**
- `<form onSubmit={handleSubmit}>` - formularz z obsługą submit
- 3x `<InputField>` - pola email, hasło, potwierdzenie hasła
- `<p className="text-sm text-gray-600">` - hint text z wymaganiami hasła
- `<button type="submit">` - przycisk "Zarejestruj się"

**Obsługiwane zdarzenia:**
- `onSubmit`: walidacja wszystkich pól, wywołanie API `/api/auth/register`, obsługa sukcesu/błędów
- `onChange` dla każdego input: walidacja inline w czasie rzeczywistym
- `onBlur` dla każdego input: walidacja po opuszczeniu pola (opcjonalnie)

**Warunki walidacji:**

**Email:**
- **Wymagane:** pole nie może być puste
- **Format:** regex `/^[^\s@]+@[^\s@]+\.[^\s@]+$/` (podstawowa walidacja email)
- **Komunikat błędu (puste):** "Email jest wymagany"
- **Komunikat błędu (nieprawidłowy format):** "Nieprawidłowy format email"
- **Walidacja backend:** unikalność email (komunikat: "Email jest już zajęty")

**Hasło:**
- **Wymagane:** pole nie może być puste
- **Minimalna długość:** 8 znaków
- **Wielka litera:** minimum 1 znak A-Z
- **Cyfra:** minimum 1 znak 0-9
- **Znak specjalny:** minimum 1 znak spoza alfanumerycznych
- **Komunikat błędu (puste):** "Hasło jest wymagane"
- **Komunikat błędu (za krótkie):** "Hasło musi mieć min. 8 znaków"
- **Komunikat błędu (brak wielkiej litery):** "Hasło musi zawierać wielką literę"
- **Komunikat błędu (brak cyfry):** "Hasło musi zawierać cyfrę"
- **Komunikat błędu (brak znaku specjalnego):** "Hasło musi zawierać znak specjalny"

**Potwierdzenie hasła:**
- **Wymagane:** pole nie może być puste
- **Identyczne z hasłem:** wartość musi być dokładnie taka sama jak pole "Hasło"
- **Komunikat błędu (puste):** "Potwierdzenie hasła jest wymagane"
- **Komunikat błędu (niezgodne):** "Hasła nie są identyczne"

**Typy:**
```typescript
interface RegisterFormData {
  email: string;
  password: string;
  confirmPassword: string;
}

interface RegisterFormErrors {
  email?: string[];
  password?: string[];
  confirmPassword?: string[];
}

interface RegisterRequest {
  email: string;
  password: string;
  confirmPassword: string;
}

interface RegisterResponse {
  message: string;
}
```

**Propsy:**
- Brak (zarządza własnym stanem)

### 4.3 InputField (komponent reużywalny)

**Opis:** Uniwersalny komponent pola formularza z labelem, inputem i wyświetlaniem błędów inline.

**Główne elementy HTML:**
- `<div>` - kontener pola
- `<label htmlFor={id}>` - etykieta pola
- `<input id={id} type={type} value={value} onChange={onChange}>` - pole input
- `<p className="text-red-600 text-sm">` - komunikat błędu (jeśli istnieje)

**Obsługiwane interakcje:**
- `onChange`: aktualizacja wartości w stanie rodzica
- `onBlur`: opcjonalne uruchomienie walidacji

**Obsługiwana walidacja:**
- Wyświetlanie komunikatów błędów przekazanych przez rodzica
- Visual feedback: czerwony border jeśli błąd (`border-red-500`), domyślny w przeciwnym razie

**Typy:**
```typescript
interface InputFieldProps {
  label: string;
  id: string;
  type: 'text' | 'email' | 'password';
  value: string;
  onChange: (value: string) => void;
  onBlur?: () => void;
  error?: string;
  placeholder?: string;
  required?: boolean;
}
```

**Propsy:**
- `label`: etykieta pola (np. "Email")
- `id`: identyfikator dla powiązania label-input
- `type`: typ input (email, password, text)
- `value`: aktualna wartość
- `onChange`: callback zmiany wartości
- `onBlur`: opcjonalny callback po opuszczeniu pola
- `error`: komunikat błędu do wyświetlenia
- `placeholder`: placeholder tekstu
- `required`: czy pole jest wymagane

### 4.4 ErrorModal (komponent współdzielony)

**Opis:** Modal wyświetlający błędy zwrócone przez API przy submit formularza.

**Główne elementy:**
- Modal overlay (semi-transparent backdrop)
- Modal content box (centered, biały tło)
- Tytuł: "Błąd"
- Lista błędów (bullet points, czerwony tekst)
- Przycisk [Zamknij]

**Obsługiwane interakcje:**
- Kliknięcie backdrop: zamknięcie modala
- Kliknięcie [Zamknij]: zamknięcie modala
- Klawisz Escape: zamknięcie modala
- Focus trap wewnątrz modala

**Obsługiwana walidacja:**
- Brak (tylko wyświetla błędy)

**Typy:**
```typescript
interface ErrorModalProps {
  isOpen: boolean;
  onClose: () => void;
  errors: string[] | string;
}
```

**Propsy:**
- `isOpen`: czy modal jest otwarty
- `onClose`: callback zamknięcia modala
- `errors`: lista komunikatów błędów lub pojedynczy string

## 5. Typy

### 5.1 Request/Response DTOs

```typescript
// Request do POST /api/auth/register
interface RegisterRequest {
  email: string;
  password: string;
  confirmPassword: string;
}

// Response z POST /api/auth/register (sukces)
interface RegisterResponse {
  message: string; // "Rejestracja zakończona sukcesem"
}

// Response z POST /api/auth/register (błąd walidacji)
interface RegisterErrorResponse {
  errors: {
    email?: string[];
    password?: string[];
    confirmPassword?: string[];
  };
}
```

### 5.2 ViewModels i lokalne typy

```typescript
// Lokalny stan formularza
interface RegisterFormData {
  email: string;
  password: string;
  confirmPassword: string;
}

// Błędy walidacji inline
interface RegisterFormErrors {
  email?: string;
  password?: string;
  confirmPassword?: string;
}

// Stan komponentu RegisterForm
interface RegisterFormState {
  formData: RegisterFormData;
  errors: RegisterFormErrors;
  isSubmitting: boolean;
  isErrorModalOpen: boolean;
  apiErrors: string[];
}
```

## 6. Zarządzanie stanem

### 6.1 Lokalny stan w RegisterForm

**Stan formularza zarządzany przez React useState:**

```typescript
const [formData, setFormData] = useState<RegisterFormData>({
  email: '',
  password: '',
  confirmPassword: ''
});

const [errors, setErrors] = useState<RegisterFormErrors>({});
const [isSubmitting, setIsSubmitting] = useState(false);
const [isErrorModalOpen, setIsErrorModalOpen] = useState(false);
const [apiErrors, setApiErrors] = useState<string[]>([]);
```

**Aktualizacja stanu:**
- `onChange` handler dla każdego pola aktualizuje `formData`
- Walidacja inline aktualizuje `errors` w czasie rzeczywistym
- Submit ustawia `isSubmitting` na true podczas wywołania API
- Błędy API aktualizują `apiErrors` i otwierają ErrorModal

### 6.2 Brak customowego hooka w MVP

W MVP nie jest wymagany dedykowany hook. Logika walidacji i submit znajduje się bezpośrednio w komponencie RegisterForm. Ewentualny refactoring do `useRegisterForm()` hook może nastąpić w późniejszych iteracjach.

## 7. Integracja API

### 7.1 Endpoint

**POST `/api/auth/register`**

**Headers:**
```
Content-Type: application/json
X-TOKEN: <VITE_APP_TOKEN>
```

**Request body:**
```json
{
  "email": "user@example.com",
  "password": "SecurePass123!",
  "confirmPassword": "SecurePass123!"
}
```

**Response (sukces - 201 Created):**
```json
{
  "message": "Rejestracja zakończona sukcesem"
}
```

**Response (błąd walidacji - 400 Bad Request):**
```json
{
  "errors": {
    "email": ["Email jest już zajęty"],
    "password": ["Hasło musi zawierać wielką literę"],
    "confirmPassword": ["Hasła nie są identyczne"]
  }
}
```

**Response (błąd serwera - 500):**
```json
{
  "error": "Wystąpił problem z serwerem"
}
```

### 7.2 Wykorzystanie ApiService

```typescript
const apiService = useAppContext().getApiService();

try {
  const response = await apiService.register(formData);
  // Sukces: redirect /login
  navigate('/login');
} catch (error) {
  if (error instanceof ApiError) {
    if (error.status === 400 && error.data.errors) {
      // Błędy walidacji z backendu
      const errorMessages = Object.values(error.data.errors).flat();
      setApiErrors(errorMessages);
      setIsErrorModalOpen(true);
    } else {
      // Inne błędy API
      setApiErrors(['Wystąpił problem. Spróbuj ponownie.']);
      setIsErrorModalOpen(true);
    }
  } else if (error instanceof NetworkError) {
    setApiErrors([error.message]);
    setIsErrorModalOpen(true);
  }
} finally {
  setIsSubmitting(false);
}
```

### 7.3 Typy żądania i odpowiedzi

**Dokładne typy określone w sekcji 5.1**

## 8. Interakcje użytkownika

### 8.1 Wypełnianie formularza

**Scenariusz:**
1. Użytkownik wchodzi na `/register`
2. Auto-focus ustawia się na pierwszym polu (Email)
3. Użytkownik wprowadza email: "jan.kowalski@example.com"
4. Inline validation (onChange): sprawdzenie formatu email
   - Jeśli poprawny: brak błędu (opcjonalnie zielony border)
   - Jeśli niepoprawny: "Nieprawidłowy format email" (czerwony border + tekst)
5. Użytkownik przechodzi do pola Hasło (Tab)
6. Wprowadza hasło: "securepass" (brak wielkiej litery)
7. Inline validation (onChange): sprawdzenie wymagań hasła
   - Błąd: "Hasło musi zawierać wielką literę" (czerwony border + tekst)
8. Użytkownik poprawia: "SecurePass123!"
9. Inline validation: wszystkie wymagania spełnione (brak błędu)
10. Użytkownik przechodzi do pola Potwierdzenie hasła (Tab)
11. Wprowadza: "SecurePass123!" (identyczne)
12. Inline validation: hasła są identyczne (brak błędu)

### 8.2 Submit formularza - sukces

**Scenariusz:**
1. Użytkownik wypełnił wszystkie pola poprawnie
2. Klika przycisk "Zarejestruj się" lub naciska Enter
3. Trigger `onSubmit` event
4. Frontend validation: sprawdzenie wszystkich pól
5. Wszystkie pola poprawne → wywołanie API
6. `isSubmitting` ustawione na true (przycisk disabled, opcjonalnie spinner)
7. API zwraca 201 Created
8. Redirect → `/login`
9. Użytkownik widzi stronę logowania (może się teraz zalogować)

### 8.3 Submit formularza - błąd walidacji backend

**Scenariusz:**
1. Użytkownik wypełnia formularz:
   - Email: "existing@example.com" (już zajęty w bazie)
   - Hasło: "SecurePass123!"
   - Potwierdzenie: "SecurePass123!"
2. Frontend validation przechodzi (format ok, hasła identyczne)
3. Klika "Zarejestruj się"
4. API call POST /api/auth/register
5. Backend zwraca 400 Bad Request:
   ```json
   {
     "errors": {
       "email": ["Email jest już zajęty"]
     }
   }
   ```
6. Frontend otwiera ErrorModal z komunikatem: "Email jest już zajęty"
7. Użytkownik klika [Zamknij] w modalu
8. Modal się zamyka, użytkownik wraca do formularza
9. Poprawia email na inny
10. Ponownie submit → sukces → redirect `/login`

### 8.4 Submit formularza - błąd serwera

**Scenariusz:**
1. Użytkownik wypełnia formularz poprawnie
2. Klika "Zarejestruj się"
3. API call POST /api/auth/register
4. Błąd sieciowy / serwer zwraca 500
5. Frontend otwiera ErrorModal z komunikatem: "Wystąpił problem z serwerem. Spróbuj ponownie."
6. Użytkownik klika [Zamknij]
7. Modal się zamyka
8. Użytkownik może spróbować ponownie lub wrócić później

### 8.5 Nawigacja do logowania

**Scenariusz:**
1. Użytkownik wchodzi na `/register`
2. Widzi tekst: "Masz już konto? Zaloguj się" (link)
3. Klika link "Zaloguj się"
4. Redirect → `/login`
5. Użytkownik widzi stronę logowania

## 9. Warunki i walidacja

### 9.1 Walidacja inline (real-time)

**Gdzie:** Komponenty InputField w RegisterForm

**Kiedy:** onChange event (po każdej zmianie wartości)

**Warunki:**

**Email:**
- Puste pole → "Email jest wymagany"
- Nieprawidłowy format → "Nieprawidłowy format email"
- Poprawny format → brak błędu

**Hasło:**
- Puste pole → "Hasło jest wymagane"
- Długość < 8 → "Hasło musi mieć min. 8 znaków"
- Brak wielkiej litery → "Hasło musi zawierać wielką literę"
- Brak cyfry → "Hasło musi zawierać cyfrę"
- Brak znaku specjalnego → "Hasło musi zawierać znak specjalny"
- Wszystkie wymagania spełnione → brak błędu

**Potwierdzenie hasła:**
- Puste pole → "Potwierdzenie hasła jest wymagane"
- Różne od hasła → "Hasła nie są identyczne"
- Identyczne z hasłem → brak błędu

**Wpływ na UI:**
- Błąd: czerwony border (`border-red-500`), komunikat błędu pod polem (czerwony tekst `text-red-600`)
- Poprawne: domyślny border (szary), brak komunikatu błędu

### 9.2 Walidacja przy submit

**Gdzie:** RegisterForm.handleSubmit

**Kiedy:** onSubmit event (kliknięcie "Zarejestruj się" lub Enter)

**Proces:**
1. Sprawdzenie wszystkich warunków inline (jak w 9.1)
2. Jeśli jakiekolwiek błędy inline → STOP, nie wywołuj API, pozostań na formularzu
3. Jeśli wszystkie pola poprawne → wywołanie API POST /api/auth/register
4. Obsługa odpowiedzi API:
   - 201 Created → redirect `/login`
   - 400 Bad Request (błędy walidacji) → ErrorModal z listą błędów
   - 5xx / network error → ErrorModal "Wystąpił problem z serwerem"

### 9.3 Walidacja backend (dopełniająca)

**Gdzie:** Backend API /api/auth/register

**Warunki dodatkowe:**
- Email unikalny w bazie (UNIQUE constraint): "Email jest już zajęty"
- Zgodność hasła i potwierdzenia: "Hasła nie są identyczne" (duplikacja frontendu dla bezpieczeństwa)
- Wszystkie wymagania hasła (duplikacja frontendu)

**Komunikacja błędów:**
- Backend zwraca 400 Bad Request z obiektem `errors`
- Frontend mapuje błędy do ErrorModal (lista komunikatów)

## 10. Obsługa błędów

### 10.1 Błędy walidacji inline

**Typ błędu:** Nieprawidłowy format danych (email, hasło, potwierdzenie)

**Obsługa:**
- Wyświetlenie komunikatu błędu pod polem (czerwony tekst)
- Czerwony border pola input
- Blokada submit dopóki błędy nie zostaną naprawione (przycisk disabled lub walidacja w handleSubmit)

**Przykład:**
```tsx
{errors.email && (
  <p className="text-red-600 text-sm mt-1">{errors.email}</p>
)}
```

### 10.2 Błędy API - walidacja backend

**Typ błędu:** 400 Bad Request z obiektem `errors`

**Obsługa:**
1. Wyciągnięcie wszystkich komunikatów błędów z response.data.errors
2. Spłaszczenie do tablicy stringów (Object.values().flat())
3. Otwarcie ErrorModal z listą błędów
4. Użytkownik klika [Zamknij] → modal się zamyka, wraca do formularza

**Przykład:**
```tsx
if (error.status === 400 && error.data.errors) {
  const errorMessages = Object.values(error.data.errors).flat();
  setApiErrors(errorMessages);
  setIsErrorModalOpen(true);
}
```

### 10.3 Błędy serwera (5xx)

**Typ błędu:** 500 Internal Server Error

**Obsługa:**
1. Generyczny komunikat: "Wystąpił problem z serwerem. Spróbuj ponownie."
2. ErrorModal z pojedynczym komunikatem
3. Użytkownik może zamknąć modal i spróbować ponownie

**Przykład:**
```tsx
else {
  setApiErrors(['Wystąpił problem z serwerem. Spróbuj ponownie.']);
  setIsErrorModalOpen(true);
}
```

### 10.4 Błędy sieciowe

**Typ błędu:** NetworkError (brak połączenia, timeout)

**Obsługa:**
1. Komunikat z obiektu error: error.message (np. "Brak połączenia z serwerem")
2. ErrorModal z komunikatem
3. Użytkownik może zamknąć modal i spróbować ponownie

**Przykład:**
```tsx
if (error instanceof NetworkError) {
  setApiErrors([error.message]);
  setIsErrorModalOpen(true);
}
```

### 10.5 Empty state

**Typ błędu:** Brak (nie dotyczy formularza rejestracji)

Formularz rejestracji zawsze wyświetla pola do wypełnienia. Nie ma scenariusza "pustego stanu".

## 11. Kroki implementacji

### Krok 1: Stworzenie struktury plików
- Utwórz `src/pages/auth/register-page.tsx` (główny komponent strony)
- Utwórz `src/components/Auth/RegisterForm.tsx` (komponent formularza)
- Upewnij się, że istnieje `src/components/Shared/InputField.tsx` (reużywalny input)
- Upewnij się, że istnieje `src/components/Shared/ErrorModal.tsx` (modal błędów)

### Krok 2: Zdefiniowanie typów
- Zdefiniuj interfejsy w `src/types/auth.ts`:
  - `RegisterRequest`
  - `RegisterResponse`
  - `RegisterErrorResponse`
  - `RegisterFormData`
  - `RegisterFormErrors`

### Krok 3: Implementacja InputField (jeśli nie istnieje)
- Stwórz komponent `InputField` z propsami: label, id, type, value, onChange, error, placeholder
- Implementacja wyświetlania błędów inline (czerwony border + tekst)
- Accessibility: label powiązany z input (`htmlFor`), aria-describedby dla błędów

### Krok 4: Implementacja ErrorModal (jeśli nie istnieje)
- Stwórz komponent `ErrorModal` z propsami: isOpen, onClose, errors
- Implementacja:
  - Backdrop semi-transparent
  - Modal content centered (białe tło, rounded corners)
  - Tytuł "Błąd"
  - Lista błędów (bullet points, czerwony tekst)
  - Przycisk [Zamknij]
  - Focus trap (Tab cyklicznie w modalu)
  - Escape key zamyka modal

### Krok 5: Implementacja RegisterForm
- Stwórz stan lokalny: formData, errors, isSubmitting, isErrorModalOpen, apiErrors
- Implementacja handleChange dla każdego pola:
  - Aktualizacja formData
  - Walidacja inline (validateEmail, validatePassword, validateConfirmPassword)
  - Aktualizacja errors
- Implementacja validateEmail:
  - Sprawdzenie czy puste → "Email jest wymagany"
  - Sprawdzenie formatu regex → "Nieprawidłowy format email"
- Implementacja validatePassword:
  - Sprawdzenie czy puste → "Hasło jest wymagane"
  - Sprawdzenie długości ≥8 → "Hasło musi mieć min. 8 znaków"
  - Sprawdzenie regex wielka litera `/[A-Z]/` → "Hasło musi zawierać wielką literę"
  - Sprawdzenie regex cyfra `/[0-9]/` → "Hasło musi zawierać cyfrę"
  - Sprawdzenie regex znak specjalny `/[\W_]/` → "Hasło musi zawierać znak specjalny"
- Implementacja validateConfirmPassword:
  - Sprawdzenie czy puste → "Potwierdzenie hasła jest wymagane"
  - Sprawdzenie identyczności z password → "Hasła nie są identyczne"
- Implementacja handleSubmit:
  - Walidacja wszystkich pól
  - Jeśli błędy inline → return (nie wywołuj API)
  - setIsSubmitting(true)
  - Wywołanie apiService.register(formData)
  - Sukces (201): redirect `/login` (useNavigate z react-router)
  - Błąd (400 z errors): mapowanie do apiErrors, otwarcie ErrorModal
  - Błąd (5xx / network): generyczny komunikat w ErrorModal
  - finally: setIsSubmitting(false)
- Renderowanie:
  - `<form onSubmit={handleSubmit}>`
  - 3x `<InputField>` z odpowiednimi propsami
  - Hint text pod polem hasła: "Min. 8 znaków, 1 wielka litera, 1 cyfra, 1 znak specjalny"
  - `<button type="submit" disabled={isSubmitting}>`: "Zarejestruj się" (lub spinner jeśli isSubmitting)
  - `<ErrorModal isOpen={isErrorModalOpen} onClose={...} errors={apiErrors}>`

### Krok 6: Implementacja RegisterPage
- Stwórz layout strony:
  - `<main>`
  - Centered container (max-width 400px dla desktop)
  - `<h2>Zarejestruj się</h2>`
  - `<RegisterForm />`
  - `<p>Masz już konto? <Link to="/login">Zaloguj się</Link></p>`
- Styling Tailwind:
  - Centered layout: `className="flex items-center justify-center min-h-screen bg-gray-50"`
  - Form container: `className="bg-white p-8 rounded-lg shadow-md max-w-md w-full"`

### Krok 7: Dodanie endpointu do ApiService
- W `src/services/api-service.ts` dodaj metodę:
  ```typescript
  async register(data: RegisterRequest): Promise<RegisterResponse> {
    return this.request('/api/auth/register', {
      method: 'POST',
      body: JSON.stringify(data)
    });
  }
  ```

### Krok 8: Routing w React Router
- W `src/main.tsx` (lub router config) dodaj route:
  ```tsx
  {
    path: '/register',
    element: <RegisterPage />
  }
  ```
- Upewnij się, że route `/register` jest publiczny (brak ProtectedRoute wrapper)

### Krok 9: Testy manualne
- Test 1: Wypełnienie formularza z poprawnymi danymi → sukces → redirect `/login`
- Test 2: Email nieprawidłowy format → inline błąd "Nieprawidłowy format email"
- Test 3: Hasło za krótkie → inline błąd "Hasło musi mieć min. 8 znaków"
- Test 4: Hasło brak wielkiej litery → inline błąd "Hasło musi zawierać wielką literę"
- Test 5: Hasła niezgodne → inline błąd "Hasła nie są identyczne"
- Test 6: Email już zajęty (backend 400) → ErrorModal "Email jest już zajęty"
- Test 7: Błąd serwera 500 → ErrorModal "Wystąpił problem z serwerem"
- Test 8: Brak połączenia → ErrorModal z komunikatem NetworkError
- Test 9: Keyboard navigation (Tab przez pola, Enter submit)
- Test 10: Accessibility (screen reader, focus indicators)

### Krok 10: Responsywność
- Mobile: single column, full-width inputs, vertical stack
- Desktop: centered form (max-width 400px), większe buttony
- Test w Chrome DevTools (responsive mode) dla różnych rozdzielczości

### Krok 11: Polish translation audit
- Sprawdzenie wszystkich tekstów:
  - Nagłówek: "Zarejestruj się"
  - Labels: "Email", "Hasło", "Potwierdź hasło"
  - Hint: "Min. 8 znaków, 1 wielka litera, 1 cyfra, 1 znak specjalny"
  - Przycisk: "Zarejestruj się"
  - Link: "Masz już konto? Zaloguj się"
  - Wszystkie komunikaty błędów w języku polskim

### Krok 12: Finalizacja
- Code review (sprawdzenie kodu)
- Refactoring (jeśli potrzebny)
- Commit do repozytorium
- Przejście do kolejnego widoku (Login Page)
