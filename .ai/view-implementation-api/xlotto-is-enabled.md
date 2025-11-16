# Plan implementacji API: XLotto Is Enabled

## 1. Przegląd

Endpoint służy do sprawdzenia czy funkcjonalność XLotto jest włączona poprzez odczyt Feature Flag z konfiguracji. Umożliwia frontend'owi dynamiczne dostosowanie interfejsu użytkownika (wyświetlanie/ukrywanie przycisku "Pobierz z XLotto") w zależności od konfiguracji środowiska.

**Główne funkcje:**
- Odczyt wartości `GoogleGemini:Enable` z konfiguracji
- Zwracanie wartości boolean określającej czy funkcja jest aktywna
- Kontrola widoczności funkcjonalności XLotto w UI

## 2. Endpoint Definition

**Ścieżka:** `GET /api/xlotto/is-enabled`

**Typ dostępu:** Chroniony (wymaga autentykacji JWT)

**Parametry zapytania:** Brak

**Przykład zapytania:**
```
GET /api/xlotto/is-enabled
```

## 3. Request/Response

### Request Headers
```
Authorization: Bearer <JWT_TOKEN>
X-TOKEN: <APP_TOKEN>
Content-Type: application/json
```

### Response (sukces)
**Status Code:** 200 OK

```json
{
  "success": true,
  "data": true
}
```

**Opis pól:**
- `success` (boolean): Status odpowiedzi, zawsze `true` dla sukcesu
- `data` (boolean): Wartość Feature Flag - `true` jeśli funkcja włączona, `false` jeśli wyłączona

### Response (błąd)
**Status Code:** 401 Unauthorized
```json
{
  "error": "Brak autoryzacji. Wymagany token JWT."
}
```

**Status Code:** 500 Internal Server Error
```json
{
  "error": "Błąd serwera podczas sprawdzania statusu funkcji XLotto"
}
```

## 4. Logika biznesowa

### Krok 1: Odczyt konfiguracji
```csharp
var isEnabled = _googleGeminiOptions.Enable;
```

### Krok 2: Zwrot wyniku
```csharp
return new Response(isEnabled);
```

**Uwaga:** Wartość `GoogleGemini:Enable` jest wstrzykiwana przez `IOptions<GoogleGeminiOptions>` w konstruktorze handler'a.

## 5. Feature Flag Configuration

### appsettings.json (Production)
```json
{
  "GoogleGemini": {
    "ApiKey": "base64_your-google-gemini-api-key-here",
    "Model": "gemini-2.5-flash",
    "Enable": false
  }
}
```

### appsettings.Development.json (Development)
```json
{
  "GoogleGemini": {
    "ApiKey": "QUl6YVN5QTBDNTgzOWlLRWE2U2JNcDFLMDMwSVkzaXJjZEhmRzVF",
    "Model": "gemini-2.0-flash",
    "Enable": true
  }
}
```

**Strategia:**
- **Produkcja (appsettings.json):** `Enable: false` - funkcja domyślnie wyłączona
- **Development (appsettings.Development.json):** `Enable: true` - funkcja włączona dla deweloperów

## 6. Walidacja

### Brak walidacji parametrów
Endpoint nie przyjmuje żadnych parametrów wejściowych, więc walidacja nie jest wymagana.

### Walidacja konfiguracji
Konfiguracja `GoogleGemini:Enable` jest typu boolean - .NET automatycznie deserializuje wartość z JSON.

## 7. Obsługa błędów

### General Errors
```csharp
catch (Exception ex) {
    _logger.LogError(ex, "Error checking XLotto feature status");
    throw;
}
```

**Uwaga:** W praktyce ten endpoint jest bardzo prosty i błędy są mało prawdopodobne - jedynie odczyt wartości z konfiguracji.

## 8. Bezpieczeństwo

### Autoryzacja
- Endpoint wymaga ważnego JWT tokenu
- Token przekazywany w header `Authorization: Bearer <token>`
- **NIE wymaga uprawnień administratora (IsAdmin)** - dostępny dla wszystkich zalogowanych użytkowników

### Uzasadnienie
Frontend musi znać status funkcji przed otwarciem formularza, więc wszyscy użytkownicy potrzebują dostępu do tego endpointu.

## 9. Wydajność

### Metryki
- Czas wykonania: <50ms (bardzo szybkie - tylko odczyt z pamięci)
- Brak operacji I/O (baza danych, HTTP)
- Brak obliczeń

### Optymalizacje
- Wartość konfiguracji jest cache'owana przez `IOptions<T>` w ASP.NET Core
- Singleton pattern dla konfiguracji
- Brak potrzeby dodatkowego cachowania

## 10. Logging

### Kluczowe punkty logowania
```csharp
// Brak dedykowanego logowania dla tak prostego endpointu
// Ewentualnie tylko debug level:
_logger.LogDebug("XLotto feature enabled: {IsEnabled}", isEnabled);
```

### Error logging
```csharp
_logger.LogError(ex, "Error checking XLotto feature status");
```

## 11. Testy

### Unit Tests
```csharp
[Fact]
public async Task Handle_ReturnsTrue_WhenFeatureEnabled()
{
    // Arrange
    var options = Options.Create(new GoogleGeminiOptions { Enable = true });
    var handler = new Handler(options);
    var request = new Contracts.Request();

    // Act
    var result = await handler.Handle(request, CancellationToken.None);

    // Assert
    Assert.True(result.IsEnabled);
}

[Fact]
public async Task Handle_ReturnsFalse_WhenFeatureDisabled()
{
    // Arrange
    var options = Options.Create(new GoogleGeminiOptions { Enable = false });
    var handler = new Handler(options);
    var request = new Contracts.Request();

    // Act
    var result = await handler.Handle(request, CancellationToken.None);

    // Assert
    Assert.False(result.IsEnabled);
}
```

### Integration Tests
```csharp
[Fact]
public async Task GetXLottoIsEnabled_ReturnsOk_WithBooleanValue()
{
    // Arrange
    var client = _factory.CreateClient();
    var token = await GetValidJwtToken();
    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", token);

    // Act
    var response = await client.GetAsync("/api/xlotto/is-enabled");

    // Assert
    response.EnsureSuccessStatusCode();
    var content = await response.Content.ReadAsStringAsync();
    var result = JsonSerializer.Deserialize<XLottoIsEnabledResponse>(content);

    Assert.NotNull(result);
    Assert.True(result.Success);
    Assert.IsType<bool>(result.Data);
}
```

## 12. Integracja z Frontend

### TypeScript Interface
```typescript
export interface XLottoIsEnabledResponse {
  success: boolean;
  data: boolean;
}
```

### API Service Method
```typescript
public async xLottoIsEnabled(): Promise<XLottoIsEnabledResponse> {
  const url = `${this.apiUrl}/api/xlotto/is-enabled`;

  const response = await fetch(url, {
    method: 'GET',
    headers: this.getHeaders()
  });

  if (!response.ok) {
    if (response.status === 401) {
      throw new Error('Brak autoryzacji. Wymagany token JWT.');
    }

    if (response.status === 500) {
      const errorData = await response.json();
      throw new Error(errorData.detail || 'Błąd serwera podczas sprawdzania statusu funkcji XLotto');
    }

    throw new Error(`Error checking XLotto feature status: ${response.statusText}`);
  }

  const result: XLottoIsEnabledResponse = await response.json();
  return result;
}
```

### Frontend Usage (React Component)
```typescript
const [xLottoEnabled, setXLottoEnabled] = useState(false);

useEffect(() => {
  if (isOpen) {
    // ... other initialization

    // Check if XLotto feature is enabled
    const checkXLottoEnabled = async () => {
      try {
        const apiService = getApiService();
        if (apiService) {
          const response = await apiService.xLottoIsEnabled();
          setXLottoEnabled(response.data);
        }
      } catch (error) {
        console.error('Error checking XLotto feature status:', error);
        setXLottoEnabled(false);
      }
    };

    checkXLottoEnabled();
  }
}, [isOpen, mode, initialValues]);
```

### Conditional Rendering
```typescript
{xLottoEnabled && (
  <Button
    variant="secondary"
    onClick={handleLoadFromXLotto}
    disabled={submitting || loading}
    className="flex-1 sm:flex-none"
  >
    {loading ? 'Pobieranie...' : 'Pobierz z XLotto'}
  </Button>
)}
```

## 13. Uwagi implementacyjne

### Vertical Slice Architecture
```
Features/
└── XLotto/
    └── IsEnabled/
        ├── Contracts.cs     - Request/Response records
        ├── Handler.cs       - MediatR handler (odczyt z IOptions)
        └── Endpoint.cs      - Minimal API endpoint definition
```

### Options Pattern
```csharp
// Options/GoogleGeminiOptions.cs
public class GoogleGeminiOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gemini-2.0-flash";
    public bool Enable { get; set; } = false;
}
```

### Rejestracja w Program.cs
```csharp
// Configuration Options
builder.Services.Configure<GoogleGeminiOptions>(
    builder.Configuration.GetSection("GoogleGemini"));

// Endpoint
LottoTM.Server.Api.Features.XLotto.IsEnabled.Endpoint.AddEndpoint(app);
```

## 14. Wpływ na istniejącą funkcjonalność

### Modyfikacja GET /api/xlotto/actual-draws
Gdy `Enable = false`, endpoint zwraca pustą tablicę:
```csharp
if (!_googleGeminiOptions.Enable)
{
    _logger.LogDebug("GoogleGemini feature is disabled. Returning empty data.");
    return new Contracts.Response("{\"Data\":[]}");
}
```

### Warunkowe wyświetlanie UI
Frontend dynamicznie pokazuje/ukrywa przycisk "Pobierz z XLotto" w:
- `src/client/app01/src/components/draws/draw-form-modal.tsx`

## 15. Scenariusze użycia

### Scenariusz 1: Funkcja włączona (Development)
1. User otwiera formularz dodawania losowania
2. Frontend wywołuje `GET /api/xlotto/is-enabled`
3. Backend zwraca `{ success: true, data: true }`
4. Frontend wyświetla przycisk "Pobierz z XLotto"
5. User może kliknąć przycisk i pobrać wyniki

### Scenariusz 2: Funkcja wyłączona (Production)
1. User otwiera formularz dodawania losowania
2. Frontend wywołuje `GET /api/xlotto/is-enabled`
3. Backend zwraca `{ success: true, data: false }`
4. Frontend **ukrywa** przycisk "Pobierz z XLotto"
5. User musi wprowadzić liczby ręcznie

### Scenariusz 3: Przełączanie w trakcie działania
1. Administrator zmienia `Enable: false` → `Enable: true` w `appsettings.json`
2. Restart aplikacji (wymagany dla odświeżenia konfiguracji)
3. Nowi użytkownicy widzą przycisk "Pobierz z XLotto"

**Uwaga:** `IOptions<T>` w ASP.NET Core cache'uje wartości - wymagany restart aplikacji po zmianie konfiguracji. Dla hot reload można użyć `IOptionsSnapshot<T>`.

---

**Koniec dokumentu: XLotto Is Enabled API Implementation**
