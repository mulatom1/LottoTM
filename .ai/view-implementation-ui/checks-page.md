# Plan implementacji widoku Checks Page

## 1. Przegląd

Checks Page to widok umożliwiający użytkownikom weryfikację swoich zestawów liczb LOTTO względem wyników losowań w określonym zakresie czasowym. System automatycznie identyfikuje wygrane (3 lub więcej trafień) i prezentuje je w przejrzysty sposób z wyróżnieniem trafionych liczb.

**Główne funkcjonalności:**
- Wybór zakresu dat do weryfikacji (domyślnie ostatni tydzień, maksymalnie konfigurowalny przez `Features:Verification:Days`, domyślnie 31 dni)
- Frontend pobiera limit z endpointu `GET /api/config` przy ładowaniu strony
- Opcjonalne filtrowanie kuponów według nazwy grupy (groupName) z wyszukiwaniem częściowym (Contains)
- Granularne filtrowanie wyników: kontrola wyświetlania kuponów według liczby trafień (0, 3, 4, 5, 6 trafień) - lokalne filtrowanie bez ponownego odpytywania backendu
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
├── CheckSummary (podsumowanie statystyk - wyświetlane po zakończeniu weryfikacji)
│   ├── Header z przyciskiem toggle (ikona statystyk + "Podsumowanie wyników")
│   └── Content (grid ze statystykami - domyślnie rozwinięte)
│       ├── StatCard (Liczba losowań)
│       ├── StatCard (Liczba kuponów)
│       ├── StatCard (Suma nakładów)
│       ├── WinStatCard (Wygrane 1° - trójki)
│       ├── WinStatCard (Wygrane 2° - czwórki)
│       ├── WinStatCard (Wygrane 3° - piątki)
│       ├── WinStatCard (Wygrane 4° - szóstki)
│       ├── WinStatCard (Suma wygranych)
│       └── BalanceCard (Bilans z emoji)
└── CheckResults (sekcja z wynikami z granularnymi filtrami)
    ├── Filtry wyświetlania (checkboxy dla każdego typu wyników)
    │   ├── Checkbox "Pokaż kupony bez trafień"
    │   ├── Checkbox "Pokaż 3 trafienia"
    │   ├── Checkbox "Pokaż 4 trafienia"
    │   ├── Checkbox "Pokaż 5 trafień"
    │   └── Checkbox "Pokaż 6 trafień"
    └── DrawCard[] (dla każdego losowania - kupony przefiltrowane według aktywnych filtrów)
        ├── DrawHeader (zawsze widoczny, nie klikany)
        │   ├── Data losowania (duża czcionka, pogrubiona)
        │   ├── Badge typu losowania (LOTTO/LOTTO PLUS)
        │   ├── DrawSystemId (mniejsza czcionka, szary kolor)
        │   └── Wylosowane numery (niebieskie kółka)
        ├── ExpandableSection1 (Koszt kuponu - domyślnie ukryta)
        │   ├── Header (kliknąlny): "Koszt kuponu" + ikona ▼/▶
        │   └── Content (po rozwinięciu):
        │       ├── Cena biletu
        │       └── WinPoolStatsGrid (statystyki wygranych 1-4 stopnia)
        │           ├── WinPoolCard (Stopień 1 - 6 trafień, zielona)
        │           ├── WinPoolCard (Stopień 2 - 5 trafień, niebieska)
        │           ├── WinPoolCard (Stopień 3 - 4 trafienia, żółta)
        │           └── WinPoolCard (Stopień 4 - 3 trafienia, pomarańczowa)
        └── ExpandableSection2 (Ilość wygranych zestawów - domyślnie ukryta)
            ├── Header (kliknąlny): "Ilość wygranych zestawów (X)" + ikona ▼/▶
            └── Content (po rozwinięciu):
                └── WinningTicketsList (lista wygranych kuponów)
                    └── WinningTicketItem[] (dla każdego wygrywającego kuponu)
                        ├── GroupName badge (szary)
                        ├── WinBadge (stopień wygranej: 3-6 trafień)
                        └── TicketNumbers (szare kółka dla nietrafionych, niebieskie pogrubione dla trafionych)
```

## 4. Szczegóły komponentów

### 4.1 ChecksPage (główny komponent)

**Opis komponentu:**
Główny kontener strony weryfikacji wygranych. Zarządza stanem formularza zakresu dat, wywołuje API weryfikacji i renderuje wyniki w formie accordion. Strona ma animowane tło z losowymi numerami loterii tworzącymi dynamiczny, wizualnie atrakcyjny interfejs.

**Główne elementy HTML i komponenty:**
- `<main>` - kontener główny z gradientowym tłem (`bg-gradient-to-br from-gray-100 via-blue-50 to-yellow-50`) i responsywnym paddingiem (`py-8 px-4 sm:px-6 lg:px-8`)
- **Animowane tło** - `<div>` z 200 losowymi numerami loterii (1-49):
  - Losowe pozycjonowanie (x, y w zakresie 0-100%)
  - Losowy rozmiar czcionki (1.5-4rem)
  - Losowa opacity (0.05-0.20)
  - Losowy czas trwania animacji (15-35s)
  - Losowe opóźnienie animacji (0-5s)
  - Kolory: text-gray-500, text-blue-600, text-yellow-600
  - Animacja: `animate-float` (unoszenie się)
  - `pointer-events-none` (nie blokuje interakcji)
- `<h1>` - nagłówek strony "Sprawdź swoje wygrane" (text-3xl font-bold text-gray-900)
- `<p>` - podtytuł strony "Weryfikuj swoje zestawy liczb względem wyników losowań w wybranym zakresie dat" (text-gray-600)
- `<CheckPanel />` - panel z formularzem zakresu dat
- `<Spinner />` - wskaźnik ładowania (warunkowe renderowanie)
- `<CheckSummary />` - podsumowanie statystyk (warunkowe renderowanie po zakończeniu weryfikacji)
- `<CheckResults />` - sekcja z wynikami (warunkowe renderowanie)

**Obsługiwane zdarzenia:**
- `onSubmitCheck(dateFrom, dateTo, groupName)` - wywołanie API weryfikacji
- `onDateChange` - aktualizacja stanu dat w formularzu
- `onGroupNameChange` - aktualizacja nazwy grupy w formularzu

**Warunki walidacji:**
- `dateFrom` nie może być późniejsza niż `dateTo`
- Zakres dat nie może przekraczać limitu z `Features:Verification:Days` (walidacja na backendzie i frontendzie, frontend pobiera wartość z `GET /api/config`)
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
  verificationMaxDays: number;       // Limit dni z backend config (domyślnie 31)
  isLoadingConfig: boolean;          // Stan ładowania konfiguracji
  dateFrom: string;                  // Format YYYY-MM-DD
  dateTo: string;                    // Format YYYY-MM-DD
  groupName: string;                 // Nazwa grupy (opcjonalnie)
  showNonWinningTickets: boolean;    // Filtr: pokaż kupony bez trafień (domyślnie false)
  show3Hits: boolean;                // Filtr: pokaż kupony z 3 trafieniami (domyślnie true)
  show4Hits: boolean;                // Filtr: pokaż kupony z 4 trafieniami (domyślnie true)
  show5Hits: boolean;                // Filtr: pokaż kupony z 5 trafieniami (domyślnie true)
  show6Hits: boolean;                // Filtr: pokaż kupony z 6 trafieniami (domyślnie true)
  isLoading: boolean;                // Stan ładowania
  drawsResults: DrawsResult[] | null;      // Wyniki losowań z API
  ticketsResults: TicketsResult[] | null;  // Wyniki kuponów z API
  executionTimeMs: number;           // Czas wykonania zapytania (ms)
  error: string | null;              // Komunikat błędu
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

### 4.3 CheckSummary (podsumowanie statystyk)

**Opis komponentu:**
Komponent wyświetlający zagregowane statystyki wyników weryfikacji. Pokazuje podsumowanie liczby losowań, kuponów, nakładów, wygranych po poziomach (3-6 trafień) oraz bilans zysku/straty. Domyślnie rozwinięty z możliwością zwinięcia przez użytkownika.

**Główne elementy HTML:**
- `<div>` - kontener główny (gradient background, border, rounded)
- `<button>` - nagłówek z przyciskiem toggle (ikona statystyk + "Podsumowanie wyników" + ikona ▼/▲)
- `<div>` - zawartość rozwijalna (conditional rendering)
  - `<div>` - grid ze statystykami (responsive: 1/2/3 kolumny)
    - `<StatCard>` × 3 - podstawowe statystyki (losowania, kupony, nakłady)
    - `<WinStatCard>` × 5 - statystyki wygranych (1°-4° + suma)
    - `<BalanceCard>` - bilans z emoji

**Obsługiwane interakcje:**
- `onClick` na nagłówku - toggle rozwinięcia/zwinięcia (zmiana stanu `isExpanded`)
- Animacja rotacji ikony (▼/▲) podczas toggle

**Logika kalkulacji statystyk:**

1. **Liczba losowań**: liczba unikalnych dat losowań (`Set(drawsResults.map(d => d.drawDate)).size`)
   - Każda data to 1 losowanie (zawiera zarówno LOTTO jak i LOTTO PLUS)
2. **Liczba kuponów**: `ticketsResults.length` (unikalne zestawy użytkownika)
3. **Suma nakładów**: `drawsCount × ticketsCount × (lottoPrice + lottoPlusPrice)`
   - Pobranie cen: `Map<lottoType, ticketPrice>` dla LOTTO i LOTTO PLUS
   - Suma cen: `pricePerTicketPerDraw = lottoPrice + lottoPlusPrice`
   - Koszt całkowity: `drawsCount × ticketsCount × pricePerTicketPerDraw`
   - Przykład:
     - 5 losowań (unikalnych dat)
     - 10 kuponów
     - LOTTO = 3.00 zł, LOTTO PLUS = 1.50 zł
     - Koszt = 5 × 10 × (3.00 + 1.50) = 5 × 10 × 4.50 = 225.00 zł
4. **Wygrane po poziomach**:
   - Iteracja przez `drawsResults.forEach(draw => draw.winningTicketsResult)`
   - Dla każdego wygrywającego kuponu:
     - `hits = winningTicket.matchingNumbers.length`
     - Mapowanie na poziomy:
       - 3 trafienia → wins3 (count++, value += draw.winPoolAmount4)
       - 4 trafienia → wins4 (count++, value += draw.winPoolAmount3)
       - 5 trafień → wins5 (count++, value += draw.winPoolAmount2)
       - 6 trafień → wins6 (count++, value += draw.winPoolAmount1)
5. **Suma wygranych**: suma count i value z wins3-6
6. **Bilans**: `totalWinValue - totalCost`
   - Emoji: 😊 jeśli bilans ≥ 0, 😠 jeśli < 0

**Typy:**
```typescript
interface CheckSummaryProps {
  drawsResults: DrawsResult[];
  ticketsResults: TicketsResult[];
}

interface WinStats {
  count: number;
  value: number;
}

interface SummaryStats {
  drawsCount: number;
  ticketsCount: number;
  totalCost: number;
  wins3: WinStats;  // 3 trafienia (winPoolAmount4)
  wins4: WinStats;  // 4 trafienia (winPoolAmount3)
  wins5: WinStats;  // 5 trafień (winPoolAmount2)
  wins6: WinStats;  // 6 trafień (winPoolAmount1)
  totalWinCount: number;
  totalWinValue: number;
}
```

**Propsy:**
```typescript
interface CheckSummaryProps {
  drawsResults: DrawsResult[];  // Wyniki losowań z API
  ticketsResults: TicketsResult[];  // Zestawy użytkownika
}
```

**Stan lokalny:**
```typescript
interface CheckSummaryState {
  isExpanded: boolean;  // Domyślnie true
}
```

**Responsywność:**
- Mobile (< 640px): 1 kolumna grid
- Tablet (640-1024px): 2 kolumny grid
- Desktop (≥ 1024px): 3 kolumny grid

**Kolorystyka kart statystyk:**
- Liczba losowań: niebieska (bg-blue-100)
- Liczba kuponów: zielona (bg-green-100)
- Suma nakładów: pomarańczowa (bg-orange-100)
- Wygrane 1° (trójki): żółta (bg-yellow-100)
- Wygrane 2° (czwórki): jasnozielona (bg-lime-100)
- Wygrane 3° (piątki): ciemnozielona (bg-emerald-100)
- Wygrane 4° (szóstki): fioletowa (bg-purple-100)
- Suma wygranych: granatowa (bg-indigo-100)
- Bilans: zielona (zysk, bg-green-100) lub czerwona (strata, bg-red-100)

**Format wyświetlania:**
- Waluta: `XX.XX zł` (zawsze 2 miejsca po przecinku)
- Wygrane: `ilość | wartość zł` (np. "5 | 25.00 zł")
  - ilość = liczba wygranych kuponów
  - wartość = suma wszystkich wygranych dla danego poziomu (już zsumowana)
- Bilans: `+XX.XX zł` lub `-XX.XX zł` + podpis "Zysk"/"Strata"

**Układ grid (3 kolumny × 3 rzędy):**
- Rząd 1: Liczba losowań | Liczba kuponów | **Suma nakładów** (kolumna 3)
- Rząd 2: Wygrane 1° | Wygrane 2° | **Suma wygranych** (kolumna 3)
- Rząd 3: Wygrane 3° | Wygrane 4° | **Bilans** (kolumna 3)

---

### 4.4 CheckResults (kontener wyników)

**Opis komponentu:**
Kontener renderujący wyniki weryfikacji w formie listy Draw Cards. Każde losowanie to osobna karta z dwoma rozwijalnymi sekcjami. Komponent transformuje dane z API (osobne listy `drawsResults` i `ticketsResults`) do struktury UI (`DrawWithTickets[]`) przez połączenie danych za pomocą `ticketId`. Zawiera granularne filtry do kontroli wyświetlania kuponów według liczby trafień (0, 3, 4, 5, 6 trafień).

**Główne elementy:**
- `<div>` - kontener główny
- `<div>` - header sekcji wyników z filtrami:
  - `<div>` - info o liczbie wyników (opcjonalnie: "Znaleziono X losowań")
  - **Panel filtrów (checkboxy):**
    - `<label>` + `<input type="checkbox">` - "Pokaż kupony bez trafień" (showNonWinningTickets, domyślnie odznaczony)
    - `<label>` + `<input type="checkbox">` - "Pokaż 3 trafienia" (show3Hits, domyślnie zaznaczony)
    - `<label>` + `<input type="checkbox">` - "Pokaż 4 trafienia" (show4Hits, domyślnie zaznaczony)
    - `<label>` + `<input type="checkbox">` - "Pokaż 5 trafień" (show5Hits, domyślnie zaznaczony)
    - `<label>` + `<input type="checkbox">` - "Pokaż 6 trafień" (show6Hits, domyślnie zaznaczony)
- `<DrawCard[]>` - lista Draw Cards (dla każdego losowania - kupony wewnątrz przefiltrowane według aktywnych filtrów)
- Empty state `<div>` - gdy brak wyników: "Nie znaleziono wygranych w wybranym zakresie dat."

**Transformacja danych (w komponencie CheckResults):**
```typescript
// Funkcja pomocnicza do transformacji response API do struktury UI
function transformResponseToDrawsWithTickets(response: CheckResponse): DrawWithTickets[] {
  // Tworzenie mapy ticketId -> TicketsResult dla szybkiego wyszukiwania
  const ticketsMap = new Map<number, TicketsResult>();
  response.ticketsResults.forEach(ticket => {
    ticketsMap.set(ticket.ticketId, ticket);
  });

  // Transformacja każdego draw
  return response.drawsResults.map(draw => {
    // Łączenie WinningTicketResult z TicketsResult
    const winningTickets: WinningTicketWithDetails[] = draw.winningTicketsResult.map(winTicket => {
      const ticketDetails = ticketsMap.get(winTicket.ticketId);
      if (!ticketDetails) {
        console.warn(`Ticket ${winTicket.ticketId} not found in ticketsResults`);
        return null;
      }
      return {
        ticketId: winTicket.ticketId,
        groupName: ticketDetails.groupName,
        ticketNumbers: ticketDetails.ticketNumbers,
        matchingNumbers: winTicket.matchingNumbers,
      };
    }).filter(Boolean) as WinningTicketWithDetails[];

    return {
      drawId: draw.drawId,
      drawDate: draw.drawDate,
      drawSystemId: draw.drawSystemId,
      lottoType: draw.lottoType,
      drawNumbers: draw.drawNumbers,
      ticketPrice: draw.ticketPrice,
      winPoolCount1: draw.winPoolCount1,
      winPoolAmount1: draw.winPoolAmount1,
      winPoolCount2: draw.winPoolCount2,
      winPoolAmount2: draw.winPoolAmount2,
      winPoolCount3: draw.winPoolCount3,
      winPoolAmount3: draw.winPoolAmount3,
      winPoolCount4: draw.winPoolCount4,
      winPoolAmount4: draw.winPoolAmount4,
      winningTickets,
    };
  });
}
```

**Logika filtrowania (w komponencie CheckResults):**
```typescript
// Filtrowanie kuponów w każdym losowaniu według aktywnych filtrów
// Każdy DrawCard otrzymuje przefiltrowaną listę winningTickets na podstawie:
// - showNonWinningTickets: czy pokazywać kupony z 0 trafień
// - show3Hits: czy pokazywać kupony z 3 trafieniami
// - show4Hits: czy pokazywać kupony z 4 trafieniami
// - show5Hits: czy pokazywać kupony z 5 trafieniami
// - show6Hits: czy pokazywać kupony z 6 trafieniami

// Przykładowa logika filtrowania:
const shouldShowTicket = (hits: number): boolean => {
  if (hits === 0) return showNonWinningTickets;
  if (hits === 3) return show3Hits;
  if (hits === 4) return show4Hits;
  if (hits === 5) return show5Hits;
  if (hits === 6) return show6Hits;
  return false;
};
```

**Obsługiwane interakcje:**
- `onChange` na każdym checkboxie - wywołanie odpowiedniego callbacka:
  - `onShowNonWinningTicketsChange(value: boolean)`
  - `onShow3HitsChange(value: boolean)`
  - `onShow4HitsChange(value: boolean)`
  - `onShow5HitsChange(value: boolean)`
  - `onShow6HitsChange(value: boolean)`

**Obsługiwana walidacja:**
- Brak

**Typy:**
```typescript
// Response z API
interface CheckResponse {
  executionTimeMs: number;
  drawsResults: DrawsResult[];
  ticketsResults: TicketsResult[];
}

interface DrawsResult {
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
  winningTicketsResult: WinningTicketResult[];
}

interface WinningTicketResult {
  ticketId: number;
  matchingNumbers: number[];
}

interface TicketsResult {
  ticketId: number;
  groupName: string;
  ticketNumbers: number[];
}

// Typy pomocnicze dla UI (po transformacji)
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
  winningTickets: WinningTicketWithDetails[];
}

interface WinningTicketWithDetails {
  ticketId: number;
  groupName: string;
  ticketNumbers: number[];
  matchingNumbers: number[];
}
```

**Propsy:**
```typescript
interface CheckResultsProps {
  drawsResults: DrawsResult[];
  ticketsResults: TicketsResult[];
  executionTimeMs: number;
  showNonWinningTickets: boolean;
  onShowNonWinningTicketsChange: (value: boolean) => void;
  show3Hits: boolean;
  onShow3HitsChange: (value: boolean) => void;
  show4Hits: boolean;
  onShow4HitsChange: (value: boolean) => void;
  show5Hits: boolean;
  onShow5HitsChange: (value: boolean) => void;
  show6Hits: boolean;
  onShow6HitsChange: (value: boolean) => void;
}
```

---

### 4.5 DrawCard (karta pojedynczego losowania)

**Opis komponentu:**
Karta reprezentująca pojedyncze losowanie z dwoma rozwijalnymi sekcjami. Header główny (zawsze widoczny) zawiera datę, ID systemowe, typ gry i wylosowane liczby. Sekcja 1 (rozwijalna) zawiera koszt kuponu i statystyki wygranych dla 4 stopni. Sekcja 2 (rozwijalna) zawiera listę wygranych kuponów użytkownika.

**Główne elementy HTML:**
- `<div>` - kontener główny karty (border, shadow, padding)
- **Header główny (zawsze widoczny, nie klikany):**
  - `<div>` - data losowania (duża, pogrubiona czcionka)
  - `<span>` - badge typu gry (LOTTO / LOTTO PLUS, różne kolory)
  - `<span>` - ID systemowe (mniejsza czcionka, szary)
  - `<div>` - wylosowane liczby (6 niebieskich kółek)

- **Rozwijalna sekcja 1 (domyślnie ukryta):**
  - `<button>` - nagłówek sekcji (klikany): "Koszt kuponu" + ikona ▼/▶
  - `<div>` - zawartość (collapsible, aria-hidden gdy collapsed):
    - `<div>` - cena biletu: "Cena biletu: X.XX zł" (lub "Brak danych")
    - `<WinPoolStatsGrid>` - grid 4 kart z statystykami wygranych (stopień 1-4)

- **Rozwijalna sekcja 2 (domyślnie ukryta):**
  - `<button>` - nagłówek sekcji (klikany): "Ilość wygranych zestawów (X)" + ikona ▼/▶
  - `<div>` - zawartość (collapsible, aria-hidden gdy collapsed):
    - `<WinningTicketItem[]>` - lista wygranych kuponów
    - Empty state: "Brak wygranych kuponów dla tego losowania" (jeśli brak wygranych)

**Obsługiwane interakcje:**
- `onClick` na nagłówku sekcji 1 - toggle expand/collapse sekcji 1
- `onClick` na nagłówku sekcji 2 - toggle expand/collapse sekcji 2
- Keyboard: Enter/Space - toggle expand/collapse
- Niezależne stany rozwinięcia dla obu sekcji (useState dla każdej)

**Obsługiwana walidacja:**
- Brak

**Propsy:**
```typescript
interface DrawCardProps {
  draw: DrawWithTickets;
}
```

**Stan lokalny:**
```typescript
const [isSection1Expanded, setIsSection1Expanded] = useState(false); // Domyślnie ukryta
const [isSection2Expanded, setIsSection2Expanded] = useState(false); // Domyślnie ukryta
```

---

### 4.6 WinningTicketItem (pojedynczy wygrany kupon)

**Opis komponentu:**
Prezentacja pojedynczego wygrywającego kuponu użytkownika z wyróżnionymi trafionymi liczbami. Wyświetlana tylko w sekcji 2 DrawCard dla kuponów z ≥3 trafieniami. Zawiera badge nazwy grupy, badge stopnia wygranej i liczby kuponu (szare dla nietrafionych, niebieskie pogrubione dla trafionych).

**Główne elementy HTML:**
- `<div>` - kontener główny kuponu (card/row z padding, border)
- `<div>` - header kuponu (flex layout):
  - `<span>` - badge nazwy grupy (szary, mniejszy)
  - `<span>` - badge wygranej (WinBadge - kolorowy z emoji i tekstem)
- `<div>` - liczby kuponu:
  - `<span>` × 6 - liczby zestawu (trafione: niebieskie kółka z `font-bold`, nietrafione: szare kółka)

**Obsługiwane interakcje:**
- Brak (komponent prezentacyjny)

**Obsługiwana walidacja:**
- Brak

**Propsy:**
```typescript
interface WinningTicketItemProps {
  ticket: WinningTicketWithDetails;
}
```

**Logika renderowania:**
```typescript
// Liczba trafień (długość matchingNumbers)
const hits = ticket.matchingNumbers.length;

// Dla każdej liczby w zestawie
ticket.ticketNumbers.map(num => {
  const isMatched = ticket.matchingNumbers.includes(num);
  return (
    <span className={`
      inline-flex items-center justify-center w-8 h-8 rounded-full text-sm
      ${isMatched
        ? 'font-bold bg-blue-600 text-white'  // Trafione: niebieskie, pogrubione
        : 'bg-gray-100 text-gray-700'         // Nietrafione: szare
      }
    `}>
      {num}
    </span>
  );
})
```

---

### 4.7 WinPoolStatsGrid (grid statystyk wygranych)

**Opis komponentu:**
Grid zawierający 4 karty (WinPoolCard) reprezentujące statystyki wygranych dla każdego stopnia (1-4). Wyświetlany w sekcji 1 DrawCard po rozwinięciu. Responsywny layout: 4 kolumny na desktop, 2 na tablet, 1 na mobile.

**Główne elementy HTML:**
- `<div>` - kontener grid (Tailwind: `grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4`)
- `<WinPoolCard>` × 4 - karty dla każdego stopnia wygranej

**Obsługiwane interakcje:**
- Brak (komponent prezentacyjny)

**Obsługiwana walidacja:**
- Brak

**Propsy:**
```typescript
interface WinPoolStatsGridProps {
  ticketPrice: number | null;
  winPoolCount1: number | null;
  winPoolAmount1: number | null;
  winPoolCount2: number | null;
  winPoolAmount2: number | null;
  winPoolCount3: number | null;
  winPoolAmount3: number | null;
  winPoolCount4: number | null;
  winPoolAmount4: number | null;
}
```

---

### 4.8 WinPoolCard (karta statystyk dla pojedynczego stopnia)

**Opis komponentu:**
Kolorowa karta wyświetlająca statystyki wygranej dla jednego stopnia (1-4). Zawiera ikonę z liczbą trafień, ilość wygranych i kwotę. Każdy stopień ma inny kolor.

**Główne elementy HTML:**
- `<div>` - kontener karty (padding, border, rounded, kolorowe tło)
  - `<div>` - ikona stopnia (duża, pogrubiona liczba w kolorowym kółku)
  - `<div>` - nagłówek: "Stopień X (Y trafień)"
  - `<div>` - ilość wygranych: "Ilość: Z osób" (lub "Brak danych")
  - `<div>` - kwota: "Kwota: W zł" (lub "Brak danych")

**Warianty kolorystyczne (dla każdego stopnia):**
- **Stopień 1 (6 trafień):**
  - Border: `border-green-300`
  - Tło ikony: `bg-green-500 text-white`
  - Tekst: "Stopień 1 (6 trafień)"
- **Stopień 2 (5 trafień):**
  - Border: `border-blue-300`
  - Tło ikony: `bg-blue-500 text-white`
  - Tekst: "Stopień 2 (5 trafień)"
- **Stopień 3 (4 trafienia):**
  - Border: `border-yellow-300`
  - Tło ikony: `bg-yellow-500 text-white`
  - Tekst: "Stopień 3 (4 trafienia)"
- **Stopień 4 (3 trafienia):**
  - Border: `border-orange-300`
  - Tło ikony: `bg-orange-500 text-white`
  - Tekst: "Stopień 4 (3 trafienia)"

**Obsługiwane interakcje:**
- Brak (komponent prezentacyjny)

**Obsługiwana walidacja:**
- Brak

**Propsy:**
```typescript
interface WinPoolCardProps {
  tier: 1 | 2 | 3 | 4;
  count: number | null;
  amount: number | null;
}
```

**Logika renderowania:**
```typescript
// Mapowanie tier → label i kolor
const tierConfig = {
  1: { label: 'Stopień 1 (6 trafień)', icon: '6', borderColor: 'border-green-300', bgColor: 'bg-green-500' },
  2: { label: 'Stopień 2 (5 trafień)', icon: '5', borderColor: 'border-blue-300', bgColor: 'bg-blue-500' },
  3: { label: 'Stopień 3 (4 trafienia)', icon: '4', borderColor: 'border-yellow-300', bgColor: 'bg-yellow-500' },
  4: { label: 'Stopień 4 (3 trafienia)', icon: '3', borderColor: 'border-orange-300', bgColor: 'bg-orange-500' },
};

const config = tierConfig[tier];
```

---

### 4.9 WinBadge (badge wygranej)

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

### 4.10 DatePicker (komponent współdzielony)

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
  executionTimeMs: number;
  drawsResults: DrawsResult[];
  ticketsResults: TicketsResult[];
}

interface DrawsResult {
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
  winningTicketsResult: WinningTicketResult[];
}

interface WinningTicketResult {
  ticketId: number;
  matchingNumbers: number[];
}

interface TicketsResult {
  ticketId: number;
  groupName: string;
  ticketNumbers: number[];
}

// Typy pomocnicze dla UI (po transformacji w CheckResults)
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
  winningTickets: WinningTicketWithDetails[];
}

interface WinningTicketWithDetails {
  ticketId: number;
  groupName: string;
  ticketNumbers: number[];
  matchingNumbers: number[];
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
const [verificationMaxDays, setVerificationMaxDays] = useState<number>(31);  // z backend config
const [isLoadingConfig, setIsLoadingConfig] = useState<boolean>(true);
const [dateFrom, setDateFrom] = useState<string>(getDefaultDateFrom());  // -1 tydzień
const [dateTo, setDateTo] = useState<string>(getDefaultDateTo());        // dzisiaj
const [groupName, setGroupName] = useState<string>('');                  // puste domyślnie
const [showNonWinningTickets, setShowNonWinningTickets] = useState<boolean>(false);  // domyślnie false (ukrywa losowania bez dopasowań)
const [show3Hits, setShow3Hits] = useState<boolean>(true);               // domyślnie true
const [show4Hits, setShow4Hits] = useState<boolean>(true);               // domyślnie true
const [show5Hits, setShow5Hits] = useState<boolean>(true);               // domyślnie true
const [show6Hits, setShow6Hits] = useState<boolean>(true);               // domyślnie true
const [isLoading, setIsLoading] = useState<boolean>(false);
const [drawsResults, setDrawsResults] = useState<DrawsResult[] | null>(null);
const [ticketsResults, setTicketsResults] = useState<TicketsResult[] | null>(null);
const [executionTimeMs, setExecutionTimeMs] = useState<number>(0);
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
function validateDateRange(dateFrom: string, dateTo: string, maxDays: number): string | null {
  if (dateFrom > dateTo) {
    return "Data 'Od' musi być wcześniejsza lub równa 'Do'";
  }

  // Sprawdzenie zakresu (limit z backend config przez GET /api/config)
  const from = new Date(dateFrom);
  const to = new Date(dateTo);
  const diffDays = Math.ceil((to.getTime() - from.getTime()) / (1000 * 60 * 60 * 24));

  if (diffDays > maxDays) {
    return `Zakres dat nie może przekraczać ${maxDays} dni`;
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
- Zakres dat max `Features:Verification:Days` (domyślnie 31 dni) - walidacja na backendzie
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

### 8.4 Scenariusz błędu - przekroczenie zakresu maksymalnego

1. Użytkownik wybiera zakres większy niż limit z `Features:Verification:Days` (np. > 31 dni)
2. Frontend walidacja inline (komunikat błędu pod polem "Do")
3. Lub jeśli bypass frontend: backend walidacja → ErrorModal:
   - Tytuł: "Błąd"
   - Treść: "Zakres dat nie może przekraczać {maxDays} dni"
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

2. Zakres dat ≤ limit z `Features:Verification:Days` (domyślnie 31 dni)
   - Weryfikacja: Backend i frontend (frontend pobiera limit z `GET /api/config` przy ładowaniu strony)
   - Moment weryfikacji: onChange (inline), onSubmit
   - Wpływ na UI: Komunikat błędu inline lub ErrorModal z komunikatem "Zakres dat nie może przekraczać {maxDays} dni"

3. Format daty YYYY-MM-DD
   - Weryfikacja: Natywna HTML5 (input type="date")
   - Moment weryfikacji: onChange
   - Wpływ na UI: Natywna walidacja HTML5 (browser-dependent)

**Komunikaty błędów (polski):**
- "Data 'Od' musi być wcześniejsza lub równa 'Do'"
- "Zakres dat nie może przekraczać {maxDays} dni" (gdzie maxDays pochodzi z `GET /api/config`)
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

### Krok 6: Implementacja WinPoolCard
- Renderowanie kolorowej karty dla pojedynczego stopnia wygranej (1-4)
- Ikona z liczbą trafień w kolorowym kółku
- Nagłówek: "Stopień X (Y trafień)"
- Wyświetlanie ilości wygranych i kwoty (lub "Brak danych" dla wartości null)
- Warianty kolorystyczne dla każdego stopnia (zielony, niebieski, żółty, pomarańczowy)

### Krok 7: Implementacja WinPoolStatsGrid
- Responsywny grid layout (4 kolumny desktop, 2 tablet, 1 mobile)
- Renderowanie 4 kart WinPoolCard (dla stopni 1-4)
- Przekazanie odpowiednich danych (count i amount) do każdej karty

### Krok 8: Implementacja WinningTicketItem
- Renderowanie liczb jako kółek (niebieskie pogrubione dla trafionych, szare dla nietrafionych)
- Badge nazwy grupy (szary)
- Badge stopnia wygranej (WinBadge)
- Obliczanie liczby trafień z `matchingNumbers.length`

### Krok 9: Implementacja DrawCard
- Stan lokalny dla dwóch sekcji: `isSection1Expanded`, `isSection2Expanded` (useState)
- Header główny (zawsze widoczny, nie klikany):
  - Data losowania (duża, pogrubiona)
  - Badge typu gry (LOTTO/LOTTO PLUS z różnymi kolorami)
  - DrawSystemId (mniejsza czcionka, szary)
  - Wylosowane liczby jako niebieskie kółka
- Rozwijalna sekcja 1 (domyślnie ukryta):
  - Header klikany: "Koszt kuponu" + ikona ▼/▶
  - Content: cena biletu + WinPoolStatsGrid
- Rozwijalna sekcja 2 (domyślnie ukryta):
  - Header klikany: "Ilość wygranych zestawów (X)" + ikona ▼/▶
  - Content: lista WinningTicketItem[] (tylko kupony z ≥3 trafieniami)
  - Empty state: "Brak wygranych kuponów dla tego losowania"
- ARIA attributes (aria-expanded, aria-controls) dla obu sekcji
- Keyboard navigation (Enter/Space) dla obu nagłówków

### Krok 10: Implementacja CheckResults
- Transformacja response API (`CheckResponse`) do struktury UI (`DrawWithTickets[]`)
- Funkcja `transformResponseToDrawsWithTickets`:
  - Tworzenie mapy `ticketId → TicketsResult` dla szybkiego lookup
  - Dla każdego draw z `drawsResults`: łączenie `winningTicketsResult` z `ticketsResults` przez `ticketId`
  - Budowanie `WinningTicketWithDetails[]` (połączenie matchingNumbers + ticketNumbers + groupName)
- Sortowanie losowań według daty (najnowsze na górze)
- Mapowanie `DrawWithTickets[]` → DrawCard[]
- Empty state dla pustej listy
- Conditional rendering (loading spinner vs results)

### Krok 11: Implementacja CheckPanel
- Formularz z 2 date pickerami i text inputem dla groupName
- Inline validation (dateFrom ≤ dateTo)
- Pole groupName (opcjonalne) z placeholderem "np. Ulubione"
- Opis pomocniczy: "Wyszukiwanie częściowe - wpisz fragment nazwy grupy (np. 'ulu' znajdzie 'Ulubione')"
- Button "Sprawdź wygrane" z disabled state
- Callback `onSubmit(dateFrom, dateTo, groupName)`

### Krok 12: Implementacja ChecksPage (główny komponent)
- Stan lokalny (verificationMaxDays, isLoadingConfig, dateFrom, dateTo, groupName, showNonWinningTickets, show3Hits, show4Hits, show5Hits, show6Hits, isLoading, drawsResults, ticketsResults, executionTimeMs, error)
- **useEffect do pobrania konfiguracji:**
  - Wywołanie `apiService.getConfig()` przy montowaniu komponentu
  - Ustawienie `verificationMaxDays` z odpowiedzi (domyślnie 31 jeśli błąd)
  - Ustawienie `isLoadingConfig` na false po zakończeniu
- **Animowane tło** - generowanie 200 losowych numerów loterii z `useMemo`:
  - Losowe pozycje (x, y), rozmiar, opacity, duration, delay
  - Kolory: text-gray-500, text-blue-600, text-yellow-600
  - Renderowanie jako absolute positioned divs z `animate-float` i `pointer-events-none`
- Helper functions (getDefaultDateFrom, getDefaultDateTo)
- Handler `handleSubmit()` z API call (przekazuje dateFrom, dateTo, groupName)
- Renderowanie:
  - Kontener główny `<main>` z gradientem (`bg-gradient-to-br from-gray-100 via-blue-50 to-yellow-50`)
  - Animowane tło
  - Header z h1 i podtytułem
  - CheckPanel + Spinner + CheckSummary + CheckResults
- Error handling z ErrorModal

### Krok 13: Stylizacja Tailwind CSS
- Mobile-first responsive design
- Gradient background (from-gray-100 via-blue-50 to-yellow-50)
- Responsywny padding: `py-8 px-4 sm:px-6 lg:px-8`
- Animowane tło: animacja `animate-float` (zdefiniowana w Tailwind config)
- Date range picker layout (vertical/inline)
- DrawCard styling (border, shadow, padding)
- Rozwijalne sekcje (hover effects, transitions)
- WinPoolCard styling (kolorowe borders i backgrounds)
- Badge variants (green/blue/orange/red dla różnych stopni)
- Touch targets min 44x44px

### Krok 14: Accessibility audit
- Semantic HTML (`<main>`, `<form>`, `<button>`)
- ARIA attributes (aria-expanded, aria-controls, aria-describedby) dla obu sekcji
- Keyboard navigation testing (Tab, Enter/Space, Escape)
- Focus management (focus trap w ErrorModal)
- Screen reader testing (NVDA/JAWS)

### Krok 15: Integracja z routing
- Dodanie route `/checks` w `src/main.tsx` (React Router 7)
- Protected route wrapper (redirect `/login` jeśli niezalogowany)
- Dodanie zakładki "Sprawdź Wygrane" w Navbar

### Krok 16: Testowanie
- Test manual: happy path (weryfikacja z wygranymi)
- Test manual: rozwijanie/zwijanie sekcji 1 i 2
- Test manual: empty state (brak wygranych)
- Test manual: edge cases (brak zestawów, brak losowań)
- Test manual: walidacja (nieprawidłowy zakres, >3 lata)
- Test manual: responsywność (mobile/tablet/desktop - grid WinPoolStatsGrid)
- Test manual: keyboard navigation (obie sekcje rozwijalne)
- Opcjonalnie: testy jednostkowe dla helper functions (validateDateRange, getDefaultDateFrom, transformResponseToDrawsWithTickets)

### Krok 17: Polish translation audit
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
