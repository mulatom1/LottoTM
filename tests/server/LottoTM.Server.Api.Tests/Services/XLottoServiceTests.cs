using System.Net;
using System.Text;
using System.Text.Json;
using LottoTM.Server.Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace LottoTM.Server.Api.Tests.Services;

/// <summary>
/// Unit tests for XLottoService business logic
/// </summary>
public class XLottoServiceTests
{
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<XLottoService>> _mockLogger;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;

    public XLottoServiceTests()
    {
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<XLottoService>>();
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();

        // Setup default configuration
        var apiKeyBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes("test-gemini-api-key"));
        _mockConfiguration.Setup(c => c["GoogleGemini:ApiKey"]).Returns(apiKeyBase64);
        _mockConfiguration.Setup(c => c["GoogleGemini:Model"]).Returns("gemini-2.0-flash");
        
        // Setup XLotto:Url configuration section
        var urlSection = new Mock<IConfigurationSection>();
        urlSection.Setup(s => s.Value).Returns("https://xlotto.pl");
        _mockConfiguration.Setup(c => c.GetSection("XLotto:Url")).Returns(urlSection.Object);
    }

    /// <summary>
    /// Test successful retrieval of actual draws from XLotto
    /// </summary>
    [Fact]
    public async Task GetActualDraws_WithValidParameters_ReturnsJsonData()
    {
        // Arrange
        var date = new DateTime(2025, 1, 15);
        var lottoType = "LOTTO";
        var htmlContent = "<html><body>LOTTO 2025-01-15: 3, 12, 25, 31, 42, 48</body></html>";
        var geminiResponseJson = "{\"Data\":[{\"DrawDate\":\"2025-01-15\",\"GameType\":\"LOTTO\",\"Numbers\":[3,12,25,31,42,48]}]}";
        var geminiApiResponse = CreateGeminiApiResponse(geminiResponseJson);

        SetupHttpClientMock(htmlContent, geminiApiResponse);

        var service = new XLottoService(_mockHttpClientFactory.Object, _mockConfiguration.Object, _mockLogger.Object);

        // Act
        var result = await service.GetActualDraws(date, lottoType);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("DrawDate", result);
        Assert.Contains("2025-01-15", result);
        Assert.Contains("LOTTO", result);
    }

    /// <summary>
    /// Test retrieval with default parameters (null date)
    /// </summary>
    [Fact]
    public async Task GetActualDraws_WithNullDate_UsesToday()
    {
        // Arrange
        var htmlContent = "<html><body>LOTTO najnowsze wyniki</body></html>";
        var geminiResponseJson = "{\"Data\":[{\"DrawDate\":\"2025-11-14\",\"GameType\":\"LOTTO\",\"Numbers\":[1,5,15,25,35,45]}]}";
        var geminiApiResponse = CreateGeminiApiResponse(geminiResponseJson);

        SetupHttpClientMock(htmlContent, geminiApiResponse);

        var service = new XLottoService(_mockHttpClientFactory.Object, _mockConfiguration.Object, _mockLogger.Object);

        // Act
        var result = await service.GetActualDraws(null, "LOTTO");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Data", result);
    }

    /// <summary>
    /// Test retrieval for LOTTO PLUS game type
    /// </summary>
    [Fact]
    public async Task GetActualDraws_WithLottoPlusType_ReturnsCorrectData()
    {
        // Arrange
        var date = new DateTime(2025, 1, 15);
        var htmlContent = "<html><body>LOTTO PLUS 2025-01-15: 2, 8, 18, 28, 38, 48</body></html>";
        var geminiResponseJson = "{\"Data\":[{\"DrawDate\":\"2025-01-15\",\"GameType\":\"LOTTO PLUS\",\"Numbers\":[2,8,18,28,38,48]}]}";
        var geminiApiResponse = CreateGeminiApiResponse(geminiResponseJson);

        SetupHttpClientMock(htmlContent, geminiApiResponse);

        var service = new XLottoService(_mockHttpClientFactory.Object, _mockConfiguration.Object, _mockLogger.Object);

        // Act
        var result = await service.GetActualDraws(date, "LOTTO PLUS");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("LOTTO PLUS", result);
    }

    /// <summary>
    /// Test handling when XLotto website is unreachable
    /// </summary>
    [Fact]
    public async Task GetActualDraws_WhenXLottoWebsiteUnreachable_ThrowsInvalidOperationException()
    {
        // Arrange
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("xlotto.pl")),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection failed"));

        var httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var service = new XLottoService(_mockHttpClientFactory.Object, _mockConfiguration.Object, _mockLogger.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetActualDraws(new DateTime(2025, 1, 15), "LOTTO")
        );

        Assert.Contains("Failed to fetch draw results from XLotto", exception.Message);
    }

    /// <summary>
    /// Test handling when Gemini API returns error
    /// </summary>
    [Fact]
    public async Task GetActualDraws_WhenGeminiApiReturnsError_ThrowsInvalidOperationException()
    {
        // Arrange
        var htmlContent = "<html><body>LOTTO content</body></html>";

        _mockHttpMessageHandler
            .Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(htmlContent, Encoding.UTF8, "text/html")
            })
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent("API Error")
            });

        var httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var service = new XLottoService(_mockHttpClientFactory.Object, _mockConfiguration.Object, _mockLogger.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetActualDraws(new DateTime(2025, 1, 15), "LOTTO")
        );

        Assert.Contains("Failed to fetch draw results from XLotto or Gemini API", exception.Message);
    }

    /// <summary>
    /// Test handling when Gemini API returns empty response
    /// </summary>
    [Fact]
    public async Task GetActualDraws_WhenGeminiReturnsEmptyText_ThrowsInvalidOperationException()
    {
        // Arrange
        var htmlContent = "<html><body>LOTTO content</body></html>";
        var geminiApiResponse = CreateGeminiApiResponse("");

        SetupHttpClientMock(htmlContent, geminiApiResponse);

        var service = new XLottoService(_mockHttpClientFactory.Object, _mockConfiguration.Object, _mockLogger.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetActualDraws(new DateTime(2025, 1, 15), "LOTTO")
        );

        Assert.Contains("Gemini API returned empty response", exception.Message);
    }

    /// <summary>
    /// Test handling when API key is not configured
    /// </summary>
    [Fact]
    public async Task GetActualDraws_WhenApiKeyNotConfigured_ThrowsInvalidOperationException()
    {
        // Arrange
        _mockConfiguration.Setup(c => c["GoogleGemini:ApiKey"]).Returns((string?)null);
        _mockConfiguration.Setup(c => c.GetSection("XLotto:Url").Value).Returns("https://xlotto.pl");

        var htmlContent = "<html><body>LOTTO content</body></html>";
        SetupXLottoWebsiteMock(htmlContent);

        var service = new XLottoService(_mockHttpClientFactory.Object, _mockConfiguration.Object, _mockLogger.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetActualDraws(new DateTime(2025, 1, 15), "LOTTO")
        );

        Assert.Contains("Google Gemini API Key not configured", exception.Message);
    }

    /// <summary>
    /// Test cleaning of markdown code blocks from Gemini response
    /// </summary>
    [Fact]
    public async Task GetActualDraws_WithMarkdownCodeBlocks_CleansResponse()
    {
        // Arrange
        var htmlContent = "<html><body>LOTTO content</body></html>";
        var jsonContent = "{\"Data\":[{\"DrawDate\":\"2025-01-15\",\"GameType\":\"LOTTO\",\"Numbers\":[1,2,3,4,5,6]}]}";
        var geminiResponseWithMarkdown = "```json\n" + jsonContent + "\n```";
        var geminiApiResponse = CreateGeminiApiResponse(geminiResponseWithMarkdown);

        SetupHttpClientMock(htmlContent, geminiApiResponse);

        var service = new XLottoService(_mockHttpClientFactory.Object, _mockConfiguration.Object, _mockLogger.Object);

        // Act
        var result = await service.GetActualDraws(new DateTime(2025, 1, 15), "LOTTO");

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("```", result);
        Assert.Contains("Data", result);
        
        // Verify it's valid JSON
        var parsed = JsonSerializer.Deserialize<JsonElement>(result);
        Assert.True(parsed.ValueKind == JsonValueKind.Object);
    }

    /// <summary>
    /// Test handling when no results found for given date
    /// </summary>
    [Fact]
    public async Task GetActualDraws_WhenNoResultsFound_ReturnsEmptyData()
    {
        // Arrange
        var htmlContent = "<html><body>LOTTO content without specific date</body></html>";
        var geminiResponseJson = "{\"Data\":[]}";
        var geminiApiResponse = CreateGeminiApiResponse(geminiResponseJson);

        SetupHttpClientMock(htmlContent, geminiApiResponse);

        var service = new XLottoService(_mockHttpClientFactory.Object, _mockConfiguration.Object, _mockLogger.Object);

        // Act
        var result = await service.GetActualDraws(new DateTime(2025, 1, 15), "LOTTO");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("\"Data\":[]", result.Replace(" ", ""));
    }

    /// <summary>
    /// Test handling of different character encodings
    /// </summary>
    [Fact]
    public async Task GetActualDraws_WithDifferentEncoding_HandlesCorrectly()
    {
        // Arrange
        var htmlContent = "<html><body>LOTTO z polskimi znakami: ąćęłńóśźż</body></html>";
        var geminiResponseJson = "{\"Data\":[{\"DrawDate\":\"2025-01-15\",\"GameType\":\"LOTTO\",\"Numbers\":[1,2,3,4,5,6]}]}";
        var geminiApiResponse = CreateGeminiApiResponse(geminiResponseJson);

        SetupHttpClientMock(htmlContent, geminiApiResponse, "iso-8859-2");

        var service = new XLottoService(_mockHttpClientFactory.Object, _mockConfiguration.Object, _mockLogger.Object);

        // Act
        var result = await service.GetActualDraws(new DateTime(2025, 1, 15), "LOTTO");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Data", result);
    }

    /// <summary>
    /// Test handling when Gemini returns invalid JSON
    /// </summary>
    [Fact]
    public async Task GetActualDraws_WhenGeminiReturnsInvalidJson_ThrowsInvalidOperationException()
    {
        // Arrange
        var htmlContent = "<html><body>LOTTO content</body></html>";
        var invalidJson = "This is not valid JSON {{{";
        var geminiApiResponse = CreateGeminiApiResponse(invalidJson);

        SetupHttpClientMock(htmlContent, geminiApiResponse);

        var service = new XLottoService(_mockHttpClientFactory.Object, _mockConfiguration.Object, _mockLogger.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetActualDraws(new DateTime(2025, 1, 15), "LOTTO")
        );

        Assert.Contains("Failed to parse API response", exception.Message);
    }

    /// <summary>
    /// Test logging during successful operation
    /// </summary>
    [Fact]
    public async Task GetActualDraws_WithSuccessfulOperation_LogsInformation()
    {
        // Arrange
        var htmlContent = "<html><body>LOTTO content</body></html>";
        var geminiResponseJson = "{\"Data\":[{\"DrawDate\":\"2025-01-15\",\"GameType\":\"LOTTO\",\"Numbers\":[1,2,3,4,5,6]}]}";
        var geminiApiResponse = CreateGeminiApiResponse(geminiResponseJson);

        SetupHttpClientMock(htmlContent, geminiApiResponse);

        var service = new XLottoService(_mockHttpClientFactory.Object, _mockConfiguration.Object, _mockLogger.Object);

        // Act
        await service.GetActualDraws(new DateTime(2025, 1, 15), "LOTTO");

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Fetching XLotto website content")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Successfully fetched XLotto website content")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Test logging during error scenarios
    /// </summary>
    [Fact]
    public async Task GetActualDraws_WithError_LogsError()
    {
        // Arrange
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var service = new XLottoService(_mockHttpClientFactory.Object, _mockConfiguration.Object, _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetActualDraws(new DateTime(2025, 1, 15), "LOTTO")
        );

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("HTTP request failed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // Helper methods

    private void SetupHttpClientMock(string xlottoHtmlContent, string geminiApiResponse, string? charset = null)
    {
        var responseMessage = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(xlottoHtmlContent, Encoding.UTF8, "text/html")
        };

        if (charset != null)
        {
            responseMessage.Content.Headers.ContentType!.CharSet = charset;
        }

        _mockHttpMessageHandler
            .Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responseMessage)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(geminiApiResponse, Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
    }

    private void SetupXLottoWebsiteMock(string htmlContent)
    {
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("xlotto.pl")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(htmlContent, Encoding.UTF8, "text/html")
            });

        var httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
    }

    private string CreateGeminiApiResponse(string generatedText)
    {
        var response = new
        {
            candidates = new[]
            {
                new
                {
                    content = new
                    {
                        parts = new[]
                        {
                            new { text = generatedText }
                        }
                    }
                }
            }
        };

        return JsonSerializer.Serialize(response);
    }
}
