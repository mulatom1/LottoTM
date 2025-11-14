#### **Stos technologiczny (MVP):**

*   **Baza danych:** SQL Server 2022
*   **Backend:** .NET 8 (C#) z wykorzystaniem:
    *   ASP.NET Core Web API
    *   Minimal APIs
    *   Architektura Vertical Slice
    *   Entity Framework Core
    *   Serilog dla logowania
    *   Fluent Validation dla walidacji danych
    *   Obsługa CORS (MediatR)
    *   Globalna obsługa wyjątków i błędów
    *   Google Gemini API dla ekstrakcji danych z XLotto
    *   xUnit, Moq, WebApplicationFactory dla testów
    *   Feature Flags z appsettings.json (Enable/Disable Google Gemini)
*   **Frontend:** React 19 z wykorzystaniem:
    *   react-router 7 do zarządzania trasami.
    *   TypeScript
    *   biblioteką Tailwind CSS 4 do stylizacji.
*   **Autoryzacja:** Zabezpieczenie systemu za pomocą tokenów JWT (JSON Web Tokens).
*   **Deployment:** MVP wdrożone i dostępne pod adresem **https://tomsoft1.pl/lottotm**