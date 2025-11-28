# Plan implementacji widoku Historia Losowań (Draws Page)

## 1. Przegląd

Widok Historia Losowań (Draws Page) umożliwia przeglądanie wszystkich wprowadzonych wyników losowań LOTTO z możliwością filtrowania po zakresie dat i paginacji. Użytkownicy z uprawnieniami administratora mają dodatkowo możliwość dodawania, edycji i usuwania wyników losowań.

**Główne funkcje:**
- Przeglądanie globalnej listy wyników losowań LOTTO (dostępne dla wszystkich zalogowanych użytkowników)
- Filtrowanie wyników po zakresie dat (Od/Do)
- Paginacja (20 wyników na stronę)
- Zarządzanie losowaniami (dodawanie/edycja/usuwanie) - tylko dla administratorów

## 2. Routing widoku

**Ścieżka:** `/draws`

**Typ dostępu:** Chroniony (wymaga autentykacji JWT)

**Redirect:**
- Niezalogowany użytkownik → `/login`
- Po zalogowaniu dostęp bezpośredni dla wszystkich użytkowników

## 3. Struktura komponentów

```
DrawsPage
├── Header Section
│   ├── Nagłówek h1 "Historia losowań"
│   └── Przycisk "+ Dodaj wynik" (conditional - tylko admin)
├── DrawsFilterPanel
│   ├── DatePicker (Od)
│   ├── DatePicker (Do)
│   ├── Button "Filtruj"
│   └── Button "Wyczyść" (conditional - gdy filtr aktywny)
├── FilterInfo (conditional - gdy filtr aktywny)
│   └── Text "Filtr aktywny: YYYY-MM-DD - YYYY-MM-DD"
├── DrawList
│   ├── DrawItem[] (lista losowań)
│   │   ├── Data losowania
│   │   ├── Wylosowane liczby (6 badges)
│   │   ├── Data wprowadzenia
│   │   └── Akcje (conditional - tylko admin): [Edytuj] [Usuń]
│   └── EmptyState (gdy brak wyników)
├── Pagination
│   ├── Button "« Poprzednia"
│   ├── Page Numbers (1 2 [3] 4 5)
│   ├── Button "Następna »"
│   └── Info "Strona X z Y (Z losowań)"
└── Modals
    ├── DrawFormModal (dodawanie/edycja - admin only)
    ├── DeleteConfirmationModal (usuwanie - admin only)
    ├── ErrorModal (błędy walidacji/API)
    └── Toast (komunikaty sukcesu)
```

## 4. Szczegóły komponentów

### 4.1 DrawsPage (główny komponent)

**Opis:** Główny kontener zarządzający stanem całego widoku, pobieraniem danych z API i obsługą interakcji użytkownika.

**Główne elementy:**
- Header z tytułem i przyciskiem dodawania (conditional)
- Panel filtrowania (DrawsFilterPanel)
- Info o aktywnym filtrze (conditional)
- Lista losowań (DrawList)
- Paginacja (Pagination)
- Modale (dodawanie/edycja/usuwanie/błędy)
- Toast notifications

**Obsługiwane interakcje:**
- Kliknięcie "+ Dodaj wynik" → otwiera DrawFormModal (mode: 'add')
- Zastosowanie filtra → odświeżenie listy z parametrami dateFrom/dateTo, reset do strony 1
- Wyczyszczenie filtra → odświeżenie listy bez parametrów, reset do strony 1
- Zmiana strony → odświeżenie listy z nowym numerem strony (zachowanie filtra)
- Kliknięcie [Edytuj] → otwiera DrawFormModal (mode: 'edit', pre-wypełnione dane)
- Kliknięcie [Usuń] → otwiera DeleteConfirmationModal
- Submit formularza → walidacja + API call + odświeżenie listy + Toast
- Błąd API → ErrorModal z listą błędów

**Obsługiwana walidacja:**
- Data "Od" nie może być późniejsza niż "Do" (inline validation)
- Data losowania nie może być w przyszłości (przy dodawaniu/edycji)
- 6 unikalnych liczb w zakresie 1-49 (przy dodawaniu/edycji)
- Limit Admin: tylko użytkownicy z flagą isAdmin widzą przyciski CRUD

**Typy:**
- `Draw` - DTO pojedynczego losowania
- `DrawsResponse` - DTO odpowiedzi API GET /api/draws
- `DrawFormData` - ViewModel formularza dodawania/edycji
- `FilterState` - Stan filtra (dateFrom, dateTo, isActive)
- `PaginationState` - Stan paginacji (currentPage, totalPages, pageSize, totalCount)
- `ModalState` - Stan modali (isOpen, type, data)

**Props:** Brak (główny komponent route)

---

### 4.2 DrawsFilterPanel

**Opis:** Panel filtrowania zawierający date range picker i przyciski akcji.

**Główne elementy:**
- 2x DatePicker (Od/Do)
- Button "Filtruj" (primary)
- Button "Wyczyść" (secondary, widoczny tylko gdy filtr aktywny)

**Obsługiwane interakcje:**
- Zmiana wartości w DatePicker → aktualizacja local state
- Kliknięcie "Filtruj" → walidacja inline (Od ≤ Do), wywołanie onFilter(dateFrom, dateTo)
- Kliknięcie "Wyczyść" → reset pól do pustych, wywołanie onClearFilter()

**Obsługiwana walidacja:**
- Inline: dateFrom nie może być późniejsza niż dateTo
- Komunikat błędu pod polami: "Data 'Od' musi być wcześniejsza lub równa 'Do'"
- Oba pola opcjonalne (domyślnie puste = brak filtra)

**Typy:**
- `FilterState` - { dateFrom?: string, dateTo?: string }

**Props:**
```typescript
interface DrawsFilterPanelProps {
  onFilter: (dateFrom?: string, dateTo?: string) => void;
  onClearFilter: () => void;
  isFilterActive: boolean;
  initialDateFrom?: string;
  initialDateTo?: string;
}
```

---

### 4.3 DrawList

**Opis:** Lista wszystkich losowań z obsługą empty state.

**Główne elementy:**
- Mapowanie draws[] na DrawItem componenty
- EmptyState gdy draws.length === 0

**Obsługiwane interakcje:**
- Przekazywanie funkcji onEdit/onDelete do DrawItem
- Conditional rendering akcji admin (isAdmin prop)

**Obsługiwana walidacja:** Brak (tylko display)

**Typy:**
- `Draw[]` - tablica losowań

**Props:**
```typescript
interface DrawListProps {
  draws: Draw[];
  isAdmin: boolean;
  onEdit: (drawId: number) => void;
  onDelete: (drawId: number) => void;
  loading: boolean;
}
```

---

### 4.4 DrawItem

**Opis:** Pojedyncze losowanie w liście (card/row).

**Główne elementy:**
- **Header section:**
  - Data losowania (format: YYYY-MM-DD)
  - Typ losowania ("LOTTO" lub "LOTTO PLUS") - wyświetlony jako kolorowy badge
  - ID systemu (drawSystemId) - wyświetlony jako "ID: 20250001" obok daty
  - Przyciski akcji (conditional - tylko admin): [Edytuj] [Usuń]
- **Sekcja liczb:**
  - 6 liczb wylosowanych (badges: bg-blue-100 text-blue-800, rounded-full, w-10 h-10)
- **Cena biletu** (jeśli dostępna):
  - Format: "Cena biletu: X.XX zł"
  - Wyświetlana pod liczbami
- **Sekcja wygranych** (jeśli dostępne dane):
  - **Ramka gradientowa** (bg-gradient-to-br from-gray-50 to-gray-100, rounded-xl, border-2 border-gray-300, padding 4)
  - **Nagłówek sekcji:** "Informacje o wygranych" z kolorowym paskiem gradientowym (w-1 h-6, bg-gradient-to-b from-green-500 to-orange-500)
  - **Grid 4 kart** (grid-cols-1 sm:grid-cols-2 lg:grid-cols-4, gap-3):
    - **Karta Stopień 1 (6 trafionych):**
      - Tło białe, border-2 border-green-300, shadow-sm
      - Kolorowa ikona z liczbą "6" (w-8 h-8, bg-green-500, rounded-full, text-white)
      - Label "trafionych" (text-xs font-semibold text-green-800)
      - Ilość wygranych (text-lg font-bold text-green-900)
      - Kwota (text-sm font-semibold text-green-800, format: X.XX zł)
    - **Karta Stopień 2 (5 trafionych):**
      - Tło białe, border-2 border-blue-300, shadow-sm
      - Kolorowa ikona "5" (bg-blue-500)
      - Style jak wyżej z kolorystyką niebieską
    - **Karta Stopień 3 (4 trafione):**
      - Tło białe, border-2 border-yellow-300, shadow-sm
      - Kolorowa ikona "4" (bg-yellow-500)
      - Style jak wyżej z kolorystyką żółtą
    - **Karta Stopień 4 (3 trafione):**
      - Tło białe, border-2 border-orange-300, shadow-sm
      - Kolorowa ikona "3" (bg-orange-500)
      - Style jak wyżej z kolorystyką pomarańczową

**Obsługiwane interakcje:**
- Kliknięcie [Edytuj] → wywołanie onEdit(draw.id)
- Kliknięcie [Usuń] → wywołanie onDelete(draw.id)

**Obsługiwana walidacja:** Brak (tylko display)

**Typy:**
- `Draw` - pojedyncze losowanie

**Props:**
```typescript
interface DrawItemProps {
  draw: Draw;
  isAdmin: boolean;
  onEdit: () => void;
  onDelete: () => void;
}
```

---

### 4.5 DrawFormModal

**Opis:** Modal do dodawania lub edycji wyniku losowania (tylko admin).

**Główne elementy:**
- Tytuł: "Dodaj wynik losowania" lub "Edytuj wynik losowania" (zależnie od mode)
- DatePicker: "Data losowania" (required, max: dzisiaj)
- Dropdown/RadioButtons: "Typ losowania" (LOTTO lub LOTTO PLUS)
- 6x NumberInput: "Liczba 1" do "Liczba 6" (required, min: 1, max: 49)
- NumberInput: "DrawSystemId" (required)
- NumberInput: "Cena biletu" (optional, min: 0, step: 0.01)
- **Sekcja "Wygrane 1. stopnia (6 trafionych)":**
  - NumberInput: "Ilość wygranych" (optional, min: 0) - winPoolCount1
  - NumberInput: "Kwota wygranej" (optional, min: 0, step: 0.01) - winPoolAmount1
- **Sekcja "Wygrane 2. stopnia (5 trafionych)":**
  - NumberInput: "Ilość wygranych" (optional, min: 0) - winPoolCount2
  - NumberInput: "Kwota wygranej" (optional, min: 0, step: 0.01) - winPoolAmount2
- **Sekcja "Wygrane 3. stopnia (4 trafione)":**
  - NumberInput: "Ilość wygranych" (optional, min: 0) - winPoolCount3
  - NumberInput: "Kwota wygranej" (optional, min: 0, step: 0.01) - winPoolAmount3
- **Sekcja "Wygrane 4. stopnia (3 trafione)":**
  - NumberInput: "Ilość wygranych" (optional, min: 0) - winPoolCount4
  - NumberInput: "Kwota wygranej" (optional, min: 0, step: 0.01) - winPoolAmount4
- Inline validation pod polami (czerwone komunikaty)
- Przyciski:
  - "Pobierz z XLotto" (secondary, left, **warunkowo wyświetlany przez Feature Flag**) - automatyczne pobieranie wyników z XLotto.pl
  - "Wyczyść" (secondary, left) - reset wszystkich pól
  - "Anuluj" (secondary, right) - zamknięcie bez zapisu
  - "Zapisz" (primary, right) - submit formularza

**Obsługiwane interakcje:**
- **Sprawdzenie Feature Flag przy otwarciu modalu:**
  - API call: `GET /api/xlotto/is-enabled`
  - Ustawienie state `xLottoEnabled` na podstawie odpowiedzi `response.data`
  - Conditional rendering przycisku "Pobierz z XLotto"
- Zmiana wartości w polach → inline validation w czasie rzeczywistym
- Kliknięcie "Pobierz z XLotto" (jeśli widoczny) → automatyczne pobieranie wyników z XLotto.pl:
  - Walidacja: data losowania i typ muszą być wybrane
  - API call GET /api/xlotto/actual-draws?Date={drawDate}&LottoType={lottoType}
  - Przetwarzanie odpowiedzi JSON i automatyczne wypełnienie 6 pól z liczbami
  - Loading state podczas pobierania (disabled przyciski, spinner)
  - Obsługa błędów: alert z komunikatem błędu
- Kliknięcie "Wyczyść" → reset formularza do wartości początkowych (empty dla add, initial dla edit)
- Kliknięcie "Anuluj" → zamknięcie modalu (onCancel)
- Kliknięcie "Zapisz" → zbieranie błędów walidacji:
  - Jeśli błędy inline: wyświetlenie wszystkich w ErrorModal
  - Jeśli valid: API call POST/PUT /api/draws → Toast sukcesu + odświeżenie listy + zamknięcie modalu
  - Jeśli błąd API: ErrorModal z komunikatami z backendu
- Escape key → zamknięcie modalu (jak Anuluj)

**Obsługiwana walidacja:**
- **Data losowania:**
  - Wymagana
  - Nie może być w przyszłości (date ≤ dzisiaj)
  - Komunikat: "Data losowania nie może być w przyszłości"
- **Liczby (każde pole):**
  - Wymagane (wszystkie 6 pól)
  - Zakres 1-49
  - Komunikat: "Liczba musi być w zakresie 1-49"
- **Unikalność liczb:**
  - Wszystkie 6 liczb muszą być unikalne w zestawie
  - Komunikat: "Liczby muszą być unikalne"
- **Walidacja API (backend):**
  - Jeśli losowanie na daną datę już istnieje → nadpisanie (UPDATE) bez błędu (decyzja biznesowa z PRD)

**Typy:**
- `DrawFormData` - { drawDate: string, numbers: (number | '')[] }
- `DrawFormErrors` - { drawDate?: string, numbers?: string[] }

**Props:**
```typescript
interface DrawFormModalProps {
  isOpen: boolean;
  mode: 'add' | 'edit';
  initialValues?: DrawFormData;
  onSubmit: (data: DrawFormData) => Promise<void>;
  onCancel: () => void;
}
```

---

### 4.6 DeleteConfirmationModal

**Opis:** Modal potwierdzenia usunięcia losowania (tylko admin).

**Główne elementy:**
- Tytuł: "Usuń wynik losowania"
- Treść:
  ```
  Czy na pewno chcesz usunąć wynik losowania z dnia {drawDate}?
  [{number1}, {number2}, {number3}, {number4}, {number5}, {number6}]
  ```
- Przyciski:
  - "Anuluj" (secondary, focus default) - zamknięcie bez akcji
  - "Usuń" (danger, czerwony) - potwierdzenie i usunięcie

**Obsługiwane interakcje:**
- Kliknięcie "Anuluj" → zamknięcie modalu (onCancel)
- Kliknięcie "Usuń" → API call DELETE /api/draws/{id}:
  - Sukces: Toast "Wynik usunięty pomyślnie" + odświeżenie listy + zamknięcie modalu
  - Błąd: ErrorModal z komunikatem błędu API
- Escape key → zamknięcie modalu (jak Anuluj)

**Obsługiwana walidacja:**
- Backend sprawdza uprawnienia IsAdmin (403 Forbidden jeśli nie admin)
- 404 jeśli losowanie nie istnieje

**Typy:**
- `Draw` - dane losowania do wyświetlenia w komunikacie

**Props:**
```typescript
interface DeleteConfirmationModalProps {
  isOpen: boolean;
  draw: Draw | null;
  onConfirm: (drawId: number) => Promise<void>;
  onCancel: () => void;
}
```

---

### 4.7 Pagination

**Opis:** Kontrolki paginacji dla nawigacji między stronami listy.

**Główne elementy:**
- Button "« Poprzednia" (disabled jeśli currentPage === 1)
- Page numbers: max 5 widocznych (centered wokół currentPage)
  - Current page: highlighted (bg-blue-600 text-white)
  - Other pages: clickable (hover:bg-gray-100)
- Button "Następna »" (disabled jeśli currentPage === totalPages)
- Info text: "Strona {currentPage} z {totalPages} ({totalCount} losowań)"

**Obsługiwane interakcje:**
- Kliknięcie "« Poprzednia" → onPageChange(currentPage - 1)
- Kliknięcie page number → onPageChange(clickedPage)
- Kliknięcie "Następna »" → onPageChange(currentPage + 1)

**Obsługiwana walidacja:** Brak (disabled state na buttonach)

**Typy:**
- `PaginationProps` - { currentPage, totalPages, totalCount, onPageChange }

**Props:**
```typescript
interface PaginationProps {
  currentPage: number;
  totalPages: number;
  totalCount: number;
  onPageChange: (page: number) => void;
}
```

---

## 5. Typy

### 5.1 DTO z API (contracts/draw.ts)

```typescript
// Response z GET /api/draws
export interface DrawsResponse {
  draws: Draw[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  filters?: {
    dateFrom?: string;
    dateTo?: string;
  };
}

// Pojedyncze losowanie
export interface Draw {
  id: number;
  drawDate: string; // format: YYYY-MM-DD (DateTime from backend)
  lottoType: string; // "LOTTO" lub "LOTTO PLUS"
  numbers: number[]; // 6 liczb 1-49
  drawSystemId: number; // identyfikator systemu losowania (required)
  ticketPrice: number | null; // opcjonalna cena biletu
  winPoolCount1: number | null; // liczba wygranych w 1. stopniu (6 trafionych)
  winPoolAmount1: number | null; // kwota wygranej w 1. stopniu
  winPoolCount2: number | null; // liczba wygranych w 2. stopniu (5 trafionych)
  winPoolAmount2: number | null; // kwota wygranej w 2. stopniu
  winPoolCount3: number | null; // liczba wygranych w 3. stopniu (4 trafione)
  winPoolAmount3: number | null; // kwota wygranej w 3. stopniu
  winPoolCount4: number | null; // liczba wygranych w 4. stopniu (3 trafione)
  winPoolAmount4: number | null; // kwota wygranej w 4. stopniu
  createdAt: string; // ISO datetime
}

// Request dla POST /api/draws
export interface CreateDrawRequest {
  drawDate: string; // YYYY-MM-DD
  lottoType: string; // "LOTTO" lub "LOTTO PLUS"
  numbers: number[]; // 6 liczb
  drawSystemId: number; // identyfikator systemu (required)
  ticketPrice?: number; // opcjonalne
  winPoolCount1?: number; // opcjonalne
  winPoolAmount1?: number; // opcjonalne
  winPoolCount2?: number; // opcjonalne
  winPoolAmount2?: number; // opcjonalne
  winPoolCount3?: number; // opcjonalne
  winPoolAmount3?: number; // opcjonalne
  winPoolCount4?: number; // opcjonalne
  winPoolAmount4?: number; // opcjonalne
}

// Request dla PUT /api/draws/{id}
export interface UpdateDrawRequest {
  drawDate: string;
  lottoType: string; // "LOTTO" lub "LOTTO PLUS"
  numbers: number[];
  drawSystemId: number; // identyfikator systemu (required)
  ticketPrice?: number; // opcjonalne
  winPoolCount1?: number; // opcjonalne
  winPoolAmount1?: number; // opcjonalne
  winPoolCount2?: number; // opcjonalne
  winPoolAmount2?: number; // opcjonalne
  winPoolCount3?: number; // opcjonalne
  winPoolAmount3?: number; // opcjonalne
  winPoolCount4?: number; // opcjonalne
  winPoolAmount4?: number; // opcjonalne
}

// Response dla POST/PUT/DELETE (sukces)
export interface DrawActionResponse {
  message: string;
}

// Response dla błędów walidacji (400)
export interface DrawValidationError {
  errors: {
    drawDate?: string[];
    numbers?: string[];
  };
}
```

---

### 5.2 ViewModel dla komponentów (types/draws.ts)

```typescript
// Stan filtra
export interface FilterState {
  dateFrom: string; // YYYY-MM-DD lub pusty string
  dateTo: string; // YYYY-MM-DD lub pusty string
  isActive: boolean; // true jeśli jakikolwiek filtr zastosowany
}

// Stan paginacji
export interface PaginationState {
  currentPage: number;
  totalPages: number;
  pageSize: number;
  totalCount: number;
}

// Dane formularza dodawania/edycji
export interface DrawFormData {
  drawDate: string; // YYYY-MM-DD
  lottoType: string; // "LOTTO" lub "LOTTO PLUS"
  numbers: (number | '')[]; // 6 elementów, '' oznacza puste pole
  drawSystemId: number | ''; // wymagane w backend, '' oznacza puste pole w UI
  ticketPrice?: number | ''; // opcjonalne, '' oznacza puste pole
  winPoolCount1?: number | ''; // opcjonalne
  winPoolAmount1?: number | ''; // opcjonalne
  winPoolCount2?: number | ''; // opcjonalne
  winPoolAmount2?: number | ''; // opcjonalne
  winPoolCount3?: number | ''; // opcjonalne
  winPoolAmount3?: number | ''; // opcjonalne
  winPoolCount4?: number | ''; // opcjonalne
  winPoolAmount4?: number | ''; // opcjonalne
}

// Błędy walidacji formularza
export interface DrawFormErrors {
  drawDate?: string;
  numbers?: string[]; // 6 elementów, jeden error per pole
}

// Stan modali
export interface ModalState {
  isFormOpen: boolean;
  formMode: 'add' | 'edit';
  editingDraw: Draw | null;
  isDeleteOpen: boolean;
  deletingDraw: Draw | null;
  isErrorOpen: boolean;
  errorMessages: string[];
}

// Parametry zapytania API
export interface DrawsQueryParams {
  page: number;
  pageSize: number;
  sortBy: 'drawDate' | 'createdAt';
  sortOrder: 'asc' | 'desc';
  dateFrom?: string; // YYYY-MM-DD
  dateTo?: string; // YYYY-MM-DD
}
```

---

## 6. Zarządzanie stanem

### 6.1 Local state w DrawsPage

Stan jest zarządzany lokalnie w komponencie DrawsPage za pomocą useState hooks. **Nie jest wymagany custom hook** - logika jest wystarczająco prosta do zarządzania inline.

**Główne zmienne stanu:**

```typescript
// Dane listy losowań
const [draws, setDraws] = useState<Draw[]>([]);
const [loading, setLoading] = useState<boolean>(false);

// Paginacja
const [paginationState, setPaginationState] = useState<PaginationState>({
  currentPage: 1,
  totalPages: 1,
  pageSize: 20, // stała wartość w MVP
  totalCount: 0
});

// Filtr
const [filterState, setFilterState] = useState<FilterState>({
  dateFrom: '',
  dateTo: '',
  isActive: false
});

// Modale
const [modalState, setModalState] = useState<ModalState>({
  isFormOpen: false,
  formMode: 'add',
  editingDraw: null,
  isDeleteOpen: false,
  deletingDraw: null,
  isErrorOpen: false,
  errorMessages: []
});

// Toast
const [toastMessage, setToastMessage] = useState<string | null>(null);

// User context (z AppContext)
const { user, isLoggedIn, getApiService } = useAppContext();
const apiService = getApiService();
const isAdmin = user?.isAdmin || false;
```

---

### 6.2 Funkcje pomocnicze w DrawsPage

```typescript
// Pobieranie listy losowań z API
const fetchDraws = async (page: number = paginationState.currentPage) => {
  setLoading(true);
  try {
    const params: DrawsQueryParams = {
      page,
      pageSize: 20,
      sortBy: 'drawDate',
      sortOrder: 'desc',
      ...(filterState.isActive && {
        dateFrom: filterState.dateFrom || undefined,
        dateTo: filterState.dateTo || undefined
      })
    };

    const response = await apiService.getDraws(params);

    setDraws(response.draws);
    setPaginationState({
      currentPage: response.page,
      totalPages: response.totalPages,
      pageSize: response.pageSize,
      totalCount: response.totalCount
    });
  } catch (error) {
    handleApiError(error, 'Nie udało się pobrać listy losowań');
  } finally {
    setLoading(false);
  }
};

// Zastosowanie filtra
const handleFilter = (dateFrom?: string, dateTo?: string) => {
  // Walidacja inline
  if (dateFrom && dateTo && dateFrom > dateTo) {
    setModalState(prev => ({
      ...prev,
      isErrorOpen: true,
      errorMessages: ["Data 'Od' musi być wcześniejsza lub równa 'Do'"]
    }));
    return;
  }

  setFilterState({
    dateFrom: dateFrom || '',
    dateTo: dateTo || '',
    isActive: !!(dateFrom || dateTo)
  });

  // Reset do strony 1 i odświeżenie
  fetchDraws(1);
};

// Wyczyszczenie filtra
const handleClearFilter = () => {
  setFilterState({
    dateFrom: '',
    dateTo: '',
    isActive: false
  });

  // Odświeżenie bez filtra, strona 1
  fetchDraws(1);
};

// Zmiana strony
const handlePageChange = (page: number) => {
  fetchDraws(page);
};

// Otworzenie modalu dodawania
const handleOpenAddModal = () => {
  setModalState(prev => ({
    ...prev,
    isFormOpen: true,
    formMode: 'add',
    editingDraw: null
  }));
};

// Otworzenie modalu edycji
const handleOpenEditModal = (drawId: number) => {
  const draw = draws.find(d => d.id === drawId);
  if (!draw) return;

  setModalState(prev => ({
    ...prev,
    isFormOpen: true,
    formMode: 'edit',
    editingDraw: draw
  }));
};

// Otworzenie modalu usuwania
const handleOpenDeleteModal = (drawId: number) => {
  const draw = draws.find(d => d.id === drawId);
  if (!draw) return;

  setModalState(prev => ({
    ...prev,
    isDeleteOpen: true,
    deletingDraw: draw
  }));
};

// Submit formularza (dodawanie/edycja)
const handleFormSubmit = async (data: DrawFormData) => {
  try {
    if (modalState.formMode === 'add') {
      await apiService.createDraw(data);
      setToastMessage('Wynik losowania zapisany pomyślnie');
    } else {
      await apiService.updateDraw(modalState.editingDraw!.id, data);
      setToastMessage('Wynik zaktualizowany pomyślnie');
    }

    // Zamknięcie modalu
    setModalState(prev => ({
      ...prev,
      isFormOpen: false,
      editingDraw: null
    }));

    // Odświeżenie aktualnej strony
    fetchDraws();
  } catch (error) {
    handleApiError(error, 'Nie udało się zapisać wyniku losowania');
  }
};

// Potwierdzenie usunięcia
const handleDeleteConfirm = async (drawId: number) => {
  try {
    await apiService.deleteDraw(drawId);
    setToastMessage('Wynik usunięty pomyślnie');

    // Zamknięcie modalu
    setModalState(prev => ({
      ...prev,
      isDeleteOpen: false,
      deletingDraw: null
    }));

    // Odświeżenie aktualnej strony (lub poprzedniej jeśli usunięto ostatni element)
    const shouldGoBack = draws.length === 1 && paginationState.currentPage > 1;
    fetchDraws(shouldGoBack ? paginationState.currentPage - 1 : undefined);
  } catch (error) {
    handleApiError(error, 'Nie udało się usunąć wyniku losowania');
  }
};

// Obsługa błędów API
const handleApiError = (error: any, defaultMessage: string) => {
  let errorMessages: string[] = [];

  if (error.status === 403) {
    errorMessages = ['Nie masz uprawnień do wykonania tej operacji'];
  } else if (error.status === 404) {
    errorMessages = ['Wynik losowania nie istnieje'];
  } else if (error.status >= 400 && error.status < 500 && error.data?.errors) {
    // Flattening błędów walidacji z backendu
    errorMessages = Object.values(error.data.errors).flat() as string[];
  } else {
    errorMessages = [defaultMessage];
  }

  setModalState(prev => ({
    ...prev,
    isErrorOpen: true,
    errorMessages
  }));
};
```

---

### 6.3 useEffect dla initial load

```typescript
useEffect(() => {
  // Pobranie listy losowań przy pierwszym mount
  fetchDraws(1);
}, []); // Empty dependency array - run once
```

---

## 7. Integracja API

### 7.1 Metody ApiService (api-service.ts)

```typescript
export class ApiService {
  // ... existing methods

  /**
   * GET /api/draws - Pobieranie listy losowań z paginacją i filtrowaniem
   */
  async getDraws(params: DrawsQueryParams): Promise<DrawsResponse> {
    const queryString = new URLSearchParams({
      page: params.page.toString(),
      pageSize: params.pageSize.toString(),
      sortBy: params.sortBy,
      sortOrder: params.sortOrder,
      ...(params.dateFrom && { dateFrom: params.dateFrom }),
      ...(params.dateTo && { dateTo: params.dateTo })
    }).toString();

    const response = await this.request(`/api/draws?${queryString}`, {
      method: 'GET'
    });

    return response.json();
  }

  /**
   * POST /api/draws - Dodanie nowego wyniku losowania (admin only)
   */
  async createDraw(data: CreateDrawRequest): Promise<DrawActionResponse> {
    const response = await this.request('/api/draws', {
      method: 'POST',
      body: JSON.stringify(data)
    });

    return response.json();
  }

  /**
   * PUT /api/draws/{id} - Edycja wyniku losowania (admin only)
   */
  async updateDraw(id: number, data: UpdateDrawRequest): Promise<DrawActionResponse> {
    const response = await this.request(`/api/draws/${id}`, {
      method: 'PUT',
      body: JSON.stringify(data)
    });

    return response.json();
  }

  /**
   * DELETE /api/draws/{id} - Usunięcie wyniku losowania (admin only)
   */
  async deleteDraw(id: number): Promise<DrawActionResponse> {
    const response = await this.request(`/api/draws/${id}`, {
      method: 'DELETE'
    });

    return response.json();
  }

  /**
   * GET /api/xlotto/is-enabled - Sprawdzenie Feature Flag dla XLotto
   */
  async xLottoIsEnabled(): Promise<XLottoIsEnabledResponse> {
    const url = `${this.apiUrl}/api/xlotto/is-enabled`;
    const response = await fetch(url, {
      method: 'GET',
      headers: this.getHeaders()
    });

    if (!response.ok) {
      if (response.status === 401) {
        throw new Error('Brak autoryzacji. Wymagany token JWT.');
      }
      throw new Error(`Error checking XLotto feature status: ${response.statusText}`);
    }

    const result: XLottoIsEnabledResponse = await response.json();
    return result;
  }

  /**
   * GET /api/xlotto/actual-draws - Pobieranie wyników z XLotto.pl przez Gemini API
   */
  async xLottoActualDraws(request: XLottoActualDrawsRequest): Promise<XLottoActualDrawsResponse> {
    const params = new URLSearchParams();
    params.append('Date', request.date);
    params.append('LottoType', request.lottoType);

    const url = `${this.apiUrl}/api/xlotto/actual-draws?${params.toString()}`;
    const response = await fetch(url, {
      method: 'GET',
      headers: this.getHeaders()
    });

    if (!response.ok) {
      if (response.status === 401) {
        throw new Error('Brak autoryzacji. Wymagany token JWT.');
      }
      if (response.status === 400) {
        const errorData = await response.json();
        if (errorData.errors) {
          const errorMessages = Object.entries(errorData.errors)
            .map(([field, messages]) => {
              const msgArray = messages as string[];
              return `${field}: ${msgArray.join(', ')}`;
            })
            .join('; ');
          throw new Error(errorMessages);
        }
        throw new Error('Błąd walidacji danych wejściowych');
      }
      if (response.status === 500) {
        const errorData = await response.json();
        throw new Error(errorData.detail || 'Błąd serwera podczas pobierania wyników z XLotto');
      }
      throw new Error(`Error fetching XLotto draws: ${response.statusText}`);
    }

    const result: XLottoActualDrawsResponse = await response.json();
    return result;
  }

  // ... private request method with error handling
}
```

---

### 7.2 Typy żądań i odpowiedzi

**Typy zdefiniowane w sekcji 5.1** - wszystkie typy są zgodne z API Plan (api-plan.md).

**Kluczowe endpointy:**
- `GET /api/draws?page=1&pageSize=20&sortBy=drawDate&sortOrder=desc&dateFrom=YYYY-MM-DD&dateTo=YYYY-MM-DD`
- `POST /api/draws` - Body: `{ drawDate, numbers }`
- `PUT /api/draws/{id}` - Body: `{ drawDate, numbers }`
- `DELETE /api/draws/{id}` - Brak body

**Headers (automatycznie dodawane przez ApiService):**
- `Content-Type: application/json`
- `Authorization: Bearer {token}` (JWT z AppContext)
- `X-TOKEN: {appToken}` (z .env)

---

## 8. Interakcje użytkownika

### 8.1 Przepływ: Przeglądanie listy losowań

1. Użytkownik wchodzi na `/draws`
2. Komponent DrawsPage wywołuje `fetchDraws(1)` w useEffect
3. Loading spinner pojawia się w obszarze listy
4. API call: `GET /api/draws?page=1&pageSize=20&sortBy=drawDate&sortOrder=desc`
5. Backend zwraca `DrawsResponse` z listą losowań (sorted by drawDate desc)
6. Lista renderowana przez DrawList → DrawItem[]
7. Pagination renderowana na dole z info o aktualnej stronie
8. Użytkownik widzi listę losowań (data, 6 liczb, data wprowadzenia)

---

### 8.2 Przepływ: Filtrowanie po zakresie dat

1. Użytkownik wypełnia date range picker:
   - DatePicker "Od": 2025-10-01
   - DatePicker "Do": 2025-10-31
2. Kliknięcie "Filtruj"
3. Inline validation: dateFrom ≤ dateTo
   - Jeśli valid: wywołanie `handleFilter(dateFrom, dateTo)`
   - Jeśli invalid: ErrorModal "Data 'Od' musi być wcześniejsza lub równa 'Do'"
4. Stan filtra ustawiany na aktywny: `{ dateFrom, dateTo, isActive: true }`
5. API call: `GET /api/draws?page=1&pageSize=20&dateFrom=2025-10-01&dateTo=2025-10-31`
6. Lista odświeżana z wynikami tylko z października 2025
7. Info nad listą: "Filtr aktywny: 2025-10-01 - 2025-10-31"
8. Przycisk "Wyczyść" pojawia się obok "Filtruj"
9. Użytkownik może kliknąć "Wyczyść" aby wrócić do pełnej listy (wywołanie `handleClearFilter()`)

---

### 8.3 Przepływ: Paginacja

1. Użytkownik widzi paginację na dole listy: "Strona 1 z 12 (1156 losowań)"
2. Kliknięcie page number "2"
3. Wywołanie `handlePageChange(2)`
4. API call: `GET /api/draws?page=2&pageSize=20` (zachowanie filtra jeśli aktywny)
5. Lista odświeżana z wynikami strony 2
6. Pagination aktualizowana: current page = 2, highlighted

**Responsywność:**
- Desktop: full pagination (Previous + 5 page numbers + Next)
- Mobile: simplified (tylko Previous/Next + text "Strona 2 z 12")

---

### 8.4 Przepływ: Admin dodaje wynik losowania

**Warunek:** Użytkownik z flagą `isAdmin === true`

1. Admin widzi przycisk "+ Dodaj wynik" w header (conditional rendering)
2. Kliknięcie "+ Dodaj wynik"
3. Wywołanie `handleOpenAddModal()`
4. DrawFormModal otwiera się (mode: 'add')
5. Admin wypełnia formularz:
   - DatePicker: 2025-11-09
   - Dropdown: "LOTTO"
   - 6x NumberInput: 1, 7, 14, 21, 35, 42
   - DrawSystemId: 20250001
   - (opcjonalnie) TicketPrice: 3.00
   - (opcjonalnie) WinPoolCount1: 0, WinPoolAmount1: 0
6. Inline validation w czasie rzeczywistym (zielone checki przy poprawnych polach)
7. Kliknięcie "Zapisz"
8. Zbieranie błędów walidacji:
   - Jeśli błędy inline: ErrorModal z listą błędów
   - Jeśli valid: API call `POST /api/draws` → body: `{ drawDate: "2025-11-09", lottoType: "LOTTO", numbers: [1, 7, 14, 21, 35, 42], drawSystemId: 20250001, ticketPrice: 3.00, winPoolCount1: 0, winPoolAmount1: 0 }`
9. Backend waliduje i zapisuje (lub nadpisuje jeśli data już istnieje)
10. Response 201 Created: `{ message: "Losowanie utworzone pomyślnie" }`
11. Modal zamyka się
12. Toast "Wynik losowania zapisany pomyślnie" (zielony, auto-dismiss 3-4s)
13. Lista odświeża się (`fetchDraws()` - aktualna strona)
14. Nowy wynik widoczny na górze listy (sorted by drawDate desc)

---

### 8.5 Przepływ: Admin edytuje wynik losowania

1. Admin klika [Edytuj] przy losowaniu z 2025-11-08
2. Wywołanie `handleOpenEditModal(drawId)`
3. DrawFormModal otwiera się (mode: 'edit', pola pre-wypełnione z wszystkimi danymi)
4. Admin zmienia jedną liczbę: 44 → 49
5. Inline validation (wszystkie pola valid)
6. Kliknięcie "Zapisz"
7. API call `PUT /api/draws/{id}` → body: `{ drawDate: "2025-11-08", lottoType: "LOTTO", numbers: [5, 12, 18, 25, 37, 49], drawSystemId: 20250001, ticketPrice: 3.00, ... }`
8. Backend waliduje i aktualizuje (transakcja: UPDATE Draws + DELETE/INSERT DrawNumbers)
9. Response 200 OK: `{ message: "Losowanie zaktualizowane pomyślnie" }`
10. Modal zamyka się
11. Toast "Wynik zaktualizowany pomyślnie"
12. Lista odświeża się
13. Zaktualizowane liczby widoczne w liście

---

### 8.6 Przepływ: Admin usuwa wynik losowania

1. Admin klika [Usuń] przy losowaniu z 2025-11-01
2. Wywołanie `handleOpenDeleteModal(drawId)`
3. DeleteConfirmationModal otwiera się:
   ```
   Czy na pewno chcesz usunąć wynik losowania z dnia 2025-11-01?
   [3, 9, 15, 22, 31, 48]
   ```
4. Admin klika [Usuń] (danger button, czerwony)
5. API call `DELETE /api/draws/{id}`
6. Backend sprawdza uprawnienia IsAdmin + usuwa (CASCADE DELETE → DrawNumbers)
7. Response 200 OK: `{ message: "Losowanie usunięte pomyślnie" }`
8. Modal zamyka się
9. Toast "Wynik usunięty pomyślnie"
10. Lista odświeża się:
    - Jeśli usunięto ostatni element na stronie i currentPage > 1: redirect do poprzedniej strony
    - W przeciwnym razie: odświeżenie aktualnej strony
11. Losowanie zniknęło z listy

---

### 8.7 Przepływ: Admin pobiera wyniki z XLotto

**Warunki:**
- Użytkownik z flagą `isAdmin === true`
- **Feature Flag `GoogleGemini:Enable = true` w konfiguracji backend**

1. Admin otwiera DrawFormModal (dodawanie lub edycja)
2. **Frontend automatycznie sprawdza Feature Flag:**
   - API call: `GET /api/xlotto/is-enabled`
   - Jeśli `response.data === true`: przycisk "Pobierz z XLotto" jest widoczny
   - Jeśli `response.data === false`: przycisk jest ukryty
3. Admin wybiera datę losowania: 2025-11-04
4. Admin wybiera typ losowania: "LOTTO" (dropdown lub radio buttons)
5. Kliknięcie "Pobierz z XLotto" (jeśli widoczny)
6. Walidacja inline: czy data i typ są wybrane
   - Jeśli brak daty: alert "Proszę najpierw wybrać datę losowania"
   - Jeśli data i typ wybrane: kontynuacja
7. Loading state: przyciski disabled, spinner przy przycisku "Pobierz z XLotto"
8. API call: `GET /api/xlotto/actual-draws?Date=2025-11-04&LottoType=LOTTO`
9. Backend:
   - Sprawdza Feature Flag `GoogleGemini:Enable`
   - Jeśli `Enable = false`: zwraca pustą tablicę `{"Data":[]}`
   - Jeśli `Enable = true`:
     - Pobiera HTML ze strony XLotto.pl
     - Przetwarza przez Google Gemini API
     - Zwraca JSON: `{ success: true, data: "{\"Data\": [...]}" }`
10. Frontend parsuje odpowiedź:
   - Deserializacja JSON string z pola `data`
   - Wyszukanie obiektu DrawItem dla wybranego GameType
   - Ekstrakcja tablicy Numbers
11. Automatyczne wypełnienie 6 pól NumberInput wylosowanymi liczbami
12. Loading state zakończony
13. Admin może:
    - Zmodyfikować liczby ręcznie (jeśli potrzebne)
    - Kliknąć "Zapisz" aby zapisać wynik
    - Kliknąć "Wyczyść" aby zresetować formularz
14. Obsługa błędów:
    - 401: Alert "Brak autoryzacji. Wymagany token JWT."
    - 400: Alert z komunikatami walidacji z backendu
    - 500: Alert "Nie udało się pobrać wyników z XLotto lub Gemini API"
    - Network error: Alert "Błąd podczas pobierania danych z XLotto"

**Uwaga:** Funkcjonalność działa dla obu typów losowania: LOTTO i LOTTO PLUS. Backend zwraca wyniki dla obu typów, ale frontend automatycznie wybiera właściwy na podstawie formData.lottoType.

---

## 9. Warunki i walidacja

### 9.1 Walidacja inline (frontend)

**DrawsFilterPanel:**
- **dateFrom ≤ dateTo**
  - Komponent: DrawsFilterPanel
  - Warunek: Jeśli oba pola wypełnione, dateFrom nie może być późniejsza niż dateTo
  - Komunikat: "Data 'Od' musi być wcześniejsza lub równa 'Do'" (czerwony tekst pod polami)
  - Wpływ na UI: Przycisk "Filtruj" disabled jeśli warunek niespełniony (opcjonalnie) lub ErrorModal przy submit

**DrawFormModal:**
- **drawDate wymagana**
  - Komponent: DatePicker w DrawFormModal
  - Warunek: Pole nie może być puste
  - Komunikat: "Data losowania jest wymagana" (czerwony tekst pod polem)
  - Wpływ na UI: Border czerwony, error text pod polem

- **drawDate ≤ dzisiaj**
  - Komponent: DatePicker w DrawFormModal
  - Warunek: Data nie może być w przyszłości
  - Komunikat: "Data losowania nie może być w przyszłości"
  - Wpływ na UI: Border czerwony, error text pod polem

- **numbers[i] wymagane (wszystkie 6 pól)**
  - Komponent: NumberInput w DrawFormModal
  - Warunek: Każde pole musi być wypełnione
  - Komunikat: "To pole jest wymagane"
  - Wpływ na UI: Border czerwony, error text pod polem

- **numbers[i] w zakresie 1-49**
  - Komponent: NumberInput w DrawFormModal
  - Warunek: Wartość >= 1 && <= 49
  - Komunikat: "Liczba musi być w zakresie 1-49"
  - Wpływ na UI: Border czerwony, error text pod polem

- **numbers[] unikalne (wszystkie 6 liczb)**
  - Komponent: DrawFormModal (validation logic)
  - Warunek: Żadne dwie liczby nie mogą być identyczne
  - Komunikat: "Liczby muszą być unikalne"
  - Wpływ na UI: Duplikaty podświetlone czerwonym borderem, error text pod duplikatami

---

### 9.2 Walidacja API (backend)

**POST /api/draws:**
- **drawDate unikalny globalnie (nadpisanie)**
  - Logika backend: Jeśli losowanie na daną datę już istnieje → UPDATE zamiast INSERT (decyzja PRD: nadpisanie bez błędu)
  - Komunikat: Brak błędu, tylko UPDATE wykonany
  - Wpływ na UI: Toast "Wynik losowania zapisany pomyślnie" (bez rozróżnienia create/update)

**PUT /api/draws/{id}:**
- **Walidacja uprawnień IsAdmin**
  - Logika backend: Sprawdzenie flagi IsAdmin z JWT claims
  - Błąd: 403 Forbidden jeśli IsAdmin === false
  - Komunikat: "Nie masz uprawnień do wykonania tej operacji"
  - Wpływ na UI: ErrorModal z komunikatem, modal formularza pozostaje otwarty

- **Walidacja istnienia zasobu**
  - Logika backend: Sprawdzenie czy Draw o podanym ID istnieje
  - Błąd: 404 Not Found
  - Komunikat: "Wynik losowania nie istnieje"
  - Wpływ na UI: ErrorModal z komunikatem, modal zamyka się

**DELETE /api/draws/{id}:**
- **Walidacja uprawnień IsAdmin** (jak PUT)
- **Walidacja istnienia zasobu** (jak PUT)

---

### 9.3 Warunki UI (conditional rendering)

**Przycisk "+ Dodaj wynik" (header):**
- **Warunek:** `user.isAdmin === true`
- **Logika:** `{isAdmin && <Button onClick={handleOpenAddModal}>+ Dodaj wynik</Button>}`
- **Wpływ:** Przycisk widoczny tylko dla administratorów

**Przyciski [Edytuj] [Usuń] (DrawItem):**
- **Warunek:** `user.isAdmin === true`
- **Logika:** `{isAdmin && (<><Button onClick={onEdit}>Edytuj</Button><Button onClick={onDelete}>Usuń</Button></>)}`
- **Wpływ:** Przyciski widoczne tylko dla administratorów

**Info o aktywnym filtrze (FilterInfo):**
- **Warunek:** `filterState.isActive === true`
- **Logika:** `{filterState.isActive && <p>Filtr aktywny: {filterState.dateFrom} - {filterState.dateTo}</p>}`
- **Wpływ:** Info widoczne tylko gdy filtr zastosowany

**Przycisk "Wyczyść" (DrawsFilterPanel):**
- **Warunek:** `isFilterActive === true`
- **Logika:** `{isFilterActive && <Button onClick={onClearFilter}>Wyczyść</Button>}`
- **Wpływ:** Przycisk widoczny tylko gdy filtr aktywny

**Przycisk "Pobierz z XLotto" (DrawFormModal):**
- **Warunek:** `xLottoEnabled === true` (Feature Flag z backendu)
- **Logika:**
  ```tsx
  const [xLottoEnabled, setXLottoEnabled] = useState(false);

  useEffect(() => {
    if (isOpen) {
      const checkXLottoEnabled = async () => {
        try {
          const apiService = getApiService();
          const response = await apiService.xLottoIsEnabled();
          setXLottoEnabled(response.data);
        } catch (error) {
          setXLottoEnabled(false);
        }
      };
      checkXLottoEnabled();
    }
  }, [isOpen]);

  {xLottoEnabled && (
    <Button variant="secondary" onClick={handleLoadFromXLotto}>
      Pobierz z XLotto
    </Button>
  )}
  ```
- **Wpływ:** Przycisk widoczny tylko gdy funkcja XLotto jest włączona w konfiguracji backend (`GoogleGemini:Enable = true`)

**EmptyState (DrawList):**
- **Warunek:** `draws.length === 0 && !loading`
- **Logika:** `{draws.length === 0 && !loading && <EmptyState />}`
- **Wpływ:** EmptyState widoczny tylko gdy brak wyników i nie trwa ładowanie

**Loading Spinner (DrawList):**
- **Warunek:** `loading === true`
- **Logika:** `{loading && <Spinner />}`
- **Wpływ:** Spinner widoczny tylko podczas ładowania danych

---

## 10. Obsługa błędów

### 10.1 Błędy walidacji inline (frontend)

**Scenariusz:** Użytkownik wprowadza nieprawidłowe dane w formularzu (DrawFormModal)

**Obsługa:**
1. Inline validation w czasie rzeczywistym (onChange dla każdego pola)
2. Błędy wyświetlane pod polami (czerwony tekst + border)
3. Przy kliknięciu "Zapisz": zbieranie wszystkich błędów inline
4. Jeśli błędy istnieją: wyświetlenie ErrorModal z listą wszystkich błędów
5. Formularz pozostaje otwarty, użytkownik może poprawić dane

**Przykład komunikatów:**
- "Data losowania jest wymagana"
- "Data losowania nie może być w przyszłości"
- "To pole jest wymagane"
- "Liczba musi być w zakresie 1-49"
- "Liczby muszą być unikalne"

---

### 10.2 Błędy API (400 Bad Request)

**Scenariusz:** Backend zwraca błąd walidacji (400) z polem `errors`

**Struktura odpowiedzi:**
```json
{
  "errors": {
    "drawDate": ["Data losowania nie może być w przyszłości", "Losowanie z tą datą już istnieje"],
    "numbers": ["Wymagane dokładnie 6 liczb", "Liczby muszą być unikalne"]
  }
}
```

**Obsługa:**
1. ApiService rzuca wyjątek z `error.status = 400` i `error.data = { errors }`
2. Catch block w `handleFormSubmit()` wywołuje `handleApiError(error, defaultMessage)`
3. Flattening błędów: `Object.values(error.data.errors).flat()`
4. Wyświetlenie ErrorModal z listą błędów
5. Formularz pozostaje otwarty

**Przykład komunikatów (z backendu):**
- "Data losowania nie może być w przyszłości"
- "Wymagane dokładnie 6 liczb"
- "Liczby muszą być unikalne"
- "Liczby muszą być w zakresie 1-49"

---

### 10.3 Błędy uprawnień (403 Forbidden)

**Scenariusz:** Użytkownik bez uprawnień admin próbuje wykonać operację CRUD

**Obsługa:**
1. Backend zwraca 403 Forbidden
2. ApiService rzuca wyjątek z `error.status = 403`
3. Catch block wywołuje `handleApiError(error, defaultMessage)`
4. ErrorModal: "Nie masz uprawnień do wykonania tej operacji"
5. Modal formularza zamyka się

**Uwaga:** W MVP conditional rendering (isAdmin) powinien zapobiec temu scenariuszowi (przyciski CRUD niewidoczne dla non-admin). Obsługa 403 to dodatkowe zabezpieczenie.

---

### 10.4 Błędy 404 Not Found

**Scenariusz:** Użytkownik próbuje edytować/usunąć losowanie które nie istnieje

**Obsługa:**
1. Backend zwraca 404 Not Found
2. ApiService rzuca wyjątek z `error.status = 404`
3. Catch block wywołuje `handleApiError(error, defaultMessage)`
4. ErrorModal: "Wynik losowania nie istnieje"
5. Modal zamyka się, lista odświeża się

---

### 10.5 Błędy serwera (5xx) i network errors

**Scenariusz:** Problem z serwerem lub połączeniem sieciowym

**Obsługa:**
1. ApiService rzuca wyjątek z `error.status >= 500` lub NetworkError
2. Catch block wywołuje `handleApiError(error, defaultMessage)`
3. ErrorModal z generycznym komunikatem:
   - "Nie udało się pobrać listy losowań"
   - "Nie udało się zapisać wyniku losowania"
   - "Nie udało się usunąć wyniku losowania"
4. Modal zamyka się (jeśli był otwarty)

**Użytkownik może:**
- Ponowić operację (np. ponownie kliknąć "Zapisz")
- Sprawdzić połączenie internetowe
- Odświeżyć stronę

---

### 10.6 Błąd wygasłego tokenu (401 Unauthorized)

**Scenariusz:** Token JWT wygasł podczas sesji użytkownika

**Obsługa:**
1. ApiService wykrywa 401 Unauthorized
2. Wywołanie `logout()` z AppContext (czyszczenie localStorage)
3. Redirect do `/login`
4. ErrorModal (opcjonalnie): "Twoja sesja wygasła. Zaloguj się ponownie."

**Implementacja w ApiService:**
```typescript
private async request(endpoint: string, options: RequestInit) {
  const response = await fetch(`${API_URL}${endpoint}`, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${this.token}`,
      'X-TOKEN': APP_TOKEN,
      ...options.headers
    }
  });

  if (response.status === 401) {
    // Wygasły token - silent failure
    this.clearAuthToken();
    window.location.href = '/login'; // Redirect
    throw new ApiError('Unauthorized', 401);
  }

  if (!response.ok) {
    const data = await response.json().catch(() => ({}));
    throw new ApiError('API Error', response.status, data);
  }

  return response;
}
```

---

## 11. Kroki implementacji

### Krok 1: Definicja typów i interfejsów
**Czas:** 30 minut

**Zadania:**
1. Utworzenie pliku `src/services/contracts/draw.ts`:
   - Definicja typów DTO: `DrawsResponse`, `Draw`, `CreateDrawRequest`, `UpdateDrawRequest`, `DrawActionResponse`, `DrawValidationError`
2. Utworzenie pliku `src/types/draws.ts`:
   - Definicja typów ViewModel: `FilterState`, `PaginationState`, `DrawFormData`, `DrawFormErrors`, `ModalState`, `DrawsQueryParams`
3. Export wszystkich typów z index files

---

### Krok 2: Implementacja metod API (ApiService)
**Czas:** 45 minut

**Zadania:**
1. Otwarcie `src/services/api-service.ts`
2. Dodanie 4 nowych metod:
   - `getDraws(params: DrawsQueryParams): Promise<DrawsResponse>`
   - `createDraw(data: CreateDrawRequest): Promise<DrawActionResponse>`
   - `updateDraw(id: number, data: UpdateDrawRequest): Promise<DrawActionResponse>`
   - `deleteDraw(id: number): Promise<DrawActionResponse>`
3. Implementacja error handling (rzucanie ApiError dla 4xx/5xx)
4. Testy manualne z Postmanem (weryfikacja endpointów backend)

---

### Krok 3: Komponenty Shared (jeśli nie istnieją)
**Czas:** 1 godzina

**Zadania:**
1. Sprawdzenie czy istnieją:
   - `Button`, `Modal`, `ErrorModal`, `Toast`, `DatePicker`, `NumberInput`, `Spinner`, `Pagination`
2. Jeśli nie istnieją: implementacja brakujących komponentów zgodnie z UI Plan (sekcja 5.1)
3. Testowanie komponentów w izolacji (Storybook opcjonalnie)

---

### Krok 4: Komponent DrawsFilterPanel
**Czas:** 1 godzina

**Zadania:**
1. Utworzenie `src/components/Draws/DrawsFilterPanel.tsx`
2. Implementacja:
   - 2x DatePicker (Od/Do)
   - Local state dla dateFrom/dateTo
   - Inline validation (dateFrom ≤ dateTo)
   - Button "Filtruj" (wywołanie onFilter)
   - Button "Wyczyść" (conditional, wywołanie onClearFilter)
3. Styling: Tailwind CSS
   - Mobile: vertical stack
   - Desktop: inline (Od | Do obok siebie)
4. Testowanie walidacji inline

---

### Krok 5: Komponent DrawItem
**Czas:** 45 minut

**Zadania:**
1. Utworzenie `src/components/Draws/DrawItem.tsx`
2. Implementacja:
   - Display daty losowania (format: DD.MM.YYYY)
   - Display 6 liczb (badges: bg-blue-100 text-blue-800)
   - Display daty wprowadzenia (format: "Wprowadzono: DD.MM.YYYY HH:MM")
   - Przyciski [Edytuj] [Usuń] (conditional: isAdmin)
3. Styling: Tailwind CSS (card/row layout)
4. Testowanie z mock data

---

### Krok 6: Komponent DrawList
**Czas:** 30 minut

**Zadania:**
1. Utworzenie `src/components/Draws/DrawList.tsx`
2. Implementacja:
   - Mapowanie draws[] na DrawItem[]
   - EmptyState gdy draws.length === 0 && !loading
   - Loading Spinner gdy loading === true
3. Props: draws[], isAdmin, onEdit, onDelete, loading
4. Testowanie z mock data (empty state, loading, lista)

---

### Krok 7: Komponent DrawFormModal
**Czas:** 2.5 godziny

**Zadania:**
1. Utworzenie `src/components/Draws/DrawFormModal.tsx`
2. Implementacja:
   - Modal wrapper (używa shared Modal)
   - Tytuł dynamiczny (mode: 'add' | 'edit')
   - DatePicker dla drawDate
   - Dropdown/RadioButtons dla lottoType ("LOTTO" lub "LOTTO PLUS")
   - 6x NumberInput dla numbers[]
   - Local state: formData, formErrors, loading, **xLottoEnabled**
   - **useEffect: Sprawdzenie Feature Flag przy otwarciu modalu**
     - API call `GET /api/xlotto/is-enabled`
     - Ustawienie state `xLottoEnabled` na podstawie `response.data`
   - Inline validation w czasie rzeczywistym (onChange)
   - Przycisk "Pobierz z XLotto" (**conditional: xLottoEnabled**, wywołanie handleLoadFromXLotto)
   - Przycisk "Wyczyść" (reset formularza)
   - Przycisk "Anuluj" (zamknięcie onCancel)
   - Przycisk "Zapisz" (submit → zbieranie błędów → onSubmit)
3. Implementacja funkcji handleLoadFromXLotto:
   - Walidacja: sprawdzenie czy drawDate jest wybrany
   - API call: xLottoActualDraws({ date: formData.drawDate, lottoType: formData.lottoType })
   - Parsowanie odpowiedzi: JSON.parse(response.data)
   - Wyszukanie DrawItem dla wybranego GameType
   - Automatyczne wypełnienie pól numbers[] wynikami
   - Loading state: disabled przyciski, spinner
   - Obsługa błędów: alert z komunikatem błędu
4. Logika walidacji:
   - Wszystkie pola wymagane
   - drawDate ≤ dzisiaj
   - numbers[i] w zakresie 1-49
   - numbers[] unikalne (porównanie wszystkich par)
5. Layout:
   - Mobile: vertical stack (6 inputs)
   - Desktop: 2 kolumny (3 inputs per row)
6. Testowanie wszystkich scenariuszy walidacji i integracji XLotto

---

### Krok 8: Komponent DeleteConfirmationModal
**Czas:** 30 minut

**Zadania:**
1. Utworzenie `src/components/Draws/DeleteConfirmationModal.tsx`
2. Implementacja:
   - Modal wrapper (używa shared Modal)
   - Display daty i liczb losowania do usunięcia
   - Button "Anuluj" (onCancel, focus default)
   - Button "Usuń" (onConfirm, danger variant)
3. Testowanie z mock data

---

### Krok 9: Komponent DrawsPage (główna logika)
**Czas:** 3 godziny

**Zadania:**
1. Utworzenie `src/pages/draws/DrawsPage.tsx`
2. Implementacja stanu lokalnego:
   - draws[], loading
   - paginationState
   - filterState
   - modalState
   - toastMessage
3. Implementacja funkcji:
   - fetchDraws(page)
   - handleFilter(dateFrom, dateTo)
   - handleClearFilter()
   - handlePageChange(page)
   - handleOpenAddModal(), handleOpenEditModal(id), handleOpenDeleteModal(id)
   - handleFormSubmit(data)
   - handleDeleteConfirm(id)
   - handleApiError(error, defaultMessage)
4. useEffect: initial load (`fetchDraws(1)`)
5. Renderowanie:
   - Header z tytułem i przyciskiem "+ Dodaj wynik" (conditional: isAdmin)
   - DrawsFilterPanel
   - FilterInfo (conditional: filterState.isActive)
   - DrawList
   - Pagination
   - DrawFormModal (conditional: modalState.isFormOpen)
   - DeleteConfirmationModal (conditional: modalState.isDeleteOpen)
   - ErrorModal (conditional: modalState.isErrorOpen)
   - Toast (conditional: toastMessage)
6. Testowanie wszystkich przepływów interakcji:
   - Przeglądanie listy
   - Filtrowanie
   - Paginacja
   - Dodawanie (admin)
   - Edycja (admin)
   - Usuwanie (admin)

---

### Krok 10: Integracja z routing (React Router)
**Czas:** 15 minut

**Zadania:**
1. Otwarcie `src/App.tsx` lub głównego pliku routing
2. Dodanie route `/draws` z ProtectedRoute wrapper:
   ```tsx
   <Route path="/draws" element={<ProtectedRoute><DrawsPage /></ProtectedRoute>} />
   ```
3. Weryfikacja że niezalogowany użytkownik redirectuje do `/login`
4. Testowanie nawigacji z Navbar

---

### Krok 11: Testowanie E2E
**Czas:** 2 godziny

**Zadania:**
1. **Test 1: Przeglądanie listy jako zwykły użytkownik**
   - Login jako non-admin
   - Nawigacja do `/draws`
   - Weryfikacja: brak przycisków CRUD (+ Dodaj, Edytuj, Usuń)
   - Weryfikacja: lista widoczna, paginacja działa

2. **Test 2: Filtrowanie po zakresie dat**
   - Wypełnienie date range picker (Od: 2025-10-01, Do: 2025-10-31)
   - Kliknięcie "Filtruj"
   - Weryfikacja: lista odświeżona, info "Filtr aktywny" widoczne
   - Kliknięcie "Wyczyść"
   - Weryfikacja: lista pełna, info zniknęło

3. **Test 3: Paginacja**
   - Kliknięcie page number 2
   - Weryfikacja: lista odświeżona, current page = 2
   - Kliknięcie "Następna"
   - Weryfikacja: current page = 3

4. **Test 4: Admin dodaje wynik losowania**
   - Login jako admin
   - Kliknięcie "+ Dodaj wynik"
   - Wypełnienie formularza (data + 6 liczb)
   - Kliknięcie "Zapisz"
   - Weryfikacja: Toast sukcesu, modal zamknięty, nowy wynik na liście

5. **Test 5: Admin edytuje wynik**
   - Kliknięcie [Edytuj] przy losowaniu
   - Zmiana jednej liczby
   - Kliknięcie "Zapisz"
   - Weryfikacja: Toast sukcesu, zaktualizowane dane na liście

6. **Test 6: Admin usuwa wynik**
   - Kliknięcie [Usuń] przy losowaniu
   - Potwierdzenie w modalu
   - Weryfikacja: Toast sukcesu, losowanie zniknęło z listy

7. **Test 7: Błędy walidacji**
   - Otwarcie formularza dodawania
   - Wprowadzenie daty w przyszłości → sprawdzenie inline error
   - Wprowadzenie duplikatu liczby → sprawdzenie inline error
   - Kliknięcie "Zapisz" → sprawdzenie ErrorModal z listą błędów

8. **Test 8: Responsywność**
   - Testowanie na mobile (dev tools)
   - Weryfikacja: vertical stack filtra, simplified pagination

---

### Krok 12: Finalizacja i polish
**Czas:** 1 godzina

**Zadania:**
1. Code review (self-review lub peer review)
2. Refactoring powtarzalnego kodu
3. Dodanie komentarzy JSDoc (opcjonalnie)
4. Weryfikacja accessibility:
   - ARIA labels
   - Keyboard navigation
   - Focus management
5. Performance check:
   - React DevTools Profiler
   - Optymalizacja re-renderów (React.memo jeśli potrzebne)
6. Commit i push do repozytorium

---

**Łączny czas implementacji: ~13 godzin roboczych (2 dni pracy)**

---

## Podsumowanie

Plan implementacji widoku Historia Losowań (Draws Page) obejmuje wszystkie kluczowe funkcjonalności zdefiniowane w PRD, UI Plan i API Plan:

✅ **Przeglądanie listy losowań** - Dostępne dla wszystkich zalogowanych użytkowników, globalna tabela Draws
✅ **Filtrowanie po zakresie dat** - Date range picker z inline validation
✅ **Paginacja** - 20 wyników na stronę, adaptive controls (desktop/mobile)
✅ **Zarządzanie losowaniami (admin only)** - Dodawanie/edycja/usuwanie z walidacją i error handling
✅ **Automatyczne pobieranie wyników z XLotto.pl** - Przycisk "Pobierz z XLotto" w formularzu dodawania/edycji wykorzystujący Google Gemini API do ekstrakcji wyników
✅ **Conditional rendering** - Role-based UI (admin vs non-admin)
✅ **Integracja API** - 5 endpointów (GET/POST/PUT/DELETE draws + GET xlotto/actual-draws) z JWT authentication
✅ **Obsługa błędów** - Inline validation + ErrorModal dla błędów API
✅ **Responsywność** - Mobile-first CSS, adaptive layouts
✅ **Accessibility** - ARIA attributes, keyboard navigation, focus management

Widok jest w pełni kompatybilny z architekturą aplikacji (Context API dla auth, lokalny stan dla danych, Tailwind CSS dla stylów) i gotowy do implementacji zgodnie z powyższymi krokami.
