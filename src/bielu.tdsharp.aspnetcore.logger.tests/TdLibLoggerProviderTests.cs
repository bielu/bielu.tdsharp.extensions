// SPDX-FileCopyrightText: 2024 tdsharp contributors <https://github.com/egramtel/tdsharp>
//
// SPDX-License-Identifier: MIT

using Microsoft.Extensions.Logging;
using Moq;
using TdLib;

namespace bielu.tdsharp.aspnetcore.logger.tests;

public class TdLibLoggerProviderTests
{
    [Fact]
    public void Constructor_WithNullClient_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TdLibLoggerProvider(null!));
    }

    [Fact]
    public void Constructor_WithValidClient_ShouldSucceed()
    {
        // Arrange
        var mockClient = new Mock<TdClient>();

        // Act
        var provider = new TdLibLoggerProvider(mockClient.Object);

        // Assert
        Assert.NotNull(provider);
    }

    [Fact]
    public void CreateLogger_ShouldReturnTdLibLogger()
    {
        // Arrange
        var mockClient = new Mock<TdClient>();
        var provider = new TdLibLoggerProvider(mockClient.Object);

        // Act
        var logger = provider.CreateLogger("TestCategory");

        // Assert
        Assert.NotNull(logger);
        Assert.IsType<TdLibLogger>(logger);
    }

    [Fact]
    public void CreateLogger_WithDifferentCategories_ShouldReturnDifferentInstances()
    {
        // Arrange
        var mockClient = new Mock<TdClient>();
        var provider = new TdLibLoggerProvider(mockClient.Object);

        // Act
        var logger1 = provider.CreateLogger("Category1");
        var logger2 = provider.CreateLogger("Category2");

        // Assert
        Assert.NotNull(logger1);
        Assert.NotNull(logger2);
        Assert.NotSame(logger1, logger2);
    }

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        // Arrange
        var mockClient = new Mock<TdClient>();
        var provider = new TdLibLoggerProvider(mockClient.Object);

        // Act & Assert - Should not throw
        provider.Dispose();
    }
}
