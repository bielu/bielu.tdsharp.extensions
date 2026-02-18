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
    public void AddTdLib_WithNullFactory_ShouldThrow()
    {
        // Arrange
        ILoggerFactory factory = null!;
        using var client = new TdClient();

        // Act
        var act = () => factory.AddTdLib(client);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("factory");
    }

    [Fact]
    public void AddTdLib_WithNullClient_ShouldThrow()
    {
        // Arrange
        var factory = Substitute.For<ILoggerFactory>();

        // Act
        var act = () => factory.AddTdLib(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("client");
    }

    [Fact]
    public void AddTdLib_ShouldAddProviderAndReturnFactory()
    {
        // Arrange
        var factory = Substitute.For<ILoggerFactory>();
        using var client = new TdClient();

        // Act
        var result = factory.AddTdLib(client, TdLogLevel.Info);

        // Assert
        result.Should().BeSameAs(factory);
        factory.Received(1).AddProvider(Arg.Any<TdLibLoggerProvider>());
    }
}
