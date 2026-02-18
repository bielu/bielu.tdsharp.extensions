// SPDX-FileCopyrightText: 2024 tdsharp contributors <https://github.com/egramtel/tdsharp>
//
// SPDX-License-Identifier: MIT

using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TdLib;

namespace bielu.tdsharp.aspnetcore.logger.tests;

public class TdLibLoggerFactoryExtensionsTests
{
    [Fact]
    public void AddTdLib_WithNullFactory_ShouldThrowArgumentNullException()
    {
        // Arrange
        ILoggerFactory factory = null!;
        var mockClient = Substitute.For<TdClient>();

        // Act
        var act = () => factory.AddTdLib(mockClient);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("factory");
    }

    [Fact]
    public void AddTdLib_WithNullClient_ShouldThrowArgumentNullException()
    {
        // Arrange
        var factory = LoggerFactory.Create(builder => { });

        // Act
        var act = () => factory.AddTdLib(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("client");
    }

    [Fact]
    public void AddTdLib_WithValidParameters_ShouldReturnFactory()
    {
        // Arrange
        var factory = LoggerFactory.Create(builder => { });
        var mockClient = Substitute.For<TdClient>();

        // Act
        var result = factory.AddTdLib(mockClient, TdLogLevel.Info);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(factory);
    }

    [Fact]
    public void AddTdLib_ShouldAllowCreatingLoggers()
    {
        // Arrange
        var factory = LoggerFactory.Create(builder => { });
        var mockClient = Substitute.For<TdClient>();
        factory.AddTdLib(mockClient, TdLogLevel.Info);

        // Act
        var logger = factory.CreateLogger("TestCategory");

        // Assert
        logger.Should().NotBeNull();
    }
}
