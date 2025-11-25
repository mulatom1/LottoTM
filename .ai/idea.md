### **System Wspomagający Gracza: LottoTM - Wersja MVP**

**Wizja produktu:** Stworzenie intuicyjnego systemu dla graczy LOTTO, który ułatwi zarządzanie wieloma zestawami liczb i wskaże wygrane, zwiększając komfort i efektywność gry.

#### **Główny problem, który rozwiązujemy:**

Wielu graczy LOTTO posiada liczne, często zróżnicowane zestawy liczb. Ręczne sprawdzanie każdego z nich po losowaniu jest procesem czasochłonnym, monotonnym i podatnym na błędy, co może prowadzić do przeoczenia wygranej. Aplikacja LottoTM ma na celu całkowitą eliminację tego problemu.

---

#### **Podstawowe funkcje systemu (MVP):**

**Zarządzanie kontem użytkownika:**
*   **Logowanie i wylogowywanie:** Bezpieczny dostęp do systemu dla zarejestrowanych użytkowników.
*   **Izolacja danych:** Każdy zalogowany użytkownik ma dostęp wyłącznie do swojego prywatnego zbioru zestawów liczb.

---

**Obsługa losowań:**
*   **Wprowadzanie wyników:**
*    - Użytkownik jako administrator może ręcznie wprowadzić oficjalne wyniki losowania dla konkretnej daty i typu gry w celu ich weryfikacji.
*    - **Integracja z XLotto** - użytkownik może ściągnąć wyniki przed zapisem z oficjalnej strony XLotto (po ekstrakcji danych przy pomocy LLM - dostępne jako Feature Flag).
*    - **Integracja z Lotto OpenApi** - system automatycznie może ściągnąć wyniki w danym przedziale czasowym i zapisać z oficjalnego API LOTTO (dostępne jako Feature Flag):
        - **Background Worker** (`LottoWorker`) działa w oknie czasowym 22:15-23:00
        - Automatyczne pobieranie wyników LOTTO i LOTTO PLUS z https://www.lotto.pl        
        - Feature Flag: `LottoWorker:Enable` (true/false)

---

**Zarządzanie kuponami (zestawami liczb):**
*   **Przeglądanie zestawów:** Czytelna lista wszystkich zapisanych przez użytkownika zestawów.
*   **Dodawanie ręczne:** Możliwość manualnego wprowadzenia nowego zestawu liczb.
*   **Generator zestawów:**
    *   **Pojedynczy zestaw losowy:** Automatyczne generowanie nowego, unikalnego zestawu liczb.
    *   **Generowanie "systemowe":** Tworzenie do 9 zestawów, które łącznie pokrywają wszystkie 49 liczb w grze, maksymalizując szanse na trafienie.
*   **Usuwanie zestawów:** Możliwość łatwego usunięcia wybranego zestawu.
*   **Imort/export zestawów (Feature Flag):**
    *   **Import z pliku CSV:** Umożliwienie użytkownikowi importu wielu zestawów liczb z pliku CSV.
    *   **Eksport do pliku CSV:** Możliwość eksportu zapisanych zestawów do pliku CSV dla celów archiwizacji lub analizy.
*   **Weryfikacja wygranych:**
    *   Funkcja sprawdzania, czy któryś z zapisanych zestawów okazał się wygrany w określonym przedziale dat.
    *   System precyzyjnie wskaże zwycięski zestaw oraz wyróżni trafione liczby.

---

#### **Funkcje wyłączone z zakresu MVP (do rozważenia w przyszłości):**

*   Import i eksport zestawów liczb z plików zewnętrznych innych niż CSV (np. PDF).
*   Funkcje społecznościowe, takie jak współdzielenie zestawów z innymi użytkownikami.
*   Integracje z zewnętrznymi systemami (np. automatyczne pobieranie wyników losowań).
*   Dedykowana aplikacja mobilna (w pierwszej fazie projekt dostępny będzie jako aplikacja webowa).

---

#### **Kryteria sukcesu MVP:**

*   **Akceptacja generatora:** Co najmniej 75% zestawów liczb wygenerowanych przez system jest akceptowanych i zapisywanych przez użytkowników.
*   **Szybkość weryfikacji:** Sprawdzenie 100 zestawów liczb pod kątem jednego losowania trwa nie dłużej niż 2 sekundy.
*   **Retencja użytkowników:** 40% użytkowników, którzy założyli konto, wraca do aplikacji co najmniej raz w tygodniu (w tygodniach z losowaniami).
*   **Pozytywne opinie:** Uzyskanie średniej oceny 4/5 lub wyższej w ankietach satysfakcji zbieranych od pierwszych użytkowników.
*   **Niski wskaźnik błędów:** Mniej niż 1% sesji użytkownika kończy się niespodziewanym błędem krytycznym.