# Plan implementacji widoku HomePage (Landing Page)

## 1. Przegląd

Widok HomePage jest publiczną stroną startową aplikacji LottoTM, która służy jako punkt wejścia dla nowych i niezalogowanych użytkowników. Głównym celem jest przedstawienie wartości produktu i zachęcenie użytkowników do rejestracji lub logowania w systemie. Widok powinien być minimalistyczny, skoncentrowany na kluczowych Call-to-Action buttonach, oraz przyjazny dla użytkownika.

## 2. Routing widoku

**Ścieżka:** `/` (root path, index route)

**Dostęp:** Publiczny (dostępny dla wszystkich użytkowników, bez wymaganej autentykacji)

**Logika przekierowania (opcjonalna):**
- Zalogowany użytkownik wchodzący na `/` może być automatycznie przekierowany na `/tickets` (główny widok po zalogowaniu)
- Ta logika powinna być implementowana za pomocą sprawdzenia `isLoggedIn` z AppContext i użycia `<Navigate to="/tickets" />` z React Router

## 3. Struktura komponentów

Widok HomePage składa się z głównego komponentu `HomePage` oraz może wykorzystywać następujące komponenty współdzielone:

```
HomePage
├── Logo/Branding (opcjonalnie)
├── Header Section
│   └── h1 (tytuł aplikacji)
├── Description Section
│   └── p (krótki opis wartości produktu)
└── CTA Buttons Section
    ├── Button "Zaloguj się" (primary)
    └── Button "Zarejestruj się" (secondary)
```

**Komponenty zewnętrzne:**
- `Button` (shared component) - dla przycisków CTA
- `useNavigate` (React Router) - dla przekierowań
- `useAppContext` - dla sprawdzenia stanu logowania

## 4. Szczegóły komponentów

### HomePage (główny komponent widoku)

**Opis komponentu:**
Główny kontener widoku Landing Page, odpowiedzialny za prezentację wartości aplikacji i nawigację do rejestracji/logowania. Komponent jest statyczny (nie wykonuje API calls), zawiera jedynie elementy prezentacyjne i przyciski nawigacyjne.

**Główne elementy HTML i komponenty:**
- `<main>` - semantyczny kontener głównej zawartości
- `<div>` - wrapper z klasami Tailwind dla centrowania i stylizacji
- `<h1>` - nagłówek z nazwą aplikacji
- `<p>` - krótki tekst opisowy
- `<div>` - kontener dla przycisków CTA
- 2x `<Button>` - komponenty przycisków z React Router navigation

**Obsługiwane zdarzenia:**
- `onClick` na przycisku "Zaloguj się" → przekierowanie do `/login`
- `onClick` na przycisku "Zarejestruj się" → przekierowanie do `/register`
- `useEffect` przy mount → sprawdzenie `isLoggedIn`, jeśli true → redirect `/tickets` (opcjonalnie)

**Warunki walidacji:**
- Brak walidacji (widok statyczny, bez formularzy)
- Opcjonalna walidacja: sprawdzenie `isLoggedIn` dla warunkowego przekierowania

**Typy (DTO i ViewModel) wymagane przez komponent:**
- Brak specyficznych DTO (komponent nie wykonuje API calls)
- Wykorzystuje typ `AppContextType` z AppContext dla dostępu do `isLoggedIn`

**Propsy komponentu:**
```typescript
// HomePage nie przyjmuje propsów - jest to standalone page component
interface HomePageProps {}
```

### Button (shared component - wykorzystywany)

**Opis komponentu:**
Komponent współdzielony dla przycisków CTA. Powinien wspierać różne warianty wizualne (primary, secondary) oraz obsługę onClick events.

**Propsy:**
```typescript
interface ButtonProps {
  variant: 'primary' | 'secondary';
  onClick: () => void;
  children: React.ReactNode;
  className?: string;
}
```

**Wykorzystanie w HomePage:**
- Button "Zaloguj się" - `variant="primary"`
- Button "Zarejestruj się" - `variant="secondary"`

## 5. Typy

### Typy wykorzystywane przez HomePage

**AppContextType** (z AppContext):
```typescript
type AppContextType = {
  user?: User | null;
  isLoggedIn: boolean;
  login: (user: User) => void;
  logout: () => void;
  getApiService: () => ApiService | undefined;
};
```

**Wykorzystanie:**
- `isLoggedIn: boolean` - sprawdzenie czy użytkownik jest zalogowany dla warunkowego przekierowania

**Brak nowych typów DTO:**
HomePage nie wymaga żadnych nowych typów DTO ani ViewModels, ponieważ nie wykonuje żadnych API calls ani nie zarządza złożonymi danymi.

## 6. Zarządzanie stanem

**Stan lokalny:**
HomePage nie wymaga lokalnego stanu (useState). Jest to komponent prezentacyjny bez interaktywnych formularzy czy dynamicznych danych.

**Stan globalny (AppContext):**
- Dostęp do `isLoggedIn` przez `useAppContext()` dla opcjonalnej logiki przekierowania

**Customowy hook:**
Nie jest wymagany customowy hook dla HomePage. Wystarczy standardowy hook `useAppContext()` oraz `useNavigate()` z React Router.

**Przykładowy kod zarządzania stanem:**
```typescript
const HomePage = () => {
  const { isLoggedIn } = useAppContext();
  const navigate = useNavigate();

  // Opcjonalne: auto-redirect zalogowanych użytkowników
  useEffect(() => {
    if (isLoggedIn) {
      navigate('/tickets', { replace: true });
    }
  }, [isLoggedIn, navigate]);

  const handleLoginClick = () => {
    navigate('/login');
  };

  const handleRegisterClick = () => {
    navigate('/register');
  };

  return (
    // JSX rendering
  );
};
```

## 7. Integracja API

**Brak integracji API:**
HomePage jest widokiem statycznym i nie wykonuje żadnych wywołań API. Nie ma żadnych typów żądania ani odpowiedzi.

**Ewentualne przyszłe rozszerzenia (poza MVP):**
- Możliwe pobranie statystyk aplikacji (np. liczba użytkowników, zestawów) do wyświetlenia na Landing Page
- Endpoint: `GET /api/stats` (nie istnieje w MVP)

## 8. Interakcje użytkownika

### Interakcja 1: Kliknięcie przycisku "Zaloguj się"

**Trigger:** Użytkownik klika przycisk "Zaloguj się"

**Akcja:**
1. Event `onClick` wywołuje funkcję `handleLoginClick`
2. Funkcja wywołuje `navigate('/login')`
3. React Router przekierowuje użytkownika na stronę logowania

**Oczekiwany rezultat:**
- Użytkownik widzi stronę logowania (`/login`)
- URL zmienia się z `/` na `/login`

### Interakcja 2: Kliknięcie przycisku "Zarejestruj się"

**Trigger:** Użytkownik klika przycisk "Zarejestruj się"

**Akcja:**
1. Event `onClick` wywołuje funkcję `handleRegisterClick`
2. Funkcja wywołuje `navigate('/register')`
3. React Router przekierowuje użytkownika na stronę rejestracji

**Oczekiwany rezultat:**
- Użytkownik widzi stronę rejestracji (`/register`)
- URL zmienia się z `/` na `/register`

### Interakcja 3: Automatyczne przekierowanie zalogowanego użytkownika (opcjonalna)

**Trigger:** Zalogowany użytkownik wchodzi na `/` (przez wpisanie URL lub link)

**Akcja:**
1. `useEffect` uruchamia się przy mount komponentu
2. Sprawdzenie warunku `if (isLoggedIn)`
3. Jeśli `true`, wywołanie `navigate('/tickets', { replace: true })`

**Oczekiwany rezultat:**
- Użytkownik jest automatycznie przekierowany na `/tickets`
- Nie ma wpisu w historii przeglądarki dla `/` (dzięki `replace: true`)
- Użytkownik nie widzi Landing Page, tylko bezpośrednio Tickets Page

### Interakcja 4: Nawigacja klawiaturą

**Trigger:** Użytkownik używa klawisza Tab do nawigacji

**Akcja:**
1. Focus przesuwa się w kolejności: Logo (jeśli klikalne) → Przycisk "Zaloguj się" → Przycisk "Zarejestruj się"
2. Użytkownik może aktywować przyciski klawiszem Enter lub Space

**Oczekiwany rezultat:**
- Widoczny focus indicator na aktywnym elemencie
- Możliwość pełnej nawigacji bez użycia myszy

## 9. Warunki i walidacja

HomePage nie zawiera formularzy ani pól input, więc nie wymaga walidacji danych wejściowych.

**Jedyny warunek:**
- Sprawdzenie `isLoggedIn` dla opcjonalnego automatycznego przekierowania
- Warunek: `if (isLoggedIn === true)` → redirect `/tickets`
- Ten warunek NIE wpływa na wyświetlanie UI (użytkownik nie widzi HomePage jeśli zalogowany)

**Brak walidacji biznesowej:**
HomePage jest widokiem publicznym, statycznym, bez żadnych operacji wymagających walidacji.

## 10. Obsługa błędów

HomePage nie wykonuje API calls ani operacji, które mogłyby generować błędy. Potencjalne scenariusze błędów są minimalne:

### Scenariusz 1: Problem z nawigacją (React Router)

**Błąd:** React Router nie może przekierować użytkownika (bardzo rzadki przypadek)

**Obsługa:**
- React Router powinien automatycznie obsłużyć błędy routingu
- W przypadku problemu użytkownik pozostanie na aktualnej stronie
- Brak specjalnej obsługi błędów wymagane w HomePage

### Scenariusz 2: AppContext undefined

**Błąd:** `useAppContext()` zwraca `undefined` (kontekst nie został dostarczony przez provider)

**Obsługa:**
- `useAppContext` hook powinien rzucić błąd z komunikatem: `"useAppContext must be used within a AppContextProvider"`
- Ten błąd jest obsługiwany na poziomie samego hooka (już zaimplementowane w app-context.ts)

### Scenariusz 3: JavaScript disabled w przeglądarce

**Błąd:** Użytkownik ma wyłączony JavaScript

**Obsługa:**
- Aplikacja React nie zadziała bez JavaScript
- Można dodać tag `<noscript>` z komunikatem: "Ta aplikacja wymaga włączonego JavaScript"
- To jest obsługa na poziomie całej aplikacji (index.html), nie specyficzne dla HomePage

**Brak ErrorModal:**
HomePage nie wymaga ErrorModal, ponieważ nie ma żadnych operacji, które mogłyby generować błędy wymagające interakcji użytkownika.

## 11. Kroki implementacji

### Krok 1: Przygotowanie struktury komponentu

**Akcja:**
- Otwórz plik `src/client/app01/src/pages/home/home-page.tsx`
- Usuń obecny kod testowy (API version check, login/logout buttons)
- Stwórz czystą strukturę komponentu funkcyjnego HomePage

**Kod początkowy:**
```typescript
import { useNavigate } from "react-router";
import { useAppContext } from "../../context/app-context";
import { useEffect } from "react";

function HomePage() {
  const { isLoggedIn } = useAppContext();
  const navigate = useNavigate();

  return (
    <main>
      {/* Zawartość zostanie dodana w kolejnych krokach */}
    </main>
  );
}

export default HomePage;
```

### Krok 2: Implementacja logiki automatycznego przekierowania (opcjonalna)

**Akcja:**
- Dodaj `useEffect` hook do sprawdzenia `isLoggedIn`
- Jeśli użytkownik jest zalogowany, przekieruj na `/tickets`

**Kod:**
```typescript
useEffect(() => {
  if (isLoggedIn) {
    navigate('/tickets', { replace: true });
  }
}, [isLoggedIn, navigate]);
```

**Walidacja:**
- Zaloguj się w aplikacji
- Wpisz URL `/` w pasku adresu
- Sprawdź czy automatycznie przekierowuje na `/tickets`

### Krok 3: Implementacja struktury HTML i layoutu

**Akcja:**
- Stwórz semantyczną strukturę HTML z użyciem Tailwind CSS
- Centered layout z max-width dla czytelności
- Mobile-first responsive design

**Kod:**
```typescript
return (
  <main className="min-h-screen flex items-center justify-center bg-gray-50 px-4">
    <div className="max-w-md w-full text-center space-y-8">
      {/* Logo/Branding - opcjonalne */}
      {/* <div className="mb-8">
        <svg className="mx-auto h-12 w-12 text-blue-600" />
      </div> */}
      
      {/* Header section */}
      <div className="space-y-4">
        <h1 className="text-4xl font-bold text-gray-900">
          LottoTM - System Zarządzania Kuponami LOTTO
        </h1>
        
        {/* Description section */}
        <p className="text-lg text-gray-600">
          Zarządzaj swoimi zestawami liczb LOTTO i automatycznie weryfikuj wygrane. 
          Szybko, wygodnie, bezpiecznie.
        </p>
      </div>
      
      {/* CTA Buttons section */}
      <div className="space-y-4 pt-8">
        {/* Przyciski zostaną dodane w następnym kroku */}
      </div>
    </div>
  </main>
);
```

**Klasy Tailwind:**
- `min-h-screen` - pełna wysokość viewportu
- `flex items-center justify-center` - centrowanie zawartości wertykalnie i horyzontalnie
- `bg-gray-50` - neutralne tło
- `px-4` - padding horizontal dla mobile
- `max-w-md` - maksymalna szerokość 448px (medium)
- `text-center` - wycentrowany tekst
- `space-y-8` - vertical spacing między sekcjami
- `text-4xl font-bold` - duży, pogrubiony nagłówek
- `text-lg text-gray-600` - średni tekst opisowy w subtelnym kolorze

### Krok 4: Stworzenie komponentu Button (shared component)

**Akcja:**
- Jeśli komponent Button nie istnieje, stwórz go w `src/client/app01/src/components/shared/Button.tsx`
- Zaimplementuj warianty `primary` i `secondary`

**Kod Button.tsx:**
```typescript
import React from 'react';

interface ButtonProps {
  variant: 'primary' | 'secondary' | 'danger';
  onClick: () => void;
  disabled?: boolean;
  className?: string;
  children: React.ReactNode;
  type?: 'button' | 'submit' | 'reset';
}

const Button: React.FC<ButtonProps> = ({
  variant,
  onClick,
  disabled = false,
  className = '',
  children,
  type = 'button',
}) => {
  const variantClasses = {
    primary: 'bg-blue-600 hover:bg-blue-700 text-white',
    secondary: 'bg-gray-200 hover:bg-gray-300 text-gray-800',
    danger: 'bg-red-600 hover:bg-red-700 text-white',
  };

  const baseClasses = 'px-6 py-3 rounded-lg font-medium transition-colors focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 disabled:opacity-50 disabled:cursor-not-allowed';

  return (
    <button
      type={type}
      onClick={onClick}
      disabled={disabled}
      className={`${baseClasses} ${variantClasses[variant]} ${className}`}
    >
      {children}
    </button>
  );
};

export default Button;
```

**Alternatywa:**
Jeśli komponent Button już istnieje, upewnij się że wspiera wymagane warianty i propsy.

### Krok 5: Dodanie przycisków CTA z obsługą kliknięć

**Akcja:**
- Zaimportuj komponent Button
- Dodaj dwa przyciski z handlerami kliknięć
- Ustaw odpowiednie warianty (primary, secondary)

**Import:**
```typescript
import Button from '../../components/shared/Button';
```

**Kod przycisków (w sekcji CTA Buttons):**
```typescript
<div className="space-y-4 pt-8">
  <Button
    variant="primary"
    onClick={() => navigate('/login')}
    className="w-full"
  >
    Zaloguj się
  </Button>
  
  <Button
    variant="secondary"
    onClick={() => navigate('/register')}
    className="w-full"
  >
    Zarejestruj się
  </Button>
</div>
```

**Klasy dodatkowe:**
- `w-full` - przyciski na pełną szerokość kontenera (lepsze dla mobile UX)

### Krok 6: Testowanie responsywności

**Akcja:**
- Otwórz aplikację w przeglądarce
- Użyj DevTools do testowania różnych rozmiarów ekranu
- Sprawdź czy layout działa poprawnie na mobile, tablet i desktop

**Scenariusze testowe:**
- **Mobile (320px-640px):**
  - Tytuł powinien być czytelny (możliwe zmniejszenie font-size)
  - Przyciski pełnej szerokości, łatwe do kliknięcia
  - Vertical spacing wystarczający
  
- **Tablet (640px-1024px):**
  - Centered content z max-width
  - Większe buttony (opcjonalnie)
  
- **Desktop (>1024px):**
  - Content centered z max-width 448px
  - Wystarczający padding wokół

**Ewentualne poprawki:**
Jeśli nagłówek jest za długi na mobile, możesz dodać responsive font-size:
```typescript
<h1 className="text-3xl sm:text-4xl font-bold text-gray-900">
```

### Krok 7: Dodanie dostępności (Accessibility)

**Akcja:**
- Upewnij się że używamy semantic HTML (`<main>`, `<h1>`, `<p>`, `<button>`)
- Sprawdź kontrast kolorów (WCAG AA minimum 4.5:1)
- Przetestuj keyboard navigation

**Checklist Accessibility:**
- [x] Semantic HTML: `<main>` jako główny kontener
- [x] Hierarchia nagłówków: `<h1>` dla głównego tytułu
- [x] Focus indicators: `focus:ring-2` na przyciskach (już w Button component)
- [x] Keyboard navigation: Tab order naturalny (h1 → opis → button1 → button2)
- [x] Color contrast: sprawdź w DevTools (Lighthouse Accessibility audit)

**Opcjonalne ulepszenia:**
- Dodaj `aria-label` do przycisków jeśli zawierają tylko ikony (nie dotyczy naszego przypadku - mamy text)
- Dodaj `lang="pl"` w `<html>` tagu (w index.html) dla screen readerów

### Krok 8: Dodanie opcjonalnego logo/brandingu

**Akcja (opcjonalna):**
- Jeśli posiadasz logo aplikacji, dodaj je nad nagłówkiem
- Możesz użyć SVG inline lub `<img>` tag

**Przykład z SVG (placeholder):**
```typescript
<div className="mb-8">
  <svg 
    className="mx-auto h-16 w-16 text-blue-600" 
    fill="currentColor" 
    viewBox="0 0 24 24"
    aria-hidden="true"
  >
    {/* SVG path - placeholder, zastąp właściwym logo */}
    <circle cx="12" cy="12" r="10" />
  </svg>
</div>
```

**Lub z obrazem:**
```typescript
<div className="mb-8">
  <img 
    src="/logo.svg" 
    alt="LottoTM Logo" 
    className="mx-auto h-16 w-auto"
  />
</div>
```

### Krok 9: Testowanie interakcji użytkownika

**Akcja:**
- Przetestuj wszystkie scenariusze interakcji opisane w sekcji 8

**Test 1: Kliknięcie "Zaloguj się"**
1. Otwórz `/` w przeglądarce
2. Kliknij przycisk "Zaloguj się"
3. Sprawdź czy przekierowuje na `/login`

**Test 2: Kliknięcie "Zarejestruj się"**
1. Otwórz `/` w przeglądarce
2. Kliknij przycisk "Zarejestruj się"
3. Sprawdź czy przekierowuje na `/register`

**Test 3: Automatyczne przekierowanie zalogowanego**
1. Zaloguj się w aplikacji (przejdź przez `/login`)
2. Wpisz URL `/` w pasku adresu
3. Sprawdź czy automatycznie przekierowuje na `/tickets`
4. Sprawdź że nie ma wpisu `/` w historii przeglądarki (dzięki `replace: true`)

**Test 4: Keyboard navigation**
1. Otwórz `/` w przeglądarce
2. Naciśnij Tab kilka razy
3. Sprawdź czy focus przesuwa się w logicznej kolejności
4. Zaznacz przycisk i naciśnij Enter/Space
5. Sprawdź czy następuje przekierowanie

### Krok 10: Finalizacja i code review

**Akcja:**
- Przejrzyj kod pod kątem czystości i best practices
- Upewnij się że wszystkie importy są poprawne
- Usuń nieużywane zmienne i komentarze

**Checklist:**
- [x] Importy na górze pliku (React hooks, Router, AppContext, Button)
- [x] Komponent eksportowany jako default
- [x] Tailwind classes czytelne i spójne
- [x] Brak console.log ani testowego kodu
- [x] Typy TypeScript poprawne (brak `any`)
- [x] Spójność z resztą aplikacji (style, konwencje nazewnictwa)

**Końcowy kod HomePage.tsx (kompletny przykład):**
```typescript
import { useEffect } from "react";
import { useNavigate } from "react-router";
import { useAppContext } from "../../context/app-context";
import Button from "../../components/shared/Button";

function HomePage() {
  const { isLoggedIn } = useAppContext();
  const navigate = useNavigate();

  // Automatyczne przekierowanie zalogowanych użytkowników
  useEffect(() => {
    if (isLoggedIn) {
      navigate('/tickets', { replace: true });
    }
  }, [isLoggedIn, navigate]);

  return (
    <main className="min-h-screen flex items-center justify-center bg-gray-50 px-4">
      <div className="max-w-md w-full text-center space-y-8">
        {/* Header section */}
        <div className="space-y-4">
          <h1 className="text-3xl sm:text-4xl font-bold text-gray-900">
            LottoTM - System Zarządzania Kuponami LOTTO
          </h1>
          
          {/* Description section */}
          <p className="text-lg text-gray-600">
            Zarządzaj swoimi zestawami liczb LOTTO i automatycznie weryfikuj wygrane. 
            Szybko, wygodnie, bezpiecznie.
          </p>
        </div>
        
        {/* CTA Buttons section */}
        <div className="space-y-4 pt-8">
          <Button
            variant="primary"
            onClick={() => navigate('/login')}
            className="w-full"
          >
            Zaloguj się
          </Button>
          
          <Button
            variant="secondary"
            onClick={() => navigate('/register')}
            className="w-full"
          >
            Zarejestruj się
          </Button>
        </div>
      </div>
    </main>
  );
}

export default HomePage;
```

### Krok 11: Dokumentacja i commit

**Akcja:**
- Zapisz wszystkie zmiany
- Dodaj commit message zgodnie z konwencją projektu
- Opcjonalnie: dodaj komentarze w kodzie dla przyszłych developerów

**Przykładowy commit message:**
```
feat: Implement HomePage (Landing Page) with CTA buttons

- Added centered layout with max-width for readability
- Implemented automatic redirect for logged-in users
- Created reusable Button component with primary/secondary variants
- Ensured mobile-first responsive design
- Added accessibility features (semantic HTML, focus indicators, keyboard navigation)
- Integrated with AppContext and React Router for navigation
```

---

## Podsumowanie

Plan implementacji HomePage jest kompletny i gotowy do wdrożenia. Widok jest stosunkowo prosty (statyczny, bez API calls), ale spełnia wszystkie wymagania PRD i UI Plan:

✅ **Publiczny dostęp** - dostępny dla niezalogowanych użytkowników  
✅ **Minimalistyczny design** - focused na CTA buttons  
✅ **Język polski** - wszystkie teksty po polsku  
✅ **Responsywność** - mobile-first z Tailwind CSS  
✅ **Dostępność** - semantic HTML, keyboard navigation, focus indicators  
✅ **Integracja z AppContext** - automatyczne przekierowanie zalogowanych  
✅ **React Router** - nawigacja do /login i /register  

Szacowany czas implementacji: **2-3 godziny** (włącznie z testowaniem i code review).
