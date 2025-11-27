using System.Net;
using System.Text;
using System.Text.Json;
using LottoTM.Server.Api.Services.LottoOpenApi;
using LottoTM.Server.Api.Services.LottoOpenApi.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace LottoTM.Server.Api.Tests.Services;

/// <summary>
/// Unit tests for LottoOpenApiService business logic
/// </summary>
public class LottoOpenApiServiceTests
{
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<LottoOpenApiService>> _mockLogger;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;

    public LottoOpenApiServiceTests()
    {
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<LottoOpenApiService>>();
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();

        // Setup configuration using IConfigurationSection mock
        var urlSection = new Mock<IConfigurationSection>();
        urlSection.Setup(s => s.Value).Returns("https://www.lotto.pl");
        _mockConfiguration.Setup(c => c.GetSection("LottoOpenApi:Url")).Returns(urlSection.Object);

        var apiKeySection = new Mock<IConfigurationSection>();
        apiKeySection.Setup(s => s.Value).Returns("test-api-key");
        _mockConfiguration.Setup(c => c.GetSection("LottoOpenApi:ApiKey")).Returns(apiKeySection.Object);
    }

    /// <summary>
    /// Test successful retrieval of draw results from Lotto Open API
    /// </summary>
    [Fact]
    public async Task GetDrawsLottoByDate_WithValidParameters_ReturnsDrawResponse()
    {
        // Arrange
        var date = new DateTime(2025, 1, 15);
        var apiResponse = CreateLottoOpenApiResponse(
            new DateTime(2025, 1, 15),
            "Lotto",
            new[] { 3, 12, 25, 31, 42, 48 });

        SetupHttpClientMock(apiResponse);

        var service = new LottoOpenApiService(_mockHttpClientFactory.Object, _mockConfiguration.Object, _mockLogger.Object);

        // Act
        var result = await service.GetDrawsLottoByDate(DateOnly.FromDateTime(date));

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalRows);
        Assert.NotNull(result.Items);
        Assert.Single(result.Items);
        Assert.NotNull(result.Items[0].Results);
        Assert.Single(result.Items[0].Results);
        Assert.Equal("Lotto", result.Items[0].Results[0].GameType);
        Assert.Equal(new[] { 3, 12, 25, 31, 42, 48 }, result.Items[0].Results[0].ResultsJson);
    }

    /// <summary>
    /// Test retrieval with default parameters (null date)
    /// </summary>
    [Fact]
    public async Task GetDrawsLottoByDate_WithNullDate_UsesToday()
    {
        // Arrange
        var apiResponse = CreateLottoOpenApiResponse(
            DateTime.Today,
            "Lotto",
            new[] { 1, 5, 15, 25, 35, 45 });

        SetupHttpClientMock(apiResponse);

        var service = new LottoOpenApiService(_mockHttpClientFactory.Object, _mockConfiguration.Object, _mockLogger.Object);

        // Act
        var result = await service.GetDrawsLottoByDate(null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalRows);
    }

    /// <summary>
    /// Test retrieval for both LOTTO and LOTTO PLUS game types
    /// </summary>
    [Fact]
    public async Task GetDrawsLottoByDate_WithMultipleGameTypes_ReturnsAllResults()
    {
        // Arrange
        var date = new DateTime(2025, 1, 15);
        var apiResponse = CreateLottoOpenApiResponseWithMultipleGames(date);

        SetupHttpClientMock(apiResponse);

        var service = new LottoOpenApiService(_mockHttpClientFactory.Object, _mockConfiguration.Object, _mockLogger.Object);

        // Act
        var result = await service.GetDrawsLottoByDate(DateOnly.FromDateTime(date));

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalRows);
        Assert.NotNull(result.Items);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal("Lotto", result.Items[0].Results?[0].GameType);
        Assert.Equal("LottoPlus", result.Items[1].Results?[0].GameType);
    }

    /// <summary>
    /// Test handling when API is unreachable
    /// </summary>
    [Fact]
    public async Task GetDrawsLottoByDate_WhenApiUnreachable_ThrowsInvalidOperationException()
    {
        // Arrange
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection failed"));

        var httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var service = new LottoOpenApiService(_mockHttpClientFactory.Object, _mockConfiguration.Object, _mockLogger.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetDrawsLottoByDate(DateOnly.FromDateTime(new DateTime(2025, 1, 15)))
        );

        Assert.Contains("Failed to fetch draw results from Lotto Open API", exception.Message);
    }

    /// <summary>
    /// Test handling when API returns error status code
    /// </summary>
    [Fact]
    public async Task GetDrawsLottoByDate_WhenApiReturnsError_ThrowsInvalidOperationException()
    {
        // Arrange
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent("API Error")
            });

        var httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var service = new LottoOpenApiService(_mockHttpClientFactory.Object, _mockConfiguration.Object, _mockLogger.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetDrawsLottoByDate(DateOnly.FromDateTime(new DateTime(2025, 1, 15)))
        );

        Assert.Contains("Lotto Open API returned status", exception.Message);
    }

    /// <summary>
    /// Test handling when API returns empty items
    /// </summary>
    [Fact]
    public async Task GetDrawsLottoByDate_WhenApiReturnsEmptyItems_ReturnsEmptyResponse()
    {
        // Arrange
        var apiResponse = CreateEmptyLottoOpenApiResponse();

        SetupHttpClientMock(apiResponse);

        var service = new LottoOpenApiService(_mockHttpClientFactory.Object, _mockConfiguration.Object, _mockLogger.Object);

        // Act
        var result = await service.GetDrawsLottoByDate(DateOnly.FromDateTime(new DateTime(2025, 1, 15)));

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalRows);
        Assert.NotNull(result.Items);
        Assert.Empty(result.Items);
    }

    /// <summary>
    /// Test handling when URL is not configured
    /// </summary>
    [Fact]
    public async Task GetDrawsLottoByDate_WhenUrlNotConfigured_ThrowsInvalidOperationException()
    {
        // Arrange
        var emptyUrlSection = new Mock<IConfigurationSection>();
        emptyUrlSection.Setup(s => s.Value).Returns(string.Empty);
        _mockConfiguration.Setup(c => c.GetSection("LottoOpenApi:Url")).Returns(emptyUrlSection.Object);

        // Setup HttpClient so the code can reach the URL validation
        var httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var service = new LottoOpenApiService(_mockHttpClientFactory.Object, _mockConfiguration.Object, _mockLogger.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetDrawsLottoByDate(DateOnly.FromDateTime(new DateTime(2025, 1, 15)))
        );

        Assert.Contains("LottoOpenApi URL not configured", exception.Message);
    }

    /// <summary>
    /// Test handling when API returns invalid JSON
    /// </summary>
    [Fact]
    public async Task GetDrawsLottoByDate_WhenApiReturnsInvalidJson_ThrowsInvalidOperationException()
    {
        // Arrange
        var invalidJson = "This is not valid JSON {{{";

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(invalidJson, Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var service = new LottoOpenApiService(_mockHttpClientFactory.Object, _mockConfiguration.Object, _mockLogger.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetDrawsLottoByDate(DateOnly.FromDateTime(new DateTime(2025, 1, 15)))
        );

        Assert.Contains("Failed to parse API response", exception.Message);
    }

    /// <summary>
    /// Test handling when API returns null response - should return default empty response
    /// </summary>
    [Fact]
    public async Task GetDrawsLottoByDate_WhenApiReturnsNull_ReturnsDefaultEmptyResponse()
    {
        // Arrange
        var nullJson = "null";

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(nullJson, Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var service = new LottoOpenApiService(_mockHttpClientFactory.Object, _mockConfiguration.Object, _mockLogger.Object);

        // Act
        var result = await service.GetDrawsLottoByDate(DateOnly.FromDateTime(new DateTime(2025, 1, 15)));

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalRows);
        Assert.NotNull(result.Items);
        Assert.Empty(result.Items);
    }

    /// <summary>
    /// Test logging during successful operation
    /// </summary>
    [Fact]
    public async Task GetDrawsLottoByDate_WithSuccessfulOperation_LogsDebugInformation()
    {
        // Arrange
        var apiResponse = CreateLottoOpenApiResponse(
            new DateTime(2025, 1, 15),
            "Lotto",
            new[] { 1, 2, 3, 4, 5, 6 });

        SetupHttpClientMock(apiResponse);

        var service = new LottoOpenApiService(_mockHttpClientFactory.Object, _mockConfiguration.Object, _mockLogger.Object);

        // Act
        await service.GetDrawsLottoByDate(DateOnly.FromDateTime(new DateTime(2025, 1, 15)));

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Fetching draws from Lotto Open API")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Received response from Lotto Open API")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Test logging during error scenarios
    /// </summary>
    [Fact]
    public async Task GetDrawsLottoByDate_WithHttpError_LogsError()
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

        var service = new LottoOpenApiService(_mockHttpClientFactory.Object, _mockConfiguration.Object, _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetDrawsLottoByDate(DateOnly.FromDateTime(new DateTime(2025, 1, 15)))
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

    /// <summary>
    /// Test that HTTP headers are set correctly
    /// </summary>
    [Fact]
    public async Task GetDrawsLottoByDate_SetsCorrectHttpHeaders()
    {
        // Arrange
        var apiResponse = CreateLottoOpenApiResponse(
            DateTime.Today,
            "Lotto",
            new[] { 1, 2, 3, 4, 5, 6 });

        HttpRequestMessage? capturedRequest = null;
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(apiResponse, Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var service = new LottoOpenApiService(_mockHttpClientFactory.Object, _mockConfiguration.Object, _mockLogger.Object);

        // Act
        await service.GetDrawsLottoByDate(DateOnly.FromDateTime(DateTime.Today));

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Contains(capturedRequest.Headers, h => h.Key == "User-Agent" && h.Value.Contains("LottoTM.Server.Api.LottoOpenApiService/1.0"));
        Assert.Contains(capturedRequest.Headers, h => h.Key == "Accept" && h.Value.Contains("application/json"));
        Assert.Contains(capturedRequest.Headers, h => h.Key == "secret" && h.Value.Contains("test-api-key"));
    }

    /// <summary>
    /// Test that correct API URL is constructed
    /// </summary>
    [Fact]
    public async Task GetDrawsLottoByDate_ConstructsCorrectUrl()
    {
        // Arrange
        var date = new DateTime(2025, 1, 15);
        var apiResponse = CreateLottoOpenApiResponse(date, "Lotto", new[] { 1, 2, 3, 4, 5, 6 });

        HttpRequestMessage? capturedRequest = null;
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(apiResponse, Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var service = new LottoOpenApiService(_mockHttpClientFactory.Object, _mockConfiguration.Object, _mockLogger.Object);

        // Act
        await service.GetDrawsLottoByDate(DateOnly.FromDateTime(date));

        // Assert
        Assert.NotNull(capturedRequest);
        var expectedUrl = "https://www.lotto.pl/api/open/v1/lotteries/draw-results/by-date-per-game?drawDate=2025-01-15&gameType=Lotto&size=10&sort=drawDate&order=DESC&index=1";
        Assert.Equal(expectedUrl, capturedRequest.RequestUri?.ToString());
    }

    // Helper methods

    private void SetupHttpClientMock(string apiResponse)
    {
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(apiResponse, Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
    }

    private string CreateLottoOpenApiResponse(DateTime drawDate, string gameType, int[] numbers)
    {
        var response = new
        {
            totalRows = 1,
            items = new[]
            {
                new
                {
                    drawSystemId = 7273,
                    drawDate = drawDate.ToString("yyyy-MM-ddT21:00:00"),
                    gameType = gameType,
                    multiplierValue = 0,
                    results = new[]
                    {
                        new
                        {
                            drawDate = drawDate.ToString("yyyy-MM-ddT21:00:00"),
                            drawSystemId = 7273,
                            gameType = gameType,
                            resultsJson = numbers,
                            specialResults = Array.Empty<object>()
                        }
                    },
                    showSpecialResults = true,
                    isNewEuroJackpotDraw = false
                }
            },
            meta = new { },
            code = 200
        };

        return JsonSerializer.Serialize(response);
    }

    private string CreateLottoOpenApiResponseWithMultipleGames(DateTime drawDate)
    {
        var response = new
        {
            totalRows = 2,
            items = new[]
            {
                new
                {
                    drawSystemId = 7273,
                    drawDate = drawDate.ToString("yyyy-MM-ddT21:00:00"),
                    gameType = "Lotto",
                    multiplierValue = 0,
                    results = new[]
                    {
                        new
                        {
                            drawDate = drawDate.ToString("yyyy-MM-ddT21:00:00"),
                            drawSystemId = 7273,
                            gameType = "Lotto",
                            resultsJson = new[] { 1, 14, 47, 26, 5, 46 },
                            specialResults = Array.Empty<object>()
                        }
                    },
                    showSpecialResults = true,
                    isNewEuroJackpotDraw = false
                },
                new
                {
                    drawSystemId = 7274,
                    drawDate = drawDate.ToString("yyyy-MM-ddT21:00:00"),
                    gameType = "LottoPlus",
                    multiplierValue = 0,
                    results = new[]
                    {
                        new
                        {
                            drawDate = drawDate.ToString("yyyy-MM-ddT21:00:00"),
                            drawSystemId = 7274,
                            gameType = "LottoPlus",
                            resultsJson = new[] { 49, 23, 27, 25, 5, 6 },
                            specialResults = Array.Empty<object>()
                        }
                    },
                    showSpecialResults = true,
                    isNewEuroJackpotDraw = false
                }
            },
            meta = new { },
            code = 200
        };

        return JsonSerializer.Serialize(response);
    }

    private string CreateEmptyLottoOpenApiResponse()
    {
        var response = new
        {
            totalRows = 0,
            items = Array.Empty<object>(),
            meta = new { },
            code = 200
        };

        return JsonSerializer.Serialize(response);
    }
}
