# Analiza pomysłu LottoTM

**Data analizy:** 2025-10-31
**Kontekst:** projekt 2-tygodniowy, praca po godzinach z AI

## Profil dewelopera

- **Doświadczenie .NET/C#:** Zaawansowane
- **Doświadczenie React/TypeScript:** Podstawowe
- **Doświadczenie JWT/Security:** Tak, implementował JWT
- **Baza danych:** SQL Server 2022 (dostępny)
- **Dostępny czas:** 10-20 godzin/tydzień

---

## 1. Czy aplikacja rozwiązuje realny problem?

**TAK - 9/10**

Aplikacja rozwiązuje konkretny, namacalny problem:
- Osoby grające w LOTTO z wieloma zestawami liczb faktycznie muszą je ręcznie sprawdzać
- Proces jest monotonny, czasochłonny i podatny na błędy
- Ryzyko przeoczenia wygranej jest realne

### Mocne strony:
- Problem jest dobrze zdefiniowany i zrozumiały
- Wartość dodana jest natychmiastowa i mierzalna (oszczędność czasu, eliminacja błędów)
- Jest nisza użytkowników, którzy regularnie grają i mają wiele zestawów

### Potencjalne zastrzeżenie:
- Wielkość rynku może być ograniczona (tylko gracze z wieloma zestawami)
- Warto rozważyć research: ile osób faktycznie gra na wielu kuponach jednocześnie?

---

## 2. Czy można skupić się na 1-2 kluczowych funkcjach?

**CZĘŚCIOWO - 6/10**

Propozycja MVP ma **4-5 głównych obszarów funkcjonalnych**, co jest ryzykowne:

### Obecny zakres:
1. System logowania/autoryzacji (JWT)
2. Zarządzanie zestawami (CRUD)
3. Generator liczb (prosty + systemowy)
4. Wprowadzanie wyników losowań
5. Weryfikacja wygranych

### Rekomendacja - uproszczone MVP:

Sugeruję skupienie się na **2 kluczowych funkcjach**:

1. **Zarządzanie zestawami** (dodaj ręcznie, usuń, przeglądaj)
2. **Weryfikacja wygranych** (wprowadź wynik losowania → system pokazuje trafienia)

### Co można odłożyć na później:
- ✂️ Generator systemowy (9 zestawów) - skomplikowana logika biznesowa
- ✂️ Generator losowy - Nice to have, nie jest kluczowy dla problemu
- ↓ Pełna autoryzacja JWT - na start można użyć prostszej formy (np. single user mode lub basic auth)

---

## 3. Czy jest możliwe wdrożenie w 2 tygodni (25h/tydzień)?

**TAK, ale tylko z ograniczonym zakresem - 7/10**

### Analiza czasowa (50 godzin total):

| Zadanie | Czas (h) | Uwagi |
|---------|----------|-------|
| Setup projektu (.NET + React) | 4 | Docker, CI/CD basic |
| Baza danych + EF Core migrations | 4 | Schemat, relacje, seedowanie |
| **Autoryzacja JWT** | 4 | **Największe ryzyko czasowe** |
| Backend API (CRUD kuponów) | 8 | Vertical Slice |
| Weryfikacja wygranych (logika) | 4 | Algorytm porównywania |
| Frontend - komponenty React | 8 | Formularze, listy, routing |
| **Frontend - state management** | 8 | **Dla początkującego w React** |
| Styling (Tailwind) | 2 | |
| Testy + debugging | 8 | |
| **TOTAL** | **50h** | |

### Werdykt:
- Z pełnym zakresem MVP: **Bardzo napiętym harmonogram** (70h przy górnej granicy czasu)
- Z uproszczonym zakresem (bez generatorów, podstawowa auth): **Realistyczne** (~50h)

### Kluczowe ryzyka:
- React/TypeScript - podstawowy poziom, co może wydłużyć czas developmentu frontendu o 20-30%
- Vertical Slice Architecture - jeśli to nowe podejście, może wymagać więcej czasu na research

---

## 4. Potencjalne trudności

### A. Trudności techniczne:

#### Wysokie ryzyko:

**1. Frontend React (doświadczenie podstawowe)**
- State management (wybór: Context API vs Redux vs React Query?)
- Formularzowanie w React (React Hook Form?)
- TypeScript integration
- **Mitygacja:** Zacznij od prostych komponentów, używaj AI do wsparcia, rozważ template/starter

**2. Generator systemowy (9 zestawów pokrywających 49 liczb)**
- Skomplikowana logika kombinatoryczna
- Zapewnienie pokrycia wszystkich liczb
- **Mitygacja:** Odłóż to na post-MVP

#### Średnie ryzyko:

**3. Vertical Slice Architecture**
- Jeśli brak doświadczenia, może być krzywą uczenia
- **Mitygacja:** Użyj prostszej struktury (klasyczny Controller-Service-Repository na start)

**4. Wydajność weryfikacji (kryterium: 100 zestawów w <2s)**
- Przy prostej implementacji może być problem
- Wymaga optymalizacji zapytań SQL, indeksów
- **Mitygacja:** Zacznij od działającej wersji, optymalizuj później

### B. Trudności projektowe:

#### Średnie ryzyko:

**5. Scope creep (rozrost zakresu)**
- Generator, różne warianty LOTTO, statystyki
- **Mitygacja:** Sztywno trzymać się MVP, lista "Not Now" features

**6. UX/UI design**
- Jeśli brak doświadczenia w designie
- **Mitygacja:** Użyj gotowych komponentów (Shadcn/ui, DaisyUI), inspiruj się istniejącymi apps

### C. Dane i logika biznesowa:

#### Niskie ryzyko:

**7. Zasady LOTTO**
- Czy są różne warianty? (LOTTO Plus, Multi-Multi?)
- **Mitygacja:** Na MVP tylko podstawowe LOTTO (6 z 49)

---

## PODSUMOWANIE I REKOMENDACJE

### Ocena wykonalności: 7.5/10

### ✅ Mocne strony projektu:
- Realny, konkretny problem
- Dobry wybór stacku technologicznego (doświadczenie z .NET)
- Jasno określone kryteria sukcesu
- Odpowiednia ilość czasu (10-15h/tydz.)

### ⚠️ Obszary ryzyka:
- Za szeroki zakres MVP
- Ograniczone doświadczenie z React/TypeScript
- Ambitious feature set (generatory)

---

## Konkretne rekomendacje:

### 1. Zredukuj MVP do absolutnego minimum:

**MUST HAVE (Core Value):**
- ✅ Logowanie (prosty JWT lub nawet basic auth)
- ✅ Dodaj zestaw ręcznie
- ✅ Lista moich zestawów
- ✅ Wprowadź wynik losowania
- ✅ Zobacz trafienia (highlight trafionych liczb)

**NICE TO HAVE (Post-MVP):**
- ⏸️ Generator losowy
- ⏸️ Generator systemowy
- ⏸️ Zakres dat weryfikacji
- ⏸️ Statystyki

### 2. Uproszczenia techniczne:
- Rozważ klasyczną architekturę (Controller-Service-Repository) zamiast Vertical Slice na start
- Użyj gotowej biblioteki UI dla React (Shadcn/ui + Tailwind)
- Użyj React Hook Form dla formularzy
- Rozważ Context API zamiast Redux dla state managementu

### 3. Harmonogram (3 tygodnie):

```
Tydzień 1: Planning, Backend setup, baza danych, basic CRUD API, frontend setup, komponenty, integracja z API
Tydzień 1: Logika weryfikacji wygranych
Tydzień 1: Testy, bug fixing
```