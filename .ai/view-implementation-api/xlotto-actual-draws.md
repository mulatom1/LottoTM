# Plan implementacji API: XLotto Actual Draws

## 1. Przegląd

Endpoint służy do pobierania aktualnych wyników losowań z witryny XLotto.pl za pomocą Google Gemini API. Funkcjonalność umożliwia administratorom szybkie wypełnienie formularza wyników losowania danymi pobranymi automatycznie z zewnętrznej strony.

**Główne funkcje:**
- Pobieranie zawartości HTML ze strony XLotto.pl
- Przetwarzanie treści przez Google Gemini API w celu ekstrakcji wyników
- Zwracanie wyników dla LOTTO i LOTTO PLUS w formacie JSON

## 2. Endpoint Definition

**Ścieżka:** `GET /api/xlotto/actual-draws`

**Typ dostępu:** Chroniony (wymaga autentykacji JWT)

**Parametry zapytania:**
- `Date` (wymagany): Data losowania w formacie YYYY-MM-DD
- `LottoType` (wymagany): Typ losowania ("LOTTO" lub "LOTTO PLUS")

**Przykład zapytania:**
```
GET /api/xlotto/actual-draws?Date=2025-11-04&LottoType=LOTTO
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
  "data": "{\"Data\": [ { \"DrawDate\": \"2025-11-04\", \"GameType\": \"LOTTO\", \"Numbers\": [4, 23, 34, 37, 45, 46] }, { \"DrawDate\": \"2025-11-04\", \"GameType\": \"LOTTO PLUS\", \"Numbers\": [15, 17, 18, 23, 43, 46] } ]}"
}
```

**Struktura JSON w polu `data`:**
```json
{
  "Data": [
    {
      "DrawDate": "2025-11-04",
      "GameType": "LOTTO",
      "Numbers": [4, 23, 34, 37, 45, 46]
    },
    {
      "DrawDate": "2025-11-04",
      "GameType": "LOTTO PLUS",
      "Numbers": [15, 17, 18, 23, 43, 46]
    }
  ]
}
```

### Response (błąd)
**Status Code:** 401 Unauthorized
```json
{
  "error": "Brak autoryzacji. Wymagany token JWT."
}
```

**Status Code:** 400 Bad Request
```json
{
  "errors": {
    "Date": ["Data jest wymagana", "Nieprawidłowy format daty"],
    "LottoType": ["Typ losowania jest wymagany", "Dozwolone wartości: LOTTO, LOTTO PLUS"]
  }
}
```

**Status Code:** 500 Internal Server Error
```json
{
  "error": "Nie udało się pobrać wyników z XLotto lub Gemini API"
}
```

## 4. Logika biznesowa

### Krok 1: Pobranie zawartości XLotto
```csharp
var httpClient = _httpClientFactory.CreateClient();
httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0...");
var response = await httpClient.GetAsync("https://www.xlotto.pl/");
var htmlContent = await response.Content.ReadAsStringAsync();
```

### Krok 2: Wywołanie Google Gemini API
```csharp
var geminiApiKey = _configuration["GoogleGemini:ApiKey"];
var geminiModel = _configuration["GoogleGemini:Model"] ?? "gemini-2.0-flash";
var geminiApiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{geminiModel}:generateContent?key={geminiApiKey}";

var prompt = "Przeszukaj zawartość contentu i podaj najnowsze wyniki dla LOTTO, LOTTO PLUS. Wynik oddaj w formacie w JSON { \"Draws\": [ \"LOTTO\": [1,2,3,4,5,6], \"LOTTO PLUS\": [1,2,3,4,5,6] ] } i nie zwracaj niczego innego.Tylko JSON.";

var geminiRequest = new {
    contents = new[] {
        new {
            parts = new[] {
                new { text = $"{prompt}\n\nHTML Content:\n{htmlContent}" }
            }
        }
    }
};
```

### Krok 3: Przetworzenie odpowiedzi Gemini
```csharp
var geminiResult = JsonSerializer.Deserialize<JsonElement>(geminiResponseContent);
var generatedText = geminiResult
    .GetProperty("candidates")[0]
    .GetProperty("content")
    .GetProperty("parts")[0]
    .GetProperty("text")
    .GetString();

// Czyszczenie markdown code blocks
var cleanedText = generatedText.Trim();
if (cleanedText.StartsWith("```json"))
    cleanedText = cleanedText.Substring(7);
if (cleanedText.EndsWith("```"))
    cleanedText = cleanedText.Substring(0, cleanedText.Length - 3);

cleanedText = cleanedText.Trim();

// Walidacja JSON
JsonSerializer.Deserialize<JsonElement>(cleanedText);

return cleanedText;
```

## 5. Walidacja

### Walidacja parametrów
- **Date:**
  - Wymagany
  - Format: YYYY-MM-DD
  - Nie może być w przyszłości

- **LottoType:**
  - Wymagany
  - Dozwolone wartości: "LOTTO", "LOTTO PLUS"

### Walidacja odpowiedzi Gemini
- Sprawdzenie czy odpowiedź zawiera poprawny JSON
- Walidacja struktury danych (Data array z obiektami DrawDate, GameType, Numbers)
- Numbers musi być tablicą 6 liczb w zakresie 1-49

## 6. Obsługa błędów

### HTTP Request Errors
```csharp
catch (HttpRequestException ex) {
    _logger.LogError(ex, "HTTP request failed while fetching draw results");
    throw new InvalidOperationException("Failed to fetch draw results from XLotto or Gemini API", ex);
}
```

### JSON Parse Errors
```csharp
catch (JsonException ex) {
    _logger.LogError(ex, "Failed to parse JSON response");
    throw new InvalidOperationException("Failed to parse API response", ex);
}
```

### General Errors
```csharp
catch (Exception ex) {
    _logger.LogError(ex, "Unexpected error while fetching draw results");
    throw;
}
```

## 7. Konfiguracja

### appsettings.json
```json
{
  "GoogleGemini": {
    "ApiKey": "YOUR_GEMINI_API_KEY",
    "Model": "gemini-2.0-flash"
  }
}
```

### Wymagane zmienne środowiskowe
- `GoogleGemini:ApiKey` - Klucz API Google Gemini (obowiązkowy)
- `GoogleGemini:Model` - Model Gemini do użycia (opcjonalny, domyślnie: gemini-2.0-flash)

## 8. Bezpieczeństwo

### Autoryzacja
- Endpoint wymaga ważnego JWT tokenu
- Token przekazywany w header `Authorization: Bearer <token>`

### Rate Limiting
- Rozważyć implementację rate limiting dla ochrony przed nadużyciami
- Gemini API ma własne limity zapytań

### API Key Security
- Klucz API Gemini przechowywany w konfiguracji (nie w kodzie)
- W produkcji: używać Azure Key Vault lub podobnego rozwiązania

## 9. Wydajność

### Metryki
- Czas pobierania HTML z XLotto.pl: ~200-500ms
- Czas przetwarzania przez Gemini API: ~1-3 sekundy
- Całkowity czas: ~2-4 sekundy

### Optymalizacje
- Cache wyników (opcjonalnie, TTL: 15-30 minut)
- Asynchroniczne wywołania HTTP
- Retry policy dla błędów przejściowych

## 10. Logging

### Kluczowe punkty logowania
```csharp
_logger.LogInformation("Fetching XLotto website content...");
_logger.LogInformation("Successfully fetched XLotto website content. Size: {Size} bytes", htmlContent.Length);
_logger.LogInformation("Sending request to Google Gemini API...");
_logger.LogInformation("Received response from Google Gemini API");
_logger.LogInformation("Successfully extracted draw results from Gemini response");
```

### Error logging
```csharp
_logger.LogError(ex, "HTTP request failed while fetching draw results");
_logger.LogError(ex, "Failed to parse JSON response");
_logger.LogError(ex, "Unexpected error while fetching draw results");
```

## 11. Testy

### Unit Tests
- Test parsowania odpowiedzi Gemini
- Test czyszczenia markdown code blocks
- Test walidacji JSON

### Integration Tests
- Test pełnego flow z mock HTTP client
- Test obsługi błędów HTTP
- Test obsługi błędów JSON parsing

### E2E Tests (opcjonalnie)
- Test z rzeczywistym Gemini API (w środowisku testowym)

## 12. Integracja z Frontend

### TypeScript Interface
```typescript
export interface XLottoActualDrawsResponse {
  success: boolean;
  data: string;
}

export interface XLottoDrawItem {
  DrawDate: string;
  GameType: string;
  Numbers: number[];
}

export interface XLottoDrawsData {
  Data: XLottoDrawItem[];
}
```

### API Service Method
```typescript
public async xLottoActualDraws(request: XLottoActualDrawsRequest): Promise<XLottoActualDrawsResponse> {
  const params = new URLSearchParams();
  params.append('Date', request.date);
  params.append('LottoType', request.lottoType);

  const url = `${this.apiUrl}/api/xlotto/actual-draws?${params.toString()}`;
  const response = await fetch(url, {
    method: 'GET',
    headers: this.getHeaders()
  });

  if (!response.ok) {
    // Error handling
    throw new Error('Failed to fetch XLotto draws');
  }

  return await response.json();
}
```

### Frontend Usage
```typescript
const handleLoadFromXLotto = async () => {
  if (!formData.drawDate) {
    alert('Proszę najpierw wybrać datę losowania');
    return;
  }

  setLoading(true);
  try {
    const apiService = getApiService();
    const response = await apiService.xLottoActualDraws({
      date: formData.drawDate,
      lottoType: formData.lottoType,
    });

    const drawsData: XLottoDrawsData = JSON.parse(response.data);
    const drawItem = drawsData.Data.find(
      (item) => item.GameType === formData.lottoType
    );

    if (drawItem) {
      setFormData((prev) => ({
        ...prev,
        numbers: drawItem.Numbers as (number | '')[],
      }));
    }
  } catch (error) {
    console.error('Error loading numbers from XLotto:', error);
    alert('Błąd podczas pobierania danych z XLotto');
  } finally {
    setLoading(false);
  }
};
```

## 13. Uwagi implementacyjne

### Vertical Slice Architecture
```
Features/
└── XLotto/
    └── ActualDraws/
        ├── Contracts.cs     - Request/Response DTOs
        ├── Handler.cs       - MediatR handler z logiką biznesową
        └── Endpoint.cs      - Minimal API endpoint definition
```

### Services
```
Services/
├── IXLottoService.cs       - Interface
└── XLottoService.cs        - Implementacja z HttpClient i Gemini API
```

### Rejestracja w Program.cs
```csharp
// Services
builder.Services.AddHttpClient();
builder.Services.AddScoped<IXLottoService, XLottoService>();

// Endpoint
app.MapXLottoActualDrawsEndpoint();
```

---

**Koniec dokumentu: XLotto Actual Draws API Implementation**
