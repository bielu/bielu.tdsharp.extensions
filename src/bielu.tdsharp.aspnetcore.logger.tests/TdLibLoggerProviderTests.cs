// SPDX-FileCopyrightText: 2024 tdsharp contributors <https://github.com/egramtel/tdsharp>
//
// SPDX-License-Identifier: MIT

using FluentAssertions;
using NSubstitute;
using TdLib;

namespace bielu.tdsharp.aspnetcore.logger.tests;

public class TdLibLoggerProviderTests
{
    [Fact]
    public void Constructor_WithNullClient_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new TdLibLoggerProvider(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("client");
    }

    [Fact]
    public void Constructor_WithValidClient_ShouldSucceed()
    {
        // Arrange
        var mockClient = Substitute.For<TdClient>();

        // Act
        var provider = new TdLibLoggerProvider(mockClient);

        // Assert
        provider.Should().NotBeNull();
    }

    [Fact]
    public void CreateLogger_ShouldReturnTdLibLogger()
    {
        // Arrange
        var mockClient = Substitute.For<TdClient>();
        var provider = new TdLibLoggerProvider(mockClient);

        // Act
        var logger = provider.CreateLogger("TestCategory");

        // Assert
        logger.Should().NotBeNull();
        logger.Should().BeOfType<TdLibLogger>();
    }

    [Fact]
    public void CreateLogger_WithDifferentCategories_ShouldReturnDifferentInstances()
    {
        // Arrange
        var mockClient = Substitute.For<TdClient>();
        var provider = new TdLibLoggerProvider(mockClient);

        // Act
        var logger1 = provider.CreateLogger("Category1");
        var logger2 = provider.CreateLogger("Category2");

        // Assert
        logger1.Should().NotBeNull();
        logger2.Should().NotBeNull();
        logger1.Should().NotBeSameAs(logger2);
    }

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        // Arrange
        var mockClient = Substitute.For<TdClient>();
        var provider = new TdLibLoggerProvider(mockClient);

        // Act
        var act = () => provider.Dispose();

        // Assert
        act.Should().NotThrow();
    }
}
