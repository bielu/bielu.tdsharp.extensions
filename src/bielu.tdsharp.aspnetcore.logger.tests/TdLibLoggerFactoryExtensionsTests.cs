// SPDX-FileCopyrightText: 2024 tdsharp contributors <https://github.com/egramtel/tdsharp>
//
// SPDX-License-Identifier: MIT

using Microsoft.Extensions.Logging;
using Moq;
using TdLib;

namespace bielu.tdsharp.aspnetcore.logger.tests;

public class TdLibLoggerFactoryExtensionsTests
{
    [Fact]
    public void AddTdLib_WithNullFactory_ShouldThrowArgumentNullException()
    {
        // Arrange
        ILoggerFactory factory = null!;
        var mockClient = new Mock<TdClient>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => factory.AddTdLib(mockClient.Object));
    }

    [Fact]
    public void AddTdLib_WithNullClient_ShouldThrowArgumentNullException()
    {
        // Arrange
        var factory = LoggerFactory.Create(builder => { });

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => factory.AddTdLib(null!));
    }

    [Fact]
    public void AddTdLib_WithValidParameters_ShouldReturnFactory()
    {
        // Arrange
        var factory = LoggerFactory.Create(builder => { });
        var mockClient = new Mock<TdClient>();

        // Act
        var result = factory.AddTdLib(mockClient.Object, TdLogLevel.Info);

        // Assert
        Assert.NotNull(result);
        Assert.Same(factory, result);
    }

    [Fact]
    public void AddTdLib_ShouldAllowCreatingLoggers()
    {
        // Arrange
        var factory = LoggerFactory.Create(builder => { });
        var mockClient = new Mock<TdClient>();
        factory.AddTdLib(mockClient.Object, TdLogLevel.Info);

        // Act
        var logger = factory.CreateLogger("TestCategory");

        // Assert
        Assert.NotNull(logger);
    }
}
