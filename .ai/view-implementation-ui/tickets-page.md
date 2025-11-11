# Plan implementacji widoku Tickets Page

## 1. Przegląd

Tickets Page to główny widok zarządzania zestawami liczb LOTTO użytkownika. Umożliwia przeglądanie, dodawanie ręczne, edycję, usuwanie oraz generowanie zestawów (losowy i systemowy). Jest to pierwszy widok, na który użytkownik trafia po zalogowaniu.

**Główne cele widoku:**
- Wyświetlenie listy wszystkich zestawów użytkownika (max 100)
- Zarządzanie CRUD zestawami liczb (6 liczb z zakresu 1-49)
- Generowanie losowych zestawów (pojedynczy)
- Generowanie zestawów systemowych (9 zestawów pokrywających liczby 1-49)
- Monitoring limitu zestawów (licznik X/100 z progresywną kolorystyką)
- Walidacja unikalności zestawów (duplikaty niedozwolone)

## 2. Routing widoku

**Ścieżka:** `/tickets`

**Dostęp:** Chroniony (wymaga autentykacji)
- Użytkownik niezalogowany → redirect `/login`
- Użytkownik zalogowany po pomyślnej autentykacji → redirect `/tickets` (domyślny widok)

**Route definition (React Router 7):**
```tsx
<Route path="/tickets" element={<ProtectedRoute><TicketsPage /></ProtectedRoute>} />
```

## 3. Struktura komponentów

### Hierarchia komponentów

```
TicketsPage (główny kontener)
├── Layout (wrapper z Navbar)
│   └── Navbar (widoczny dla zalogowanych)
│
├── Header Section
│   ├── h1: "Moje zestawy"
│   └── TicketCounter (licznik "X/100")
│
├── Action Buttons Row
│   ├── Button: "+ Dodaj ręcznie" (primary)
│   ├── Button: "🎲 Generuj losowy" (secondary)
│   └── Button: "🔢 Generuj systemowy" (secondary)
│
├── TicketList (lista zestawów)
│   ├── Empty State (jeśli brak zestawów)
│   └── TicketItem[] (dla każdego zestawu)
│       ├── Liczby: [3, 12, 25, 31, 42, 48]
│       ├── Data: "Utworzono: 2025-10-15 14:30"
│       └── Action Buttons
│           ├── Button: [Edytuj]
│           └── Button: [Usuń]
│
├── Modale (conditional rendering)
│   ├── TicketFormModal (dodawanie/edycja)
│   │   ├── 6× NumberInput
│   │   └── Buttons: [Wyczyść] | [Anuluj] [Zapisz]
│   ├── GeneratorPreviewModal (losowy)
│   │   ├── Wygenerowane liczby: [7, 19, 22, 33, 38, 45]
│   │   └── Buttons: [Generuj ponownie] | [Anuluj] [Zapisz]
│   ├── GeneratorPreviewModal (systemowy)
│   │   ├── Tooltip/wyjaśnienie algorytmu
│   │   ├── Grid 9 zestawów (3x3 desktop, vertical mobile)
│   │   └── Buttons: [Generuj ponownie] | [Anuluj] [Zapisz wszystkie]
│   ├── DeleteConfirmModal
│   │   ├── Treść: "Czy na pewno chcesz usunąć zestaw? [3, 12, 25, 31, 42, 48]"
│   │   └── Buttons: [Anuluj] [Usuń] (danger)
│   └── ErrorModal (dla wszystkich błędów)
│       ├── Tytuł: "Błąd"
│       ├── Lista błędów: • Błąd 1, • Błąd 2
│       └── Button: [Zamknij]
│
└── ToastContainer (sukces notifications, overlay)
    └── Toast[] (auto-dismiss 3-4s)
```

## 4. Szczegóły komponentów

### 4.1 TicketsPage (główny kontener)

**Opis komponentu:**
Główny komponent-strona zarządzający stanem widoku Tickets, obsługujący wszystkie interakcje użytkownika (CRUD, generatory) i komunikację z API.

**Główne elementy HTML i komponenty dzieci:**
- `<Layout>` (wrapper z Navbar)
- `<div className="container">` (główny kontener)
  - Header section z nagłówkiem h1 i TicketCounter
  - Row z 3 action buttons
  - TicketList (lub Empty State)
  - 5 modalnych komponentów (conditional rendering)
  - ToastContainer

**Obsługiwane zdarzenia:**
- `onAddTicket()` - otwiera modal dodawania zestawu
- `onEditTicket(ticketId)` - otwiera modal edycji z pre-wypełnionymi danymi
- `onDeleteTicket(ticketId)` - otwiera modal potwierdzenia usunięcia
- `onGenerateRandom()` - generuje losowy zestaw, otwiera modal preview
- `onGenerateSystem()` - generuje 9 zestawów systemowych, otwiera modal preview
- `onSaveTicket(numbers)` - zapisuje nowy/edytowany zestaw (API call)
- `onSaveGeneratedRandom(numbers)` - zapisuje wygenerowany losowy zestaw
- `onSaveGeneratedSystem(tickets)` - zapisuje 9 wygenerowanych zestawów
- `onConfirmDelete(ticketId)` - usuwa zestaw (API call)
- `refreshTicketList()` - odświeża listę zestawów (API call GET /api/tickets)

**Warunki walidacji:**
- **Limit zestawów:** Sprawdzenie `tickets.length < 100` przed otwarciem modalu dodawania/generatora
  - Jeśli ≥100: ErrorModal "Osiągnięto limit 100 zestawów. Usuń istniejące zestawy, aby dodać nowe."
- **Miejsce na 9 zestawów (generator systemowy):** Sprawdzenie `100 - tickets.length ≥ 9`
  - Jeśli brak miejsca: ErrorModal "Brak miejsca na 9 zestawów. Dostępne: X zestawy. Usuń istniejące zestawy, aby kontynuować."
- **Unikalność zestawów:** Backend walidacja przy zapisie (frontend tylko UI feedback via ErrorModal)

**Typy (DTO i ViewModel):**
```tsx
// DTO z API (GET /api/tickets response)
interface Ticket {
  id: number;              // INT (klucz główny)
  userId: number;
  groupName: string;       // Max 100 znaków, domyślnie pusty string
  numbers: number[];       // 6 liczb z zakresu 1-49
  createdAt: string;       // ISO 8601 datetime
}

interface GetTicketsResponse {
  tickets: Ticket[];
  totalCount: number;
  limit: number;           // Max 100
}

// Request DTO (POST/PUT /api/tickets)
interface TicketRequest {
  groupName?: string;      // Opcjonalne, max 100 znaków
  numbers: number[];       // 6 liczb
}

// Generator random response (POST /api/tickets/generate-random)
interface GenerateRandomResponse {
  message: string;
}

// Generator system response (POST /api/tickets/generate-system)
interface GenerateSystemResponse {
  message: string;
}

// ViewModel dla stanu lokalnego
interface TicketFormState {
  mode: 'add' | 'edit';
  groupName: string;       // Nazwa grupy (max 100 znaków)
  initialNumbers?: number[];
  ticketId?: number;       // Dla edycji
}

interface GeneratorState {
  type: 'random' | 'system';
  numbers: number[] | number[][]; // Random: 6 liczb, System: 9x6 liczb
}
```

**Propsy:** Brak (główny component routingu, otrzymuje dane z AppContext)

**State lokalny:**
```tsx
const [tickets, setTickets] = useState<Ticket[]>([]);
const [totalCount, setTotalCount] = useState<number>(0);
const [loading, setLoading] = useState<boolean>(false);

// Modale
const [isTicketFormOpen, setIsTicketFormOpen] = useState<boolean>(false);
const [ticketFormState, setTicketFormState] = useState<TicketFormState | null>(null);
const [isGeneratorPreviewOpen, setIsGeneratorPreviewOpen] = useState<boolean>(false);
const [generatorState, setGeneratorState] = useState<GeneratorState | null>(null);
const [isDeleteConfirmOpen, setIsDeleteConfirmOpen] = useState<boolean>(false);
const [ticketToDelete, setTicketToDelete] = useState<Ticket | null>(null);
const [isErrorModalOpen, setIsErrorModalOpen] = useState<boolean>(false);
const [errors, setErrors] = useState<string[]>([]);

// Toast
const [toastMessage, setToastMessage] = useState<string | null>(null);
const [toastVariant, setToastVariant] = useState<'success' | 'error'>('success');
```

---

### 4.2 TicketCounter (licznik zestawów)

**Opis komponentu:**
Wyświetla licznik zestawów użytkownika w formacie "X/100" z progresywną kolorystyką ostrzegającą o zbliżającym się limicie.

**Główne elementy:**
- `<div>` z tekstem: "[42/100]"
- Kolorystyka Tailwind: `text-green-600`, `text-yellow-600`, `text-red-600`

**Obsługiwane interakcje:**
Brak (komponent display-only)

**Obsługiwana walidacja:**
Brak

**Typy:**
```tsx
interface TicketCounterProps {
  count: number;          // Aktualna liczba zestawów
  max: number;            // Limit (default 100)
}
```

**Propsy:**
- `count: number` - aktualna liczba zestawów użytkownika
- `max?: number` - limit (default 100)

**Logika kolorystyki:**
```tsx
function getCounterColor(count: number, max: number = 100): string {
  const percentage = (count / max) * 100;
  if (percentage <= 70) return 'text-green-600';
  if (percentage <= 90) return 'text-yellow-600';
  return 'text-red-600'; // 91-100
}
```

**Toast ostrzegawczy:**
Jeśli `count > 95`: Toast "Uwaga: Pozostało tylko {100 - count} wolnych miejsc" (wyświetlany przy mount/refresh listy)

---

### 4.3 TicketList (lista zestawów)

**Opis komponentu:**
Wyświetla listę wszystkich zestawów użytkownika w formie scrollowalnego kontenera (max 100 zestawów, bez paginacji). Zestawy sortowane według daty utworzenia (najnowsze na górze).

**Główne elementy:**
- `<div className="space-y-4">` (vertical stack)
  - Jeśli `tickets.length === 0`: Empty State
  - Jeśli `tickets.length > 0`: Array.map → TicketItem[]

**Empty State:**
```tsx
<div className="text-center py-12 text-gray-500">
  <p>Nie masz jeszcze żadnych zestawów.</p>
  <p>Dodaj swój pierwszy zestaw używając przycisków powyżej.</p>
</div>
```

**Obsługiwane interakcje:**
Przekazuje handlery onEdit/onDelete do TicketItem

**Obsługiwana walidacja:**
Brak (walidacja w TicketFormModal)

**Typy:**
```tsx
interface TicketListProps {
  tickets: Ticket[];
  loading: boolean;
  onEdit: (ticketId: number) => void;
  onDelete: (ticketId: number) => void;
}
```

**Propsy:**
- `tickets: Ticket[]` - lista zestawów do wyświetlenia
- `loading: boolean` - flag loading state (Spinner podczas fetch)
- `onEdit: (ticketId) => void` - callback edycji zestawu
- `onDelete: (ticketId) => void` - callback usunięcia zestawu

---

### 4.4 TicketItem (pojedynczy zestaw w liście)

**Opis komponentu:**
Reprezentuje pojedynczy zestaw liczb w liście z wyświetleniem 6 liczb, daty utworzenia i przycisków akcji (Edytuj, Usuń).

**Główne elementy:**
```tsx
<div className="border rounded-lg p-4 flex justify-between items-center">
  <div className="flex-1">
    <div className="flex gap-2 mb-2">
      {numbers.map(num => (
        <span className="px-3 py-1 bg-blue-100 text-blue-800 rounded-full font-semibold">
          {num}
        </span>
      ))}
    </div>
    <p className="text-sm text-gray-500">Utworzono: {formatDate(createdAt)}</p>
  </div>
  <div className="flex gap-2">
    <Button onClick={onEdit} variant="secondary">Edytuj</Button>
    <Button onClick={onDelete} variant="danger">Usuń</Button>
  </div>
</div>
```

**Obsługiwane interakcje:**
- Kliknięcie [Edytuj] → wywołuje `onEdit(ticket.id)`
- Kliknięcie [Usuń] → wywołuje `onDelete(ticket.id)`

**Obsługiwana walidacja:**
Brak

**Typy:**
```tsx
interface TicketItemProps {
  ticket: Ticket;
  onEdit: () => void;
  onDelete: () => void;
}
```

**Propsy:**
- `ticket: Ticket` - dane zestawu do wyświetlenia
- `onEdit: () => void` - callback edycji
- `onDelete: () => void` - callback usunięcia

**Helper funkcja formatowania daty:**
```tsx
function formatDate(isoDate: string): string {
  const date = new Date(isoDate);
  return date.toLocaleString('pl-PL', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit'
  });
}
```

---

### 4.5 TicketFormModal (dodawanie/edycja zestawu)

**Opis komponentu:**
Modal zawierający formularz z 6 polami numerycznymi do wprowadzenia/edycji zestawu liczb LOTTO. Wspiera tryb dodawania (puste pola) i edycji (pre-wypełnione pola).

**Główne elementy:**
```tsx
<Modal isOpen={isOpen} onClose={onClose} title={mode === 'add' ? "Dodaj nowy zestaw" : "Edytuj zestaw"} size="lg">
  <form onSubmit={handleSubmit}>
    <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
      {[1, 2, 3, 4, 5, 6].map(position => (
        <NumberInput
          label={`Liczba ${position}`}
          value={numbers[position - 1]}
          onChange={(val) => handleNumberChange(position - 1, val)}
          error={errors[position - 1]}
          min={1}
          max={49}
          required
        />
      ))}
    </div>
    <div className="flex justify-between mt-6">
      <Button type="button" onClick={handleClear} variant="secondary">Wyczyść</Button>
      <div className="flex gap-2">
        <Button type="button" onClick={onClose} variant="secondary">Anuluj</Button>
        <Button type="submit" variant="primary">Zapisz</Button>
      </div>
    </div>
  </form>
</Modal>
```

**Obsługiwane interakcje:**
- **onChange w NumberInput:** Zmiana wartości liczby, inline validation (zakres 1-49, unikalność)
- **Kliknięcie [Wyczyść]:** Resetuje wszystkie pola do wartości pustych (`''`)
- **Kliknięcie [Anuluj]:** Zamyka modal bez zapisu (`onClose()`)
- **Submit formularza ([Zapisz]):** Walidacja + wywołanie `onSubmit(numbers)`
  - Inline validation: wszystkie błędy zbierane
  - Jeśli błędy: wyświetlenie w ErrorModal (po submit)
  - Jeśli OK: API call `POST /api/tickets` lub `PUT /api/tickets/{id}`

**Obsługiwana walidacja:**
**Inline validation (real-time podczas onChange):**
1. **Zakres 1-49:**
   - Warunek: `num >= 1 && num <= 49`
   - Komunikat błędu: "Liczba musi być w zakresie 1-49"
2. **Unikalność liczb w zestawie:**
   - Warunek: Sprawdzenie czy `numbers.filter(n => n === num).length > 1`
   - Komunikat błędu: "Liczby w zestawie muszą być unikalne"
3. **Wymagane pola:**
   - Warunek: Wszystkie 6 pól muszą być wypełnione
   - Komunikat błędu: "Wszystkie pola są wymagane"

**Walidacja przy submit (zbieranie wszystkich błędów):**
```tsx
function validateNumbers(numbers: (number | '')[]): string[] {
  const errors: string[] = [];

  // Sprawdzenie wypełnienia
  if (numbers.some(n => n === '')) {
    errors.push('Wszystkie pola są wymagane');
  }

  // Sprawdzenie zakresu
  const validNumbers = numbers.filter(n => n !== '') as number[];
  if (validNumbers.some(n => n < 1 || n > 49)) {
    errors.push('Liczby muszą być w zakresie 1-49');
  }

  // Sprawdzenie unikalności
  const uniqueNumbers = new Set(validNumbers);
  if (uniqueNumbers.size !== validNumbers.length) {
    errors.push('Liczby w zestawie muszą być unikalne');
  }

  return errors;
}
```

**Błędy biznesowe z backendu (400 Bad Request):**
- "Osiągnięto limit 100 zestawów. Usuń istniejące zestawy, aby dodać nowe."
- "Zestaw już istnieje" (duplikat zestawu dla użytkownika)

**Typy:**
```tsx
interface TicketFormModalProps {
  isOpen: boolean;
  onClose: () => void;
  mode: 'add' | 'edit';
  initialNumbers?: number[];      // Pre-wypełnione dla edycji
  ticketId?: number;               // ID zestawu dla edycji
  onSubmit: (numbers: number[], ticketId?: number) => Promise<void>;
}
```

**Propsy:**
- `isOpen: boolean` - czy modal jest otwarty
- `onClose: () => void` - callback zamknięcia modalu
- `mode: 'add' | 'edit'` - tryb (dodawanie/edycja)
- `initialNumbers?: number[]` - opcjonalne pre-wypełnione liczby (dla edycji)
- `ticketId?: number` - ID zestawu (dla edycji)
- `onSubmit: (numbers, ticketId?) => Promise<void>` - callback zapisu zestawu

**State lokalny:**
```tsx
const [numbers, setNumbers] = useState<(number | '')[]>(
  initialNumbers || ['', '', '', '', '', '']
);
const [inlineErrors, setInlineErrors] = useState<(string | undefined)[]>(
  [undefined, undefined, undefined, undefined, undefined, undefined]
);
```

---

### 4.6 GeneratorPreviewModal (losowy)

**Opis komponentu:**
Modal preview wygenerowanego losowego zestawu z możliwością ponownego generowania lub zapisu.

**Główne elementy:**
```tsx
<Modal isOpen={isOpen} onClose={onClose} title="Generator losowy" size="md">
  <div className="mb-6">
    <p className="text-sm text-gray-600 mb-4">Wygenerowany zestaw:</p>
    <div className="flex gap-2 justify-center">
      {numbers.map(num => (
        <span className="px-4 py-2 bg-green-100 text-green-800 rounded-full font-bold text-lg">
          {num}
        </span>
      ))}
    </div>
  </div>
  <div className="flex justify-between">
    <Button onClick={onRegenerate} variant="secondary">Generuj ponownie</Button>
    <div className="flex gap-2">
      <Button onClick={onClose} variant="secondary">Anuluj</Button>
      <Button onClick={handleSave} variant="primary">Zapisz</Button>
    </div>
  </div>
</Modal>
```

**Obsługiwane interakcje:**
- **Kliknięcie [Generuj ponownie]:** Regeneruje losowy zestaw (algorytm frontend lub API call `POST /api/tickets/generate-random` ponownie)
- **Kliknięcie [Anuluj]:** Zamyka modal bez zapisu
- **Kliknięcie [Zapisz]:**
  - Sprawdzenie limitu (frontend: `tickets.length < 100`)
  - Jeśli ≥100: ErrorModal z komunikatem o limicie
  - Jeśli OK: API call `POST /api/tickets` z `{ numbers }`
  - Po sukcesie: Toast "Zestaw wygenerowany i zapisany", modal zamyka, lista odświeża

**Obsługiwana walidacja:**
- **Limit 100 zestawów** (przed zapisem)
  - Warunek: `tickets.length < 100`
  - Komunikat: "Osiągnięto limit 100 zestawów. Usuń istniejące zestawy, aby dodać nowe."

**Typy:**
```tsx
interface GeneratorPreviewModalProps {
  isOpen: boolean;
  onClose: () => void;
  numbers: number[];                      // 6 liczb wygenerowanych
  onRegenerate: () => void;
  onSave: (numbers: number[]) => Promise<void>;
}
```

**Propsy:**
- `isOpen: boolean`
- `onClose: () => void`
- `numbers: number[]` - 6 wygenerowanych liczb
- `onRegenerate: () => void` - callback ponownego generowania
- `onSave: (numbers) => Promise<void>` - callback zapisu zestawu

---

### 4.7 GeneratorPreviewModal (systemowy)

**Opis komponentu:**
Modal preview 9 wygenerowanych zestawów systemowych z wyjaśnieniem algorytmu i możliwością zapisu wszystkich.

**Główne elementy:**
```tsx
<Modal isOpen={isOpen} onClose={onClose} title="Generator systemowy (9 zestawów)" size="xl">
  <div className="mb-4">
    <p className="text-sm text-gray-600 mb-2">
      Generator tworzy 9 zestawów pokrywających wszystkie liczby od 1 do 49.
      Każda liczba pojawia się minimum raz.
    </p>
  </div>

  <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-6">
    {tickets.map((numbers, index) => (
      <div key={index} className="border rounded p-3">
        <p className="text-sm font-semibold mb-2">Zestaw {index + 1}:</p>
        <div className="flex flex-wrap gap-1">
          {numbers.map(num => (
            <span className="px-2 py-1 bg-blue-100 text-blue-800 rounded text-sm">
              {num}
            </span>
          ))}
        </div>
      </div>
    ))}
  </div>

  <div className="flex justify-between">
    <Button onClick={onRegenerate} variant="secondary">Generuj ponownie</Button>
    <div className="flex gap-2">
      <Button onClick={onClose} variant="secondary">Anuluj</Button>
      <Button onClick={handleSaveAll} variant="primary">Zapisz wszystkie</Button>
    </div>
  </div>
</Modal>
```

**Obsługiwane interakcje:**
- **Kliknięcie [Generuj ponownie]:** Regeneruje 9 zestawów (algorytm frontend lub API call ponownie)
- **Kliknięcie [Anuluj]:** Zamyka modal bez zapisu
- **Kliknięcie [Zapisz wszystkie]:**
  - Sprawdzenie miejsca na 9 zestawów: `100 - tickets.length >= 9`
  - Jeśli brak miejsca: ErrorModal "Brak miejsca na 9 zestawów. Dostępne: X zestawy..."
  - Jeśli OK: API call `POST /api/tickets/generate-system` (bulk save 9 zestawów)
  - Po sukcesie: Toast "9 zestawów wygenerowanych i zapisanych", modal zamyka, lista odświeża

**Obsługiwana walidacja:**
- **Miejsce na 9 zestawów** (przed zapisem)
  - Warunek: `100 - tickets.length >= 9`
  - Komunikat: "Brak miejsca na 9 zestawów. Dostępne: {dostępne} zestawy. Usuń istniejące zestawy, aby kontynuować."

**Typy:**
```tsx
interface GeneratorSystemPreviewModalProps {
  isOpen: boolean;
  onClose: () => void;
  tickets: number[][];                    // 9 zestawów × 6 liczb
  onRegenerate: () => void;
  onSaveAll: (tickets: number[][]) => Promise<void>;
}
```

**Propsy:**
- `isOpen: boolean`
- `onClose: () => void`
- `tickets: number[][]` - 9 wygenerowanych zestawów (każdy po 6 liczb)
- `onRegenerate: () => void` - callback ponownego generowania
- `onSaveAll: (tickets) => Promise<void>` - callback zapisu wszystkich 9 zestawów

**Responsywność layoutu grid:**
- Mobile: `grid-cols-1` (vertical stack, 9 cards jeden pod drugim)
- Desktop: `grid-cols-3` (grid 3x3)

---

### 4.8 DeleteConfirmModal (potwierdzenie usunięcia)

**Opis komponentu:**
Modal potwierdzenia akcji usunięcia zestawu z wyświetleniem liczb do usunięcia.

**Główne elementy:**
```tsx
<Modal isOpen={isOpen} onClose={onClose} title="Usuń zestaw" size="sm">
  <div className="mb-6">
    <p className="mb-2">Czy na pewno chcesz usunąć ten zestaw?</p>
    <div className="flex gap-2 justify-center mt-4">
      {numbers.map(num => (
        <span className="px-3 py-1 bg-gray-200 text-gray-700 rounded font-semibold">
          {num}
        </span>
      ))}
    </div>
  </div>
  <div className="flex justify-end gap-2">
    <Button onClick={onClose} variant="secondary">Anuluj</Button>
    <Button onClick={onConfirm} variant="danger">Usuń</Button>
  </div>
</Modal>
```

**Obsługiwane interakcje:**
- **Kliknięcie [Anuluj]:** Zamyka modal bez usunięcia
- **Kliknięcie [Usuń]:**
  - API call `DELETE /api/tickets/{id}`
  - Po sukcesie: Toast "Zestaw usunięty pomyślnie", modal zamyka, lista odświeża
  - Po błędzie (np. 404, 403): ErrorModal z komunikatem

**Obsługiwana walidacja:**
Brak (backend sprawdza właściciela, zwraca 403 jeśli nie należy do użytkownika)

**Typy:**
```tsx
interface DeleteConfirmModalProps {
  isOpen: boolean;
  onClose: () => void;
  ticket: Ticket;                         // Zestaw do usunięcia
  onConfirm: (ticketId: number) => Promise<void>;
}
```

**Propsy:**
- `isOpen: boolean`
- `onClose: () => void`
- `ticket: Ticket` - dane zestawu do usunięcia (wyświetlane liczby)
- `onConfirm: (ticketId) => Promise<void>` - callback potwierdzenia usunięcia

**Focus management:**
- Auto-focus na [Anuluj] button przy otwarciu modalu (default safe action)
- [Usuń] button z `variant="danger"` (czerwony, wyraźnie odróżniony)

---

## 5. Typy

### 5.1 DTO (Data Transfer Objects) - kontrakty z API

```tsx
// GET /api/tickets response
interface Ticket {
  id: number;                             // INT (klucz główny)
  userId: number;
  groupName: string;                      // Max 100 znaków, domyślnie pusty string
  numbers: number[];                      // 6 liczb z zakresu 1-49
  createdAt: string;                      // ISO 8601 datetime, np. "2025-10-25T10:00:00Z"
}

interface GetTicketsResponse {
  tickets: Ticket[];
  totalCount: number;                     // Liczba zestawów użytkownika
  limit: number;                          // Max 100
}

// POST /api/tickets request
interface TicketRequest {
  groupName?: string;                     // Opcjonalne, max 100 znaków
  numbers: number[];                      // 6 liczb
}

// POST /api/tickets response (sukces)
interface TicketResponse {
  message: string;                        // "Zestaw utworzony pomyślnie"
}

// PUT /api/tickets/{id} request (identyczny jak POST)
// PUT /api/tickets/{id} response (sukces)
interface UpdateTicketResponse {
  message: string;                        // "Zestaw zaktualizowany pomyślnie"
}

// DELETE /api/tickets/{id} response (sukces)
interface DeleteTicketResponse {
  message: string;                        // "Zestaw usunięty pomyślnie"
}

// POST /api/tickets/generate-random response
interface GenerateRandomResponse {
  message: string;                        // "Zestaw wygenerowany pomyślnie"
}

// POST /api/tickets/generate-system response
interface GenerateSystemResponse {
  message: string;                        // "9 zestawów wygenerowanych i zapisanych pomyślnie"
}

// Błąd API (400 Bad Request)
interface ApiErrorResponse {
  errors?: {
    [field: string]: string[];
  };
  error?: string;
}
```

### 5.2 ViewModel - typy stanu lokalnego UI

```tsx
// Stan formularza dodawania/edycji
interface TicketFormState {
  mode: 'add' | 'edit';
  groupName: string;                      // Nazwa grupy (max 100 znaków)
  initialNumbers?: number[];              // Pre-wypełnione dla edycji
  ticketId?: number;                      // ID dla edycji
}

// Stan generatora (preview)
interface GeneratorState {
  type: 'random' | 'system';
  numbers: number[] | number[][];         // Random: 6 liczb, System: 9x6 liczb
}

// Stan modalu usunięcia
interface DeleteModalState {
  ticket: Ticket | null;                  // Zestaw do usunięcia
}

// Toast notification
interface ToastState {
  message: string;
  variant: 'success' | 'error' | 'warning';
  visible: boolean;
}
```

### 5.3 Props Interfaces dla komponentów

```tsx
// TicketsPage - brak propsów (główny component routingu)

interface TicketCounterProps {
  count: number;
  max?: number;                           // Default 100
}

interface TicketListProps {
  tickets: Ticket[];
  loading: boolean;
  onEdit: (ticketId: number) => void;
  onDelete: (ticketId: number) => void;
}

interface TicketItemProps {
  ticket: Ticket;
  onEdit: () => void;
  onDelete: () => void;
}

interface TicketFormModalProps {
  isOpen: boolean;
  onClose: () => void;
  mode: 'add' | 'edit';
  initialNumbers?: number[];
  ticketId?: number;
  onSubmit: (numbers: number[], ticketId?: number) => Promise<void>;
}

interface GeneratorPreviewModalProps {
  isOpen: boolean;
  onClose: () => void;
  numbers: number[];
  onRegenerate: () => void;
  onSave: (numbers: number[]) => Promise<void>;
}

interface GeneratorSystemPreviewModalProps {
  isOpen: boolean;
  onClose: () => void;
  tickets: number[][];
  onRegenerate: () => void;
  onSaveAll: (tickets: number[][]) => Promise<void>;
}

interface DeleteConfirmModalProps {
  isOpen: boolean;
  onClose: () => void;
  ticket: Ticket;
  onConfirm: (ticketId: number) => Promise<void>;
}
```

### 5.4 Shared Components Props (użyte w widoku)

```tsx
// NumberInput (z sekcji 5 Shared Components w ui-plan.md)
interface NumberInputProps {
  label: string;
  value: number | '';
  onChange: (value: number | '') => void;
  error?: string;
  min?: number;                           // Default 1
  max?: number;                           // Default 49
  required?: boolean;
}

// Modal (z sekcji 5 Shared Components)
interface ModalProps {
  isOpen: boolean;
  onClose: () => void;
  title: string;
  children: ReactNode;
  size?: 'sm' | 'md' | 'lg' | 'xl';
}

// ErrorModal (z sekcji 5 Shared Components)
interface ErrorModalProps {
  isOpen: boolean;
  onClose: () => void;
  errors: string[] | string;
}

// Toast (z sekcji 5 Shared Components)
interface ToastProps {
  message: string;
  variant: 'success' | 'error' | 'warning';
  duration?: number;                      // Default 3000ms
}

// Button (z sekcji 5 Shared Components)
interface ButtonProps {
  variant: 'primary' | 'secondary' | 'danger';
  onClick?: () => void;
  disabled?: boolean;
  className?: string;
  children: ReactNode;
  type?: 'button' | 'submit' | 'reset';
}
```

## 6. Zarządzanie stanem

### 6.1 Strategia zarządzania stanem

**Context API:**
- `AppContext` (globalny) - autentykacja użytkownika (token JWT, email, isLoggedIn, logout())
- Dostęp przez `useAppContext()` hook

**Lokalny stan komponentu TicketsPage:**
- `tickets: Ticket[]` - lista zestawów użytkownika (fetch z API przy mount)
- `totalCount: number` - liczba zestawów (dla TicketCounter)
- `loading: boolean` - flag loading state (podczas API calls)
- State modalnych komponentów (isOpen flags, dane do wyświetlenia)
- State toast notifications (message, variant, visible)

**Brak Redux/Context dla danych biznesowych:**
Decyzja: dane zestawów (tickets) nie są współdzielone między widokami, więc lokalny stan w TicketsPage jest wystarczający. Każde wejście na `/tickets` świeży fetch z API.

### 6.2 Custom Hook: useTickets (opcjonalnie dla czystości kodu)

**Cel:** Enkapsulacja logiki zarządzania zestawami (CRUD, generatory) w custom hook.

**Interfejs:**
```tsx
interface UseTicketsReturn {
  // Data
  tickets: Ticket[];
  totalCount: number;
  loading: boolean;

  // CRUD operations
  fetchTickets: () => Promise<void>;
  addTicket: (numbers: number[]) => Promise<void>;
  updateTicket: (ticketId: number, numbers: number[]) => Promise<void>;
  deleteTicket: (ticketId: number) => Promise<void>;

  // Generators
  generateRandom: () => Promise<number[]>;
  generateSystem: () => Promise<number[][]>;
  saveGeneratedRandom: (numbers: number[]) => Promise<void>;
  saveGeneratedSystem: (tickets: number[][]) => Promise<void>;

  // Error handling
  error: string | null;
}

function useTickets(): UseTicketsReturn {
  const { getApiService } = useAppContext();
  const apiService = getApiService();

  const [tickets, setTickets] = useState<Ticket[]>([]);
  const [totalCount, setTotalCount] = useState<number>(0);
  const [loading, setLoading] = useState<boolean>(false);
  const [error, setError] = useState<string | null>(null);

  // useEffect dla initial fetch
  useEffect(() => {
    fetchTickets();
  }, []);

  // Implementacja CRUD operations (API calls via apiService)
  // ...

  return {
    tickets,
    totalCount,
    loading,
    fetchTickets,
    addTicket,
    updateTicket,
    deleteTicket,
    generateRandom,
    generateSystem,
    saveGeneratedRandom,
    saveGeneratedSystem,
    error
  };
}
```

**Użycie w TicketsPage:**
```tsx
function TicketsPage() {
  const {
    tickets,
    totalCount,
    loading,
    fetchTickets,
    addTicket,
    updateTicket,
    deleteTicket,
    generateRandom,
    generateSystem,
    error
  } = useTickets();

  // Reszta logiki UI (modale, toast, handlers)
  // ...
}
```

**Zalety:**
- Separation of concerns (logika biznesowa vs UI)
- Reusability (można użyć useTickets w innych komponentach jeśli potrzeba)
- Testability (łatwiejsze mock'owanie w testach)

## 7. Integracja API

### 7.1 ApiService metody (wykorzystane w widoku)

**Lokalizacja:** `src/services/api-service.ts`

**Metody dla Tickets:**

```tsx
class ApiService {
  private baseUrl: string;
  private appToken: string;
  private authToken: string | null = null;

  // ... constructor, setAuthToken, clearAuthToken

  // GET /api/tickets
  async getTickets(): Promise<GetTicketsResponse> {
    const response = await this.request('/api/tickets', {
      method: 'GET'
    });
    return response;
  }

  // POST /api/tickets
  async createTicket(numbers: number[]): Promise<TicketResponse> {
    const response = await this.request('/api/tickets', {
      method: 'POST',
      body: JSON.stringify({ numbers })
    });
    return response;
  }

  // PUT /api/tickets/{id}
  async updateTicket(ticketId: number, numbers: number[]): Promise<UpdateTicketResponse> {
    const response = await this.request(`/api/tickets/${ticketId}`, {
      method: 'PUT',
      body: JSON.stringify({ numbers })
    });
    return response;
  }

  // DELETE /api/tickets/{id}
  async deleteTicket(ticketId: number): Promise<DeleteTicketResponse> {
    const response = await this.request(`/api/tickets/${ticketId}`, {
      method: 'DELETE'
    });
    return response;
  }

  // POST /api/tickets/generate-random
  async generateRandomTicket(): Promise<GenerateRandomResponse> {
    const response = await this.request('/api/tickets/generate-random', {
      method: 'POST'
    });
    return response;
  }

  // POST /api/tickets/generate-system
  async generateSystemTickets(): Promise<GenerateSystemResponse> {
    const response = await this.request('/api/tickets/generate-system', {
      method: 'POST'
    });
    return response;
  }

  // Private method wykonujący fetch z error handling
  private async request(endpoint: string, options: RequestInit): Promise<any> {
    const headers = {
      'Content-Type': 'application/json',
      'X-TOKEN': this.appToken,
      ...(this.authToken && { 'Authorization': `Bearer ${this.authToken}` })
    };

    try {
      const response = await fetch(`${this.baseUrl}${endpoint}`, {
        ...options,
        headers: { ...headers, ...options.headers }
      });

      if (!response.ok) {
        const errorData = await response.json();
        throw new ApiError(response.status, errorData);
      }

      return await response.json();
    } catch (error) {
      if (error instanceof ApiError) throw error;
      throw new NetworkError('Błąd połączenia z serwerem');
    }
  }
}

// Custom Error classes
class ApiError extends Error {
  constructor(public status: number, public data: any) {
    super(`API Error: ${status}`);
  }
}

class NetworkError extends Error {
  constructor(message: string) {
    super(message);
  }
}
```

### 7.2 Error handling pattern w komponentach

**Przykład: handleAddTicket**

```tsx
async function handleAddTicket(numbers: number[]) {
  const apiService = getApiService();

  try {
    await apiService.createTicket(numbers);

    // Sukces: Toast + refresh
    setToastMessage('Zestaw zapisany pomyślnie');
    setToastVariant('success');
    await fetchTickets(); // Refresh listy
    setIsTicketFormOpen(false); // Zamknij modal

  } catch (error) {
    if (error instanceof ApiError) {
      // Błąd API (4xx, 5xx)
      if (error.status >= 400 && error.status < 500) {
        // 4xx: szczegółowe komunikaty z backendu
        const errors = error.data.errors
          ? Object.values(error.data.errors).flat()
          : [error.data.error || 'Wystąpił błąd'];

        setErrors(errors);
        setIsErrorModalOpen(true);
      } else {
        // 5xx: generyczny komunikat
        setErrors(['Wystąpił problem z serwerem. Spróbuj ponownie za chwilę.']);
        setIsErrorModalOpen(true);
      }

      // Specjalny przypadek: wygasły token (401)
      if (error.status === 401) {
        logout();
        navigate('/login');
        setErrors(['Twoja sesja wygasła. Zaloguj się ponownie.']);
        setIsErrorModalOpen(true);
      }
    } else if (error instanceof NetworkError) {
      // Network error
      setErrors([error.message]);
      setIsErrorModalOpen(true);
    } else {
      // Nieoczekiwany błąd
      setErrors(['Wystąpił nieoczekiwany błąd']);
      setIsErrorModalOpen(true);
    }
  }
}
```

### 7.3 Typy żądań i odpowiedzi

**Request types (wysyłane do API):**

```tsx
// POST /api/tickets, PUT /api/tickets/{id}
interface TicketRequest {
  numbers: number[]; // 6 liczb z zakresu 1-49
}
```

**Response types (otrzymywane z API):**

```tsx
// GET /api/tickets
interface GetTicketsResponse {
  tickets: Ticket[];
  totalCount: number;
  limit: number;
}

// POST /api/tickets (sukces)
interface TicketResponse {
  message: string; // "Zestaw utworzony pomyślnie"
}

// PUT /api/tickets/{id} (sukces)
interface UpdateTicketResponse {
  message: string; // "Zestaw zaktualizowany pomyślnie"
}

// DELETE /api/tickets/{id} (sukces)
interface DeleteTicketResponse {
  message: string; // "Zestaw usunięty pomyślnie"
}

// POST /api/tickets/generate-random (sukces)
interface GenerateRandomResponse {
  message: string; // "Zestaw wygenerowany pomyślnie"
}

// POST /api/tickets/generate-system (sukces)
interface GenerateSystemResponse {
  message: string; // "9 zestawów wygenerowanych i zapisanych pomyślnie"
}

// Błąd API (400 Bad Request)
interface ApiErrorResponse {
  errors?: {
    numbers?: string[];
    limit?: string[];
    duplicate?: string[];
    [field: string]: string[] | undefined;
  };
  error?: string; // Pojedynczy błąd (fallback)
}
```

**Komunikaty błędów z backendu (z api-plan.md):**

**POST /api/tickets (400 Bad Request):**
```json
{
  "errors": {
    "numbers": [
      "Wymagane dokładnie 6 liczb",
      "Liczby muszą być unikalne",
      "Liczby muszą być w zakresie 1-49",
      "Zestaw już istnieje"
    ],
    "limit": [
      "Osiągnięto limit 100 zestawów. Usuń istniejące zestawy, aby dodać nowe."
    ]
  }
}
```

**POST /api/tickets/generate-system (400 Bad Request):**
```json
{
  "errors": {
    "limit": [
      "Brak miejsca na 9 zestawów. Dostępne: 3 zestawy. Usuń istniejące zestawy, aby kontynuować."
    ]
  }
}
```

## 8. Interakcje użytkownika

### 8.1 Główny przepływ: Dodawanie zestawu ręcznie

**Krok 1:** Użytkownik klika przycisk "+ Dodaj ręcznie"

**Krok 2:** System sprawdza limit zestawów
- Jeśli `tickets.length >= 100`: ErrorModal "Osiągnięto limit 100 zestawów..."
- Jeśli OK: otwiera TicketFormModal w trybie 'add'

**Krok 3:** Użytkownik wypełnia formularz
- Wprowadza 6 liczb (onChange w każdym NumberInput)
- Inline validation w czasie rzeczywistym:
  - Czerwony border + komunikat błędu pod polem jeśli nieprawidłowa wartość
  - Zielony border (opcjonalnie) jeśli OK

**Krok 4:** Użytkownik klika [Zapisz]
- Frontend: zbiera wszystkie błędy inline validation
- Jeśli błędy: wyświetla ErrorModal z listą błędów
- Jeśli OK: API call `POST /api/tickets { numbers }`

**Krok 5a (sukces):**
- Backend: walidacja + zapis do bazy
- Response 201: `{ message: "Zestaw utworzony pomyślnie" }`
- Frontend:
  - Toast "Zestaw zapisany pomyślnie" (zielony, auto-dismiss 3s)
  - Modal zamyka się
  - Lista zestawów odświeża (API call `GET /api/tickets`)
  - Licznik aktualizuje się (np. 42/100 → 43/100)

**Krok 5b (błąd biznesowy - duplikat zestawu):**
- Backend: wykrywa duplikat po walidacji unikalności
- Response 400: `{ errors: { numbers: ["Zestaw już istnieje"] } }`
- Frontend:
  - ErrorModal: "• Zestaw już istnieje"
  - Modal TicketFormModal pozostaje otwarty (użytkownik może poprawić)

**Krok 5c (błąd limitu - race condition):**
- Backend: wykrywa limit po walidacji (inny request dodał zestaw w międzyczasie)
- Response 400: `{ errors: { limit: ["Osiągnięto limit 100 zestawów..."] } }`
- Frontend: ErrorModal z komunikatem, modal zamyka

---

### 8.2 Przepływ: Edycja zestawu

**Krok 1:** Użytkownik klika [Edytuj] przy zestawie #5 w liście

**Krok 2:** System otwiera TicketFormModal w trybie 'edit'
- Pre-wypełnia pola aktualnymi wartościami zestawu
- Przechowuje `ticketId` w state (INT)

**Krok 3:** Użytkownik modyfikuje liczby (np. zmienia 48 → 49)
- Inline validation działa identycznie jak przy dodawaniu

**Krok 4:** Użytkownik klika [Zapisz]
- Walidacja inline + API call `PUT /api/tickets/{id} { numbers }`

**Krok 5a (sukces):**
- Backend: walidacja + update w bazie
- Response 200: `{ message: "Zestaw zaktualizowany pomyślnie" }`
- Frontend:
  - Toast "Zestaw zaktualizowany pomyślnie"
  - Modal zamyka, lista odświeża
  - Zestaw #5 wyświetla zaktualizowane liczby

**Krok 5b (błąd unikalności - zmodyfikowany zestaw duplikuje inny):**
- Backend: wykrywa duplikat (po walidacji, pomijając edytowany zestaw)
- Response 400: `{ errors: { duplicate: ["Taki zestaw już istnieje..."] } }`
- Frontend: ErrorModal, modal pozostaje otwarty

---

### 8.3 Przepływ: Usuwanie zestawu

**Krok 1:** Użytkownik klika [Usuń] przy zestawie #3

**Krok 2:** System otwiera DeleteConfirmModal
- Wyświetla liczby zestawu #3
- Focus na [Anuluj] button (safe default)

**Krok 3:** Użytkownik klika [Usuń]
- API call `DELETE /api/tickets/{id}`

**Krok 4a (sukces):**
- Backend: usuwa zestaw z bazy (CASCADE DELETE dla TicketNumbers)
- Response 200: `{ message: "Zestaw usunięty pomyślnie" }`
- Frontend:
  - Toast "Zestaw usunięty pomyślnie"
  - Modal zamyka, lista odświeża
  - Zestaw #3 znika z listy
  - Licznik aktualizuje (np. 43/100 → 42/100)

**Krok 4b (błąd 404 - zestaw już usunięty):**
- Backend: zestaw nie istnieje (race condition - inny request usunął)
- Response 404: `{ error: "Zestaw nie istnieje" }`
- Frontend: ErrorModal "Zestaw nie istnieje", modal zamyka, lista odświeża

**Krok 4c (błąd 403 - próba usunięcia cudzego zestawu, security issue):**
- Backend: sprawdzenie `UserId`, zestaw należy do innego użytkownika
- Response 403: `{ error: "Brak uprawnień" }`
- Frontend: ErrorModal "Brak uprawnień do usunięcia zestawu", modal zamyka

---

### 8.4 Przepływ: Generator losowy

**Krok 1:** Użytkownik klika "🎲 Generuj losowy"

**Krok 2:** System generuje losowy zestaw
- **Opcja A:** Frontend algorytm (Fishera-Yatesa shuffle)
  ```tsx
  function generateRandom(): number[] {
    const numbers = Array.from({ length: 49 }, (_, i) => i + 1);
    // Fisher-Yates shuffle
    for (let i = numbers.length - 1; i > 0; i--) {
      const j = Math.floor(Math.random() * (i + 1));
      [numbers[i], numbers[j]] = [numbers[j], numbers[i]];
    }
    return numbers.slice(0, 6).sort((a, b) => a - b); // 6 pierwszych, posortowane
  }
  ```
- **Opcja B:** Backend API call `POST /api/tickets/generate-random` (return numbers)

**Krok 3:** System otwiera GeneratorPreviewModal
- Wyświetla 6 wygenerowanych liczb jako badges

**Krok 4a:** Użytkownik klika [Generuj ponownie]
- Powtórzenie kroku 2 (nowy losowy zestaw)
- Modal pozostaje otwarty, wyświetla nowe liczby

**Krok 4b:** Użytkownik klika [Zapisz]
- Frontend sprawdza limit: `tickets.length < 100`
- Jeśli ≥100: ErrorModal "Osiągnięto limit..."
- Jeśli OK: API call `POST /api/tickets { numbers }`
- Po sukcesie: Toast "Zestaw wygenerowany i zapisany", modal zamyka, lista odświeża

**Krok 4c:** Użytkownik klika [Anuluj]
- Modal zamyka bez zapisu

---

### 8.5 Przepływ: Generator systemowy

**Krok 1:** Użytkownik klika "🔢 Generuj systemowy"

**Krok 2:** System sprawdza miejsce na 9 zestawów
- Jeśli `100 - tickets.length < 9`: ErrorModal "Brak miejsca na 9 zestawów. Dostępne: X zestawy..."
- Jeśli OK: generuje 9 zestawów

**Krok 3:** Generowanie 9 zestawów
- **Opcja A:** Frontend algorytm (z api-plan.md, sekcja 4.2.4)
- **Opcja B:** Backend API call `POST /api/tickets/generate-system` (return 9 zestawów)

**Krok 4:** System otwiera GeneratorSystemPreviewModal
- Desktop: grid 3x3 (9 kart)
- Mobile: vertical list (9 kart stacked)
- Tooltip wyjaśniający algorytm

**Krok 5a:** Użytkownik klika [Generuj ponownie]
- Powtórzenie kroku 3 (nowe 9 zestawów)
- Modal pozostaje otwarty, wyświetla nowe zestawy

**Krok 5b:** Użytkownik klika [Zapisz wszystkie]
- Frontend sprawdza ponownie miejsce (na wypadek race condition)
- API call `POST /api/tickets/generate-system` (bulk save 9 zestawów)
- Po sukcesie: Toast "9 zestawów wygenerowanych i zapisanych", modal zamyka, lista odświeża
- Licznik aktualizuje (np. 42/100 → 51/100)

**Krok 5c:** Użytkownik klika [Anuluj]
- Modal zamyka bez zapisu

---

### 8.6 Interakcje z TicketCounter

**Progresywna kolorystyka (visual feedback):**
- **0-70 zestawów (0-70%):** Licznik zielony `text-green-600` - bezpieczna strefa
- **71-90 zestawów (71-90%):** Licznik żółty `text-yellow-600` - ostrzeżenie
- **91-100 zestawów (91-100%):** Licznik czerwony `text-red-600` - limit bliski

**Toast ostrzegawczy (proaktywny):**
- Jeśli `tickets.length > 95` przy mount/refresh listy:
  - Toast (warning, żółty): "Uwaga: Pozostało tylko {100 - tickets.length} wolnych miejsc"
  - Auto-dismiss 4s (dłużej niż success toast)

**Przykład:**
- Użytkownik ma 96 zestawów → licznik czerwony "[96/100]"
- Toast pojawia się: "Uwaga: Pozostało tylko 4 wolne miejsca"

---

### 8.7 Empty State (brak zestawów)

**Warunek:** `tickets.length === 0`

**Wyświetlane:**
```tsx
<div className="text-center py-12 text-gray-500">
  <p className="text-lg mb-2">Nie masz jeszcze żadnych zestawów.</p>
  <p className="text-sm">Dodaj swój pierwszy zestaw używając przycisków powyżej.</p>
</div>
```

**Call-to-action (wizualny):**
- Action buttons (Dodaj ręcznie, Generuj losowy, Generuj systemowy) są widoczne i dostępne
- Użytkownik od razu widzi jak może dodać pierwszy zestaw

---

## 9. Warunki i walidacja

### 9.1 Walidacja inline (real-time w NumberInput)

**Warunki sprawdzane podczas onChange:**

1. **Zakres liczb (1-49):**
   - **Warunek:** `value >= 1 && value <= 49`
   - **Komunikat błędu:** "Liczba musi być w zakresie 1-49"
   - **Efekt UI:** Czerwony border input (`border-red-500`), komunikat pod polem

2. **Unikalność liczb w zestawie:**
   - **Warunek:** Sprawdzenie czy aktualna wartość nie pojawia się w innych polach
   - **Algorytm:**
     ```tsx
     const isDuplicate = numbers.filter(n => n === currentValue).length > 1;
     ```
   - **Komunikat błędu:** "Liczby w zestawie muszą być unikalne"
   - **Efekt UI:** Czerwony border dla wszystkich duplikatów

3. **Puste pole (wymagane):**
   - **Warunek:** `value === ''`
   - **Komunikat błędu:** "To pole jest wymagane" (wyświetlany przy blur lub submit)
   - **Efekt UI:** Czerwony border

**Przykład stanu błędów dla pól:**
```tsx
// State w TicketFormModal
const [numbers, setNumbers] = useState<(number | '')[]>(['', '', '', '', '', '']);
const [errors, setErrors] = useState<(string | undefined)[]>([
  undefined, undefined, undefined, undefined, undefined, undefined
]);

// Funkcja walidacji inline dla pojedynczego pola
function validateField(index: number, value: number | ''): string | undefined {
  if (value === '') return 'To pole jest wymagane';
  if (value < 1 || value > 49) return 'Liczba musi być w zakresie 1-49';

  // Sprawdzenie duplikatu
  const duplicateCount = numbers.filter(n => n === value).length;
  if (duplicateCount > 1) return 'Liczby w zestawie muszą być unikalne';

  return undefined; // Brak błędu
}

// Handler onChange
function handleNumberChange(index: number, value: number | '') {
  const newNumbers = [...numbers];
  newNumbers[index] = value;
  setNumbers(newNumbers);

  // Inline validation
  const newErrors = [...errors];
  newErrors[index] = validateField(index, value);
  setErrors(newErrors);
}
```

---

### 9.2 Walidacja przy submit (zbieranie wszystkich błędów)

**Warunki sprawdzane przed API call:**

1. **Wszystkie pola wypełnione:**
   - **Warunek:** `numbers.every(n => n !== '')`
   - **Komunikat:** "Wszystkie pola są wymagane"

2. **Wszystkie liczby w zakresie 1-49:**
   - **Warunek:** `numbers.every(n => n >= 1 && n <= 49)`
   - **Komunikat:** "Liczby muszą być w zakresie 1-49"

3. **Liczby unikalne w zestawie:**
   - **Warunek:** `new Set(numbers).size === 6`
   - **Komunikat:** "Liczby w zestawie muszą być unikalne"

**Funkcja walidacji zbierająca wszystkie błędy:**
```tsx
function validateNumbers(numbers: (number | '')[]): string[] {
  const errors: string[] = [];

  // Sprawdzenie wypełnienia
  if (numbers.some(n => n === '')) {
    errors.push('Wszystkie pola są wymagane');
  }

  // Filtrowanie tylko wypełnionych liczb dla dalszej walidacji
  const validNumbers = numbers.filter(n => n !== '') as number[];

  // Sprawdzenie zakresu
  if (validNumbers.some(n => n < 1 || n > 49)) {
    errors.push('Liczby muszą być w zakresie 1-49');
  }

  // Sprawdzenie unikalności
  const uniqueNumbers = new Set(validNumbers);
  if (uniqueNumbers.size !== validNumbers.length) {
    errors.push('Liczby w zestawie muszą być unikalne');
  }

  return errors;
}

// Handler submit w TicketFormModal
async function handleSubmit(e: React.FormEvent) {
  e.preventDefault();

  // Walidacja
  const validationErrors = validateNumbers(numbers);
  if (validationErrors.length > 0) {
    // Wyświetlenie ErrorModal z listą błędów
    setErrors(validationErrors);
    setIsErrorModalOpen(true);
    return;
  }

  // API call
  try {
    await onSubmit(numbers as number[], ticketId);
    // Sukces obsługiwany w parent component
  } catch (error) {
    // Błędy API obsługiwane w parent component
  }
}
```

---

### 9.3 Walidacja limitu zestawów (przed otwarciem modalu/zapisem)

**Warunek 1: Limit 100 zestawów (dodawanie pojedynczego zestawu)**
- **Sprawdzane:** Przed otwarciem TicketFormModal lub GeneratorPreviewModal (losowy)
- **Warunek:** `tickets.length < 100`
- **Komunikat błędu (jeśli ≥100):** "Osiągnięto limit 100 zestawów. Usuń istniejące zestawy, aby dodać nowe."
- **Efekt UI:** ErrorModal, modal dodawania nie otwiera się

**Przykład kodu:**
```tsx
function handleAddTicket() {
  if (tickets.length >= 100) {
    setErrors(['Osiągnięto limit 100 zestawów. Usuń istniejące zestawy, aby dodać nowe.']);
    setIsErrorModalOpen(true);
    return;
  }

  // Otwórz modal dodawania
  setTicketFormState({ mode: 'add' });
  setIsTicketFormOpen(true);
}
```

**Warunek 2: Miejsce na 9 zestawów (generator systemowy)**
- **Sprawdzane:** Przed otwarciem GeneratorSystemPreviewModal
- **Warunek:** `100 - tickets.length >= 9`
- **Komunikat błędu (jeśli brak miejsca):**
  ```
  Brak miejsca na 9 zestawów. Dostępne: {100 - tickets.length} zestawy.
  Usuń istniejące zestawy, aby kontynuować.
  ```
- **Efekt UI:** ErrorModal, modal generatora nie otwiera się

**Przykład kodu:**
```tsx
function handleGenerateSystem() {
  const available = 100 - tickets.length;

  if (available < 9) {
    setErrors([
      `Brak miejsca na 9 zestawów. Dostępne: ${available} ${available === 1 ? 'zestaw' : 'zestawy'}. Usuń istniejące zestawy, aby kontynuować.`
    ]);
    setIsErrorModalOpen(true);
    return;
  }

  // Generuj 9 zestawów i otwórz modal preview
  const systemTickets = generateSystemTickets();
  setGeneratorState({ type: 'system', numbers: systemTickets });
  setIsGeneratorPreviewOpen(true);
}
```

---

### 9.4 Walidacja unikalności zestawu (backend)

**Sprawdzane:** Podczas API call `POST /api/tickets` lub `PUT /api/tickets/{id}`

**Algorytm backendu (z api-plan.md, sekcja 4.2.3):**
1. Pobranie wszystkich zestawów użytkownika z TicketNumbers
2. Sortowanie liczb w nowym i istniejących zestawach
3. Porównanie: `newNumbersSorted.SequenceEqual(existingNumbersSorted)`
4. Jeśli duplikat: zwrócenie 400 Bad Request

**Response 400 (duplikat):**
```json
{
  "errors": {
    "numbers": ["Zestaw już istnieje"]
  }
}
```

lub (dla edycji):
```json
{
  "errors": {
    "duplicate": ["Taki zestaw już istnieje w Twoich zapisanych zestawach"]
  }
}
```

**Obsługa w frontend:**
```tsx
catch (error) {
  if (error instanceof ApiError && error.status === 400) {
    const errors = error.data.errors
      ? Object.values(error.data.errors).flat()
      : [error.data.error || 'Wystąpił błąd'];

    // ErrorModal z komunikatem o duplikacie
    setErrors(errors);
    setIsErrorModalOpen(true);
  }
}
```

**Uwaga:** Frontend NIE implementuje walidacji unikalności zestawu (zbyt kosztowne obliczeniowo dla 100 zestawów × 6 liczb). Backend jest źródłem prawdy, frontend tylko wyświetla błąd.

---

### 9.5 Wpływ warunków na stan UI

**Progresywna kolorystyka licznika (visual feedback):**
| Stan zestawów | Kolorystyka | Toast ostrzegawczy |
|---------------|-------------|-------------------|
| 0-70 (0-70%) | Zielony `text-green-600` | Nie |
| 71-90 (71-90%) | Żółty `text-yellow-600` | Nie |
| 91-95 (91-95%) | Czerwony `text-red-600` | Nie |
| 96-100 (96-100%) | Czerwony `text-red-600` | Tak, "Uwaga: Pozostało tylko X wolnych miejsc" |

**Blokady akcji (disabled/conditional rendering):**
- Jeśli `tickets.length >= 100`:
  - Przyciski "+ Dodaj ręcznie" i "🎲 Generuj losowy" **nie są wyłączone**, ale kliknięcie pokazuje ErrorModal
  - Przycisk "🔢 Generuj systemowy" **nie jest wyłączony**, ale kliknięcie pokazuje ErrorModal jeśli `available < 9`
- **Decyzja projektowa:** Przyciski pozostają aktywne (nie `disabled`), feedback przez ErrorModal zamiast tooltip (lepsze UX dla mobile, spójne z resztą aplikacji)

**Inline validation (border colors):**
- Puste pole lub błąd: `border-red-500`
- Poprawna wartość (opcjonalnie): `border-green-500`
- Domyślny stan: `border-gray-300`

---

## 10. Obsługa błędów

### 10.1 Kategorie błędów

**1. Błędy walidacji inline (real-time):**
- **Źródło:** Frontend validation w NumberInput podczas onChange
- **Prezentacja:** Komunikat pod polem input (czerwony tekst), czerwony border
- **Przykłady:** "Liczba musi być w zakresie 1-49", "Liczby muszą być unikalne"

**2. Błędy walidacji przy submit (zbierane):**
- **Źródło:** Frontend validation przed API call
- **Prezentacja:** ErrorModal z listą błędów (bullet points)
- **Przykłady:** "Wszystkie pola są wymagane", "Liczby muszą być w zakresie 1-49"

**3. Błędy biznesowe z backendu (400 Bad Request):**
- **Źródło:** Backend walidacja (limit, unikalność zestawu)
- **Prezentacja:** ErrorModal z komunikatem z backendu
- **Przykłady:**
  - "Osiągnięto limit 100 zestawów. Usuń istniejące zestawy, aby dodać nowe."
  - "Zestaw już istnieje"
  - "Brak miejsca na 9 zestawów. Dostępne: 3 zestawy..."

**4. Błędy autoryzacji (401 Unauthorized):**
- **Źródło:** Backend (wygasły lub nieprawidłowy JWT)
- **Prezentacja:** ErrorModal + logout + redirect `/login`
- **Komunikat:** "Twoja sesja wygasła. Zaloguj się ponownie."

**5. Błędy uprawnień (403 Forbidden):**
- **Źródło:** Backend (próba dostępu do cudzego zestawu)
- **Prezentacja:** ErrorModal
- **Komunikat:** "Brak uprawnień do wykonania tej operacji"

**6. Błędy nie znaleziono zasobu (404 Not Found):**
- **Źródło:** Backend (zestaw nie istnieje, np. race condition)
- **Prezentacja:** ErrorModal
- **Komunikat:** "Zestaw nie istnieje" (lub został już usunięty)

**7. Błędy serwera (5xx):**
- **Źródło:** Backend (wewnętrzny błąd serwera, baza danych niedostępna)
- **Prezentacja:** ErrorModal
- **Komunikat:** "Wystąpił problem z serwerem. Spróbuj ponownie za chwilę."

**8. Błędy połączenia (Network Error):**
- **Źródło:** Brak połączenia z serwerem, timeout
- **Prezentacja:** ErrorModal
- **Komunikat:** "Błąd połączenia z serwerem. Sprawdź swoje połączenie internetowe."

---

### 10.2 Struktura ErrorModal (wykorzystywany dla wszystkich błędów)

**Komponent:** Shared component `ErrorModal` (z sekcji 5 Shared Components w ui-plan.md)

**Props:**
```tsx
interface ErrorModalProps {
  isOpen: boolean;
  onClose: () => void;
  errors: string[] | string; // Lista błędów lub pojedynczy string
}
```

**Layout:**
```tsx
<Modal isOpen={isOpen} onClose={onClose} title="Błąd" size="sm">
  <div className="mb-6">
    {Array.isArray(errors) ? (
      <ul className="list-disc list-inside text-red-600">
        {errors.map((error, index) => (
          <li key={index}>{error}</li>
        ))}
      </ul>
    ) : (
      <p className="text-red-600">{errors}</p>
    )}
  </div>
  <div className="flex justify-end">
    <Button onClick={onClose} variant="primary">Zamknij</Button>
  </div>
</Modal>
```

**Przykład użycia:**
```tsx
// W TicketsPage
const [isErrorModalOpen, setIsErrorModalOpen] = useState(false);
const [errors, setErrors] = useState<string[]>([]);

// ...

<ErrorModal
  isOpen={isErrorModalOpen}
  onClose={() => setIsErrorModalOpen(false)}
  errors={errors}
/>
```

---

### 10.3 Obsługa błędów w API calls

**Pattern try-catch we wszystkich handlerach:**

```tsx
async function handleAddTicket(numbers: number[]) {
  const apiService = getApiService();
  setLoading(true);

  try {
    await apiService.createTicket(numbers);

    // Sukces
    setToastMessage('Zestaw zapisany pomyślnie');
    setToastVariant('success');
    await fetchTickets();
    setIsTicketFormOpen(false);

  } catch (error) {
    // Obsługa błędów
    handleApiError(error);
  } finally {
    setLoading(false);
  }
}

// Centralized error handler (reusable w całym komponencie)
function handleApiError(error: unknown) {
  if (error instanceof ApiError) {
    // Błąd API (4xx, 5xx)
    if (error.status >= 400 && error.status < 500) {
      // 4xx: błędy klienta (walidacja, biznesowe)
      const errors = error.data.errors
        ? Object.values(error.data.errors).flat() as string[]
        : [error.data.error || 'Wystąpił błąd'];

      setErrors(errors);
      setIsErrorModalOpen(true);

      // Specjalny przypadek: 401 Unauthorized (wygasły token)
      if (error.status === 401) {
        logout();
        navigate('/login');
      }
    } else {
      // 5xx: błędy serwera
      setErrors(['Wystąpił problem z serwerem. Spróbuj ponownie za chwilę.']);
      setIsErrorModalOpen(true);
    }
  } else if (error instanceof NetworkError) {
    // Network error
    setErrors([error.message || 'Błąd połączenia z serwerem. Sprawdź swoje połączenie internetowe.']);
    setIsErrorModalOpen(true);
  } else {
    // Nieoczekiwany błąd
    console.error('Unexpected error:', error);
    setErrors(['Wystąpił nieoczekiwany błąd']);
    setIsErrorModalOpen(true);
  }
}
```

---

### 10.4 Komunikaty błędów (user-friendly, język polski)

**Zasady tworzenia komunikatów:**
1. **Jasne i konkretne** - użytkownik wie co jest nie tak i co powinien zrobić
2. **Język polski** - wszystkie komunikaty po polsku (NFR-021)
3. **Bez technicznego żargonu** - np. "Zestaw nie istnieje" zamiast "HTTP 404 Not Found"
4. **Actionable** - jeśli możliwe, wskazanie jak naprawić problem

**Przykłady dobrych komunikatów:**

| Sytuacja | Komunikat |
|----------|-----------|
| Puste pole w formularzu | "Wszystkie pola są wymagane" |
| Liczba poza zakresem | "Liczby muszą być w zakresie 1-49" |
| Duplikat liczby w zestawie | "Liczby w zestawie muszą być unikalne" |
| Duplikat zestawu | "Zestaw już istnieje" |
| Limit 100 zestawów | "Osiągnięto limit 100 zestawów. Usuń istniejące zestawy, aby dodać nowe." |
| Brak miejsca na 9 zestawów | "Brak miejsca na 9 zestawów. Dostępne: 3 zestawy. Usuń istniejące zestawy, aby kontynuować." |
| Wygasła sesja | "Twoja sesja wygasła. Zaloguj się ponownie." |
| Brak uprawnień | "Brak uprawnień do wykonania tej operacji" |
| Zestaw nie istnieje | "Zestaw nie istnieje" |
| Błąd serwera | "Wystąpił problem z serwerem. Spróbuj ponownie za chwilę." |
| Błąd połączenia | "Błąd połączenia z serwerem. Sprawdź swoje połączenie internetowe." |

---

### 10.5 Toast notifications dla sukcesu (non-intrusive feedback)

**Komponent:** Shared component `Toast` (z sekcji 5 Shared Components)

**Komunikaty sukcesu:**
- **Dodanie zestawu:** "Zestaw zapisany pomyślnie" (zielony, auto-dismiss 3s)
- **Edycja zestawu:** "Zestaw zaktualizowany pomyślnie" (zielony, auto-dismiss 3s)
- **Usunięcie zestawu:** "Zestaw usunięty pomyślnie" (zielony, auto-dismiss 3s)
- **Generator losowy:** "Zestaw wygenerowany i zapisany" (zielony, auto-dismiss 3s)
- **Generator systemowy:** "9 zestawów wygenerowanych i zapisanych" (zielony, auto-dismiss 3s)

**Toast ostrzegawczy:**
- **Limit bliski (>95 zestawów):** "Uwaga: Pozostało tylko X wolnych miejsc" (żółty, auto-dismiss 4s)

**Pozycjonowanie:**
- Top-right corner (fixed)
- Stack: multiple toasts stacked vertically (jeśli pojawia się kilka)
- Z-index: 100 (nad modalami backdrop, ale nie nad modal content)

---

## 11. Kroki implementacji

### Faza 1: Setup i Struktura Projektu (1 dzień)

**Krok 1.1: Utworzenie struktury folderów**
```
src/
├── components/
│   ├── Shared/
│   │   ├── Button.tsx
│   │   ├── Modal.tsx
│   │   ├── ErrorModal.tsx
│   │   ├── Toast.tsx
│   │   ├── Spinner.tsx
│   │   ├── NumberInput.tsx
│   │   └── index.ts
│   └── Tickets/
│       ├── TicketCounter.tsx
│       ├── TicketList.tsx
│       ├── TicketItem.tsx
│       ├── TicketFormModal.tsx
│       ├── GeneratorPreviewModal.tsx
│       ├── GeneratorSystemPreviewModal.tsx
│       ├── DeleteConfirmModal.tsx
│       └── index.ts
├── pages/
│   └── tickets/
│       └── tickets-page.tsx
├── services/
│   ├── api-service.ts
│   └── contracts/
│       └── tickets.ts
├── hooks/
│   └── useTickets.ts
├── utils/
│   ├── validators.ts
│   └── generators.ts
└── types/
    └── tickets.ts
```

**Krok 1.2: Definicja typów TypeScript**
- Utworzenie `src/types/tickets.ts` z wszystkimi interfejsami (DTO, ViewModel, Props)
- Utworzenie `src/services/contracts/tickets.ts` z request/response types

**Krok 1.3: Weryfikacja środowiska**
- Sprawdzenie zmiennych środowiskowych: `VITE_API_URL`, `VITE_APP_TOKEN`
- Test połączenia z backendem: `GET /api/apiversion` (smoke test)

---

### Faza 2: Shared Components (2 dni)

**Krok 2.1: Implementacja Button component**
- 3 warianty (primary, secondary, danger)
- Props: variant, onClick, disabled, className, children, type
- Tailwind styling z hover states
- Accessibility: focus ring, aria-disabled

**Krok 2.2: Implementacja Modal component**
- Props: isOpen, onClose, title, children, size
- Backdrop (semi-transparent overlay)
- Close button (X) w prawym górnym rogu
- Escape key handler (zamyka modal)
- Focus trap (Tab cycle w obrębie modal content)
- Auto-focus na pierwszy element przy otwarciu

**Krok 2.3: Implementacja ErrorModal component**
- Extends Modal
- Props: isOpen, onClose, errors (string[] | string)
- Layout: lista błędów (bullet points) + przycisk [Zamknij]
- Styling: komunikaty czerwone

**Krok 2.4: Implementacja Toast component**
- Props: message, variant, duration
- Pozycjonowanie: top-right corner (fixed)
- Auto-dismiss po duration ms (default 3000)
- Animation: slide-in z prawej, fade-out
- Accessibility: role="alert", aria-live="polite"

**Krok 2.5: Implementacja ToastContainer component**
- Stack multiple toasts vertically
- Auto-remove dismissed toasts
- Z-index management

**Krok 2.6: Implementacja Spinner component**
- Props: size, text
- SVG spinner (animated rotate)
- Accessibility: role="status", aria-live="polite"

**Krok 2.7: Implementacja NumberInput component**
- Props: label, value, onChange, error, min, max, required
- Type: number, zakres 1-49
- Inline validation visual feedback (border colors)
- Error message display pod inputem
- Accessibility: label for, aria-describedby, aria-invalid

**Testy jednostkowe dla każdego shared component (vitest + react-testing-library)**

---

### Faza 3: ApiService i Hooki (1.5 dnia)

**Krok 3.1: Rozszerzenie ApiService o metody Tickets**
- `getTickets(): Promise<GetTicketsResponse>`
- `createTicket(numbers): Promise<TicketResponse>`
- `updateTicket(ticketId, numbers): Promise<UpdateTicketResponse>`
- `deleteTicket(ticketId): Promise<DeleteTicketResponse>`
- `generateRandomTicket(): Promise<GenerateRandomResponse>`
- `generateSystemTickets(): Promise<GenerateSystemResponse>`

**Krok 3.2: Custom Error classes**
- `ApiError` (extends Error, props: status, data)
- `NetworkError` (extends Error)

**Krok 3.3: Implementacja useTickets custom hook**
- State: tickets, totalCount, loading, error
- useEffect: initial fetch przy mount
- CRUD methods: fetchTickets, addTicket, updateTicket, deleteTicket
- Generator methods: generateRandom, generateSystem, saveGeneratedRandom, saveGeneratedSystem
- Error handling w każdej metodzie

**Krok 3.4: Utility functions**
- `src/utils/validators.ts`:
  - `validateNumbers(numbers): string[]`
  - `validateField(index, value, numbers): string | undefined`
- `src/utils/generators.ts`:
  - `generateRandomNumbers(): number[]`
  - `generateSystemTickets(): number[][]`

**Testy jednostkowe dla validators i generators**

---

### Faza 4: Tickets Components (3 dni)

**Krok 4.1: Implementacja TicketCounter**
- Props: count, max
- Funkcja `getCounterColor(count, max)`
- Rendering z kolorystyką Tailwind

**Krok 4.2: Implementacja TicketItem**
- Props: ticket, onEdit, onDelete
- Layout: liczby (badges) + data + action buttons
- formatDate helper function

**Krok 4.3: Implementacja TicketList**
- Props: tickets, loading, onEdit, onDelete
- Empty state (conditional)
- Mapping TicketItem[] dla tickets
- Spinner podczas loading

**Krok 4.4: Implementacja TicketFormModal**
- Props: isOpen, onClose, mode, initialNumbers, ticketId, onSubmit
- State: numbers (array 6 elementów), inlineErrors
- 6× NumberInput z inline validation
- Przycisk [Wyczyść]: resetuje numbers do ['', '', '', '', '', '']
- Przycisk [Zapisz]: walidacja + onSubmit callback
- handleNumberChange: update numbers + inline validation
- validateNumbers function przy submit

**Krok 4.5: Implementacja GeneratorPreviewModal (losowy)**
- Props: isOpen, onClose, numbers, onRegenerate, onSave
- Layout: wygenerowane liczby (badges) + buttons
- handleSave: sprawdzenie limitu + onSave callback

**Krok 4.6: Implementacja GeneratorSystemPreviewModal**
- Props: isOpen, onClose, tickets (9x6 liczb), onRegenerate, onSaveAll
- Layout: grid 3x3 (desktop) / vertical (mobile)
- Tooltip wyjaśniający algorytm
- handleSaveAll: sprawdzenie miejsca + onSaveAll callback

**Krok 4.7: Implementacja DeleteConfirmModal**
- Props: isOpen, onClose, ticket, onConfirm
- Layout: pytanie + liczby zestawu + buttons [Anuluj] [Usuń]
- Auto-focus na [Anuluj]

**Testy jednostkowe dla każdego Tickets component**

---

### Faza 5: Główny Komponent TicketsPage (2 dni)

**Krok 5.1: Setup TicketsPage z routing**
- Import useTickets hook
- Import wszystkich Tickets components
- Import wszystkich Shared components
- Definicja lokalnego stanu (modale, toast, errors)

**Krok 5.2: Implementacja fetch i refresh logiki**
- useEffect: initial fetch tickets przy mount
- fetchTickets w useTickets hook
- Obsługa loading state (Spinner w TicketList)

**Krok 5.3: Implementacja handlerów CRUD**
- `handleAddTicket()`: sprawdzenie limitu + otwórz TicketFormModal
- `handleEditTicket(ticketId)`: otwórz TicketFormModal z pre-wypełnionymi danymi
- `handleDeleteTicket(ticketId)`: otwórz DeleteConfirmModal
- `handleSaveTicket(numbers, ticketId?)`: API call (POST/PUT) + error handling + success toast
- `handleConfirmDelete(ticketId)`: API call DELETE + error handling + success toast

**Krok 5.4: Implementacja handlerów generatorów**
- `handleGenerateRandom()`: generuj 6 liczb + otwórz GeneratorPreviewModal
- `handleGenerateSystem()`: sprawdzenie miejsca + generuj 9 zestawów + otwórz GeneratorSystemPreviewModal
- `handleSaveGeneratedRandom(numbers)`: sprawdzenie limitu + API call POST
- `handleSaveGeneratedSystem(tickets)`: sprawdzenie miejsca + API call POST

**Krok 5.5: Implementacja centralized error handler**
- `handleApiError(error)`: pattern z sekcji 10.3
- Obsługa ApiError (4xx, 5xx)
- Obsługa NetworkError
- Specjalny przypadek 401: logout + redirect

**Krok 5.6: Implementacja Toast ostrzegawczego**
- useEffect: sprawdzenie `tickets.length > 95` przy mount/refresh
- Jeśli tak: wyświetl Toast (warning) "Uwaga: Pozostało tylko X wolnych miejsc"

**Krok 5.7: Layout komponentu**
- Header section: h1 + TicketCounter
- Action buttons row: 3 buttons (Dodaj ręcznie, Generuj losowy, Generuj systemowy)
- TicketList (lub Empty State)
- Conditional rendering modalnych komponentów (5 modalów)
- ToastContainer

**Krok 5.8: Testy integracyjne dla TicketsPage**
- Mock ApiService
- Test przepływu: dodawanie zestawu (sukces)
- Test przepływu: dodawanie zestawu (błąd limitu)
- Test przepływu: edycja zestawu (sukces)
- Test przepływu: usuwanie zestawu (sukces)
- Test przepływu: generator losowy (sukces)
- Test przepływu: generator systemowy (sukces, błąd braku miejsca)

---

### Faza 6: Responsywność i Dostępność (1 dzień)

**Krok 6.1: Responsywność (Tailwind breakpoints)**
- **Mobile (<640px):**
  - Action buttons: vertical stack lub 2 kolumny
  - TicketList: full-width cards, vertical stack
  - TicketFormModal: 1 kolumna (6 inputs stacked)
  - GeneratorSystemPreviewModal: vertical list (9 cards stacked)
- **Desktop (≥1024px):**
  - Action buttons: horizontal row
  - TicketList: single column (opcjonalnie grid 2 kolumny)
  - TicketFormModal: 2 kolumny (3 inputs per row)
  - GeneratorSystemPreviewModal: grid 3x3

**Krok 6.2: Accessibility audit**
- **Semantic HTML:** sprawdzenie `<nav>`, `<main>`, `<button>`, `<form>`, `<label>`
- **ARIA attributes:**
  - aria-label dla buttonów z ikonami (🎲, 🔢)
  - aria-describedby dla inline errors w NumberInput
  - aria-invalid dla inputs z błędami
  - aria-live="polite" dla Toast
  - role="alert" dla Toast
  - role="dialog" dla Modal
  - aria-modal="true" dla Modal
- **Keyboard Navigation:**
  - Tab order logiczny (top to bottom, left to right)
  - Enter/Space dla buttonów
  - Escape zamyka modale
- **Focus Management:**
  - Widoczny focus indicator: `focus:ring-2 focus:ring-blue-500`
  - Focus trap w modalach
  - Auto-focus na pierwszy input w TicketFormModal
  - Return focus do trigger button po zamknięciu modalu
  - Auto-focus na [Anuluj] w DeleteConfirmModal (safe default)
- **Color Contrast:**
  - Sprawdzenie WCAG AA (4.5:1 dla tekstu)
  - Nie polegać tylko na kolorze: TicketCounter ma zarówno kolor jak i tekst "X/100"
- **Screen Reader Testing:**
  - Test z NVDA/JAWS (Windows) lub VoiceOver (Mac)
  - Sprawdzenie announce'owania Toast notifications

**Krok 6.3: Touch targets (mobile)**
- Minimum 44x44px dla wszystkich klikanych elementów
- Buttons padding: `px-4 py-2` (minimum)

---

### Faza 7: Testy E2E i QA (1 dzień)

**Krok 7.1: Testy E2E (Playwright)**
- **Przepływ 1: Happy path - dodawanie zestawu**
  1. Zaloguj się
  2. Przejdź do `/tickets`
  3. Kliknij "+ Dodaj ręcznie"
  4. Wypełnij formularz (6 liczb)
  5. Kliknij [Zapisz]
  6. Weryfikuj: Toast "Zestaw zapisany pomyślnie"
  7. Weryfikuj: Nowy zestaw widoczny w liście
  8. Weryfikuj: Licznik zaktualizowany

- **Przepływ 2: Edycja zestawu**
  1. Kliknij [Edytuj] przy zestawie
  2. Zmień jedną liczbę
  3. Kliknij [Zapisz]
  4. Weryfikuj: Toast "Zestaw zaktualizowany pomyślnie"
  5. Weryfikuj: Zaktualizowane liczby widoczne

- **Przepływ 3: Usuwanie zestawu**
  1. Kliknij [Usuń] przy zestawie
  2. Weryfikuj: Modal potwierdzenia z liczbami
  3. Kliknij [Usuń]
  4. Weryfikuj: Toast "Zestaw usunięty pomyślnie"
  5. Weryfikuj: Zestaw zniknął z listy

- **Przepływ 4: Generator losowy**
  1. Kliknij "🎲 Generuj losowy"
  2. Weryfikuj: Modal preview z 6 liczbami
  3. Kliknij [Zapisz]
  4. Weryfikuj: Toast + nowy zestaw w liście

- **Przepływ 5: Generator systemowy**
  1. Kliknij "🔢 Generuj systemowy"
  2. Weryfikuj: Modal preview z 9 zestawami
  3. Kliknij [Zapisz wszystkie]
  4. Weryfikuj: Toast + 9 nowych zestawów w liście

- **Przepływ 6: Błąd limitu**
  1. Dodaj zestawy do osiągnięcia 100
  2. Kliknij "+ Dodaj ręcznie"
  3. Weryfikuj: ErrorModal "Osiągnięto limit 100 zestawów..."

- **Przepływ 7: Błąd duplikatu zestawu**
  1. Dodaj zestaw [1, 2, 3, 4, 5, 6]
  2. Spróbuj dodać ten sam zestaw ponownie
  3. Weryfikuj: ErrorModal "Zestaw już istnieje"

**Krok 7.2: Manual QA**
- Testowanie na różnych przeglądarkach (Chrome, Firefox, Safari, Edge)
- Testowanie na różnych rozdzielczościach (mobile, tablet, desktop)
- Testowanie z różnymi danymi (0 zestawów, 50 zestawów, 100 zestawów)
- Testowanie edge cases (np. bardzo szybkie klikanie przycisku [Zapisz])

**Krok 7.3: Performance testing**
- Testowanie renderowania listy z 100 zestawami (powinno być płynne)
- Testowanie responsywności UI podczas API calls (Spinner visible, UI nie blokuje się)

---

### Faza 8: Dokumentacja i Finalizacja (0.5 dnia)

**Krok 8.1: Dokumentacja kodu**
- JSDoc comments dla kluczowych funkcji
- README dla folderu `src/components/Tickets/`
- Komentarze w skomplikowanych algorytmach (np. generator systemowy)

**Krok 8.2: Code review**
- Self-review: sprawdzenie spójności z ui-plan.md i prd.md
- Sprawdzenie zgodności z PRD requirements (F-TICKET-001 do F-TICKET-006)

**Krok 8.3: Final polish**
- Sprawdzenie wszystkich komunikatów (język polski, user-friendly)
- Sprawdzenie kolorystyki i stylingu (spójność z resztą aplikacji)
- Sprawdzenie animacji (smooth transitions dla modalów, toastów)

---

### Podsumowanie Timeline

| Faza | Czas | Opis |
|------|------|------|
| 1. Setup i Struktura | 1 dzień | Foldery, typy TypeScript, weryfikacja środowiska |
| 2. Shared Components | 2 dni | Button, Modal, ErrorModal, Toast, Spinner, NumberInput |
| 3. ApiService i Hooki | 1.5 dnia | ApiService methods, useTickets hook, validators, generators |
| 4. Tickets Components | 3 dni | TicketCounter, TicketList, TicketItem, modale (Form, Preview, Delete) |
| 5. TicketsPage | 2 dni | Główny komponent, handlery CRUD/generatory, error handling |
| 6. Responsywność i Dostępność | 1 dzień | Tailwind breakpoints, ARIA, keyboard navigation, focus management |
| 7. Testy E2E i QA | 1 dzień | Playwright, manual QA, performance testing |
| 8. Dokumentacja i Finalizacja | 0.5 dnia | JSDoc, README, code review, final polish |
| **ŁĄCZNIE** | **12 dni** | ~2.5 tygodnie robocze |

---

**Koniec planu implementacji widoku Tickets Page**
