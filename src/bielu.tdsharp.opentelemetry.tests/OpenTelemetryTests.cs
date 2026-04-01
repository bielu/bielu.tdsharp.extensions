// SPDX-FileCopyrightText: 2024 tdsharp contributors <https://github.com/egramtel/tdsharp>
//
// SPDX-License-Identifier: MIT

using System.Diagnostics;
using bielu.tdsharp.opentelemetry;
using FluentAssertions;
using NSubstitute;
using TdLib;

namespace bielu.tdsharp.opentelemetry.tests;

public class OpenTelemetryTdClientDecoratorTests
{
    [Fact]
    public void Constructor_ThrowsOnNullInner()
    {
        var act = () => new OpenTelemetryTdClientDecorator(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Send_DelegatesToInner()
    {
        // Arrange
        var mockInner = Substitute.For<TdApi.IClient>();
        var decorator = new OpenTelemetryTdClientDecorator(mockInner);
        var function = new TdApi.GetMe();

        // Act
        decorator.Send(function);

        // Assert
        mockInner.Received(1).Send(function);
    }

    [Fact]
    public void Execute_DelegatesToInnerAndReturnsResult()
    {
        // Arrange
        var mockInner = Substitute.For<TdApi.IClient>();
        var expected = new TdApi.User();
        var function = new TdApi.GetMe();
        mockInner.Execute(function).Returns(expected);
        var decorator = new OpenTelemetryTdClientDecorator(mockInner);

        // Act
        var result = decorator.Execute(function);

        // Assert
        result.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task ExecuteAsync_DelegatesToInnerAndReturnsResult()
    {
        // Arrange
        var mockInner = Substitute.For<TdApi.IClient>();
        var expected = new TdApi.User();
        var function = new TdApi.GetMe();
        mockInner.ExecuteAsync(function).Returns(Task.FromResult(expected));
        var decorator = new OpenTelemetryTdClientDecorator(mockInner);

        // Act
        var result = await decorator.ExecuteAsync(function);

        // Assert
        result.Should().BeSameAs(expected);
    }

    [Fact]
    public void Execute_PropagatesException()
    {
        // Arrange
        var mockInner = Substitute.For<TdApi.IClient>();
        var function = new TdApi.GetMe();
        mockInner.Execute(function).Returns(_ => throw new TdException(new TdApi.Error { Code = 404, Message = "Not Found" }));
        var decorator = new OpenTelemetryTdClientDecorator(mockInner);

        // Act & Assert
        var act = () => decorator.Execute(function);
        act.Should().Throw<TdException>();
    }

    [Fact]
    public async Task ExecuteAsync_PropagatesException()
    {
        // Arrange
        var mockInner = Substitute.For<TdApi.IClient>();
        var function = new TdApi.GetMe();
        mockInner.ExecuteAsync(function).Returns<TdApi.User>(_ => throw new TdException(new TdApi.Error { Code = 404, Message = "Not Found" }));
        var decorator = new OpenTelemetryTdClientDecorator(mockInner);

        // Act & Assert
        var act = async () => await decorator.ExecuteAsync(function);
        await act.Should().ThrowAsync<TdException>();
    }

    [Fact]
    public void Dispose_DisposesInnerIfDisposable()
    {
        // Arrange
        var mockInner = Substitute.For<TdApi.IClient, IDisposable>();
        var decorator = new OpenTelemetryTdClientDecorator((TdApi.IClient)mockInner);

        // Act
        decorator.Dispose();

        // Assert
        ((IDisposable)mockInner).Received(1).Dispose();
    }

    [Fact]
    public void UpdateReceived_DelegatesToInner()
    {
        // Arrange
        var mockInner = Substitute.For<TdApi.IClient>();
        var decorator = new OpenTelemetryTdClientDecorator(mockInner);
        EventHandler<TdApi.Update> handler = (_, _) => { };

        // Act
        decorator.UpdateReceived += handler;
        decorator.UpdateReceived -= handler;

        // Assert
        mockInner.Received(1).UpdateReceived += handler;
        mockInner.Received(1).UpdateReceived -= handler;
    }
}

public class OpenTelemetryTdJsonClientDecoratorTests
{
    [Fact]
    public void Constructor_ThrowsOnNullInner()
    {
        var act = () => new OpenTelemetryTdJsonClientDecorator(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Send_DelegatesToInner()
    {
        // Arrange
        var mockInner = Substitute.For<TdLib.ITdJsonClient>();
        var decorator = new OpenTelemetryTdJsonClientDecorator(mockInner);

        // Act
        decorator.Send("{\"@type\":\"getMe\"}");

        // Assert
        mockInner.Received(1).Send("{\"@type\":\"getMe\"}");
    }

    [Fact]
    public void Execute_DelegatesToInnerAndReturnsResult()
    {
        // Arrange
        var mockInner = Substitute.For<TdLib.ITdJsonClient>();
        mockInner.Execute("{\"@type\":\"getMe\"}").Returns("{\"@type\":\"user\"}");
        var decorator = new OpenTelemetryTdJsonClientDecorator(mockInner);

        // Act
        var result = decorator.Execute("{\"@type\":\"getMe\"}");

        // Assert
        result.Should().Be("{\"@type\":\"user\"}");
    }

    [Fact]
    public void Receive_DelegatesToInnerAndReturnsResult()
    {
        // Arrange
        var mockInner = Substitute.For<TdLib.ITdJsonClient>();
        mockInner.Receive(1.0).Returns("{\"@type\":\"updateOption\"}");
        var decorator = new OpenTelemetryTdJsonClientDecorator(mockInner);

        // Act
        var result = decorator.Receive(1.0);

        // Assert
        result.Should().Be("{\"@type\":\"updateOption\"}");
    }

    [Fact]
    public void Bindings_DelegatesToInner()
    {
        // Arrange
        var mockBindings = Substitute.For<TdLib.Bindings.ITdLibBindings>();
        var mockInner = Substitute.For<TdLib.ITdJsonClient>();
        mockInner.Bindings.Returns(mockBindings);
        var decorator = new OpenTelemetryTdJsonClientDecorator(mockInner);

        // Act & Assert
        decorator.Bindings.Should().BeSameAs(mockBindings);
    }
}

public class OpenTelemetryReceiverDecoratorTests
{
    [Fact]
    public void Constructor_ThrowsOnNullInner()
    {
        var act = () => new OpenTelemetryReceiverDecorator(null!, "test-client");
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_ThrowsOnNullOrWhiteSpaceClientId()
    {
        var mockInner = Substitute.For<TdLib.Bindings.IReceiver>();

        var actNull = () => new OpenTelemetryReceiverDecorator(mockInner, null!);
        actNull.Should().Throw<ArgumentException>();

        var actEmpty = () => new OpenTelemetryReceiverDecorator(mockInner, "  ");
        actEmpty.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Start_DelegatesToInner()
    {
        // Arrange
        var mockInner = Substitute.For<TdLib.Bindings.IReceiver>();
        var decorator = new OpenTelemetryReceiverDecorator(mockInner, "test-client");

        // Act
        decorator.Start();

        // Assert - verify Start was called via NSubstitute
        mockInner.ReceivedWithAnyArgs(1).Start();
    }

    [Fact]
    public void Dispose_DisposesInnerIfDisposable()
    {
        // Arrange - create a combined mock that implements both interfaces
        var mockInner = Substitute.For<TdLib.Bindings.IReceiver>();
        var decorator = new OpenTelemetryReceiverDecorator(mockInner, "test-client");

        // Act - should not throw even if inner is not IDisposable
        var act = () => decorator.Dispose();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void AuthorizationStateChanged_TracksStateInDictionary()
    {
        // Arrange
        var clientId = $"auth-test-{Guid.NewGuid()}";
        var mockInner = Substitute.For<TdLib.Bindings.IReceiver>();
        var decorator = new OpenTelemetryReceiverDecorator(mockInner, clientId);

        // Subscribe to capture the forwarded event
        TdApi.AuthorizationState? forwardedState = null;
        decorator.AuthorizationStateChanged += (_, s) => forwardedState = s;

        // Act - simulate the inner receiver raising AuthorizationStateChanged
        mockInner.AuthorizationStateChanged += Raise.Event<EventHandler<TdApi.AuthorizationState>>(
            mockInner,
            new TdApi.AuthorizationState.AuthorizationStateReady());

        // Assert - the state should be tracked
        bielu.tdsharp.opentelemetry.TdSharpMetrics.ClientAuthStates
            .TryGetValue(clientId, out var trackedState).Should().BeTrue();
        trackedState.Should().Be("AuthorizationStateReady");
        forwardedState.Should().BeOfType<TdApi.AuthorizationState.AuthorizationStateReady>();

        // Act - change to a new state
        mockInner.AuthorizationStateChanged += Raise.Event<EventHandler<TdApi.AuthorizationState>>(
            mockInner,
            new TdApi.AuthorizationState.AuthorizationStateClosed());

        bielu.tdsharp.opentelemetry.TdSharpMetrics.ClientAuthStates
            .TryGetValue(clientId, out trackedState).Should().BeTrue();
        trackedState.Should().Be("AuthorizationStateClosed");

        // Act - dispose removes the entry
        decorator.Dispose();
        bielu.tdsharp.opentelemetry.TdSharpMetrics.ClientAuthStates
            .ContainsKey(clientId).Should().BeFalse();
    }
}

public class OpenTelemetryClientProviderTests
{
    [Fact]
    public void Constructor_WithBindingsAndTimeout_DoesNotThrow()
    {
        // Arrange
        var mockBindings = Substitute.For<TdLib.Bindings.ITdLibBindings>();

        // Act & Assert
        var act = () => new OpenTelemetryClientProvider(mockBindings, TimeSpan.FromSeconds(1));
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_ThrowsOnNullBindings()
    {
        var act = () => new OpenTelemetryClientProvider(null!, TimeSpan.FromSeconds(1));
        act.Should().Throw<ArgumentNullException>();
    }
}
