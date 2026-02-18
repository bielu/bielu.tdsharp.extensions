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
    public void Constructor_WithNullCategoryName_ShouldThrow()
    {
        // Arrange
        var client = new TdClient();

        // Act
        var act = () => new TdLibLogger(null!, client, TdLogLevel.Info);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("categoryName");
        client.Dispose();
    }

    [Fact]
    public void Constructor_WithNullClient_ShouldThrow()
    {
        // Act
        var act = () => new TdLibLogger("Test", null!, TdLogLevel.Info);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("client");
    }

    [Fact]
    public void BeginScope_ShouldReturnNull()
    {
        // Arrange
        using var client = new TdClient();
        var logger = new TdLibLogger("Test", client, TdLogLevel.Info);

        // Act
        var result = logger.BeginScope(new { });

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData(LogLevel.None, false)]
    [InlineData(LogLevel.Critical, true)]
    [InlineData(LogLevel.Error, true)]
    [InlineData(LogLevel.Warning, true)]
    [InlineData(LogLevel.Information, true)]
    [InlineData(LogLevel.Debug, false)]
    [InlineData(LogLevel.Trace, false)]
    public void IsEnabled_WithInfoMinLevel_ShouldReturnCorrectValue(LogLevel logLevel, bool expected)
    {
        // Arrange
        using var client = new TdClient();
        var logger = new TdLibLogger("Test", client, TdLogLevel.Info);

        // Act
        var result = logger.IsEnabled(logLevel);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(LogLevel.None, false)]
    [InlineData(LogLevel.Critical, true)]
    [InlineData(LogLevel.Error, true)]
    [InlineData(LogLevel.Warning, true)]
    [InlineData(LogLevel.Information, true)]
    [InlineData(LogLevel.Debug, true)]
    [InlineData(LogLevel.Trace, true)]
    public void IsEnabled_WithVerboseMinLevel_ShouldReturnCorrectValue(LogLevel logLevel, bool expected)
    {
        // Arrange
        using var client = new TdClient();
        var logger = new TdLibLogger("Test", client, TdLogLevel.Verbose);

        // Act
        var result = logger.IsEnabled(logLevel);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void Log_WithNullFormatter_ShouldThrow()
    {
        // Arrange
        using var client = new TdClient();
        var logger = new TdLibLogger("Test", client, TdLogLevel.Info);

        // Act
        var act = () => logger.Log(LogLevel.Information, new EventId(0), "message", null, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}
