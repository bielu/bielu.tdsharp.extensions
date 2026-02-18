// SPDX-FileCopyrightText: 2024 tdsharp contributors <https://github.com/egramtel/tdsharp>
//
// SPDX-License-Identifier: MIT

using FluentAssertions;
using Microsoft.Extensions.Logging;
using TdLib;

namespace bielu.tdsharp.aspnetcore.logger.tests;

public class TdLibLoggerProviderTests
{
    [Fact]
    public void Constructor_WithNullClient_ShouldThrow()
    {
        // Act
        var act = () => new TdLibLoggerProvider(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("client");
    }

    [Fact]
    public void CreateLogger_ShouldReturnTdLibLogger()
    {
        // Arrange
        using var client = new TdClient();
        var provider = new TdLibLoggerProvider(client, TdLogLevel.Info);

        // Act
        var logger = provider.CreateLogger("TestCategory");

        // Assert
        logger.Should().NotBeNull();
        logger.Should().BeOfType<TdLibLogger>();
    }

    [Fact]
    public void CreateLogger_WithDifferentCategories_ShouldReturnDifferentLoggers()
    {
        // Arrange
        using var client = new TdClient();
        var provider = new TdLibLoggerProvider(client, TdLogLevel.Info);

        // Act
        var logger1 = provider.CreateLogger("Category1");
        var logger2 = provider.CreateLogger("Category2");

        // Assert
        logger1.Should().NotBeSameAs(logger2);
    }

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        // Arrange
        using var client = new TdClient();
        var provider = new TdLibLoggerProvider(client, TdLogLevel.Info);

        // Act
        var act = () => provider.Dispose();

        // Assert
        act.Should().NotThrow();
    }
}
