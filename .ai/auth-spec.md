# Diagram Architektury Autentykacji - LottoTM

## Opis

Diagram przedstawia pełną architekturę systemu autentykacji w aplikacji LottoTM, obejmującą:
- Frontend (React) - strony, formularze, komponenty UI
- Context & Services - zarządzanie stanem i komunikacja z API
- Backend (.NET) - endpointy, handlery, walidacja
- Infrastruktura - JWT, BCrypt, baza danych

## Przepływy:

1. **Login Flow**: LoginPage → ApiService → Backend → JWT → AppContext → localStorage
2. **Register Flow**: RegisterPage → ApiService → Backend → BCrypt → DB → JWT → AppContext (auto-login)
3. **Session Restore**: localStorage → AppContext (sprawdzenie expiresAt)
4. **Logout Flow**: AppContext → localStorage (clear)

## Diagram Mermaid

```mermaid
flowchart TD
    subgraph "Frontend - Strony"
        LP["LoginPage"]
        RP["RegisterPage"]
    end

    subgraph "Frontend - Formularze"
        LF["LoginForm"]
        RF["RegisterForm"]
    end

    subgraph "Frontend - Komponenty UI"
        EI["EmailInput"]
        PI["PasswordInput"]
        BTN["Button"]
        EM["ErrorModal"]
    end

    subgraph "Frontend - Context & Services"
        AC["AppContext"]
        ACP["AppContextProvider"]
        API["ApiService"]
        LS["localStorage"]
    end

    subgraph "Backend - Endpoints"
        LEP["POST /api/auth/login"]
        REP["POST /api/auth/register"]
    end

    subgraph "Backend - Handlers"
        LH["Login.Handler"]
        RH["Register.Handler"]
    end

    subgraph "Backend - Walidacja"
        LV["Login.Validator"]
        RV["Register.Validator"]
    end

    subgraph "Backend - Infrastruktura"
        JWT["IJwtService"]
        BC["BCrypt"]
        DB["AppDbContext"]
        UE["User Entity"]
    end

    %% Login Flow
    LP -->|renderuje| LF
    LF -->|używa| EI
    LF -->|używa| PI
    LF -->|używa| BTN
    LP -->|submit| API
    API -->|POST request| LEP
    LEP -->|wywołuje| LH
    LH -->|waliduje| LV
    LH -->|weryfikuje hasło| BC
    LH -->|pobiera użytkownika| DB
    LH -->|generuje token| JWT
    JWT -->|token JWT| LH
    LH -->|Response| LEP
    LEP -->|Response| API
    API -->|sukces| ACP
    ACP -->|login| AC
    ACP -->|zapisuje token| LS
    ACP -->|redirect| LP
    API -->|błąd| EM
    LP -->|wyświetla błędy| EM

    %% Register Flow
    RP -->|renderuje| RF
    RF -->|używa| EI
    RF -->|używa| PI
    RF -->|używa| BTN
    RF -->|submit| API
    API -->|POST request| REP
    REP -->|wywołuje| RH
    RH -->|waliduje| RV
    RH -->|hashuje hasło| BC
    RH -->|tworzy użytkownika| UE
    UE -->|zapisuje| DB
    RH -->|generuje token| JWT
    JWT -->|token JWT| RH
    RH -->|Response| REP
    REP -->|Response| API
    API -->|sukces + auto-login| ACP
    ACP -->|login| AC
    ACP -->|zapisuje token| LS
    RF -->|redirect do /tickets| RP
    API -->|błąd| EM
    RF -->|wyświetla błędy| EM

    %% Session Restore
    ACP -->|mount: odczyt| LS
    LS -->|token & user| ACP
    ACP -->|sprawdza expiresAt| ACP
    ACP -->|ważny token| AC
    ACP -->|wygasły token| LS

    %% Logout Flow
    AC -->|logout| ACP
    ACP -->|czyści| LS
    ACP -->|usuwa token| API

    %% Styling
    classDef page fill:#e1f5ff,stroke:#01579b,stroke-width:2px
    classDef form fill:#fff3e0,stroke:#e65100,stroke-width:2px
    classDef component fill:#f3e5f5,stroke:#4a148c,stroke-width:2px
    classDef service fill:#e8f5e9,stroke:#1b5e20,stroke-width:2px
    classDef backend fill:#fff9c4,stroke:#f57f17,stroke-width:2px
    classDef infra fill:#fce4ec,stroke:#880e4f,stroke-width:2px

    class LP,RP page
    class LF,RF form
    class EI,PI,BTN,EM component
    class AC,ACP,API,LS service
    class LEP,REP,LH,RH,LV,RV backend
    class JWT,BC,DB,UE infra
```

## Legenda kolorów:

- **Niebieski** (page): Strony główne aplikacji
- **Pomarańczowy** (form): Formularze użytkownika
- **Fioletowy** (component): Komponenty UI współdzielone
- **Zielony** (service): Serwisy i Context
- **Żółty** (backend): Backend endpoints i handlers
- **Różowy** (infra): Infrastruktura (JWT, BCrypt, DB)

## Komponenty Frontend:

### Strony:
- **LoginPage**: Kontener logiki logowania, zarządza stanem formularza, obsługuje walidację i redirect
- **RegisterPage**: Kontener dla formularza rejestracji, nawigacja do logowania

### Formularze:
- **LoginForm**: Prezentacyjny komponent z polami email, hasło i przyciskiem submit
- **RegisterForm**: Zarządza rejestrację z walidacją inline (email, hasło, potwierdzenie), auto-login po sukcesie

### Komponenty UI:
- **EmailInput**: Input do wprowadzania adresu email
- **PasswordInput**: Input do wprowadzania hasła
- **Button**: Przycisk akcji (submit, cancel itp.)
- **ErrorModal**: Modal wyświetlający błędy walidacji i błędy API

### Context & Services:
- **AppContext**: Context React z interfejsem użytkownika i funkcjami auth
- **AppContextProvider**: Provider zarządzający stanem użytkownika (login, logout, session restore)
- **ApiService**: Centralna klasa HTTP komunikacji z backend API
- **localStorage**: Przechowywanie tokenu JWT i danych użytkownika

## Komponenty Backend:

### Endpoints:
- **POST /api/auth/login**: Publiczny endpoint logowania (AllowAnonymous)
- **POST /api/auth/register**: Publiczny endpoint rejestracji (AllowAnonymous)

### Handlers (MediatR):
- **Login.Handler**: Walidacja credentials, weryfikacja BCrypt, generowanie JWT
- **Register.Handler**: Walidacja danych, hashowanie BCrypt, zapis do DB, generowanie JWT

### Walidacja (FluentValidation):
- **Login.Validator**: Walidacja request logowania
- **Register.Validator**: Walidacja request rejestracji (wymagania hasła, unikalność email)

### Infrastruktura:
- **IJwtService**: Serwis generowania i walidacji tokenów JWT
- **BCrypt**: Hashowanie i weryfikacja haseł (10 rounds, timing attack protection)
- **AppDbContext**: Entity Framework DbContext dla dostępu do bazy SQL Server
- **User Entity**: Model użytkownika w bazie danych

## Bezpieczeństwo:

- **BCrypt hashing**: 10 rounds dla bezpiecznego przechowywania haseł
- **JWT tokens**: Bearer authentication z expiresAt
- **Timing attack protection**: Dummy hash verification w Login.Handler
- **Token persistence**: localStorage z automatycznym sprawdzeniem wygaśnięcia
- **FluentValidation**: Walidacja po stronie backendu
- **Frontend validation**: Regex email, wymagania hasła (min 8 znaków, wielka litera, cyfra, znak specjalny)

## Obsługa błędów:

- **400 Bad Request**: Błędy walidacji (frontend i backend)
- **401 Unauthorized**: Nieprawidłowe credentials
- **500 Internal Server Error**: Błędy serwera
- **ErrorModal**: Wyświetlanie błędów użytkownikowi
