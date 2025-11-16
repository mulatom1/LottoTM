using LottoTM.Server.Api.Features.XLotto.ActualDraws;
using LottoTM.Server.Api.Options;
using LottoTM.Server.Api.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace LottoTM.Server.Api.Tests.Features.XLotto.ActualDraws;

/// <summary>
/// Unit tests for the XLotto ActualDraws Handler
/// </summary>
public class HandlerTests
{
    private readonly Mock<IXLottoService> _mockXLottoService;
    private readonly Mock<ILogger<Handler>> _mockLogger;
    private readonly Mock<IOptions<GoogleGeminiOptions>> _mockOptions;
    private Handler _handler;

    public HandlerTests()
    {
        _mockXLottoService = new Mock<IXLottoService>();
        _mockLogger = new Mock<ILogger<Handler>>();
        _mockOptions = new Mock<IOptions<GoogleGeminiOptions>>();

        // Default: Feature enabled
        _mockOptions.Setup(x => x.Value).Returns(new GoogleGeminiOptions { Enable = true });
        _handler = new Handler(_mockXLottoService.Object, _mockOptions.Object, _mockLogger.Object);
    }

    /// <summary>
    /// Test successful handling of valid request
    /// </summary>
    [Fact]
    public async Task Handle_WithValidRequest_ReturnsJsonData()
    {
        // Arrange
        var date = new DateTime(2025, 1, 15);
        var lottoType = "LOTTO";
        var expectedJsonData = "{\"Data\":[{\"DrawDate\":\"2025-01-15\",\"GameType\":\"LOTTO\",\"Numbers\":[3,12,25,31,42,48]}]}";

        _mockXLottoService
            .Setup(s => s.GetActualDraws(date, lottoType))
            .ReturnsAsync(expectedJsonData);

        var request = new Contracts.Request(date, lottoType);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedJsonData, result.JsonData);
        _mockXLottoService.Verify(s => s.GetActualDraws(date, lottoType), Times.Once);
    }

    /// <summary>
    /// Test handling with LOTTO PLUS game type
    /// </summary>
    [Fact]
    public async Task Handle_WithLottoPlusType_ReturnsCorrectJsonData()
    {
        // Arrange
        var date = new DateTime(2025, 1, 15);
        var lottoType = "LOTTO PLUS";
        var expectedJsonData = "{\"Data\":[{\"DrawDate\":\"2025-01-15\",\"GameType\":\"LOTTO PLUS\",\"Numbers\":[2,8,18,28,38,48]}]}";

        _mockXLottoService
            .Setup(s => s.GetActualDraws(date, lottoType))
            .ReturnsAsync(expectedJsonData);

        var request = new Contracts.Request(date, lottoType);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedJsonData, result.JsonData);
        _mockXLottoService.Verify(s => s.GetActualDraws(date, lottoType), Times.Once);
    }

    /// <summary>
    /// Test handling when service returns empty results
    /// </summary>
    [Fact]
    public async Task Handle_WithNoResults_ReturnsEmptyJsonData()
    {
        // Arrange
        var date = new DateTime(2025, 1, 15);
        var lottoType = "LOTTO";
        var expectedJsonData = "{\"Data\":[]}";

        _mockXLottoService
            .Setup(s => s.GetActualDraws(date, lottoType))
            .ReturnsAsync(expectedJsonData);

        var request = new Contracts.Request(date, lottoType);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedJsonData, result.JsonData);
    }

    /// <summary>
    /// Test handling when service throws InvalidOperationException
    /// </summary>
    [Fact]
    public async Task Handle_WhenServiceThrowsInvalidOperationException_ThrowsException()
    {
        // Arrange
        var date = new DateTime(2025, 1, 15);
        var lottoType = "LOTTO";

        _mockXLottoService
            .Setup(s => s.GetActualDraws(date, lottoType))
            .ThrowsAsync(new InvalidOperationException("Failed to fetch draw results from XLotto"));

        var request = new Contracts.Request(date, lottoType);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _handler.Handle(request, CancellationToken.None)
        );
    }

    /// <summary>
    /// Test handling when service throws HttpRequestException
    /// </summary>
    [Fact]
    public async Task Handle_WhenServiceThrowsHttpRequestException_ThrowsException()
    {
        // Arrange
        var date = new DateTime(2025, 1, 15);
        var lottoType = "LOTTO";

        _mockXLottoService
            .Setup(s => s.GetActualDraws(date, lottoType))
            .ThrowsAsync(new HttpRequestException("Network error"));

        var request = new Contracts.Request(date, lottoType);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(
            async () => await _handler.Handle(request, CancellationToken.None)
        );
    }

    /// <summary>
    /// Test handling with current date (null date parameter)
    /// </summary>
    [Fact]
    public async Task Handle_WithCurrentDate_CallsServiceWithCorrectDate()
    {
        // Arrange
        var date = DateTime.Today;
        var lottoType = "LOTTO";
        var expectedJsonData = "{\"Data\":[{\"DrawDate\":\"2025-11-14\",\"GameType\":\"LOTTO\",\"Numbers\":[1,5,15,25,35,45]}]}";

        _mockXLottoService
            .Setup(s => s.GetActualDraws(date, lottoType))
            .ReturnsAsync(expectedJsonData);

        var request = new Contracts.Request(date, lottoType);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedJsonData, result.JsonData);
        _mockXLottoService.Verify(s => s.GetActualDraws(date, lottoType), Times.Once);
    }

    /// <summary>
    /// Test handling with cancellation token
    /// </summary>
    [Fact]
    public async Task Handle_WithCancellationToken_PropagatesToken()
    {
        // Arrange
        var date = new DateTime(2025, 1, 15);
        var lottoType = "LOTTO";
        var expectedJsonData = "{\"Data\":[]}";
        var cancellationTokenSource = new CancellationTokenSource();

        _mockXLottoService
            .Setup(s => s.GetActualDraws(date, lottoType))
            .ReturnsAsync(expectedJsonData);

        var request = new Contracts.Request(date, lottoType);

        // Act
        var result = await _handler.Handle(request, cancellationTokenSource.Token);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedJsonData, result.JsonData);
    }

    /// <summary>
    /// Test logging behavior during successful request
    /// </summary>
    [Fact]
    public async Task Handle_WithValidRequest_LogsInformation()
    {
        // Arrange
        var date = new DateTime(2025, 1, 15);
        var lottoType = "LOTTO";
        var expectedJsonData = "{\"Data\":[{\"DrawDate\":\"2025-01-15\",\"GameType\":\"LOTTO\",\"Numbers\":[1,2,3,4,5,6]}]}";

        _mockXLottoService
            .Setup(s => s.GetActualDraws(date, lottoType))
            .ReturnsAsync(expectedJsonData);

        var request = new Contracts.Request(date, lottoType);

        // Act
        await _handler.Handle(request, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Pobieranie aktualnych wyników z XLotto")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Pomyślnie pobrano wyniki z XLotto")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Test handling with multiple consecutive requests
    /// </summary>
    [Fact]
    public async Task Handle_WithMultipleRequests_HandlesEachCorrectly()
    {
        // Arrange
        var date1 = new DateTime(2025, 1, 15);
        var date2 = new DateTime(2025, 1, 16);
        var lottoType = "LOTTO";
        var jsonData1 = "{\"Data\":[{\"DrawDate\":\"2025-01-15\",\"GameType\":\"LOTTO\",\"Numbers\":[1,2,3,4,5,6]}]}";
        var jsonData2 = "{\"Data\":[{\"DrawDate\":\"2025-01-16\",\"GameType\":\"LOTTO\",\"Numbers\":[7,8,9,10,11,12]}]}";

        _mockXLottoService
            .Setup(s => s.GetActualDraws(date1, lottoType))
            .ReturnsAsync(jsonData1);

        _mockXLottoService
            .Setup(s => s.GetActualDraws(date2, lottoType))
            .ReturnsAsync(jsonData2);

        var request1 = new Contracts.Request(date1, lottoType);
        var request2 = new Contracts.Request(date2, lottoType);

        // Act
        var result1 = await _handler.Handle(request1, CancellationToken.None);
        var result2 = await _handler.Handle(request2, CancellationToken.None);

        // Assert
        Assert.Equal(jsonData1, result1.JsonData);
        Assert.Equal(jsonData2, result2.JsonData);
        _mockXLottoService.Verify(s => s.GetActualDraws(date1, lottoType), Times.Once);
        _mockXLottoService.Verify(s => s.GetActualDraws(date2, lottoType), Times.Once);
    }

    /// <summary>
    /// Test that handler returns empty data when GoogleGemini feature is disabled
    /// </summary>
    [Fact]
    public async Task Handle_WhenFeatureDisabled_ReturnsEmptyData()
    {
        // Arrange
        var date = new DateTime(2025, 1, 15);
        var lottoType = "LOTTO";

        // Setup options with Enable = false
        _mockOptions.Setup(x => x.Value).Returns(new GoogleGeminiOptions { Enable = false });
        _handler = new Handler(_mockXLottoService.Object, _mockOptions.Object, _mockLogger.Object);

        var request = new Contracts.Request(date, lottoType);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("{\"Data\":[]}", result.JsonData);
        _mockXLottoService.Verify(s => s.GetActualDraws(It.IsAny<DateTime?>(), It.IsAny<string>()), Times.Never);
    }

    /// <summary>
    /// Test that handler logs warning when feature is disabled
    /// </summary>
    [Fact]
    public async Task Handle_WhenFeatureDisabled_LogsWarning()
    {
        // Arrange
        var date = new DateTime(2025, 1, 15);
        var lottoType = "LOTTO";

        _mockOptions.Setup(x => x.Value).Returns(new GoogleGeminiOptions { Enable = false });
        _handler = new Handler(_mockXLottoService.Object, _mockOptions.Object, _mockLogger.Object);

        var request = new Contracts.Request(date, lottoType);

        // Act
        await _handler.Handle(request, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("GoogleGemini feature is disabled")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Test that handler calls service when feature is enabled
    /// </summary>
    [Fact]
    public async Task Handle_WhenFeatureEnabled_CallsService()
    {
        // Arrange
        var date = new DateTime(2025, 1, 15);
        var lottoType = "LOTTO";
        var expectedJsonData = "{\"Data\":[{\"DrawDate\":\"2025-01-15\",\"GameType\":\"LOTTO\",\"Numbers\":[1,2,3,4,5,6]}]}";

        _mockOptions.Setup(x => x.Value).Returns(new GoogleGeminiOptions { Enable = true });
        _handler = new Handler(_mockXLottoService.Object, _mockOptions.Object, _mockLogger.Object);

        _mockXLottoService
            .Setup(s => s.GetActualDraws(date, lottoType))
            .ReturnsAsync(expectedJsonData);

        var request = new Contracts.Request(date, lottoType);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedJsonData, result.JsonData);
        _mockXLottoService.Verify(s => s.GetActualDraws(date, lottoType), Times.Once);
    }
}
