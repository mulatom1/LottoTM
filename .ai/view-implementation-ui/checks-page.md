# Plan implementacji widoku Checks Page

## 1. Przegląd

Checks Page to widok umożliwiający użytkownikom weryfikację swoich zestawów liczb LOTTO względem wyników losowań w określonym zakresie czasowym. System automatycznie identyfikuje wygrane (3 lub więcej trafień) i prezentuje je w przejrzysty sposób z wyróżnieniem trafionych liczb.

**Główne funkcjonalności:**
- Wybór zakresu dat do weryfikacji (domyślnie ostatni tydzień, maksymalnie 3 lata)
- Opcjonalne filtrowanie kuponów według nazwy grupy (groupName) z wyszukiwaniem częściowym (Contains)
- Automatyczne porównanie zestawów użytkownika z losowaniami w wybranym okresie
- Prezentacja wyników w formie rozwijalnego accordion (grupowanie po losowaniach)
- Wyróżnienie trafionych liczb (pogrubienie)
- Wizualne oznaczenie wygranych (badges dla ≥3 trafień)

## 2. Routing widoku

**Ścieżka:** `/checks`

**Dostęp:** Chroniony - wymaga autentykacji

**Przekierowania:**
- Niezalogowany użytkownik → redirect `/login`
- Po zalogowaniu dostępny przez zakładkę "Sprawdź Wygrane" w nawigacji

## 3. Struktura komponentów

```
ChecksPage (główny komponent strony)
├── CheckPanel (panel z formularzem zakresu dat)
│   ├── DatePicker (data Od)
│   ├── DatePicker (data Do)
│   ├── TextInput (nazwa grupy - opcjonalnie)
│   └── Button (Sprawdź wygrane)
├── Spinner (wyświetlany podczas weryfikacji)
└── CheckResults (sekcja z wynikami)
    └── AccordionItem[] (dla każdego losowania)
        ├── AccordionHeader (data + drawSystemId + typ gry + wylosowane liczby)
        └── AccordionContent (lista zestawów użytkownika)
            └── ResultTicketItem[] (dla każdego zestawu)
                ├── Ticket info (nazwa grupy)
                ├── Ticket numbers (niebieskie kółka dla trafionych, szare dla nietrafionych)
                └── Win info box (ramka - tylko dla ≥3 trafień)
                    ├── WinBadge (badge stopnia wygranej)
                    └── Win details grid (koszt kuponu, ilość wygranych, wartość wygranej)
```

## 4. Szczegóły komponentów

### 4.1 ChecksPage (główny komponent)

**Opis komponentu:**
Główny kontener strony weryfikacji wygranych. Zarządza stanem formularza zakresu dat, wywołuje API weryfikacji i renderuje wyniki w formie accordion.

**Główne elementy HTML i komponenty:**
- `<main>` - kontener główny
- `<h1>` - nagłówek strony "Sprawdź swoje wygrane"
- `<CheckPanel />` - panel z formularzem zakresu dat
- `<Spinner />` - wskaźnik ładowania (warunkowe renderowanie)
- `<CheckResults />` - sekcja z wynikami (warunkowe renderowanie)

**Obsługiwane zdarzenia:**
- `onSubmitCheck(dateFrom, dateTo, groupName)` - wywołanie API weryfikacji
- `onDateChange` - aktualizacja stanu dat w formularzu
- `onGroupNameChange` - aktualizacja nazwy grupy w formularzu

**Warunki walidacji:**
- `dateFrom` nie może być późniejsza niż `dateTo`
- Zakres dat nie może przekraczać 3 lat (walidacja na backendzie)
- Daty muszą być w formacie YYYY-MM-DD

**Typy (DTO i ViewModel):**
- `CheckRequest` - request do API
- `CheckResponse` - response z API
- `VerificationResult[]` - wyniki weryfikacji
- `DateRange` - zakres dat (Od/Do)

**Propsy:**
Brak (komponent strony, nie przyjmuje propsów)

**Stan lokalny:**
```typescript
interface ChecksPageState {
  dateFrom: string;          // Format YYYY-MM-DD
  dateTo: string;            // Format YYYY-MM-DD
  groupName: string;         // Nazwa grupy (opcjonalnie)
  isLoading: boolean;        // Stan ładowania
  results: VerificationResult[] | null;  // Wyniki weryfikacji
  error: string | null;      // Komunikat błędu
}
```

---

### 4.2 CheckPanel (panel formularza)

**Opis komponentu:**
Panel zawierający formularz wyboru zakresu dat z dwoma date pickerami i przyciskiem submit. Odpowiada za walidację dat przed wysłaniem requestu.

**Główne elementy HTML:**
- `<div>` - kontener panelu (Tailwind: `bg-white p-6 rounded-lg shadow`)
- `<form>` - formularz
- `<div>` - wrapper dla date pickerów (flex layout)
- `<DatePicker />` × 2 - inputy dat (Od/Do)
- `<TextInput />` - input dla nazwy grupy (opcjonalny)
- `<Button />` - przycisk "Sprawdź wygrane"

**Obsługiwane interakcje:**
- `onChange` dla date pickerów - aktualizacja stanu
- `onChange` dla pola groupName - aktualizacja stanu
- `onSubmit` formularza - wywołanie callback `onSubmit(dateFrom, dateTo, groupName)`
- Inline validation - komunikaty błędów pod polami

**Obsługiwana walidacja:**
- Walidacja inline: `dateFrom ≤ dateTo`
- Komunikat błędu: "Data 'Od' musi być wcześniejsza lub równa 'Do'"
- Wyświetlanie komunikatu pod polem "Do"
- Dezaktywacja buttona submit jeśli walidacja nie przechodzi

**Typy:**
```typescript
interface DateRange {
  dateFrom: string;
  dateTo: string;
}
```

**Propsy:**
```typescript
interface CheckPanelProps {
  onSubmit: (dateFrom: string, dateTo: string, groupName: string) => void;
  isLoading: boolean;  // Dla dezaktywacji buttona podczas ładowania
}
```

---

### 4.3 CheckResults (kontener wyników)

**Opis komponentu:**
Kontener renderujący wyniki weryfikacji w formie accordion. Każde losowanie to osobny accordion item z listą zestawów użytkownika.

**Główne elementy:**
- `<div>` - kontener główny
- `<div>` - info o liczbie wyników (opcjonalnie: "Znaleziono X losowań")
- `<AccordionItem[]>` - lista accordion items (dla każdego losowania)
- Empty state `<div>` - gdy brak wygranych: "Nie znaleziono wygranych w wybranym zakresie dat."

**Obsługiwane interakcje:**
- Brak bezpośrednich interakcji (delegowane do AccordionItem)

**Obsługiwana walidacja:**
- Brak

**Typy:**
```typescript
interface VerificationResult {
  drawId: number;
  drawDate: string;
  drawNumbers: number[];
  tickets: TicketMatch[];
}

interface TicketMatch {
  ticketId: string;
  numbers: number[];
  matchCount: number;
  matchedNumbers: number[];
}
```

**Propsy:**
```typescript
interface CheckResultsProps {
  results: VerificationResult[];
  loading: boolean;
}
```

---

### 4.4 AccordionItem (pojedyncze losowanie)

**Opis komponentu:**
Rozwijany accordion item reprezentujący pojedyncze losowanie. Header zawiera datę, ID systemowe losowania, typ gry i wylosowane liczby. Content zawiera listę zestawów użytkownika z wyróżnionymi trafieniami oraz szczegółowymi informacjami o wygranej (stopień, cena kuponu, ilość wygranych, wartość wygranej).

**Główne elementy HTML:**
- `<div>` - kontener accordion item
- `<button>` - header (klikany, aria-expanded)
  - `<span>` - ikona strzałki (▼/▶)
  - `<div>` - informacje o losowaniu:
    - `<span>` - data losowania
    - `<span>` - ID systemowe (drawSystemId)
    - `<span>` - typ gry (badge: LOTTO / LOTTO PLUS)
    - `<div>` - wylosowane liczby (6 kółek)
- `<div>` - content (collapsible, aria-hidden gdy collapsed)
  - `<ResultTicketItem[]>` - lista zestawów użytkownika z dodatkowymi informacjami o wygranej

**Obsługiwane interakcje:**
- `onClick` na header - toggle expand/collapse
- Keyboard: Enter/Space - toggle expand/collapse
- Stan rozwinięcia zarządzany lokalnie (useState)

**Obsługiwana walidacja:**
- Brak

**Typy:**
Jak w CheckResults (VerificationResult)

**Propsy:**
```typescript
interface AccordionItemProps {
  draw: {
    drawDate: string;
    drawSystemId: number;
    lottoType: string;
    drawNumbers: number[];
    ticketPrice: number | null;
    winPoolCount1: number | null;
    winPoolAmount1: number | null;
    winPoolCount2: number | null;
    winPoolAmount2: number | null;
    winPoolCount3: number | null;
    winPoolAmount3: number | null;
    winPoolCount4: number | null;
    winPoolAmount4: number | null;
  };
  tickets: TicketMatch[];
  defaultExpanded?: boolean;  // Domyślnie true dla pierwszego losowania
}
```

**Stan lokalny:**
```typescript
const [isExpanded, setIsExpanded] = useState(defaultExpanded);
```

---

### 4.5 ResultTicketItem (pojedynczy zestaw w wynikach)

**Opis komponentu:**
Prezentacja pojedynczego zestawu użytkownika z wyróżnionymi trafionymi liczbami (niebieskie kółka dla trafionych, szare dla nietrafionych) i ramką z szczegółowymi informacjami o wygranej (stopień, cena kuponu, ilość wygranych uczestników, wartość wygranej).

**Główne elementy HTML:**
- `<div>` - kontener główny zestawu
- `<div>` - sekcja z numerami kuponu
  - `<div>` - informacje o kuponie (nazwa grupy jako badge)
  - `<div>` - lista liczb
    - `<span>` × 6 - liczby zestawu (trafione: niebieskie kółka z `font-bold`, nietrafione: szare kółka)
- `<div>` - ramka z informacjami o wygranej (warunkowe renderowanie dla hits ≥ 3)
  - `<div>` - badge wygranej (WinBadge) z ikoną i tekstem "Wygrana X stopnia"
  - `<div>` - szczegóły wygranej (grid layout):
    - `<div>` - Koszt kuponu: {ticketPrice} zł (lub "Brak danych" jeśli null)
    - `<div>` - Ilość wygranych: {winPoolCountX} osób (lub "Brak danych" jeśli null)
    - `<div>` - Wartość wygranej: {winPoolAmountX} zł (lub "Brak danych" jeśli null)
  - Gdzie X to stopień wygranej zależny od hits: 6 hits = tier 1, 5 hits = tier 2, 4 hits = tier 3, 3 hits = tier 4
- `<span>` - tekst "Brak trafień" (dla hits < 3, zamiast ramki)

**Obsługiwane interakcje:**
- Brak (komponent prezentacyjny)

**Obsługiwana walidacja:**
- Brak

**Typy:**
```typescript
interface TicketMatch {
  ticketId: number;
  groupName: string;
  ticketNumbers: number[];
  hits: number;
  winningNumbers: number[];
}
```

**Propsy:**
```typescript
interface ResultTicketItemProps {
  ticket: TicketMatch;
  drawData: {
    ticketPrice: number | null;
    winPoolCount1: number | null;
    winPoolAmount1: number | null;
    winPoolCount2: number | null;
    winPoolAmount2: number | null;
    winPoolCount3: number | null;
    winPoolAmount3: number | null;
    winPoolCount4: number | null;
    winPoolAmount4: number | null;
  };
}
```

**Logika renderowania:**
```typescript
// Dla każdej liczby w zestawie
ticketNumbers.map(num => {
  const isMatched = winningNumbers.includes(num);
  return (
    <span className={`
      inline-flex items-center justify-center w-8 h-8 rounded-full text-sm
      ${isMatched
        ? 'font-bold bg-blue-600 text-white'  // Trafione: niebieskie
        : 'bg-gray-100 text-gray-700'         // Nietrafione: szare
      }
    `}>
      {num}
    </span>
  );
})

// Mapowanie hits → tier (stopień wygranej)
const getTierData = (hits: number) => {
  switch (hits) {
    case 6: return { tier: 1, count: drawData.winPoolCount1, amount: drawData.winPoolAmount1 };
    case 5: return { tier: 2, count: drawData.winPoolCount2, amount: drawData.winPoolAmount2 };
    case 4: return { tier: 3, count: drawData.winPoolCount3, amount: drawData.winPoolAmount3 };
    case 3: return { tier: 4, count: drawData.winPoolCount4, amount: drawData.winPoolAmount4 };
    default: return null;
  }
};

// Ramka z informacjami o wygranej (tylko dla hits >= 3)
{hits >= 3 && (
  <div className="mt-3 p-4 bg-green-50 border border-green-200 rounded-lg">
    {/* Badge wygranej */}
    <div className="mb-2">
      <WinBadge count={hits as WinLevel} />
    </div>

    {/* Szczegóły wygranej */}
    <div className="grid grid-cols-3 gap-4 text-sm">
      <div>
        <span className="text-gray-600">Koszt kuponu:</span>
        <div className="font-semibold">
          {drawData.ticketPrice !== null ? `${drawData.ticketPrice.toFixed(2)} zł` : 'Brak danych'}
        </div>
      </div>
      <div>
        <span className="text-gray-600">Ilość wygranych:</span>
        <div className="font-semibold">
          {getTierData(hits)?.count !== null ? `${getTierData(hits)!.count} osób` : 'Brak danych'}
        </div>
      </div>
      <div>
        <span className="text-gray-600">Wartość wygranej:</span>
        <div className="font-semibold">
          {getTierData(hits)?.amount !== null ? `${getTierData(hits)!.amount.toFixed(2)} zł` : 'Brak danych'}
        </div>
      </div>
    </div>
  </div>
)}

{/* Brak trafień */}
{hits < 3 && <span className="text-gray-500 text-sm">Brak trafień</span>}
```

---

### 4.6 WinBadge (badge wygranej)

**Opis komponentu:**
Badge wyświetlający liczbę trafień z emoji i opisem. Kolorystyka zależna od liczby trafień.

**Główne elementy HTML:**
- `<span>` - badge (Tailwind: `px-3 py-1 rounded-full text-sm font-medium`)
- Emoji + tekst: "🏆 Wygrana 3 (trójka)"

**Obsługiwane interakcje:**
- Brak

**Obsługiwana walidacja:**
- Brak

**Typy:**
```typescript
type WinLevel = 3 | 4 | 5 | 6;
```

**Propsy:**
```typescript
interface WinBadgeProps {
  count: WinLevel;
}
```

**Warianty kolorystyczne:**
- 3 trafienia: `bg-green-100 text-green-800` + 🏆 "Wygrana 3 (trójka)"
- 4 trafienia: `bg-blue-100 text-blue-800` + 🏆 "Wygrana 4 (czwórka)"
- 5 trafień: `bg-orange-100 text-orange-800` + 🏆 "Wygrana 5 (piątka)"
- 6 trafień: `bg-red-100 text-red-800` (lub złoty gradient) + 🎉 "Wygrana 6 (szóstka)"

---

### 4.7 DatePicker (komponent współdzielony)

**Opis komponentu:**
Input type="date" z labelem i obsługą błędów walidacji.

**Główne elementy HTML:**
- `<div>` - wrapper
- `<label>` - label (powiązany z input via htmlFor)
- `<input type="date">` - natywny HTML5 date picker
- `<span>` - komunikat błędu (warunkowe renderowanie)

**Obsługiwane interakcje:**
- `onChange` - callback przy zmianie daty
- Inline validation

**Obsługiwana walidacja:**
- Format YYYY-MM-DD (natywna walidacja HTML5)
- Custom validation (min/max dates)
- Wyświetlanie error message pod inputem

**Typy:**
```typescript
type DateString = string;  // Format YYYY-MM-DD
```

**Propsy:**
```typescript
interface DatePickerProps {
  label: string;
  value: string;
  onChange: (value: string) => void;
  error?: string;
  min?: string;
  max?: string;
  required?: boolean;
}
```

---

## 5. Typy

### 5.1 Typy Request/Response API

```typescript
// Request do POST /api/verification/check
interface CheckRequest {
  dateFrom: string;  // YYYY-MM-DD
  dateTo: string;    // YYYY-MM-DD
  groupName?: string; // Opcjonalny filtr nazwy grupy (wyszukiwanie częściowe, case-insensitive)
}

// Response z API
interface CheckResponse {
  results: VerificationResult[];
  totalTickets: number;
  totalDraws: number;
  executionTimeMs: number;
}

interface VerificationResult {
  ticketId: number;
  groupName: string;
  ticketNumbers: number[];
  draws: DrawVerificationResult[];
}

interface DrawVerificationResult {
  drawId: number;
  drawDate: string;
  drawSystemId: number; // ID systemowe losowania
  lottoType: string; // "LOTTO" lub "LOTTO PLUS"
  drawNumbers: number[];
  hits: number;
  winningNumbers: number[];
  ticketPrice: number | null; // Cena kuponu (może być null)
  winPoolCount1: number | null; // Ilość wygranych dla 6 trafień
  winPoolAmount1: number | null; // Wartość wygranej dla 6 trafień
  winPoolCount2: number | null; // Ilość wygranych dla 5 trafień
  winPoolAmount2: number | null; // Wartość wygranej dla 5 trafień
  winPoolCount3: number | null; // Ilość wygranych dla 4 trafień
  winPoolAmount3: number | null; // Wartość wygranej dla 4 trafień
  winPoolCount4: number | null; // Ilość wygranych dla 3 trafień
  winPoolAmount4: number | null; // Wartość wygranej dla 3 trafień
}

// Typ pomocniczy po transformacji w CheckResults (grupowanie po draws)
interface DrawWithTickets {
  drawId: number;
  drawDate: string;
  drawSystemId: number;
  lottoType: string;
  drawNumbers: number[];
  ticketPrice: number | null;
  winPoolCount1: number | null;
  winPoolAmount1: number | null;
  winPoolCount2: number | null;
  winPoolAmount2: number | null;
  winPoolCount3: number | null;
  winPoolAmount3: number | null;
  winPoolCount4: number | null;
  winPoolAmount4: number | null;
  tickets: TicketMatch[];
}

interface TicketMatch {
  ticketId: number;
  groupName: string;
  ticketNumbers: number[];
  hits: number;
  winningNumbers: number[];
}
```

### 5.2 Typy ViewModel

```typescript
// Zakres dat formularza
interface DateRange {
  dateFrom: string;
  dateTo: string;
}

// Stan ładowania i błędów
interface CheckState {
  isLoading: boolean;
  error: string | null;
  results: VerificationResult[] | null;
}

// Poziom wygranej
type WinLevel = 3 | 4 | 5 | 6;

// Warianty kolorystyczne badge'a
type BadgeVariant = 'green' | 'blue' | 'orange' | 'red';

// Mapowanie WinLevel → BadgeVariant
const BADGE_VARIANTS: Record<WinLevel, BadgeVariant> = {
  3: 'green',
  4: 'blue',
  5: 'orange',
  6: 'red'
};

// Mapowanie WinLevel → tekst
const WIN_LABELS: Record<WinLevel, string> = {
  3: 'Wygrana 3 (trójka)',
  4: 'Wygrana 4 (czwórka)',
  5: 'Wygrana 5 (piątka)',
  6: 'Wygrana 6 (szóstka)'
};
```

## 6. Zarządzanie stanem

**Strategia:** Stan lokalny w ChecksPage, brak custom hook w MVP

**Stan zarządzany w ChecksPage:**
```typescript
const [dateFrom, setDateFrom] = useState<string>(getDefaultDateFrom());  // -1 tydzień
const [dateTo, setDateTo] = useState<string>(getDefaultDateTo());        // dzisiaj
const [groupName, setGroupName] = useState<string>('');                   // puste domyślnie
const [isLoading, setIsLoading] = useState<boolean>(false);
const [results, setResults] = useState<VerificationResult[] | null>(null);
const [error, setError] = useState<string | null>(null);
```

**Helper functions:**
```typescript
// Obliczanie domyślnej daty Od (-1 tydzień)
function getDefaultDateFrom(): string {
  const date = new Date();
  date.setDate(date.getDate() - 7);
  return date.toISOString().split('T')[0];  // YYYY-MM-DD
}

// Obliczanie domyślnej daty Do (dzisiaj)
function getDefaultDateTo(): string {
  return new Date().toISOString().split('T')[0];  // YYYY-MM-DD
}
```

**Przepływ danych:**
1. Użytkownik zmienia daty → aktualizacja `dateFrom/dateTo` (setState)
2. Użytkownik klika "Sprawdź wygrane" → wywołanie `handleSubmit()`
3. `handleSubmit()` → walidacja → API call → `setIsLoading(true)`
4. Response z API → `setResults()` + `setIsLoading(false)`
5. Błąd API → `setError()` + ErrorModal

**Walidacja przed submit:**
```typescript
function validateDateRange(dateFrom: string, dateTo: string): string | null {
  if (dateFrom > dateTo) {
    return "Data 'Od' musi być wcześniejsza lub równa 'Do'";
  }

  // Sprawdzenie zakresu 3 lat (opcjonalnie na frontendzie)
  const from = new Date(dateFrom);
  const to = new Date(dateTo);
  const diffDays = Math.ceil((to.getTime() - from.getTime()) / (1000 * 60 * 60 * 24));

  if (diffDays > 1095) {  // 3 lata ≈ 1095 dni
    return "Zakres dat nie może przekraczać 3 lat";
  }

  return null;  // Valid
}
```

**Custom hook (opcjonalnie dla refactoringu post-MVP):**
```typescript
function useVerification() {
  const [state, setState] = useState<CheckState>({
    isLoading: false,
    error: null,
    results: null
  });

  const checkWinnings = async (dateFrom: string, dateTo: string) => {
    setState({ isLoading: true, error: null, results: null });

    try {
      const apiService = getApiService();
      const response = await apiService.checkWinnings(dateFrom, dateTo);
      setState({ isLoading: false, error: null, results: response.results });
    } catch (error) {
      setState({ isLoading: false, error: error.message, results: null });
    }
  };

  return { ...state, checkWinnings };
}
```

## 7. Integracja API

**Endpoint:** `POST /api/verification/check`

**Request payload:**
```json
{
  "dateFrom": "2025-10-09",
  "dateTo": "2025-11-09",
  "groupName": "Ulubione"
}
```

**Uwaga:** Pole `groupName` jest opcjonalne. Jeśli jest podane, API zwróci wyniki tylko dla kuponów, których nazwa grupy zawiera podany tekst (wyszukiwanie częściowe, case-insensitive). Jeśli nie jest podane lub jest puste, API zwróci wyniki dla wszystkich kuponów użytkownika.

**Response payload (sukces):**
```json
{
  "results": [
    {
      "ticketId": 1001,
      "groupName": "Ulubione",
      "ticketNumbers": [5, 12, 19, 25, 31, 44],
      "draws": [
        {
          "drawId": 123,
          "drawDate": "2025-11-08",
          "drawSystemId": 20250001,
          "lottoType": "LOTTO",
          "drawNumbers": [5, 12, 18, 25, 37, 44],
          "hits": 4,
          "winningNumbers": [5, 12, 25, 44],
          "ticketPrice": 3.00,
          "winPoolCount1": 2,
          "winPoolAmount1": 5000000.00,
          "winPoolCount2": 15,
          "winPoolAmount2": 50000.00,
          "winPoolCount3": 120,
          "winPoolAmount3": 500.00,
          "winPoolCount4": 850,
          "winPoolAmount4": 20.00
        }
      ]
    }
  ],
  "totalTickets": 42,
  "totalDraws": 10,
  "executionTimeMs": 1234
}
```

**ApiService method:**
```typescript
class ApiService {
  async checkWinnings(dateFrom: string, dateTo: string, groupName?: string): Promise<CheckResponse> {
    const payload: CheckRequest = { dateFrom, dateTo };

    // Dodaj groupName tylko jeśli nie jest puste
    if (groupName && groupName.trim() !== '') {
      payload.groupName = groupName;
    }

    const response = await this.request('/api/verification/check', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${this.token}`
      },
      body: JSON.stringify(payload)
    });

    return response.json();
  }
}
```

**Error handling:**
```typescript
async function handleSubmit() {
  // Walidacja
  const validationError = validateDateRange(dateFrom, dateTo);
  if (validationError) {
    setError(validationError);
    return;
  }

  setIsLoading(true);
  setError(null);

  try {
    const apiService = getApiService();
    const response = await apiService.checkWinnings(dateFrom, dateTo, groupName);
    setResults(response.results);
  } catch (error) {
    if (error instanceof ApiError) {
      if (error.status >= 400 && error.status < 500) {
        // Błąd walidacji z backendu
        showErrorModal(error.data.errors || error.data.error);
      } else {
        // Błąd serwera
        showErrorModal('Wystąpił problem z serwerem. Spróbuj ponownie za chwilę.');
      }
    } else if (error instanceof NetworkError) {
      showErrorModal('Brak połączenia z serwerem. Sprawdź swoje połączenie internetowe.');
    }
  } finally {
    setIsLoading(false);
  }
}
```

**Warunki weryfikowane przez API:**
- `dateFrom ≤ dateTo` - walidacja na backendzie
- Zakres dat max 3 lata - walidacja na backendzie
- Użytkownik musi być zalogowany (JWT w header)
- Backend filtruje zestawy po `UserId` z tokenu (izolacja danych)

## 8. Interakcje użytkownika

### 8.1 Scenariusz podstawowy - weryfikacja wygranych

1. **Użytkownik wchodzi na stronę /checks**
   - Widzi formularz z pre-wypełnionym zakresem dat (ostatni tydzień)
   - Date pickers: Od = dzisiaj - 7 dni, Do = dzisiaj
   - Pole groupName: puste (opcjonalne)

2. **Użytkownik opcjonalnie zmienia daty i/lub filtr grupy**
   - Klika w pole "Od" → wybiera datę z date pickera
   - Klika w pole "Do" → wybiera datę z date pickera
   - Opcjonalnie wpisuje fragment nazwy grupy (np. "ulu" lub "Ulubione") - wyszukiwanie częściowe znajdzie wszystkie grupy zawierające ten tekst
   - Inline validation: jeśli Od > Do → komunikat błędu pod polem "Do"

3. **Użytkownik klika "Sprawdź wygrane"**
   - Walidacja przechodzi → button disabled, lokalny spinner pojawia się
   - API call: `POST /api/verification/check`

4. **Backend przetwarza request (≤2s dla 100 zestawów)**
   - Pobieranie zestawów użytkownika + losowań z zakresu
   - Algorytm weryfikacji (LINQ Intersect)
   - Zwrot wyników

5. **Frontend renderuje wyniki**
   - Spinner znika
   - Accordion pojawia się z wynikami
   - Pierwsze losowanie rozwinięte (defaultExpanded=true)
   - Użytkownik widzi w headerze losowania:
     - Data losowania: 2025-11-08
     - ID systemowe: 20250001
     - Typ gry: LOTTO (badge zielony)
     - Wylosowane liczby: [5, 12, 18, 25, 37, 44] (niebieskie kółka)
   - Po rozwinięciu, użytkownik widzi listę swoich kuponów z trafieniami:
     - Nazwa grupy: "Ulubione" (badge szary)
     - Liczby kuponu: [5, 12, 19, 25, 31, 44]
       - Trafione liczby (niebieskie kółka z font-bold): 5, 12, 25, 44
       - Nietrafione liczby (szare kółka): 19, 31
     - Ramka z informacjami o wygranej (zielone tło):
       - Badge: 🏆 Wygrana 4 (czwórka)
       - Koszt kuponu: 3.00 zł
       - Ilość wygranych: 120 osób (tier 3 dla 4 trafień)
       - Wartość wygranej: 500.00 zł

6. **Użytkownik rozwinął kolejne losowania**
   - Klika na header accordion item → toggle expand/collapse
   - Przegląda wyniki dla każdego losowania

### 8.2 Scenariusz alternatywny - brak wygranych

1. Użytkownik wykonuje weryfikację (kroki 1-4 jak wyżej)
2. Backend zwraca `results: []` (brak wygranych)
3. Frontend renderuje empty state:
   - "Nie znaleziono wygranych w wybranym zakresie dat."

### 8.3 Scenariusz błędu - nieprawidłowy zakres dat

1. Użytkownik zmienia datę "Od" na późniejszą niż "Do"
2. Inline validation: komunikat błędu "Data 'Od' musi być wcześniejsza lub równa 'Do'" pod polem "Do"
3. Button "Sprawdź wygrane" disabled
4. Użytkownik poprawia datę → błąd znika, button enabled

### 8.4 Scenariusz błędu - przekroczenie zakresu 3 lat

1. Użytkownik wybiera zakres > 3 lat
2. Opcjonalnie: frontend walidacja inline (komunikat błędu)
3. Lub: backend walidacja → ErrorModal:
   - Tytuł: "Błąd"
   - Treść: "Zakres dat nie może przekraczać 3 lat"
   - Button: [Zamknij]

### 8.5 Interakcje keyboard navigation

- **Tab:** Przejście między polami (Od → Do → Sprawdź wygrane → Accordion headers)
- **Enter/Space:** Na accordion header → toggle expand/collapse
- **Enter:** Na button "Sprawdź wygrane" → submit formularza
- **Escape:** Zamknięcie ErrorModal (jeśli otwarty)

## 9. Warunki i walidacja

### 9.1 Walidacja formularza (CheckPanel)

**Warunki weryfikowane:**
1. `dateFrom ≤ dateTo`
   - Komponent: CheckPanel
   - Moment weryfikacji: onChange dla date pickerów (inline validation)
   - Wpływ na UI: Komunikat błędu pod polem "Do", button disabled

2. Zakres dat ≤ 3 lata
   - Weryfikacja: Backend (opcjonalnie frontend dla lepszego UX)
   - Moment weryfikacji: onSubmit
   - Wpływ na UI: ErrorModal z komunikatem "Zakres dat nie może przekraczać 3 lat"

3. Format daty YYYY-MM-DD
   - Weryfikacja: Natywna HTML5 (input type="date")
   - Moment weryfikacji: onChange
   - Wpływ na UI: Natywna walidacja HTML5 (browser-dependent)

**Komunikaty błędów (polski):**
- "Data 'Od' musi być wcześniejsza lub równa 'Do'"
- "Zakres dat nie może przekraczać 3 lat"
- "Nieprawidłowy format daty"

### 9.2 Warunki prezentacji wyników (CheckResults)

**Warunki renderowania badge'a wygranej:**
- `matchCount >= 3` → Badge wyświetlany (WinBadge component)
- `matchCount < 3` → Tekst "Brak trafień" (szary, mniejszy font)

**Warunki pogrubienia liczb (ResultTicketItem):**
```typescript
// Dla każdej liczby w zestawie
numbers.map(num => {
  const isMatched = matchedNumbers.includes(num);
  // Jeśli isMatched === true → font-bold
  // Jeśli isMatched === false → font-normal
})
```

**Empty state:**
- Warunek: `results.length === 0`
- Renderowanie: "Nie znaleziono wygranych w wybranym zakresie dat."

### 9.3 Warunki accordion (AccordionItem)

**Warunek domyślnego rozwinięcia:**
- Pierwsze losowanie w liście: `defaultExpanded={true}`
- Pozostałe losowania: `defaultExpanded={false}`

**Stan ARIA:**
- `aria-expanded="true"` gdy rozwinięty
- `aria-expanded="false"` gdy zwinięty

## 10. Obsługa błędów

### 10.1 Błędy walidacji (inline)

**Typ błędu:** Nieprawidłowy zakres dat (dateFrom > dateTo)

**Obsługa:**
- Inline validation w CheckPanel
- Komunikat błędu pod polem "Do" (czerwony tekst)
- Button "Sprawdź wygrane" disabled
- Brak ErrorModal (user-friendly feedback)

**Przykład:**
```tsx
{dateError && (
  <span className="text-red-600 text-sm mt-1">
    {dateError}
  </span>
)}
```

---

### 10.2 Błędy API (ErrorModal)

**Typ błędu:** 400 Bad Request (walidacja backend)

**Obsługa:**
- ErrorModal z listą błędów
- Komunikaty z backendu (po polsku)

**Przykład response:**
```json
{
  "errors": {
    "dateTo": ["Zakres dat nie może przekraczać 3 lat"]
  }
}
```

**ErrorModal:**
- Tytuł: "Błąd"
- Treść: "• Zakres dat nie może przekraczać 3 lat"
- Button: [Zamknij]

---

**Typ błędu:** 401 Unauthorized (wygasły token)

**Obsługa:**
- Silent failure: logout() + redirect `/login`
- ErrorModal: "Twoja sesja wygasła. Zaloguj się ponownie."

---

**Typ błędu:** 500 Internal Server Error

**Obsługa:**
- ErrorModal: "Wystąpił problem z serwerem. Spróbuj ponownie za chwilę."

---

**Typ błędu:** Network Error (brak połączenia)

**Obsługa:**
- ErrorModal: "Brak połączenia z serwerem. Sprawdź swoje połączenie internetowe."

---

### 10.3 Edge cases

**Case 1: Użytkownik nie ma żadnych zestawów**

**Scenariusz:**
- Użytkownik wykonuje weryfikację, ale nie ma zapisanych zestawów
- Backend zwraca: `totalTickets: 0`, `results: []`

**Obsługa:**
- Renderowanie empty state: "Nie masz żadnych zestawów do weryfikacji. Dodaj zestawy na stronie Moje Zestawy."
- Link do `/tickets` (opcjonalnie)

---

**Case 2: Brak losowań w wybranym zakresie**

**Scenariusz:**
- Użytkownik wybiera zakres, w którym nie ma losowań
- Backend zwraca: `totalDraws: 0`, `results: []`

**Obsługa:**
- Empty state: "Nie znaleziono losowań w wybranym zakresie dat."

---

**Case 3: Weryfikacja trwa >2s**

**Scenariusz:**
- Backend przekracza NFR-001 (100 zestawów × 1 losowanie ≤ 2s)

**Obsługa:**
- Spinner pozostaje widoczny (bez timeoutu w MVP)
- Post-MVP: Progress bar z tekstem "Weryfikacja może potrwać do 5 sekund..."

---

## 11. Kroki implementacji

### Krok 1: Setup struktury plików
- Utworzenie katalogu `src/components/Checks/`
- Pliki:
  - `ChecksPage.tsx` (główny komponent strony)
  - `CheckPanel.tsx` (panel formularza)
  - `CheckResults.tsx` (kontener wyników)
  - `AccordionItem.tsx` (pojedyncze losowanie)
  - `ResultTicketItem.tsx` (pojedynczy zestaw w wynikach)
  - `WinBadge.tsx` (badge wygranej)

### Krok 2: Implementacja typów
- Utworzenie pliku `src/types/verification.ts`
- Definicje:
  - `CheckRequest`
  - `CheckResponse`
  - `VerificationResult`
  - `TicketMatch`
  - `WinLevel`
  - `DateRange`

### Krok 3: Implementacja ApiService method
- Dodanie metody `checkWinnings(dateFrom, dateTo)` w `src/services/api-service.ts`
- Error handling (try-catch, ApiError, NetworkError)

### Krok 4: Implementacja DatePicker (shared component)
- Plik: `src/components/Shared/DatePicker.tsx`
- Props: label, value, onChange, error, min, max, required
- Inline validation support

### Krok 5: Implementacja WinBadge
- Plik: `src/components/Checks/WinBadge.tsx`
- Warianty kolorystyczne (green/blue/orange/red)
- Mapowanie WinLevel → kolor + emoji + tekst

### Krok 6: Implementacja ResultTicketItem
- Renderowanie liczb jako kółek (niebieskie dla trafionych, szare dla nietrafionych)
- Wyróżnienie trafionych liczb przez `font-bold` w niebieskich kółkach
- Warunkowe renderowanie ramki z informacjami o wygranej (hits ≥ 3):
  - Badge stopnia wygranej (WinBadge)
  - Grid z 3 kolumnami: koszt kuponu, ilość wygranych, wartość wygranej
  - Mapowanie hits → tier (6=tier1, 5=tier2, 4=tier3, 3=tier4) do wybrania odpowiednich pól winPoolCountX i winPoolAmountX
  - Wyświetlanie "Brak danych" dla wartości null
- Tekst "Brak trafień" dla hits < 3 (zamiast ramki)

### Krok 7: Implementacja AccordionItem
- Stan lokalny `isExpanded` (useState)
- Toggle expand/collapse na click
- Header zawiera:
  - Data losowania
  - ID systemowe (drawSystemId)
  - Badge typu gry (LOTTO / LOTTO PLUS) z różnymi kolorami
  - Wylosowane liczby jako niebieskie kółka
  - Licznik kuponów (badge po prawej stronie)
- Content przekazuje dane draw (z polami winPool*) do ResultTicketItem
- ARIA attributes (aria-expanded, aria-controls)
- Keyboard navigation (Enter/Space)

### Krok 8: Implementacja CheckResults
- Transformacja struktury danych z "tickets → draws" na "draws → tickets" (używając Map)
- Grupowanie kuponów według losowań (drawId + drawDate jako klucz)
- Przeniesienie wszystkich pól z draw (drawSystemId, lottoType, ticketPrice, winPoolCount1-4, winPoolAmount1-4) do struktury drawsMap
- Sortowanie losowań według daty (najnowsze na górze)
- Mapowanie `drawsArray` → AccordionItem[] z pełnymi danymi draw
- Empty state dla `results.length === 0`
- Conditional rendering (loading spinner vs results)

### Krok 9: Implementacja CheckPanel
- Formularz z 2 date pickerami i text inputem dla groupName
- Inline validation (dateFrom ≤ dateTo)
- Pole groupName (opcjonalne) z placeholderem "np. Ulubione"
- Opis pomocniczy: "Wyszukiwanie częściowe - wpisz fragment nazwy grupy (np. 'ulu' znajdzie 'Ulubione')"
- Button "Sprawdź wygrane" z disabled state
- Callback `onSubmit(dateFrom, dateTo, groupName)`

### Krok 10: Implementacja ChecksPage (główny komponent)
- Stan lokalny (dateFrom, dateTo, groupName, isLoading, results, error)
- Helper functions (getDefaultDateFrom, getDefaultDateTo, validateDateRange)
- Handler `handleSubmit()` z API call (przekazuje dateFrom, dateTo, groupName)
- Renderowanie: CheckPanel + Spinner + CheckResults
- Error handling z ErrorModal

### Krok 11: Stylizacja Tailwind CSS
- Mobile-first responsive design
- Date range picker layout (vertical/inline)
- Accordion styling (hover effects, transitions)
- Badge variants (green/blue/orange/red)
- Touch targets min 44x44px

### Krok 12: Accessibility audit
- Semantic HTML (`<main>`, `<form>`, `<button>`)
- ARIA attributes (aria-expanded, aria-controls, aria-describedby)
- Keyboard navigation testing (Tab, Enter/Space, Escape)
- Focus management (focus trap w ErrorModal)
- Screen reader testing (NVDA/JAWS)

### Krok 13: Integracja z routing
- Dodanie route `/checks` w `src/main.tsx` (React Router 7)
- Protected route wrapper (redirect `/login` jeśli niezalogowany)
- Dodanie zakładki "Sprawdź Wygrane" w Navbar

### Krok 14: Testowanie
- Test manual: happy path (weryfikacja z wygranymi)
- Test manual: empty state (brak wygranych)
- Test manual: edge cases (brak zestawów, brak losowań)
- Test manual: walidacja (nieprawidłowy zakres, >31 dni)
- Test manual: responsywność (mobile/tablet/desktop)
- Test manual: keyboard navigation
- Opcjonalnie: testy jednostkowe dla helper functions (validateDateRange, getDefaultDateFrom)

### Krok 15: Polish translation audit
- Przegląd wszystkich tekstów w komponentach
- Upewnienie się, że wszystkie komunikaty są po polsku
- Sprawdzenie poprawności gramatycznej i stylistycznej

---

**Estymacja czasu:** 3 dni robocze (zgodnie z harmonogramem Faza 5)

**Priorytet implementacji:** Must Have (core functionality MVP)

**Zależności:**
- DatePicker (shared component) - musi być zaimplementowany wcześniej
- ApiService - metoda checkWinnings()
- ErrorModal (shared component)
- Toast (shared component)
- Spinner (shared component)
- Protected route wrapper

---

**Koniec planu implementacji widoku Checks Page**
