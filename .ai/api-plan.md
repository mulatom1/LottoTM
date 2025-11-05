# REST API Plan - LottoTM MVP

**Wersja:** 1.0
**Data:** 2025-11-03
**Podstawa:** `.ai/db-plan.md`, `.ai/prd.md`, `.ai/tech-stack.md`

---

## 1. Zasoby

| Zasób | Tabela bazy danych | Opis |
|-------|-------------------|------|
| **Users** | `Users` | Konta użytkowników systemu |
| **Tickets** | `Tickets`, `TicketNumbers` | Zestawy liczb LOTTO należące do użytkowników |
| **Draws** | `Draws`, `DrawNumbers` | Wyniki oficjalnych losowań LOTTO wprowadzone przez użytkowników |
| **Verification** | - | Wyniki weryfikacji wygranych (obliczane on-demand) |

---

## 2. Punkty końcowe

### 2.1 Moduł: Autentykacja (Auth)

---

#### **POST /api/auth/register**

**Opis:** Rejestracja nowego użytkownika z automatycznym logowaniem

**Parametry zapytania:** Brak

**Payload żądania:**
```json
{
  "email": "user@example.com",
  "password": "SecurePass123!",
  "confirmPassword": "SecurePass123!"
}
```

**Payload odpowiedzi (sukces):**
```json
{
  "message": "Rejestracja zakończona sukcesem"
}
```

**Walidacja:**
- `email`: wymagane, format email, unikalny w systemie (UNIQUE constraint)
- `password`: wymagane, min. 8 znaków (rekomendacja: 1 wielka litera, 1 cyfra, 1 znak specjalny)
- `confirmPassword`: wymagane, musi być identyczne z `password`

**Kody sukcesu:**
- `201 Created` - Użytkownik utworzony i zalogowany

**Kody błędów:**
- `400 Bad Request` - Nieprawidłowe dane wejściowe
  ```json
  {
    "errors": {
      "email": ["Email jest już zajęty"],
      "password": ["Hasło musi mieć min. 8 znaków"],
      "confirmPassword": ["Hasła nie są identyczne"]
    }
  }
  ```

**Logika biznesowa:**
1. Walidacja formatu email (regex)
2. Sprawdzenie unikalności email w bazie (UNIQUE constraint)
3. Haszowanie hasła (bcrypt, min. 10 rounds)
4. Zapis użytkownika do tabeli `Users`

---

#### **POST /api/auth/login**

**Opis:** Logowanie istniejącego użytkownika

**Parametry zapytania:** Brak

**Payload żądania:**
```json
{
  "email": "user@example.com",
  "password": "SecurePass123!"
}
```

**Walidacja:**
- `email`: wymagane, format email
- `password`: wymagane

**Payload odpowiedzi (sukces):**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "userId": 123,
  "email": "user@example.com",
  "isAdmin": false,
  "expiresAt": "2025-11-04T10:30:00Z"
}
```

**Kody sukcesu:**
- `200 OK` - Logowanie pomyślne

**Kody błędów:**
- `401 Unauthorized` - Nieprawidłowe credentials
  ```json
  {
    "error": "Nieprawidłowy email lub hasło"
  }
  ```

**Logika biznesowa:**
1. Wyszukanie użytkownika po email (indeks IX_Users_Email)
2. Weryfikacja hasła (bcrypt verify)
3. Generowanie JWT (ważność 24h, claims: UserId, Email)
4. Zwrot tokenu + danych użytkownika

---

#### **POST /api/auth/logout**

**Opis:** Wylogowanie użytkownika (opcjonalnie: blacklisting tokenu)

**Autoryzacja:** Wymagany JWT w header `Authorization: Bearer <token>`

**Parametry zapytania:** Brak

**Payload żądania:** Brak

**Payload odpowiedzi (sukces):**
```json
{
  "message": "Wylogowano pomyślnie"
}
```

**Kody sukcesu:**
- `200 OK` - Wylogowanie pomyślne

**Kody błędów:**
- `401 Unauthorized` - Brak tokenu lub token nieprawidłowy

**Logika biznesowa:**
1. Po stronie klienta: usunięcie tokenu z localStorage/sessionStorage

---

### 2.2 Moduł: Losowania (Draws)

---

#### **GET /api/draws**

**Opis:** Pobieranie listy wszystkich wyników losowań (globalna tabela) z paginacją i sortowaniem

**Autoryzacja:** Wymagany JWT

**Parametry zapytania:**
- `page` (opcjonalny, domyślnie: 1): Numer strony
- `pageSize` (opcjonalny, domyślnie: 20, max: 100): Liczba elementów na stronie
- `sortBy` (opcjonalny, domyślnie: "drawDate"): Pole sortowania ("drawDate", "createdAt")
- `sortOrder` (opcjonalny, domyślnie: "desc"): Kierunek sortowania ("asc", "desc")

**Przykład:** `GET /api/draws?page=1&pageSize=20&sortBy=drawDate&sortOrder=desc`

**Payload odpowiedzi (sukces):**
```json
{
  "draws": [
    {
      "id": 1,
      "drawDate": "2025-10-30",
      "numbers": [3, 12, 25, 31, 42, 48],
      "createdAt": "2025-10-30T18:30:00Z"
    },
    {
      "id": 2,
      "drawDate": "2025-10-28",
      "numbers": [5, 14, 23, 29, 37, 41],
      "createdAt": "2025-10-28T19:00:00Z"
    }
  ],
  "totalCount": 42,
  "page": 1,
  "pageSize": 20,
  "totalPages": 3
}
```

**Kody sukcesu:**
- `200 OK` - Lista zwrócona

**Kody błędów:**
- `401 Unauthorized` - Brak tokenu lub token nieprawidłowy
- `400 Bad Request` - Nieprawidłowe parametry paginacji
  ```json
  {
    "errors": {
      "pageSize": ["Maksymalna wartość to 100"]
    }
  }
  ```

**Logika biznesowa:**
1. Zapytanie: `SELECT * FROM Draws INNER JOIN DrawNumbers ON Draws.Id = DrawNumbers.DrawId ORDER BY DrawDate DESC OFFSET ... ROWS FETCH NEXT ... ROWS ONLY`
2. Użycie indeksu IX_Draws_DrawDate dla wydajności
3. Eager loading liczb losowania: `.Include(d => d.Numbers)` w EF Core
4. Zwrot listy + metadanych paginacji

---

#### **GET /api/draws/{id}**

**Opis:** Pobieranie szczegółów pojedynczego losowania

**Autoryzacja:** Wymagany JWT

**Parametry URL:**
- `id` (wymagany): ID losowania (INT)

**Przykład:** `GET /api/draws/123`

**Payload odpowiedzi (sukces):**
```json
{
  "id": 123,
  "drawDate": "2025-10-30",
  "numbers": [3, 12, 25, 31, 42, 48],
  "createdAt": "2025-10-30T18:30:00Z"
}
```

**Kody sukcesu:**
- `200 OK` - Losowanie zwrócone

**Kody błędów:**
- `401 Unauthorized` - Brak tokenu
- `404 Not Found` - Losowanie nie istnieje
  ```json
  {
    "error": "Losowanie o podanym ID nie istnieje"
  }
  ```

**Logika biznesowa:**
1. Zapytanie: `SELECT * FROM Draws WHERE Id = @id` z eager loading `.Include(d => d.Numbers)`
2. Brak filtrowania po UserId - Draws jest globalną tabelą dostępną dla wszystkich użytkowników


---

#### **POST /api/draws**

**Opis:** Dodanie nowego wyniku losowania LOTTO

**Autoryzacja:** Wymagany JWT

**Parametry zapytania:** Brak

**Payload żądania:**
```json
{
  "drawDate": "2025-10-30",
  "numbers": [3, 12, 25, 31, 42, 48]
}
```

**Payload odpowiedzi (sukces):**
```json
{
  "message": "Losowanie utworzone pomyślnie"
}
```

**Walidacja:**
- `drawDate`: wymagane, format DATE (YYYY-MM-DD), nie może być w przyszłości, unikalny globalnie
- `numbers`: wymagane, tablica 6 liczb INT
  - Każda liczba w zakresie 1-49 (CHECK constraint)
  - Liczby muszą być unikalne (walidacja backend)

**Kody sukcesu:**
- `201 Created` - Losowanie utworzone

**Kody błędów:**
- `401 Unauthorized` - Brak tokenu
- `400 Bad Request` - Nieprawidłowe dane
  ```json
  {
    "errors": {
      "drawDate": ["Data losowania nie może być w przyszłości", "Losowanie z tą datą już istnieje"],
      "numbers": ["Wymagane dokładnie 6 liczb", "Liczby muszą być unikalne", "Liczby muszą być w zakresie 1-49"]
    }
  }
  ```

**Logika biznesowa:**
1. Pobranie `UserId` z JWT (dla CreatedByUserId)
2. Walidacja:
   - `drawDate` ≤ dzisiejsza data
   - `numbers.length == 6`
   - Wszystkie liczby w zakresie 1-49
   - Liczby unikalne: `numbers.Distinct().Count() == 6`
3. Sprawdzenie czy losowanie na daną datę już istnieje:
   - Jeśli TAK: nadpisanie istniejącego (UPDATE Draws + DELETE/INSERT DrawNumbers)
   - Jeśli NIE: utworzenie nowego
4. Transakcja: INSERT do `Draws` + 6x INSERT do `DrawNumbers` (Position 1-6)
5. Zwrot komunikatu o sukcesie

---

#### **DELETE /api/draws/{id}**

**Opis:** Usunięcie wyniku losowania

**Autoryzacja:** Wymagany JWT (wymaga uprawnień administratora - IsAdmin)

**Parametry URL:**
- `id` (wymagany): ID losowania (INT)

**Przykład:** `DELETE /api/draws/123`

**Payload odpowiedzi (sukces):**
```json
{
  "message": "Losowanie usunięte pomyślnie"
}
```

**Kody sukcesu:**
- `200 OK` - Losowanie usunięte

**Kody błędów:**
- `401 Unauthorized` - Brak tokenu
- `403 Forbidden` - Użytkownik nie ma uprawnień administratora
- `404 Not Found` - Losowanie nie istnieje

**Logika biznesowa:**
1. Pobranie `UserId` z JWT i sprawdzenie flagi `IsAdmin`
2. Jeśli IsAdmin == false: zwróć 403 Forbidden
3. Usunięcie: `DELETE FROM Draws WHERE Id = @id` (CASCADE DELETE usuwa również DrawNumbers)

---

#### **PUT /api/draws/{id}**

**Opis:** Edycja istniejącego wyniku losowania

**Autoryzacja:** Wymagany JWT (wymaga uprawnień administratora - IsAdmin)

**Parametry URL:**
- `id` (wymagany): ID losowania (INT)

**Przykład:** `PUT /api/draws/123`

**Payload żądania:**
```json
{
  "drawDate": "2025-10-30",
  "numbers": [3, 12, 25, 31, 42, 48]
}
```

**Payload odpowiedzi (sukces):**
```json
{
  "message": "Losowanie zaktualizowane pomyślnie"
}
```

**Walidacja:**
- `drawDate`: wymagane, format DATE (YYYY-MM-DD), nie może być w przyszłości
- `numbers`: wymagane, tablica 6 liczb INT
  - Każda liczba w zakresie 1-49 (CHECK constraint)
  - Liczby muszą być unikalne (walidacja backend)

**Kody sukcesu:**
- `200 OK` - Losowanie zaktualizowane

**Kody błędów:**
- `401 Unauthorized` - Brak tokenu
- `403 Forbidden` - Użytkownik nie ma uprawnień administratora
- `404 Not Found` - Losowanie nie istnieje
- `400 Bad Request` - Nieprawidłowe dane
  ```json
  {
    "errors": {
      "drawDate": ["Data losowania nie może być w przyszłości"],
      "numbers": ["Wymagane dokładnie 6 liczb", "Liczby muszą być unikalne", "Liczby muszą być w zakresie 1-49"]
    }
  }
  ```

**Logika biznesowa:**
1. Pobranie `UserId` z JWT i sprawdzenie flagi `IsAdmin`
2. Jeśli IsAdmin == false: zwróć 403 Forbidden
3. Walidacja danych jak w POST /api/draws
4. Sprawdzenie czy losowanie o podanym ID istnieje
5. Transakcja: UPDATE `Draws.DrawDate` + DELETE wszystkich `DrawNumbers` + 6x INSERT nowych `DrawNumbers`
6. Zwrot komunikatu o sukcesie

---

### 2.3 Moduł: Zestawy (Tickets)

---

#### **GET /api/tickets**

**Opis:** Pobieranie listy zestawów liczb użytkownika

**Autoryzacja:** Wymagany JWT

**Parametry zapytania:** BRAK

**Przykład:** `GET /api/tickets`

**Payload odpowiedzi (sukces):**
```json
{
  "tickets": [
    {
      "id": "a3bb189e-8bf9-3888-9912-ace4e6543002",
      "userId": 123,
      "numbers": [5, 14, 23, 29, 37, 41],
      "createdAt": "2025-10-25T10:00:00Z"
    },
    {
      "id": "b2cc289f-9cf0-4999-0023-bde5f7654113",
      "userId": 123,
      "numbers": [3, 12, 18, 25, 31, 44],
      "createdAt": "2025-10-24T15:30:00Z"
    }
  ],
  "totalCount": 42,
  "page": 1,
  "pageSize": 50,
  "totalPages": 1,
  "limit": 100
}
```

**Kody sukcesu:**
- `200 OK` - Lista zwrócona

**Kody błędów:**
- `401 Unauthorized` - Brak tokenu

**Logika biznesowa:**
1. Pobranie `UserId` z JWT
2. Zapytanie: `SELECT * FROM Tickets WHERE UserId = @currentUserId ORDER BY CreatedAt DESC`
3. Użycie indeksu IX_Tickets_UserId
4. Zwrot listy + metadanych (totalCount, limit 100)

---

#### **GET /api/tickets/{id}**

**Opis:** Pobieranie szczegółów pojedynczego zestawu

**Autoryzacja:** Wymagany JWT

**Parametry URL:**
- `id` (wymagany): ID zestawu (int)

**Przykład:** `GET /api/tickets/123`

**Payload odpowiedzi (sukces):**
```json
{
  "id": 123,
  "userId": 123,
  "numbers": [5, 14, 23, 29, 37, 41],
  "createdAt": "2025-10-25T10:00:00Z"
}
```

**Kody sukcesu:**
- `200 OK` - Zestaw zwrócony

**Kody błędów:**
- `401 Unauthorized` - Brak tokenu
- `403 Forbidden` - Zestaw należy do innego użytkownika
- `404 Not Found` - Zestaw nie istnieje

**Logika biznesowa:**
1. Pobranie `UserId` z JWT
2. Zapytanie: `SELECT * FROM Tickets WHERE Id = @id AND UserId = @currentUserId`
3. Jeśli brak: zwróć 403 (nie ujawniaj czy zasób istnieje)

---

#### **POST /api/tickets**

**Opis:** Dodanie nowego zestawu liczb ręcznie

**Autoryzacja:** Wymagany JWT

**Parametry zapytania:** Brak

**Payload żądania:**
```json
{
  "numbers": [5, 14, 23, 29, 37, 41]
}
```

**Walidacja:**
- `numbers`: wymagane, tablica 6 liczb INT
  - Każda liczba w zakresie 1-49 (CHECK constraint)
  - Liczby muszą być unikalne
- Każdy zestaw musi byc unikalny dla użytkownika (walidacja backend):  
- Limit: użytkownik ma <100 zestawów

**Payload odpowiedzi (sukces):**
```json
{
  "message": "Zestaw utworzony pomyślnie"
}
```

**Kody sukcesu:**
- `201 Created` - Zestaw utworzony

**Kody błędów:**
- `401 Unauthorized` - Brak tokenu
- `400 Bad Request` - Nieprawidłowe dane lub limit osiągnięty
  ```json
  {
    "errors": {
      "numbers": ["Wymagane dokładnie 6 liczb", "Liczby muszą być unikalne", "Liczby muszą być w zakresie 1-49", "Zestaw już istnieje"],
      "limit": ["Osiągnięto limit 100 zestawów. Usuń istniejące zestawy, aby dodać nowe."]
    }
  }
  ```

**Logika biznesowa:**
1. Pobranie `UserId` z JWT
2. Walidacja:
   - `numbers.length == 6`
   - Wszystkie liczby w zakresie 1-49
   - Liczby unikalne: `numbers.Distinct().Count() == 6`
3. Sprawdzenie limitu: `SELECT COUNT(*) FROM Tickets WHERE UserId = @userId`
   - Jeśli count ≥ 100: zwróć 400 z komunikatem o limicie
4. Sprawdzenie unikalności zestawu dla użytkownika (algorytm z db-plan.md):
   - Pobranie wszystkich zestawów użytkownika z TicketNumbers
   - Sortowanie liczb w nowym i istniejących zestawach
   - Porównanie: `newNumbersSorted.SequenceEqual(existingNumbersSorted)`
   - Jeśli duplikat: zwróć 400 z komunikatem o istnieniu zestawu
5. Transakcja: INSERT do `Tickets` (generuje auto-increment ID) + 6x INSERT do `TicketNumbers` (Position 1-6)
6. Zwrot komunikatu o sukcesie

---

#### **PUT /api/tickets/{id}**

**Opis:** Edycja istniejącego zestawu liczb

**Autoryzacja:** Wymagany JWT

**Parametry URL:**
- `id` (wymagany): ID zestawu (int)

**Przykład:** `PUT /api/tickets/123`

**Payload żądania:**
```json
{
  "numbers": [7, 15, 22, 33, 38, 45]
}
```

**Walidacja:**
- `numbers`: wymagane, tablica 6 liczb INT
  - Każda liczba w zakresie 1-49 (CHECK constraint)
  - Liczby muszą być unikalne
- Walidacja unikalności po edycji - edytowany zestaw musi pozostać unikalny dla użytkownika (pomijając sam siebie)

**Payload odpowiedzi (sukces):**
```json
{
  "message": "Zestaw zaktualizowany pomyślnie"
}
```

**Kody sukcesu:**
- `200 OK` - Zestaw zaktualizowany

**Kody błędów:**
- `401 Unauthorized` - Brak tokenu
- `403 Forbidden` - Zestaw należy do innego użytkownika
- `404 Not Found` - Zestaw nie istnieje
- `400 Bad Request` - Nieprawidłowe dane
  ```json
  {
    "errors": {
      "numbers": ["Wymagane dokładnie 6 liczb", "Liczby muszą być w zakresie 1-49", "Liczby muszą być unikalne"],
      "duplicate": ["Taki zestaw już istnieje w Twoich zapisanych zestawach"]
    }
  }
  ```

**Logika biznesowa:**
1. Pobranie `UserId` z JWT
2. Sprawdzenie właściciela: `SELECT * FROM Tickets WHERE Id = @id AND UserId = @currentUserId`
   - Jeśli brak: zwróć 403 Forbidden
3. Walidacja:
   - `numbers.length == 6`
   - Wszystkie liczby w zakresie 1-49
   - Liczby unikalne: `numbers.Distinct().Count() == 6`
4. Sprawdzenie unikalności zestawu dla użytkownika (pomijając edytowany zestaw):
   - Pobranie wszystkich zestawów użytkownika z TicketNumbers (z wykluczeniem edytowanego Id)
   - Sortowanie liczb w nowym i istniejących zestawach
   - Porównanie: `newNumbersSorted.SequenceEqual(existingNumbersSorted)`
   - Jeśli duplikat: zwróć 400 z komunikatem o istnieniu zestawu
5. Transakcja: UPDATE `Tickets.UpdatedAt` (jeśli kolumna istnieje) + DELETE wszystkich `TicketNumbers` dla danego TicketId + 6x INSERT nowych `TicketNumbers` (Position 1-6)
6. Zwrot komunikatu o sukcesie

---

#### **DELETE /api/tickets/{id}**

**Opis:** Usunięcie zestawu liczb

**Autoryzacja:** Wymagany JWT

**Parametry URL:**
- `id` (wymagany): ID zestawu (int)

**Przykład:** `DELETE /api/tickets/123`

**Payload odpowiedzi (sukces):**
```json
{
  "message": "Zestaw usunięty pomyślnie"
}
```

**Kody sukcesu:**
- `200 OK` - Zestaw usunięty

**Kody błędów:**
- `401 Unauthorized` - Brak tokenu
- `403 Forbidden` - Zestaw należy do innego użytkownika
- `404 Not Found` - Zestaw nie istnieje

**Logika biznesowa:**
1. Pobranie `UserId` z JWT
2. Sprawdzenie właściciela: `SELECT * FROM Tickets WHERE Id = @id AND UserId = @currentUserId`
3. Usunięcie: `DELETE FROM Tickets WHERE Id = @id`

---

#### **POST /api/tickets/generate-random**

**Opis:** Generowanie pojedynczego losowego zestawu 6 liczb (1-49)

**Autoryzacja:** Wymagany JWT

**Parametry zapytania:** Brak

**Payload żądania:** Brak

**Payload odpowiedzi (sukces):**
```json
{
  "message": "Zestaw wygenerowany pomyślnie"
}
```

**Kody sukcesu:**
- `201 Created` - Zestaw wygenerowany i zapisany

**Kody błędów:**
- `401 Unauthorized` - Brak tokenu
- `400 Bad Request` - Limit osiągnięty
  ```json
  {
    "errors": {
      "limit": ["Osiągnięto limit 100 zestawów. Usuń istniejące zestawy, aby wygenerować nowe."]
    }
  }
  ```

**Logika biznesowa:**
1. Pobranie `UserId` z JWT
2. Sprawdzenie limitu: `SELECT COUNT(*) FROM Tickets WHERE UserId = @userId`
   - Jeśli ≥ 100: zwróć 400
3. **Algorytm generowania:**
   ```csharp
   var random = new Random();
   var numbers = Enumerable.Range(1, 49)
       .OrderBy(x => random.Next())
       .Take(6)
       .OrderBy(x => x) // Sortowanie dla czytelności
       .ToArray();
   ```
4. Zapis do tabeli `Tickets`
5. Zwrot komunikatu o sukcesie

---

#### **POST /api/tickets/generate-system**

**Opis:** Generowanie 9 zestawów pokrywających wszystkie liczby 1-49 (generator systemowy)

**Autoryzacja:** Wymagany JWT

**Parametry zapytania:** Brak

**Payload żądania:** Brak

**Payload odpowiedzi (sukces):**
```json
{
  "message": "9 zestawów wygenerowanych i zapisanych pomyślnie"
}
```

**Kody sukcesu:**
- `201 Created` - 9 zestawów wygenerowanych i zapisanych

**Kody błędów:**
- `401 Unauthorized` - Brak tokenu
- `400 Bad Request` - Brak miejsca na 9 zestawów
  ```json
  {
    "errors": {
      "limit": ["Brak miejsca na 9 zestawów. Dostępne: 3 zestawy. Usuń istniejące zestawy, aby kontynuować."]
    }
  }
  ```

**Logika biznesowa:**
1. Pobranie `UserId` z JWT
2. Sprawdzenie limitu: `SELECT COUNT(*) FROM Tickets WHERE UserId = @userId`
   - Jeśli > 91 (czyli brak miejsca na 9): zwróć 400 z komunikatem o dostępnych miejscach
3. **Algorytm generowania systemowego (z db-plan.md, linie 374-422):**
   ```csharp
   var tickets = new List<int[]>();
   var random = new Random();
   var pool = Enumerable.Range(1, 49).ToList();

   // Inicjalizacja 9 zestawów
   for (int i = 0; i < 9; i++) {
       tickets.Add(new int[6]);
   }

   // Rozdziel 49 liczb w 9 zestawach
   int ticketIndex = 0;
   int positionIndex = 0;

   foreach (var number in pool.OrderBy(x => random.Next())) {
       tickets[ticketIndex][positionIndex] = number;

       positionIndex++;
       if (positionIndex == 6) {
           positionIndex = 0;
           ticketIndex++;
           if (ticketIndex == 9) ticketIndex = 0;
       }
   }

   // Dopełnij 5 pustych pozycji losowo
   for (int i = 0; i < 9; i++) {
       for (int j = 0; j < 6; j++) {
           if (tickets[i][j] == 0) {
               tickets[i][j] = pool[random.Next(pool.Count)];
           }
       }

       // Sortowanie dla czytelności
       Array.Sort(tickets[i]);
   }
   ```
4. Zapis 9 zestawów do tabeli `Tickets` (bulk insert)
5. Zwrot komunikatu o sukcesie

---

### 2.4 Moduł: Weryfikacja Wygranych (Verification)

---

#### **POST /api/verification/check**

**Opis:** Weryfikacja zestawów użytkownika względem wyników losowań w wybranym zakresie dat

**Autoryzacja:** Wymagany JWT

**Parametry zapytania:** Brak

**Payload żądania:**
```json
{
  "dateFrom": "2025-10-01",
  "dateTo": "2025-10-30"
}
```

**Walidacja:**
- `dateFrom`: wymagane, format DATE (YYYY-MM-DD)
- `dateTo`: wymagane, format DATE, musi być ≥ `dateFrom`
- Zakres dat nie może przekraczać 31 dni (NFR-005)

**Payload odpowiedzi (sukces):**
```json
{
  "results": [
    {
      "ticketId": "a3bb189e-8bf9-3888-9912-ace4e6543002",
      "numbers": [3, 12, 25, 31, 42, 48],
      "draws": [
        {
          "drawId": 123,
          "drawDate": "2025-10-28",
          "drawNumbers": [12, 18, 25, 31, 40, 49],
          "matchCount": 3,
          "matchedNumbers": [12, 25, 31]
        }
      ]
    },
    {
      "ticketId": "b2cc289f-9cf0-4999-0023-bde5f7654113",
      "numbers": [1, 2, 3, 4, 5, 6],
      "draws": []
    }
  ],
  "totalTickets": 42,
  "totalDraws": 8,
  "executionTimeMs": 1234
}
```

**Kody sukcesu:**
- `200 OK` - Weryfikacja zakończona

**Kody błędów:**
- `401 Unauthorized` - Brak tokenu
- `400 Bad Request` - Nieprawidłowe daty
  ```json
  {
    "errors": {
      "dateFrom": ["Wymagany format YYYY-MM-DD"],
      "dateTo": ["Data 'dateTo' musi być późniejsza lub równa 'dateFrom'", "Zakres dat nie może przekraczać 31 dni"]
    }
  }
  ```

**Logika biznesowa (algorytm dostosowany do znormalizowanej struktury):**

1. Pobranie `UserId` z JWT
2. Timestamp start (dla executionTimeMs)
3. **Faza 1: Pobranie danych z bazy**
   ```csharp
   var tickets = await db.Tickets
       .Where(t => t.UserId == userId)
       .Include(t => t.Numbers) // Eager loading TicketNumbers
       .ToListAsync(); // np. 100 zestawów

   var draws = await db.Draws
       .Where(d => d.DrawDate >= dateFrom && d.DrawDate <= dateTo)
       .Include(d => d.Numbers) // Eager loading DrawNumbers
       .ToListAsync(); // np. 8 losowań
   ```
   - Użycie indeksów: IX_Tickets_UserId, IX_Draws_DrawDate
   - Draws nie filtrowany po UserId - globalna tabela dostępna dla wszystkich

4. **Faza 2: Weryfikacja w pamięci (LINQ Intersect)**
   ```csharp
   var results = new List<VerificationResult>();

   foreach (var ticket in tickets) {
       var ticketNumbers = ticket.Numbers
           .OrderBy(n => n.Position)
           .Select(n => n.Number)
           .ToArray(); // 6 liczb

       var ticketDraws = new List<DrawMatch>();

       foreach (var draw in draws) {
           var drawNumbers = draw.Numbers
               .OrderBy(n => n.Position)
               .Select(n => n.Number)
               .ToArray(); // 6 liczb

           // Intersect: liczby wspólne
           var matches = ticketNumbers.Intersect(drawNumbers).ToArray();
           var matchCount = matches.Length;

           if (matchCount >= 3) { // Minimalna wygrana
               ticketDraws.Add(new DrawMatch {
                   DrawId = draw.Id,
                   DrawDate = draw.DrawDate,
                   DrawNumbers = drawNumbers,
                   MatchCount = matchCount,
                   MatchedNumbers = matches
               });
           }
       }

       results.Add(new VerificationResult {
           TicketId = ticket.Id,
           Numbers = ticketNumbers,
           Draws = ticketDraws
       });
   }
   ```

5. Timestamp end (obliczenie executionTimeMs)
6. Zwrot wyników + metadanych (totalTickets, totalDraws, executionTimeMs)

**Wydajność:**
- Wymaganie NFR-001: 100 zestawów × 1 losowanie ≤ 2 sekundy
- Złożoność: O(tickets × draws × 6) ≈ O(n × m)
- Dla 100 × 8: ~4800 operacji Intersect
- Optymalizacja (jeśli potrzebna): `Parallel.ForEach` dla dużych zbiorów

---

## 3. Uwierzytelnianie i Autoryzacja

### 3.1 Mechanizm: JWT (JSON Web Tokens)

**Implementacja:**
- Biblioteka: `System.IdentityModel.Tokens.Jwt` (ASP.NET Core)
- Algorytm: HMAC SHA-256 (HS256)
- Secret key: Przechowywany w MVP w `appsettings.json`, szyfrowana base64

**Struktura tokenu JWT:**
```json
{
  "header": {
    "alg": "HS256",
    "typ": "JWT"
  },
  "payload": {
    "userId": 123,
    "email": "user@example.com",
    "exp": 1730638800,
    "iat": 1730552400
  },
  "signature": "..."
}
```

**Claims w tokenie:**
- `userId` (lub `sub`): ID użytkownika (INT)
- `email`: Email użytkownika (dla wyświetlenia w UI)
- `isAdmin`: Flaga uprawnień administratora (BOOL)
- `exp`: Expiration time (Unix timestamp, 24 godziny od `iat`)
- `iat`: Issued at (Unix timestamp)

**Ważność tokenu:**
- 24 godziny (1440 minut)
- Konfiguracja: `appsettings.json` → `JwtSettings:ExpiryInMinutes`

**Użycie tokenu:**
- Header każdego zapytania (poza `/api/auth/register` i `/api/auth/login`):
  ```
  Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
  ```

**Walidacja tokenu (middleware):**
1. Sprawdzenie obecności tokenu w header
2. Weryfikacja podpisu (HMAC SHA-256 z secret key)
3. Sprawdzenie czasu wygaśnięcia (`exp` > current time)
4. Dodanie claims do `HttpContext.User`

**Odświeżanie tokenu (post-MVP):**
- MVP: Brak refresh tokens - użytkownik loguje się ponownie po wygaśnięciu


### 3.2 Haszowanie haseł

**Algorytm:** bcrypt (lub PBKDF2)

**Implementacja:**
- Biblioteka: `BCrypt.Net-Next` (NuGet)
- Work factor: 10 rounds (minimum, rekomendacja: 12)

**Przykład:**
```csharp
// Przy rejestracji
var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 10);

// Przy logowaniu
var isValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
```

**Przechowywanie:**
- Kolumna: `Users.Password` (NVARCHAR(256))
- Hash bcrypt: 60 znaków (np. `$2a$10$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy`)

### 3.3 Zabezpieczenia dodatkowe

**HTTPS (NFR-007):**
- Wymagane na produkcji
- Hosting: Azure App Service / Webio.pl zapewniają SSL/TLS
- Middleware: `app.UseHttpsRedirection()`

**CORS:**
- Konfiguracja dozwolonych origin dla frontendu React
- Przykład:
  ```csharp
  services.AddCors(options => {
      options.AddPolicy("AllowFrontend", builder => {
          builder.WithOrigins("https://lottotm.netlify.app")
                 .AllowAnyMethod()
                 .AllowAnyHeader()
                 .AllowCredentials();
      });
  });
  ```

**Secure Headers:**
```csharp
app.Use(async (context, next) => {
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Add("Content-Security-Policy", "default-src 'self'");
    await next();
});
```

---

## 4. Walidacja i Logika Biznesowa

### 4.1 Warunki walidacji dla każdego zasobu

#### **Users (Rejestracja/Logowanie)**

| Pole | Walidacja | Komunikat błędu |
|------|-----------|----------------|
| `email` | - Wymagane<br>- Format email (regex)<br>- Unikalny w systemie | - "Email jest wymagany"<br>- "Nieprawidłowy format email"<br>- "Email jest już zajęty" |
| `password` | - Wymagane<br>- Min. 8 znaków<br>- (Rekomendacja: 1 wielka litera, 1 cyfra, 1 znak specjalny) | - "Hasło jest wymagane"<br>- "Hasło musi mieć min. 8 znaków"<br>- "Hasło musi zawierać wielką literę, cyfrę i znak specjalny" |
| `confirmPassword` | - Wymagane<br>- Identyczne z `password` | - "Potwierdzenie hasła jest wymagane"<br>- "Hasła nie są identyczne" |

**Implementacja (FluentValidation):**
```csharp
public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email jest wymagany")
            .EmailAddress().WithMessage("Nieprawidłowy format email");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Hasło jest wymagane")
            .MinimumLength(8).WithMessage("Hasło musi mieć min. 8 znaków")
            .Matches(@"[A-Z]").WithMessage("Hasło musi zawierać wielką literę")
            .Matches(@"[0-9]").WithMessage("Hasło musi zawierać cyfrę")
            .Matches(@"[\W_]").WithMessage("Hasło musi zawierać znak specjalny");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Potwierdzenie hasła jest wymagane")
            .Equal(x => x.Password).WithMessage("Hasła nie są identyczne");
    }
}
```

---

#### **Draws (Wyniki losowań)**

| Pole | Walidacja | Komunikat błędu |
|------|-----------|----------------|
| `drawDate` | - Wymagane<br>- Format DATE (YYYY-MM-DD)<br>- Nie może być w przyszłości<br>- Unikalny globalnie | - "Data losowania jest wymagana"<br>- "Nieprawidłowy format daty"<br>- "Data losowania nie może być w przyszłości"<br>- "Losowanie z tą datą już istnieje" |
| `numbers` | - Wymagane<br>- Tablica 6 liczb INT<br>- Każda liczba w zakresie 1-49 (CHECK constraint)<br>- Liczby unikalne | - "Wymagane dokładnie 6 liczb"<br>- "Liczby muszą być w zakresie 1-49"<br>- "Liczby muszą być unikalne" |

**Implementacja (FluentValidation):**
```csharp
public class DrawRequestValidator : AbstractValidator<DrawRequest>
{
    public DrawRequestValidator()
    {
        RuleFor(x => x.DrawDate)
            .NotEmpty().WithMessage("Data losowania jest wymagana")
            .LessThanOrEqualTo(DateTime.Today).WithMessage("Data losowania nie może być w przyszłości");

        RuleFor(x => x.Numbers)
            .NotEmpty().WithMessage("Liczby są wymagane")
            .Must(n => n.Count == 6).WithMessage("Wymagane dokładnie 6 liczb")
            .Must(n => n.All(num => num >= 1 && num <= 49)).WithMessage("Liczby muszą być w zakresie 1-49")
            .Must(n => n.Distinct().Count() == 6).WithMessage("Liczby muszą być unikalne");
    }
}
```

**Walidacja unikalności daty (backend logic):**
```csharp
var exists = await db.Draws
    .Where(d => d.DrawDate == request.DrawDate)
    .AnyAsync();

// Draws jest globalny, więc nie filtrujemy po UserId
// Przy POST: jeśli exists, możemy nadpisać (UPDATE) lub zwrócić błąd (decyzja biznesowa)
if (exists)
    return BadRequest(new { errors = new { drawDate = new[] { "Losowanie z tą datą już istnieje" } } });
```

---

#### **Tickets (Zestawy liczb)**

| Pole | Walidacja | Komunikat błędu |
|------|-----------|----------------|
| `numbers` | - Wymagane<br>- Tablica 6 liczb INT<br>- Każda liczba w zakresie 1-49 (CHECK constraint)<br>- Liczby unikalne | - "Wymagane dokładnie 6 liczb"<br>- "Liczby muszą być w zakresie 1-49"<br>- "Liczby muszą być unikalne" |
| **Limit** | - Użytkownik ma <100 zestawów | - "Osiągnięto limit 100 zestawów. Usuń istniejące zestawy, aby dodać nowe." |

**Implementacja (FluentValidation + backend logic):**
```csharp
public class TicketRequestValidator : AbstractValidator<TicketRequest>
{
    public TicketRequestValidator()
    {
        RuleFor(x => x.Numbers)
            .NotEmpty().WithMessage("Liczby są wymagane")
            .Must(n => n.Count == 6).WithMessage("Wymagane dokładnie 6 liczb")
            .Must(n => n.All(num => num >= 1 && num <= 49)).WithMessage("Liczby muszą być w zakresie 1-49")
            .Must(n => n.Distinct().Count() == 6).WithMessage("Liczby muszą być unikalne");
    }
}

// Walidacja limitu (backend logic)
var count = await db.Tickets.CountAsync(t => t.UserId == currentUserId);
if (count >= 100)
    return BadRequest(new { errors = new { limit = new[] { "Osiągnięto limit 100 zestawów. Usuń istniejące zestawy, aby dodać nowe." } } });
```

---

### 4.2 Implementacja logiki biznesowej w API

#### **4.2.1 Izolacja danych użytkowników (F-AUTH-004)**

**Wymaganie:** Każdy użytkownik ma dostęp tylko do swoich danych tickets

**Implementacja:** Filtrowanie po `UserId` z JWT tokenu w każdym endpoincie

**Przykład:**
```csharp
// Pobieranie UserId z JWT claims
var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

// Filtrowanie tickets
var tickets = await db.Tickets
    .Where(t => t.UserId == currentUserId)
    .ToListAsync();

// Sprawdzanie właściciela przy GET/PUT/DELETE pojedynczego zasobu
var ticket = await db.Tickets
    .Where(t => t.Id == ticketId && t.UserId == currentUserId)
    .FirstOrDefaultAsync();

if (ticket == null)
    return Forbid(); // 403 Forbidden (nie 404 - security by obscurity)
```

**Zabezpieczenie przed nieautoryzowanym dostępem:**
- **Zawsze** sprawdzaj `UserId` przy operacjach na pojedynczych zasobach (GET/PUT/DELETE)
- **Nigdy** nie zwracaj 404 jeśli zasób istnieje ale należy do innego użytkownika
- **Zawsze** zwracaj 403 Forbidden w takim przypadku

---

#### **4.2.2 Limit 100 zestawów (F-TICKET-002, F-TICKET-005, F-TICKET-006)**

**Wymaganie:** Każdy użytkownik może mieć maksymalnie 100 zestawów

**Implementacja:** Sprawdzenie limitu przed zapisem

**Przykład (POST /api/tickets):**
```csharp
var count = await db.Tickets.CountAsync(t => t.UserId == currentUserId);

if (count >= 100) {
    return BadRequest(new {
        errors = new {
            limit = new[] { "Osiągnięto limit 100 zestawów. Usuń istniejące zestawy, aby dodać nowe." }
        }
    });
}
```

**Przykład (POST /api/tickets/generate-system):**
```csharp
var count = await db.Tickets.CountAsync(t => t.UserId == currentUserId);
var available = 100 - count;

if (available < 9) {
    return BadRequest(new {
        errors = new {
            limit = new[] { $"Brak miejsca na 9 zestawów. Dostępne: {available} zestawy. Usuń istniejące zestawy, aby kontynuować." }
        }
    });
}
```

---

#### **4.2.3 Algorytm weryfikacji wygranych (F-VERIFY-001)**

**Wymaganie:** Porównanie zestawów użytkownika z losowaniami w zakresie dat, wydajność ≤2s dla 100 zestawów × 1 losowanie

**Implementacja (2-fazowa):**

**Faza 1: Pobranie danych z bazy (optymalizacja: indeksy)**
```csharp
var tickets = await db.Tickets
    .Where(t => t.UserId == currentUserId)
    .ToListAsync(); // Indeks IX_Tickets_UserId

var draws = await db.Draws
    .Where(d => d.DrawDate >= dateFrom && d.DrawDate <= dateTo && d.UserId == currentUserId)
    .ToListAsync(); // Indeks IX_Draws_DrawDate
```

**Faza 2: Weryfikacja w pamięci (LINQ Intersect)**
```csharp
var results = new List<VerificationResult>();

foreach (var ticket in tickets) {
    var ticketNumbers = new[] {
        ticket.Number1, ticket.Number2, ticket.Number3,
        ticket.Number4, ticket.Number5, ticket.Number6
    };

    var ticketDraws = new List<DrawMatch>();

    foreach (var draw in draws) {
        var drawNumbers = new[] {
            draw.Number1, draw.Number2, draw.Number3,
            draw.Number4, draw.Number5, draw.Number6
        };

        // Intersect: liczby wspólne
        var matches = ticketNumbers.Intersect(drawNumbers).ToArray();
        var matchCount = matches.Length;

        if (matchCount >= 3) { // Minimalna wygrana (trójka)
            ticketDraws.Add(new DrawMatch {
                DrawId = draw.Id,
                DrawDate = draw.DrawDate,
                DrawNumbers = drawNumbers,
                MatchCount = matchCount,
                MatchedNumbers = matches
            });
        }
    }

    results.Add(new VerificationResult {
        TicketId = ticket.Id,
        Numbers = ticketNumbers,
        Draws = ticketDraws
    });
}
```

**Optymalizacja (jeśli potrzebna):**
```csharp
// Parallel.ForEach dla dużych zbiorów
Parallel.ForEach(tickets, ticket => {
    // ... weryfikacja
});
```

**Cache (post-MVP, jeśli weryfikacja > 2s):**
- Redis cache dla wyników weryfikacji
- Klucz: `verification:{userId}:{dateFrom}:{dateTo}`
- TTL: 24 godziny
- Invalidacja: przy dodaniu/edycji/usunięciu tickets lub draws

---

#### **4.2.4 Generator systemowy (F-TICKET-006)**

**Wymaganie:** Wygenerowanie 9 zestawów pokrywających wszystkie liczby 1-49, każda liczba min. 1 raz

**Algorytm (z db-plan.md, linie 374-422):**

```csharp
public List<int[]> GenerateSystemTickets()
{
    var tickets = new List<int[]>();
    var random = new Random();
    var pool = Enumerable.Range(1, 49).ToList(); // [1..49]

    // Krok 1: Inicjalizacja 9 zestawów
    for (int i = 0; i < 9; i++) {
        tickets.Add(new int[6]);
    }

    // Krok 2: Rozdziel 49 liczb w 9 zestawach
    int ticketIndex = 0;
    int positionIndex = 0;

    foreach (var number in pool.OrderBy(x => random.Next())) {
        tickets[ticketIndex][positionIndex] = number;

        positionIndex++;
        if (positionIndex == 6) {
            positionIndex = 0;
            ticketIndex++;
            if (ticketIndex == 9) ticketIndex = 0;
        }
    }

    // Krok 3: Dopełnij 5 pustych pozycji losowo
    for (int i = 0; i < 9; i++) {
        for (int j = 0; j < 6; j++) {
            if (tickets[i][j] == 0) {
                tickets[i][j] = pool[random.Next(pool.Count)];
            }
        }

        // Sortowanie dla czytelności
        Array.Sort(tickets[i]);
    }

    return tickets;
}
```

**Walidacja algorytmu (testy jednostkowe):**
- Każdy zestaw ma dokładnie 6 liczb
- Każda liczba 1-49 pojawia się minimum raz w 9 zestawach
- Liczby w zestawie są unikalne
- 9 zestawów × 6 liczb = 54 pozycje (49 unikalnych + 5 powtórzeń)

---

## 5. Podsumowanie

### 5.1 Kompletny wykaz endpointów

| Metoda | Endpoint | Opis | Autoryzacja |
|--------|----------|------|-------------|
| POST | `/api/auth/register` | Rejestracja + automatyczne logowanie | Nie |
| POST | `/api/auth/login` | Logowanie | Nie |
| POST | `/api/auth/logout` | Wylogowanie | Tak |
| GET | `/api/draws` | Lista wszystkich wyników losowań (globalna) | Tak |
| GET | `/api/draws/{id}` | Szczegóły losowania | Tak |
| POST | `/api/draws` | Dodanie wyniku losowania (admin) | Tak (IsAdmin) |
| PUT | `/api/draws/{id}` | Edycja wyniku losowania (admin) | Tak (IsAdmin) |
| DELETE | `/api/draws/{id}` | Usunięcie losowania (admin) | Tak (IsAdmin) |
| GET | `/api/tickets` | Lista zestawów użytkownika | Tak |
| GET | `/api/tickets/{id}` | Szczegóły zestawu (int) | Tak |
| POST | `/api/tickets` | Dodanie zestawu ręcznie | Tak |
| PUT | `/api/tickets/{id}` | Edycja zestawu (int) | Tak |
| DELETE | `/api/tickets/{id}` | Usunięcie zestawu (int) | Tak |
| POST | `/api/tickets/generate-random` | Generowanie losowego zestawu | Tak |
| POST | `/api/tickets/generate-system` | Generowanie 9 zestawów systemowych | Tak |
| POST | `/api/verification/check` | Weryfikacja wygranych | Tak |

### 5.2 Kluczowe decyzje projektowe

1. **JWT w localStorage** (nie w cookies) - prostsze dla MVP, CORS policies wystarczą
2. **Draws globalny** - Tabela Draws jest współdzielona przez wszystkich użytkowników, z kolumną CreatedByUserId tracking kto wprowadził wynik
3. **Znormalizowana struktura** - DrawNumbers i TicketNumbers zamiast Number1-Number6 dla lepszej elastyczności
4. **Tickets.Id jako INT** - Auto-increment integer dla prostoty i wydajności w MVP
5. **Opcja C dla edycji** - nadpisanie bez historii weryfikacji (db-plan.md)
6. **Weryfikacja on-demand** - brak tabeli TicketVerifications w MVP
7. **Paginacja dla list** - wszystkie endpointy GET zwracające listy (draws, tickets)
8. **IsAdmin flaga** - uprawnienia administratora dla zarządzania losowaniami (dodawanie/edycja/usuwanie)
9. **Indeksy** - IX_Users_Email, IX_Tickets_UserId, IX_Draws_DrawDate, IX_DrawNumbers_DrawId, IX_TicketNumbers_TicketId dla wydajności
10. **Walidacja w 2 warstwach** - FluentValidation (request DTOs) + logika biznesowa (limity, unikalność)
11. **Algorytm weryfikacji** - LINQ Intersect w pamięci (faza 2) po eager loading z DrawNumbers/TicketNumbers, indeksy dla fazy 1

### 5.3 Wymagania bezpieczeństwa (implementowane)

- ✅ Hasła jako bcrypt hash (min. 10 rounds) - NFR-005
- ✅ JWT z czasem wygaśnięcia (24h) - NFR-006
- ✅ HTTPS wymagane (produkcja) - NFR-007
- ✅ Parametryzowane zapytania (EF Core) - NFR-008
- ✅ CORS policies - NFR-009
- ✅ Izolacja danych (UserId filtering) - F-AUTH-004
- ✅ Secure headers (X-Frame-Options, CSP)

### 5.4 Wymagania wydajności (implementowane)

- ✅ Weryfikacja ≤ 2s (100 zestawów × 1 losowanie) - NFR-001
- ✅ CRUD ≤ 500ms (95 percentyl) - NFR-002 - indeksy IX_Tickets_UserId, IX_Draws_DrawDate
- ✅ Paginacja - zapobiega przeciążeniu przy dużych listach

---

**Koniec dokumentu REST API Plan**
