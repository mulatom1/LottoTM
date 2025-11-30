Jako analityk w Software House, przeanalizowałem dostarczoną dokumentację projektu **LottoTM MVP**. Poniżej przedstawiam szczegółowy raport dotyczący spójności logicznej, wykonalności technicznej oraz potencjalnych ryzyk.

### **Podsumowanie Wykonawcze**

Dokumentacja jest **na bardzo wysokim poziomie szczegółowości**. Jest spójna, logiczna i gotowa do przekazania zespołowi deweloperskiemu. Decyzja o zastosowaniu **Vertical Slice Architecture** w połączeniu z **Minimal APIs** i **React 19** jest nowoczesna i adekwatna do skali projektu.

Kluczowym elementem, który „ratuje” ten projekt przed problemami wydajnościowymi przy przyjętych prostych algorytmach (np. weryfikacja w pamięci, sprawdzanie duplikatów), jest **sztywny limit 100 zestawów na użytkownika**. Dzięki temu ograniczeniu, rozwiązania, które nie skalowałyby się przy 100 000 rekordów, tutaj będą działać błyskawicznie.

---

### **1. Analiza Architektury i Logiki Biznesowej**

#### **✅ Mocne strony:**
1.  **Architektura Vertical Slice:** Zastosowanie tego wzorca (Feature folder zawiera Controller, Model, Validator, Handler) drastycznie ułatwi utrzymanie kodu i testowanie. To świetna decyzja dla .NET 8.
2.  **Model Danych (Normalizacja):** Decyzja o wydzieleniu `TicketNumbers` i `DrawNumbers` (zamiast kolumn `Num1`...`Num6`) jest poprawna z punktu widzenia zasad bazodanowych. Ułatwia to zapytania typu "ile razy padła liczba 7".
3.  **Strategia Weryfikacji (On-Demand):** Rezygnacja z zapisywania wyników weryfikacji w bazie (`TicketVerifications`) na rzecz obliczania ich w locie jest **doskonałą decyzją dla MVP**. Przy limicie 100 zestawów, algorytm `Intersect` w pamięci RAM serwera zajmie milisekundy, a oszczędza to skomplikowanej logiki inwalidacji cache'u przy edycji kuponów.
4.  **Obsługa "Nowych" losowań:** Rozwiązanie oparte na `localStorage` po stronie frontendu (porównanie dat) zamiast flag w bazie danych to podręcznikowy przykład pragmatyzmu w MVP. Oszczędza to masę pracy backendowej.

#### **⚠️ Obszary ryzyka i uwagi logiczne:**

1.  **Walidacja unikalności zestawów (Backend):**
    *   *Logika:* Pobranie wszystkich zestawów użytkownika z bazy, posortowanie ich w pamięci i porównanie z nowym zestawem.
    *   *Ocena:* Przy limicie 100 zestawów jest to akceptowalne.
    *   *Ryzyko:* Jeśli limit zostanie zwiększony (np. do 1000), ta metoda stanie się wąskim gardłem.
    *   *Rekomendacja:* W fazie post-MVP warto rozważyć kolumnę `Hash` (skrót posortowanych liczb), co pozwoliłoby na sprawdzenie unikalności jednym szybkim zapytaniem SQL/indeksem.

2.  **Generator Systemowy (Logika):**
    *   *Dokumentacja:* Algorytm generuje 9 zestawów. 9 * 6 = 54 liczby. Mamy liczby 1-49. To oznacza 5 powtórzeń.
    *   *Analiza:* Algorytm w `tickets-generate-system.md` jest poprawny logicznie (losowe rozmieszczenie + dopełnienie). Jest to funkcja "stateless" (nie zapisuje do bazy), co jest bezpieczne.

3.  **LottoWorker (Background Service):**
    *   *Logika:* Okno czasowe 22:15-23:00.
    *   *Ryzyko:* Co się stanie, jeśli oficjalne API Lotto będzie miało awarię w tym oknie? Worker spróbuje ponownie następnego dnia?
    *   *Rekomendacja:* Warto dodać logikę "catch-up" przy starcie aplikacji, która sprawdza, czy wczorajsze wyniki są w bazie, a jeśli nie – próbuje je pobrać niezależnie od okna czasowego.

---

### **2. Analiza Wykonalności Technicznej (Feasibility)**

#### **Backend (.NET 8 + EF Core)**
*   **Wykonalność:** Bardzo wysoka. Planowane użycie `MediatR` i `FluentValidation` to standard przemysłowy.
*   **Wydajność:** Wymaganie NFR-001 (weryfikacja < 2s) jest trywialne do spełnienia przy założonym wolumenie danych. Eager loading (`.Include`) w EF Core zapobiegnie problemowi N+1 zapytań.
*   **Bezpieczeństwo:**
    *   Endpoint `PUT /api/auth/setadmin` jest oznaczony jako tymczasowy i niebezpieczny. **Krytyczne jest, aby nie trafił na produkcję w tej formie bez zabezpieczenia (np. tylko z localhost lub tajnym kluczem w headerze).**
    *   Izolacja danych (filtrowanie po `UserId`) jest uwzględniona w każdym handlerze. To kluczowe.

#### **Frontend (React 19)**
*   **Wykonalność:** Wysoka. Brak skomplikowanego stanu globalnego (tylko AuthContext) upraszcza sprawę.
*   **UI/UX:** Rezygnacja z Dashboardu na rzecz przekierowania do `/tickets` to dobre uproszczenie.
*   **Interakcje:** Logika `DrawFormModal` (admin) z dynamicznym zaciąganiem danych z XLotto/Gemini jest najbardziej złożonym elementem UI, ale dobrze opisanym.

#### **Integracje Zewnętrzne (XLotto & Lotto OpenApi)**
*   **Dwoistość rozwiązania:** Mamy dwie ścieżki – `XLottoService` (HTML scraping + Gemini AI) dla admina "na żądanie" oraz `LottoOpenApiService` (oficjalne API) dla workera w tle.
*   *Ocena:* To jest **świetna redundancja**. Jeśli oficjalne API padnie, admin może użyć XLotto. Jeśli XLotto zmieni strukturę HTML, worker nadal działa na API. Użycie LLM (Gemini) do parsowania HTML to nowoczesne podejście, które jest bardziej odporne na drobne zmiany w DOM niż klasyczne selektory CSS/XPath.

---

### **3. Analiza Danych (Database Schema)**

*   **Tabela `Draws`:**
    *   Constraint `UNIQUE (DrawDate, LottoType)` jest poprawny.
    *   Brak relacji `Many-to-Many` między `Draws` a `Tickets` (weryfikacja w locie) drastycznie upraszcza schemat.
*   **Tabela `Users`:**
    *   Przechowywanie hasła jako Hash (BCrypt) – standard.
    *   `IsAdmin` jako flaga w tabeli – proste i skuteczne dla MVP.

---

### **4. Identyfikacja Luk i Ryzyk (Gap Analysis)**

1.  **Race Conditions przy limicie kuponów:**
    *   *Scenariusz:* Użytkownik ma 99 kuponów. Otwiera dwie zakładki przeglądarki. W obu klika "Dodaj". Frontend przepuści oba żądania.
    *   *Backend:* Handler sprawdza `Count()` a potem robi `Insert`. To nie jest atomowe (chyba że izolacja transakcji jest Serializable, co zabija wydajność).
    *   *Wpływ:* Użytkownik może skończyć z 101 kuponami.
    *   *Ocena:* Dla MVP jest to **ryzyko akceptowalne**. Koszt naprawy (locki na bazie) przewyższa zysk.

2.  **Import CSV a Unikalność:**
    *   Plan zakłada `Batch Insert`. Jeśli w pliku CSV na 50 rekordów jeden jest duplikatem, czy odrzucamy cały plik, czy tylko ten jeden rekord?
    *   *Dokumentacja mówi:* "Raport z importu: imported 15, rejected 2". Sugeruje to podejście "best effort" (zapisz co się da). Jest to trudniejsze w implementacji transakcyjnej (wymaga albo wielu małych transakcji, albo skomplikowanej logiki savepointów/walidacji wstępnej).
    *   *Rekomendacja:* Walidacja wszystkich rekordów w pamięci **przed** otwarciem transakcji zapisu. Jeśli user widzi raport błędów, może poprawić plik i spróbować ponownie, albo zaakceptować import tylko poprawnych. Obecny plan w `tickets-import-csv.md` sugeruje walidację w pętli i dodawanie do listy `ticketsToImport` tylko poprawnych, a potem zapis w jednej transakcji. To jest **poprawne podejście**.

3.  **Usuwanie Konta:**
    *   Brakuje funkcjonalności usuwania konta przez użytkownika (RODO/GDPR). W MVP można to pominąć, ale warto mieć to na uwadze.

---

### **5. Ocena Planu Testów**

*   Plan testów jest bardzo solidny. Pokrywa happy path, edge cases oraz (co rzadkie) testy izolacji danych (czy User A widzi dane Usera B).
*   Testy jednostkowe algorytmów (np. generator systemowy, walidacja unikalności) są kluczowe i zostały uwzględnione.

---

### **Werdykt Końcowy**

Jako analityk, **zatwierdzam dokumentację do realizacji**.

Jest to jeden z najlepiej przygotowanych zestawów dokumentacji dla projektu MVP, jaki widziałem.
*   **Logika:** Spójna.
*   **Technologia:** Dobrze dobrana.
*   **Ryzyka:** Zidentyfikowane i zmitygowane (np. limit 100 kuponów rozwiązuje problemy wydajnościowe).
*   **Detale:** Bardzo wysoki poziom szczegółowości (aż do nazw klas CSS i payloadów JSON).

Zespół deweloperski może przystąpić do pracy bezpośrednio na podstawie tych plików bez konieczności długich sesji wyjaśniających (refinement).