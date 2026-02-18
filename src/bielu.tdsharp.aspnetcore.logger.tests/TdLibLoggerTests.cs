// SPDX-FileCopyrightText: 2024 tdsharp contributors <https://github.com/egramtel/tdsharp>
//
// SPDX-License-Identifier: MIT

using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TdLib;

namespace bielu.tdsharp.aspnetcore.logger.tests;

public class TdLibLoggerTests
{
    [Fact]
    public void Constructor_WithNullCategoryName_ShouldThrowArgumentNullException()
    {
        // Arrange
        var mockClient = Substitute.For<TdClient>();

        // Act
        var act = () => new TdLibLogger(null!, mockClient, TdLogLevel.Info);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("categoryName");
    }

    [Fact]
    public void Constructor_WithNullClient_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new TdLibLogger("Test", null!, TdLogLevel.Info);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("client");
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldSucceed()
    {
        // Arrange
        var mockClient = Substitute.For<TdClient>();

        // Act
        var logger = new TdLibLogger("TestCategory", mockClient, TdLogLevel.Info);

        // Assert
        logger.Should().NotBeNull();
    }

    [Theory]
    [InlineData(LogLevel.Critical, TdLogLevel.Debug, true)]  // Fatal(0) <= Debug(4)
    [InlineData(LogLevel.Error, TdLogLevel.Debug, true)]     // Error(1) <= Debug(4)
    [InlineData(LogLevel.Warning, TdLogLevel.Debug, true)]   // Warning(2) <= Debug(4)
    [InlineData(LogLevel.Information, TdLogLevel.Debug, true)] // Info(3) <= Debug(4)
    [InlineData(LogLevel.Debug, TdLogLevel.Debug, true)]     // Debug(4) <= Debug(4)
    [InlineData(LogLevel.Trace, TdLogLevel.Debug, false)]    // Verbose(5) > Debug(4)
    [InlineData(LogLevel.Critical, TdLogLevel.Warning, true)] // Fatal(0) <= Warning(2)
    [InlineData(LogLevel.Error, TdLogLevel.Warning, true)]   // Error(1) <= Warning(2)
    [InlineData(LogLevel.Warning, TdLogLevel.Warning, true)] // Warning(2) <= Warning(2)
    [InlineData(LogLevel.Information, TdLogLevel.Warning, false)] // Info(3) > Warning(2)
    [InlineData(LogLevel.Debug, TdLogLevel.Warning, false)]  // Debug(4) > Warning(2)
    [InlineData(LogLevel.None, TdLogLevel.Info, false)]
    public void IsEnabled_ShouldReturnCorrectValue(LogLevel logLevel, TdLogLevel minLevel, bool expected)
    {
        // Arrange
        var mockClient = Substitute.For<TdClient>();
        var logger = new TdLibLogger("TestCategory", mockClient, minLevel);

        // Act
        var result = logger.IsEnabled(logLevel);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void BeginScope_ShouldReturnNull()
    {
        // Arrange
        var mockClient = Substitute.For<TdClient>();
        var logger = new TdLibLogger("TestCategory", mockClient, TdLogLevel.Info);

        // Act
        var scope = logger.BeginScope("test");

        // Assert
        scope.Should().BeNull();
    }

    [Fact]
    public void Log_WithNullFormatter_ShouldThrowArgumentNullException()
    {
        // Arrange
        var mockClient = Substitute.For<TdClient>();
        var logger = new TdLibLogger("TestCategory", mockClient, TdLogLevel.Info);

        // Act
        var act = () => logger.Log(LogLevel.Information, new EventId(1), "state", null, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("formatter");
    }
}
