// SPDX-FileCopyrightText: 2024 tdsharp contributors <https://github.com/egramtel/tdsharp>
//
// SPDX-License-Identifier: MIT

using Microsoft.Extensions.Logging;
using Moq;
using TdLib;

namespace bielu.tdsharp.aspnetcore.logger.tests;

public class TdLibLoggerTests
{
    [Fact]
    public void Constructor_WithNullCategoryName_ShouldThrowArgumentNullException()
    {
        // Arrange
        var mockClient = new Mock<TdClient>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TdLibLogger(null!, mockClient.Object, TdLogLevel.Info));
    }

    [Fact]
    public void Constructor_WithNullClient_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TdLibLogger("Test", null!, TdLogLevel.Info));
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldSucceed()
    {
        // Arrange
        var mockClient = new Mock<TdClient>();

        // Act
        var logger = new TdLibLogger("TestCategory", mockClient.Object, TdLogLevel.Info);

        // Assert
        Assert.NotNull(logger);
    }

    [Theory]
    [InlineData(LogLevel.Trace, TdLogLevel.Debug, true)]
    [InlineData(LogLevel.Debug, TdLogLevel.Info, true)]
    [InlineData(LogLevel.Information, TdLogLevel.Warning, true)]
    [InlineData(LogLevel.Warning, TdLogLevel.Error, true)]
    [InlineData(LogLevel.Error, TdLogLevel.Fatal, true)]
    [InlineData(LogLevel.Trace, TdLogLevel.Warning, false)]
    [InlineData(LogLevel.Debug, TdLogLevel.Warning, false)]
    [InlineData(LogLevel.None, TdLogLevel.Info, false)]
    public void IsEnabled_ShouldReturnCorrectValue(LogLevel logLevel, TdLogLevel minLevel, bool expected)
    {
        // Arrange
        var mockClient = new Mock<TdClient>();
        var logger = new TdLibLogger("TestCategory", mockClient.Object, minLevel);

        // Act
        var result = logger.IsEnabled(logLevel);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void BeginScope_ShouldReturnNull()
    {
        // Arrange
        var mockClient = new Mock<TdClient>();
        var logger = new TdLibLogger("TestCategory", mockClient.Object, TdLogLevel.Info);

        // Act
        var scope = logger.BeginScope("test");

        // Assert
        Assert.Null(scope);
    }

    [Fact]
    public void Log_WithNullFormatter_ShouldThrowArgumentNullException()
    {
        // Arrange
        var mockClient = new Mock<TdClient>();
        var logger = new TdLibLogger("TestCategory", mockClient.Object, TdLogLevel.Info);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            logger.Log(LogLevel.Information, new EventId(1), "state", null, null!));
    }
}
