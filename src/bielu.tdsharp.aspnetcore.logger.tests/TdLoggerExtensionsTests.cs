// SPDX-FileCopyrightText: 2024 tdsharp contributors <https://github.com/egramtel/tdsharp>
//
// SPDX-License-Identifier: MIT

using FluentAssertions;
using Microsoft.Extensions.Logging;

namespace bielu.tdsharp.aspnetcore.logger.tests;

public class TdLoggerExtensionsTests
{
    [Theory]
    [InlineData(TdLogLevel.Fatal, LogLevel.Critical)]
    [InlineData(TdLogLevel.Error, LogLevel.Error)]
    [InlineData(TdLogLevel.Warning, LogLevel.Warning)]
    [InlineData(TdLogLevel.Info, LogLevel.Information)]
    [InlineData(TdLogLevel.Debug, LogLevel.Debug)]
    [InlineData(TdLogLevel.Verbose, LogLevel.Trace)]
    [InlineData(TdLogLevel.All, LogLevel.Trace)]
    public void ToLogLevel_ShouldMapTdLogLevelCorrectly(TdLogLevel tdLogLevel, LogLevel expectedLogLevel)
    {
        // Act
        var result = tdLogLevel.ToLogLevel();

        // Assert
        result.Should().Be(expectedLogLevel);
    }

    [Theory]
    [InlineData(0, LogLevel.Critical)]
    [InlineData(1, LogLevel.Error)]
    [InlineData(2, LogLevel.Warning)]
    [InlineData(3, LogLevel.Information)]
    [InlineData(4, LogLevel.Debug)]
    [InlineData(5, LogLevel.Trace)]
    [InlineData(1024, LogLevel.Trace)]
    public void ToLogLevel_ShouldMapIntegerVerbosityLevelCorrectly(int verbosityLevel, LogLevel expectedLogLevel)
    {
        // Act
        var result = TdLoggerExtensions.ToLogLevel(verbosityLevel);

        // Assert
        result.Should().Be(expectedLogLevel);
    }

    [Theory]
    [InlineData(LogLevel.Critical, TdLogLevel.Fatal)]
    [InlineData(LogLevel.Error, TdLogLevel.Error)]
    [InlineData(LogLevel.Warning, TdLogLevel.Warning)]
    [InlineData(LogLevel.Information, TdLogLevel.Info)]
    [InlineData(LogLevel.Debug, TdLogLevel.Debug)]
    [InlineData(LogLevel.Trace, TdLogLevel.Verbose)]
    [InlineData(LogLevel.None, TdLogLevel.Fatal)]
    public void ToTdLogLevel_ShouldMapLogLevelCorrectly(LogLevel logLevel, TdLogLevel expectedTdLogLevel)
    {
        // Act
        var result = logLevel.ToTdLogLevel();

        // Assert
        result.Should().Be(expectedTdLogLevel);
    }

    [Fact]
    public void ToLogLevel_WithInvalidTdLogLevel_ShouldReturnInformation()
    {
        // Arrange
        var invalidLevel = (TdLogLevel)999;

        // Act
        var result = invalidLevel.ToLogLevel();

        // Assert
        result.Should().Be(LogLevel.Information);
    }

    [Fact]
    public void ToTdLogLevel_WithInvalidLogLevel_ShouldReturnInfo()
    {
        // Arrange
        var invalidLevel = (LogLevel)999;

        // Act
        var result = invalidLevel.ToTdLogLevel();

        // Assert
        result.Should().Be(TdLogLevel.Info);
    }

    [Theory]
    [InlineData("[ 4][t 5][1771420471.389248132][AuthData.cpp:122][#2][!Session:4:main]  Create identifier", "TDLib.AuthData")]
    [InlineData("[ 3][t 2][1771414660.623883962][Td.cpp:1346][#1][!Td][&td_requests]	Sending update", "TDLib.Td")]
    [InlineData("[ 3][t 0][1771414660.622062444][Client.cpp:600]	Create client 1", "TDLib.Client")]
    [InlineData("[ 3][t 8][1771414660.689532995][MultiTd.cpp:138][#2][!MultiTd]	Create Td", "TDLib.MultiTd")]
    [InlineData("[ 2][t 2][1771414660.728623867][Td.cpp:763][#1][!Td]	Close Td in state 0", "TDLib.Td")]
    public void ExtractLoggerCategory_WithValidTdLibLogFormat_ShouldExtractSourceFile(string message, string expectedCategory)
    {
        // Act
        var result = TdLoggerExtensions.ExtractLoggerCategory(message);

        // Assert
        result.Should().Be(expectedCategory);
    }

    [Theory]
    [InlineData("Simple log message without source", "TDLib")]
    [InlineData("", "TDLib")]
    [InlineData(null, "TDLib")]
    [InlineData("Some message [not_cpp:123]", "TDLib")]
    public void ExtractLoggerCategory_WithNoSourceFile_ShouldReturnDefaultCategory(string? message, string expectedCategory)
    {
        // Act
        var result = TdLoggerExtensions.ExtractLoggerCategory(message!);

        // Assert
        result.Should().Be(expectedCategory);
    }
}
