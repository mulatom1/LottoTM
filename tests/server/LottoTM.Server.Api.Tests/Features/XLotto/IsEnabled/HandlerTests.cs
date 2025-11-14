using LottoTM.Server.Api.Features.XLotto.IsEnabled;
using LottoTM.Server.Api.Options;
using Microsoft.Extensions.Options;
using Moq;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace LottoTM.Server.Api.Tests.Features.XLotto.IsEnabled;

/// <summary>
/// Unit tests for the XLotto IsEnabled Handler
/// </summary>
public class HandlerTests
{
    /// <summary>
    /// Test that handler returns true when GoogleGemini feature is enabled
    /// </summary>
    [Fact]
    public async Task Handle_WhenFeatureEnabled_ReturnsTrue()
    {
        // Arrange
        var options = MsOptions.Create(new GoogleGeminiOptions { Enable = true });
        var handler = new Handler(options);
        var request = new Contracts.Request();

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsEnabled);
    }

    /// <summary>
    /// Test that handler returns false when GoogleGemini feature is disabled
    /// </summary>
    [Fact]
    public async Task Handle_WhenFeatureDisabled_ReturnsFalse()
    {
        // Arrange
        var options = MsOptions.Create(new GoogleGeminiOptions { Enable = false });
        var handler = new Handler(options);
        var request = new Contracts.Request();

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsEnabled);
    }

    /// <summary>
    /// Test that handler returns false when GoogleGeminiOptions is null (default)
    /// </summary>
    [Fact]
    public async Task Handle_WhenOptionsIsDefault_ReturnsFalse()
    {
        // Arrange
        var options = MsOptions.Create(new GoogleGeminiOptions()); // Default Enable = false
        var handler = new Handler(options);
        var request = new Contracts.Request();

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsEnabled);
    }

    /// <summary>
    /// Test that handler executes synchronously (returns completed task)
    /// </summary>
    [Fact]
    public void Handle_ExecutesSynchronously_ReturnsCompletedTask()
    {
        // Arrange
        var options = MsOptions.Create(new GoogleGeminiOptions { Enable = true });
        var handler = new Handler(options);
        var request = new Contracts.Request();

        // Act
        var task = handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.True(task.IsCompleted);
    }

    /// <summary>
    /// Test that handler handles cancellation token (even though it's not used)
    /// </summary>
    [Fact]
    public async Task Handle_WithCancellationToken_DoesNotThrow()
    {
        // Arrange
        var options = MsOptions.Create(new GoogleGeminiOptions { Enable = true });
        var handler = new Handler(options);
        var request = new Contracts.Request();
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act & Assert
        // Should not throw even with cancelled token (since handler doesn't use it)
        var result = await handler.Handle(request, cts.Token);
        Assert.NotNull(result);
    }

    /// <summary>
    /// Test that handler is consistent across multiple calls
    /// </summary>
    [Fact]
    public async Task Handle_MultipleCallsWithSameOptions_ReturnsConsistentResult()
    {
        // Arrange
        var options = MsOptions.Create(new GoogleGeminiOptions { Enable = true });
        var handler = new Handler(options);
        var request = new Contracts.Request();

        // Act
        var result1 = await handler.Handle(request, CancellationToken.None);
        var result2 = await handler.Handle(request, CancellationToken.None);
        var result3 = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.True(result1.IsEnabled);
        Assert.True(result2.IsEnabled);
        Assert.True(result3.IsEnabled);
        Assert.Equal(result1.IsEnabled, result2.IsEnabled);
        Assert.Equal(result2.IsEnabled, result3.IsEnabled);
    }

    /// <summary>
    /// Test that handler reads from IOptions correctly
    /// </summary>
    [Fact]
    public async Task Handle_ReadsFromIOptions_Correctly()
    {
        // Arrange
        var mockOptions = new Mock<IOptions<GoogleGeminiOptions>>();
        mockOptions.Setup(x => x.Value).Returns(new GoogleGeminiOptions { Enable = true });

        var handler = new Handler(mockOptions.Object);
        var request = new Contracts.Request();

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.True(result.IsEnabled);
        mockOptions.Verify(x => x.Value, Times.Once);
    }
}
