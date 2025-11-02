# Analiza Stosu Technologicznego LottoTM MVP

**Data analizy:** 2025-10-31
**Dokument bazowy:** `.ai/idea.md`, `.ai/prd.md`

---

## Stos technologiczny (z idea.md)

**Backend:**
- .NET 8 (C#)
- ASP.NET Core Web API
- Minimal APIs
- Architektura Vertical Slice
- Entity Framework Core

**Frontend:**
- React 19 z TypeScript
- Tailwind CSS

**Baza danych:**
- SQL Server 2022

**Autoryzacja:**
- JWT (JSON Web Tokens)

---

## Analiza krytyczna

### 1. Czy technologia pozwoli nam szybko dostarczyć MVP? ✅ TAK, ALE...

**Zalety:**
- **.NET 8 + Minimal APIs** - szybki setup, mniej boilerplate niż tradycyjne kontrolery
- **Entity Framework Core** - automatyczne migracje, łatwe CRUD
- **React 19 + TypeScript** - dojrzały ekosystem, gotowe komponenty (Tailwind, React Hook Form)
- **Vertical Slice Architecture** - mniej decyzji architektonicznych, szybsze dodawanie features

**Wątpliwości:**
- **SQL Server 2022** - wymaga licencji (koszt) lub edycji Express (ograniczenia: 10 GB, 1 socket CPU, 1 GB RAM). Dla MVP **PostgreSQL** lub **SQLite** byłyby szybsze i tańsze
- **Brak doświadczenia zespołu** - jeśli zespół nie zna .NET/React, krzywa uczenia się opóźni MVP
- **Vertical Slice Architecture** - wymaga dobrej znajomości wzorca, dla małego zespołu może być overhead

**Wniosek:** Tak, pod warunkiem że zespół zna te technologie. Dla zupełnych początkujących prostsze byłoby np. Django + SQLite + vanilla JS lub Node.js + Express.

**Szacowany czas do MVP:** 14 dni (zgodnie z PRD timeline)

---

### 2. Czy rozwiązanie będzie skalowalne? ✅ TAK

**Dobre wybory:**
- **Stateless JWT** - skalowanie horyzontalne backendu bez session storage
- **Vertical Slice Architecture** - łatwe dodawanie funkcji bez refactoru całości
- **SQL Server** - przemysłowa baza, obsługuje miliony rekordów (wymaganie: 10k użytkowników × 100 zestawów = 1M rekordów)
- **React SPA** - CDN hosting, niezależne skalowanie frontu i API
- **Stateless backend** - możliwość load balancingu

**Potencjalne wąskie gardła:**
- **Brak cache layer** - przy dużym ruchu weryfikacja może być wolna (wymaganie: 100 zestawów × 1 losowanie < 2s)
  - Rozwiązanie: Redis dla cache wyników weryfikacji
- **Brak kolejki zadań** - przy rozbudowie (np. powiadomienia email, automatyczne pobieranie losowań) będzie potrzebne
  - Rozwiązanie: RabbitMQ, Azure Service Bus, lub Hangfire

**Wymagania niefunkcjonalne (z PRD):**
- NFR-016: Min. 100 jednoczesnych użytkowników ✅ Spełnione
- NFR-017: Stateless backend dla skalowania horyzontalnego ✅ Spełnione
- NFR-018: 1M rekordów w bazie ✅ SQL Server obsłuży

**Wniosek:** Architektura MVP jest skalowalna do ~10k użytkowników. Przy większym ruchu (100k+) będzie potrzebny:
- Cache layer (Redis)
- Message queue (dla async operations)
- Database sharding (jeśli > 10M rekordów)

---

### 3. Czy koszt utrzymania i rozwoju będzie akceptowalny? ⚠️ ZALEŻY

#### **Koszty infrastruktury (miesięcznie):**

**SQL Server:**
- SQL Server Express (darmowy): 10 GB limit, 1 GB RAM
  - ✅ Wystarczy na MVP (szacunek: 1000 użytkowników × 100 zestawów × 100 bytes = 10 MB danych)
  - ❌ Niewystarczające na produkcję (10k+ użytkowników)
- Azure SQL Database Basic: ~$5/miesiąc (2 GB storage)
- Azure SQL Database Standard S0: ~$15/miesiąc (250 GB storage)
- Alternatywa: **PostgreSQL (darmowy)** na Azure/AWS/GCP - $0 (free tier) lub ~$7/miesiąc (produkcja)

**Hosting backendu (.NET 8):**
- Webio.pl (zgodnie z PRD): ~50-100 PLN/miesiąc (~$12-25)
  

**CI/CD:**
- GitHub Actions: 2000 minut/miesiąc darmowo (wystarczy na MVP)

**Monitoring/Logging (opcjonalnie dla MVP):**
- Application Insights (Azure): ~$2-10/miesiąc (zależy od volume)
- Sentry: $0 (free tier: 5k events/miesiąc)

**Szacunek całkowity dla MVP:**
- **Najtańsza opcja:** $0 (SQL Server Express + Azure/Netlify free tiers)
- **Realistyczna opcja:** ~$20-30/miesiąc (PostgreSQL + Azure App Service B1 + Netlify)
- **Webio.pl opcja:** ~50-100 PLN (~$12-25/miesiąc) - jeśli wspiera .NET 8

#### **Koszty developerskie:**

**Stawki rynkowe (Polska, 2025):**
- .NET/C# developer (mid): 100-150 PLN/h (~$25-37/h)
- React/TypeScript developer (mid): 90-130 PLN/h (~$22-32/h)
- Fullstack .NET+React (mid): 110-160 PLN/h (~$27-40/h)

**Alternatywne stosy (porównanie stawek):**
- Node.js/JavaScript fullstack: 80-120 PLN/h (~$20-30/h) - **tańsze o ~20%**
- Python/Django: 85-125 PLN/h (~$21-31/h) - **tańsze o ~15%**
- PHP/Laravel: 70-100 PLN/h (~$17-25/h) - **tańsze o ~30%**

**Koszt utrzymania (miesięcznie po MVP):**
- Bugfixy + minor features: ~20-40 h/miesiąc × stawka
- Szacunek: ~2000-6000 PLN/miesiąc (~$500-1500) dla .NET stack
- Alternatywa Node.js: ~1600-4800 PLN/miesiąc (~$400-1200) - **20% taniej**

#### **Koszty tooling/licencji:**
- Visual Studio Community: $0 (darmowe dla small teams)
- VS Code: $0 (darmowe)
- SQL Server Express: $0 (darmowe)
- SQL Server Standard (produkcja): ~$3700 jednorazy + Software Assurance
  - ⚠️ **Duży koszt** jeśli wymagane na produkcji
- Alternatywa PostgreSQL: $0 (open source)

**Wniosek:** **Koszt średni-wysoki** dla małego MVP:
- Infrastruktura: OK (~$20-30/miesiąc lub darmowo z free tiers)
- Development: **Wyższy** niż alternatywy (Node.js/Django) o 15-30%
- Licencje: **Potencjalnie wysoki** jeśli SQL Server Standard (produkcja)

**Rekomendacja:** Zamień SQL Server → **PostgreSQL** dla obniżenia kosztów i uniknięcia lock-in.

---

### 4. Czy potrzebujemy aż tak złożonego rozwiązania? ❌ NIE

#### **Nadmiarowe elementy dla MVP:**

**SQL Server 2022:**
- ❌ Przemysłowa baza do prostego CRUD (4 tabele: Users, Tickets, Draws, opcjonalnie TicketVerifications)
- ❌ Wymaga dodatkowej konfiguracji (instalacja, setup, backup strategy)
- ❌ Potencjalne koszty licencji na produkcji
- ✅ **PostgreSQL/SQLite wystarczy** - prostsze, darmowe, lżejsze

**Vertical Slice Architecture:**
- ❌ Dobra dla dużych zespołów (5+ devs) i złożonych domen
- ❌ Wymaga dobrej znajomości wzorca
- ❌ Dla MVP (14 dni, 1-2 devs) klasyczna struktura **MVC (Controllers/Services/Repositories)** jest prostsza i szybsza
- Przykład MVP - wystarczy:
  ```
  Controllers/
    AuthController.cs
    TicketsController.cs
    DrawsController.cs
  Services/
    TicketService.cs
    VerificationService.cs
  Repositories/
    TicketRepository.cs
  ```

**React 19:**
- ⚠️ Najnowsza wersja (official release: kwiecień 2024), mniej stabilnych bibliotek ekosystemu
- ⚠️ Niektóre popularne biblioteki mogą nie być jeszcze kompatybilne (np. starsze wersje react-router, state management)
- ✅ **React 18** byłby bezpieczniejszy (proven, stabilny ekosystem)

**TypeScript:**
- ⚠️ Dodaje warstwę komplikacji (setup, typy dla bibliotek, tsconfig)
- ⚠️ Spowalnia development dla małych projektów
- ✅ Dla MVP **vanilla JS + JSDoc** wystarczy (typy w komentarzach, bez TS overhead)
- Alternatywa: Dodać TS w fazie post-MVP

**Minimal APIs:**
- ✅ OK dla MVP (mniej boilerplate niż Controllers)
- Ale: Klasyczne Controllers są bardziej standardowe i łatwiejsze dla juniorów

#### **Złożoność bez korzyści:**

**Wymagania MVP (z PRD):**
- Prosty CRUD dla 3 encji (Users, Tickets, Draws)
- 1 algorytm biznesowy (weryfikacja wygranych: obliczenie liczby wspólnych elementów w dwóch tablicach)
- 1 generator (systemowy: pokrycie 1-49 w 9 zestawach)
- Brak:
  - Złożonych reguł biznesowych (workflow, state machines)
  - Integracji zewnętrznych (płatności, API)
  - Event sourcing, CQRS, mikrousług
  - Real-time (WebSockets)

**Dla tych wymagań wystarczy:**
- Prosty MVC backend
- Relacyjna baza (PostgreSQL/SQLite)
- Proste REST API
- React z podstawowymi hookami (useState, useEffect)

**Wniosek:** Stos jest **over-engineered** dla MVP. Vertical Slice, SQL Server, React 19, TypeScript dodają złożoność bez realnej wartości na tym etapie.

**Rekomendacja:** Uprość lub odłóż na post-MVP:
- Vertical Slice → MVC (dodaj po MVP jeśli projekt urośnie)
- SQL Server → PostgreSQL/SQLite
- React 19 → React 18
- TypeScript → vanilla JS + JSDoc (dodaj TS w fazie 2)

---

### 5. Czy nie istnieje prostsze podejście, które spełni nasze wymagania? ✅ TAK

#### **Alternatywne stosy dla szybszego MVP:**

### **Opcja A: Uproszczony .NET stack** (jeśli zespół zna C#)

```
Backend: ASP.NET Core MVC (klasyczne Controllers, nie Minimal APIs)
Baza: SQLite (development) → PostgreSQL (produkcja)
Frontend:
  - Opcja 1: Razor Pages (SSR, brak oddzielnego frontendu)
  - Opcja 2: Blazor Server (jeśli zespół zna C#, chce uniknąć JS)
  - Opcja 3: React 18 (bez TypeScript dla MVP)
Auth: ASP.NET Core Identity (built-in, mniej setup niż custom JWT)
Architektura: MVC (Controllers → Services → Repositories)
```

**Zalety:**
- Jeden język (C#) dla całego stacku (Blazor) lub prosty MVC
- Mniej setup (ASP.NET Identity zamiast custom JWT)
- Szybszy development (Razor Pages: brak API + osobnego frontu)
- SQLite dla dev: zero konfiguracji

**Wady:**
- Razor Pages/Blazor: mniej interaktywny UI niż React (ale wystarczy dla MVP)
- Blazor Server: wymaga stałego połączenia WebSocket (nie dla offline)

**Czas do MVP:** ~10-12 dni (vs. 14 dni dla Vertical Slice + React)

---

### **Opcja B: Node.js/JavaScript fullstack** ⭐ **REKOMENDACJA**

```
Backend: Node.js + Express + Prisma ORM
Baza: PostgreSQL (lub SQLite dla dev)
Frontend:
  - Next.js 14 (React + SSR + API routes w jednym projekcie)
  - Lub: Vite + React 18 (SPA, jak w oryginalnym stacku)
Auth: Passport.js + JWT (lub NextAuth.js dla Next.js)
Architektura: MVC (routes → controllers → services)
Styling: Tailwind CSS (jak w oryginalnym)
```

**Zalety:**
- **Jeden język (JavaScript)** dla całego stacku - łatwiejsze dla małych zespołów
- **Największy ekosystem npm** - gotowe biblioteki dla wszystkiego
- **Darmowy hosting** - Vercel (Next.js) lub Railway (Node.js) - $0 dla MVP
- **Prisma ORM** - najlepszy DX (developer experience), automatyczne migracje, type-safety
- **Szybki development** - mniej boilerplate niż .NET
- **Tańsze developerzy** - Node.js devs ~20% tańsi niż .NET devs

**Wady:**
- Brak strict typing (jeśli nie używać TypeScript) - ale dla MVP OK
- Performance niższy niż .NET dla CPU-intensive tasks - ale dla CRUD wystarczy

**Czas do MVP:** ~7-10 dni (vs. 14 dni dla .NET stack)

**Przykładowa struktura Next.js:**
```
app/
  api/
    auth/
      login/route.js
      register/route.js
    tickets/route.js
    draws/route.js
    verification/route.js
  (routes)/
    dashboard/page.jsx
    tickets/page.jsx
    draws/page.jsx
prisma/
  schema.prisma
lib/
  db.js
  auth.js
```

**Deployment:**
- Vercel (darmowy): Next.js app (frontend + API)
- Supabase (darmowy): PostgreSQL database
- **Koszt całkowity:** $0 dla MVP (free tiers)

---

### **Opcja C: Python/Django dla rychkiego prototypu**

```
Backend: Django + Django REST Framework
Baza: PostgreSQL (lub SQLite dla dev)
Frontend:
  - Django Templates (SSR, brak oddzielnego frontendu)
  - Lub: React 18 + Django REST jako API
Auth: Django Auth (built-in, najprostsze)
Architektura: MTV (Model-Template-View, Django standard)
Admin: Django Admin (darmowy admin panel out-of-the-box)
```

**Zalety:**
- **Najszybszy development** - Django ma "batteries included" (auth, admin, ORM, migrations)
- **Django Admin** - gotowy admin panel do zarządzania danymi (Users, Tickets, Draws) - oszczędza ~2 dni pracy
- **Prosty deployment** - Heroku, PythonAnywhere, Railway
- **Świetna dokumentacja** i community

**Wady:**
- Python: wolniejszy niż .NET/Node.js dla algorytmów (ale dla CRUD wystarczy)
- Django Templates: mniej interaktywny UI niż React (ale wystarczy dla MVP)

**Czas do MVP:** ~7-9 dni (najszybsza opcja dzięki Django Admin)

**Kiedy wybrać:**
- Zespół zna Python
- Potrzeba szybkiego prototypu do walidacji pomysłu
- Admin panel jest przydatny (dodawanie losowań przez admina)

---

#### **Porównanie opcji:**

| Kryterium | Oryginalny stack<br>(.NET + React) | Opcja A<br>(Uproszczony .NET) | Opcja B<br>(Node.js + Next.js) | Opcja C<br>(Django) |
|-----------|--------------------------|----------------------|----------------------|---------------|
| **Czas do MVP** | 14 dni | 10-12 dni | 7-10 dni | 7-9 dni |
| **Koszt infrastruktury** | ~$20-30/m | ~$15-25/m | $0 (free tiers) | ~$7-15/m |
| **Koszt development** | Wysoki (100-150 PLN/h) | Wysoki (100-150 PLN/h) | Średni (80-120 PLN/h) | Średni (85-125 PLN/h) |
| **Skalowalność** | Bardzo dobra | Bardzo dobra | Dobra | Dobra |
| **Krzywa uczenia** | Stroma (VSA) | Płaska (MVC) | Płaska | Bardzo płaska |
| **Ekosystem** | Duży (.NET) | Duży (.NET) | Największy (npm) | Duży (PyPI) |
| **Admin panel** | Trzeba zbudować | Trzeba zbudować | Trzeba zbudować | **Darmowy (Django Admin)** |
| **Best for** | Duże projekty, enterprise | MVP z .NET experience | Szybki MVP, startup | Najszybszy prototyp |

---

#### **Rekomendacja końcowa:**

**Jeśli zespół zna .NET:** → **Opcja A** (Uproszczony .NET: MVC + PostgreSQL + Blazor/Razor)
**Jeśli zespół zna JS:** → **Opcja B** (Node.js + Next.js + Prisma) ⭐ **NAJLEPSZA DLA MVP**
**Jeśli zespół zna Python:** → **Opcja C** (Django) - najszybszy prototyp

**Dla zupełnie nowego projektu bez preferencji stackowych:** → **Opcja B (Node.js/Next.js)** - najszybszy czas do MVP, $0 koszt hostingu, największy ekosystem.

---

### 6. Czy technologie pozwolą nam zadbać o odpowiednie bezpieczeństwo? ✅ TAK

#### **Wymagania bezpieczeństwa (z PRD sekcja 4.2):**

| NFR | Wymaganie | Czy stos spełnia? | Implementacja |
|-----|-----------|-------------------|---------------|
| NFR-005 | Hasła jako hash (bcrypt, min. 10 rounds) | ✅ TAK | **ASP.NET Core Identity** (built-in) lub custom z `BCrypt.Net-Next` |
| NFR-006 | JWT z czasem wygaśnięcia (24h) | ✅ TAK | `System.IdentityModel.Tokens.Jwt` |
| NFR-007 | HTTPS wymagane (produkcja) | ✅ TAK | Azure/Webio.pl zapewniają SSL/TLS |
| NFR-008 | Ochrona przed SQL Injection | ✅ TAK | **Entity Framework Core** - parametryzowane zapytania (automatycznie) |
| NFR-009 | Ochrona przed XSS | ✅ TAK | **React** - domyślne escapowanie dangerouslySetInnerHTML (unikać) |
| NFR-010 | Ochrona przed CSRF | ⚠️ DO IMPLEMENTACJI | **SameSite cookies** lub **Anti-CSRF tokens** (ASP.NET Core) |
| NFR-011 | Rate limiting (5 prób/min/IP) | ⚠️ DO IMPLEMENTACJI | `AspNetCoreRateLimit` (NuGet) lub middleware custom |

#### **Built-in security features (.NET 8):**

**ASP.NET Core:**
- ✅ **Data Protection API** - szyfrowanie tokenów, cookies
- ✅ **CORS policies** - kontrola cross-origin requests
- ✅ **Model validation** - automatyczna walidacja inputów (DataAnnotations)
- ✅ **Authorization policies** - role-based access control
- ✅ **Secure headers** - X-Frame-Options, X-Content-Type-Options (middleware)

**Entity Framework Core:**
- ✅ **Parametryzowane zapytania** - automatyczna ochrona przed SQL Injection
- ✅ **LINQ** - bezpieczne zapytania (no raw SQL)

**React:**
- ✅ **Automatic escaping** - wszystkie wartości w JSX są escapowane
- ⚠️ **dangerouslySetInnerHTML** - NIGDY nie używać z user input
- ⚠️ **Client-side validation** - zawsze weryfikować po stronie serwera (nie ufać frontendowi)

#### **Dodatkowe zabezpieczenia do implementacji:**

**1. Rate Limiting (NFR-011)**
```csharp
// Instalacja: dotnet add package AspNetCoreRateLimit
services.AddMemoryCache();
services.Configure<IpRateLimitOptions>(Configuration.GetSection("IpRateLimiting"));
services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
```

**2. CSRF Protection (NFR-010)**
```csharp
// Opcja A: Anti-Forgery Tokens (dla form-based auth)
services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");

// Opcja B: SameSite Cookies (dla JWT w cookies)
services.Configure<CookiePolicyOptions>(options =>
{
    options.MinimumSameSitePolicy = SameSiteMode.Strict;
});
```

**3. Secure Headers**
```csharp
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Add("Content-Security-Policy", "default-src 'self'");
    await next();
});
```

**4. Input Validation (dodatkowa warstwa)**
```csharp
// FluentValidation dla złożonych reguł
public class TicketValidator : AbstractValidator<TicketDto>
{
    public TicketValidator()
    {
        RuleFor(x => x.Numbers)
            .Must(n => n.All(num => num >= 1 && num <= 49))
            .WithMessage("Liczby muszą być w zakresie 1-49");
        RuleFor(x => x.Numbers)
            .Must(n => n.Distinct().Count() == 6)
            .WithMessage("Liczby muszą być unikalne");
    }
}
```

**5. Secrets Management**
```csharp
// NIE hardcodować secrets w kodzie
// Używać Azure Key Vault lub User Secrets (development)
dotnet user-secrets set "JwtSettings:SecretKey" "your-secret-key"

// appsettings.json
{
  "JwtSettings": {
    "SecretKey": "", // Pozostaw puste, pobierz z User Secrets/Key Vault
    "ExpiryInMinutes": 1440
  }
}
```

**6. Logging (dla security audits)**
```csharp
// Logowanie prób logowania, nieautoryzowanych dostępów
logger.LogWarning("Nieudana próba logowania: {Email}, IP: {IP}", email, ipAddress);
logger.LogWarning("Unauthorized access attempt: UserId {UserId}, Resource {Resource}", userId, resource);
```

#### **Security Checklist dla MVP:**

**Must Have (przed release):**
- [x] Hasła hashowane (bcrypt/PBKDF2)
- [x] JWT z czasem wygaśnięcia
- [x] HTTPS na produkcji
- [x] Parametryzowane zapytania (EF Core)
- [x] CORS policies
- [ ] Rate limiting (login/register)
- [ ] CSRF protection
- [ ] Secure headers (X-Frame-Options, CSP)
- [ ] Input validation (backend + frontend)
- [ ] Secrets w Key Vault/User Secrets (nie w kodzie)

**Should Have (post-MVP):**
- [ ] 2FA (Two-Factor Authentication)
- [ ] Email weryfikacja przy rejestracji
- [ ] Account lockout po X nieudanych logowań
- [ ] Security headers (HSTS, CSP strict)
- [ ] Penetration testing
- [ ] Dependency scanning (npm audit, dotnet list package --vulnerable)

**Nice to Have (przyszłość):**
- [ ] OAuth 2.0 / OpenID Connect
- [ ] API rate limiting per user
- [ ] Audit logging (wszystkie zmiany danych)
- [ ] Regular security audits

#### **Porównanie bezpieczeństwa stacków:**

| Stack | Security posture | Built-in features | Effort do zabezpieczenia MVP |
|-------|------------------|-------------------|------------------------------|
| **.NET 8** | ⭐⭐⭐⭐⭐ Bardzo dobry | ASP.NET Identity, Data Protection, EF Core | Niski (większość built-in) |
| **Node.js/Express** | ⭐⭐⭐⭐ Dobry | Helmet.js, bcrypt, Passport.js | Średni (trzeba dokonfigurować) |
| **Django** | ⭐⭐⭐⭐⭐ Bardzo dobry | Django Auth, CSRF middleware, ORM | Niski (większość built-in) |

**Wniosek:** Oryginalny stos **.NET 8** jest **bardzo bezpieczny** i ma najlepsze built-in security features. Wymaga najmniej dodatkowej pracy do zabezpieczenia MVP.

**Rekomendacja:** ✅ Zostań przy .NET 8 ze względu na bezpieczeństwo. Zaimplementuj dodatkowe zabezpieczenia z checklisty przed release.

---

## 🎯 Rekomendacje końcowe

### **Scenariusz 1: Zostajemy przy oryginalnym stacku (.NET 8 + React 19)**

✅ **Wybierz tę opcję, jeśli:**
- Zespół ma **doświadczenie z .NET i React**
- Projekt docelowo będzie **rozbudowany** (10k+ użytkowników, złożona logika biznesowa)
- Budżet pozwala na ~$20-30/miesiąc hosting
- **Bezpieczeństwo jest najwyższym priorytetem**

⚠️ **Konieczne zmiany (dla przyspieszenia MVP):**

1. **SQL Server 2022 → PostgreSQL**
   - **Dlaczego:** Darmowy, prostszy setup, brak lock-in, wspierany przez wszystkie cloud providers
   - **Jak:** Zamień `UseSqlServer()` na `UseNpgsql()` w EF Core
   - **Zysk:** -$0-15/miesiąc, łatwiejszy development

2. **Vertical Slice Architecture → MVC (dla MVP)**
   - **Dlaczego:** Prostsza dla małego zespołu, szybszy development
   - **Jak:** Użyj klasycznych `Controllers/Services/Repositories`
   - **Kiedy wrócić do VSA:** Po MVP, gdy dodajesz 5+ feature
   - **Zysk:** -2-3 dni development time

3. **React 19 → React 18**
   - **Dlaczego:** Stabilniejszy ekosystem, wszystkie biblioteki kompatybilne
   - **Jak:** `npm install react@18 react-dom@18`
   - **Zysk:** Mniej bugów z niekompatybilnymi bibliotekami

4. **TypeScript → JavaScript (dla MVP, opcjonalnie)**
   - **Dlaczego:** Szybszy development dla małego projektu
   - **Jak:** Usuń `tsconfig.json`, zmień rozszerzenia `.tsx` → `.jsx`
   - **Kiedy dodać TS:** Post-MVP, gdy codebase urośnie
   - **Zysk:** -1 dzień setup, szybszy development

5. **Dodaj cache layer (Redis) dla weryfikacji**
   - **Dlaczego:** Wymaganie NFR-001 (100 zestawów w <2s)
   - **Jak:** `StackExchange.Redis` + cache wyników weryfikacji
   - **Kiedy:** Faza 4 (Weryfikacja) - przed performance testami

**Zaktualizowany stos (wersja uproszczona):**
```
Backend: .NET 8 + ASP.NET Core MVC
Baza: PostgreSQL (Azure Database for PostgreSQL lub Supabase)
Frontend: React 18 + JavaScript (bez TypeScript na MVP)
Styling: Tailwind CSS
Auth: JWT (custom implementation)
Cache: Redis (dla weryfikacji)
Hosting: Azure App Service B1 (~$13/m) + Netlify (frontend, $0)
```

**Szacowany czas do MVP:** ~10-12 dni (zamiast 14)

---

### **Scenariusz 2: Przechodzimy na prostszy stack** ⭐ **REKOMENDACJA DLA SZYBKIEGO MVP**

🚀 **Zalecany stos dla najszybszego MVP:**

```
Backend: Node.js 20 LTS + Express 4
ORM: Prisma 5 (najlepszy DX)
Baza: PostgreSQL 15 (Supabase - darmowy tier)
Frontend: Next.js 14 (React 18 + SSR + API routes)
Styling: Tailwind CSS
Auth: NextAuth.js (OAuth + JWT built-in)
Deployment: Vercel (darmowy dla Next.js)
```

**Dlaczego ten stack?**
- ✅ **Jeden język (JavaScript)** - mniej kontekst switching
- ✅ **Najszybszy development** - Next.js ma API routes + frontend w jednym projekcie
- ✅ **$0 koszt hostingu** - Vercel (free tier) + Supabase (free tier)
- ✅ **NextAuth.js** - najmniej boilerplate dla autentykacji (JWT + OAuth ready)
- ✅ **Prisma** - najlepszy DX dla ORM (auto-migrations, type-safety z JSDoc)
- ✅ **Największy ekosystem** - npm ma paczkę na wszystko

**Struktura projektu (Next.js 14 App Router):**
```
app/
  api/
    auth/
      [...nextauth]/route.js       # NextAuth endpoints (login/register)
    tickets/
      route.js                      # GET/POST /api/tickets
      [id]/route.js                 # PUT/DELETE /api/tickets/[id]
      generate-random/route.js      # POST /api/tickets/generate-random
      generate-system/route.js      # POST /api/tickets/generate-system
    draws/
      route.js                      # GET/POST /api/draws
    verification/
      check/route.js                # POST /api/verification/check
  (auth)/
    login/page.jsx
    register/page.jsx
  (dashboard)/
    layout.jsx                      # Shared layout z navbar
    page.jsx                        # Dashboard
    tickets/page.jsx
    draws/page.jsx
    verification/page.jsx

prisma/
  schema.prisma                     # Database schema

lib/
  db.js                             # Prisma client
  auth.js                           # Auth helpers

components/
  tickets/
    TicketList.jsx
    TicketForm.jsx
  draws/
    DrawForm.jsx
  shared/
    Navbar.jsx
    Button.jsx
```

**Przykład implementacji (Tickets CRUD w Next.js):**

```javascript
// app/api/tickets/route.js
import { NextResponse } from 'next/server';
import { getServerSession } from 'next-auth';
import { prisma } from '@/lib/db';

export async function GET(request) {
  const session = await getServerSession();
  if (!session) return NextResponse.json({ error: 'Unauthorized' }, { status: 401 });

  const tickets = await prisma.ticket.findMany({
    where: { userId: session.user.id },
    orderBy: { createdAt: 'desc' }
  });

  return NextResponse.json({ tickets });
}

export async function POST(request) {
  const session = await getServerSession();
  if (!session) return NextResponse.json({ error: 'Unauthorized' }, { status: 401 });

  const { numbers } = await request.json();

  // Validation
  if (!numbers || numbers.length !== 6) {
    return NextResponse.json({ error: 'Must provide exactly 6 numbers' }, { status: 400 });
  }
  if (!numbers.every(n => n >= 1 && n <= 49)) {
    return NextResponse.json({ error: 'Numbers must be between 1 and 49' }, { status: 400 });
  }
  if (new Set(numbers).size !== 6) {
    return NextResponse.json({ error: 'Numbers must be unique' }, { status: 400 });
  }

  // Check limit (100 tickets per user)
  const count = await prisma.ticket.count({ where: { userId: session.user.id } });
  if (count >= 100) {
    return NextResponse.json({ error: 'Maximum 100 tickets reached' }, { status: 400 });
  }

  // Create ticket
  const ticket = await prisma.ticket.create({
    data: {
      userId: session.user.id,
      numbers: numbers,
    }
  });

  return NextResponse.json({ ticket }, { status: 201 });
}
```

**Prisma Schema (schema.prisma):**
```prisma
generator client {
  provider = "prisma-client-js"
}

datasource db {
  provider = "postgresql"
  url      = env("DATABASE_URL")
}

model User {
  id        Int      @id @default(autoincrement())
  email     String   @unique
  password  String
  tickets   Ticket[]
  draws     Draw[]
  createdAt DateTime @default(now())
}

model Ticket {
  id        String   @id @default(cuid())
  userId    Int
  user      User     @relation(fields: [userId], references: [id], onDelete: Cascade)
  numbers   Int[]    // PostgreSQL array
  createdAt DateTime @default(now())
  updatedAt DateTime @updatedAt

  @@index([userId])
}

model Draw {
  id        Int      @id @default(autoincrement())
  userId    Int
  user      User     @relation(fields: [userId], references: [id], onDelete: Cascade)
  drawDate  DateTime @unique
  numbers   Int[]    // PostgreSQL array
  createdAt DateTime @default(now())

  @@index([drawDate])
  @@index([userId])
}
```

**Deployment (1 komenda):**
```bash
# Połącz z GitHub repo
vercel

# Deploy (automatycznie przy każdym push do main)
git push origin main
```

**Szacowany czas do MVP:** ~7-9 dni (vs. 14 dni dla .NET stack)

**Koszt:**
- Hosting: $0 (Vercel free tier)
- Database: $0 (Supabase free tier: 500 MB storage, 2 GB transfer/month)
- **Total: $0/miesiąc** dla MVP

---

### **Porównanie końcowe:**

| Kryterium | Oryginalny stack<br>(z poprawkami) | Node.js/Next.js<br>(alternatywa) |
|-----------|------------------------------|--------------------------------|
| **Czas do MVP** | ~10-12 dni | ~7-9 dni ⚡ **SZYBSZE** |
| **Koszt/miesiąc** | ~$20-30 | $0 💰 **TANIEJ** |
| **Krzywa uczenia** | Stroma (jeśli nowy zespół) | Płaska (JS wszędzie) |
| **Skalowalność** | Bardzo dobra (9/10) | Dobra (8/10) |
| **Bezpieczeństwo** | Bardzo dobre (9/10) | Dobre (8/10) |
| **Ekosystem** | Duży (.NET) | Największy (npm) |
| **Best for** | Enterprise, long-term | Startup, szybki MVP |

---

### **Decyzja końcowa - Co wybrać?**

#### **Wybierz .NET 8 (z poprawkami), jeśli:**
- ✅ Zespół **już zna** .NET i React
- ✅ Planujesz **długoterminowy projekt** (2+ lat)
- ✅ Priorytetem jest **bezpieczeństwo** i **performance**
- ✅ Budżet na hosting nie jest problemem (~$20-30/m)

**Wtedy zastosuj zmiany:** PostgreSQL + MVC + React 18 + cache

---

#### **Wybierz Node.js/Next.js, jeśli:** ⭐ **REKOMENDACJA**
- ✅ Chcesz **najszybsze MVP** (7-9 dni)
- ✅ Budżet jest **ograniczony** (potrzebujesz $0 hostingu)
- ✅ Zespół zna **JavaScript** (lub jest w trakcie nauki)
- ✅ Priorytetem jest **time-to-market** i **walidacja pomysłu**

**Wtedy użyj:** Next.js 14 + Prisma + PostgreSQL (Supabase) + Vercel

---

## 📊 Podsumowanie oceny (0-10)

| Kryterium | Oryginalny stack | Po poprawkach | Node.js/Next.js |
|-----------|------------------|---------------|-----------------|
| **1. Szybkość dostarczenia MVP** | 6/10 | 7/10 | 9/10 ⭐ |
| **2. Skalowalność** | 9/10 | 9/10 | 8/10 |
| **3. Koszt utrzymania** | 5/10 | 7/10 | 10/10 ⭐ |
| **4. Prostota (vs. potrzeby MVP)** | 4/10 | 7/10 | 9/10 ⭐ |
| **5. Istnieją prostsze alternatywy** | NIE | NIE | TAK ⭐ |
| **6. Bezpieczeństwo** | 9/10 | 9/10 | 8/10 |
| **RAZEM** | **6.3/10** | **7.7/10** | **8.8/10** ⭐ |

---

## ✅ Werdykt końcowy

**Dla MVP LottoTM:**

### Oryginalny stos (.NET 8 + React 19 + SQL Server + Vertical Slice)
❌ **Zbyt złożony** - over-engineered dla prostego CRUD
❌ **Za drogi** - SQL Server, wyższe stawki .NET devs
✅ **Dobry dla produkcji** - ale nie optimalny dla MVP

### Oryginalny stos z poprawkami (.NET 8 + React 18 + PostgreSQL + MVC)
✅ **Akceptowalny** - jeśli zespół zna .NET
⚠️ **Średni czas/koszt** - 10-12 dni, ~$20-30/m
✅ **Najlepsze bezpieczeństwo** - built-in w .NET

### Node.js/Next.js + Prisma + PostgreSQL + Vercel ⭐ **REKOMENDACJA**
✅ **Najszybszy MVP** - 7-9 dni
✅ **Najtańszy** - $0/miesiąc
✅ **Najprostszy** - jeden język, najmniej boilerplate
✅ **Najlepszy DX** - Prisma + Next.js mają najlepszy developer experience

---

## 🚀 Action Items

### Jeśli zostajecie przy .NET:
- [ ] Zamień SQL Server → PostgreSQL
- [ ] Użyj MVC zamiast Vertical Slice (dla MVP)
- [ ] Downgrade React 19 → React 18
- [ ] Rozważ pominięcie TypeScript (dla MVP)
- [ ] Dodaj Redis (cache) w Fazie 4

### Jeśli przechodzimy na Node.js:
- [ ] Setup Next.js 14 projekt (`npx create-next-app@latest`)
- [ ] Setup Prisma + PostgreSQL (Supabase)
- [ ] Implementuj NextAuth.js (autentykacja)
- [ ] Deploy na Vercel (1 komenda)

---

**Dokument przygotowany:** 2025-10-31
**Kontekst:** Analiza dla projektu LottoTM MVP
**Podstawa:** `.ai/idea.md`, `.ai/prd.md`, `CLAUDE.md`
