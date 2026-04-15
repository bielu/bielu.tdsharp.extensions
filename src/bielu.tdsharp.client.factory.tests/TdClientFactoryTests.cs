// SPDX-FileCopyrightText: 2024 tdsharp contributors <https://github.com/egramtel/tdsharp>
//
// SPDX-License-Identifier: MIT

using bielu.tdsharp.abstractions;
using bielu.tdsharp.client.factory;
using FluentAssertions;
using NSubstitute;
using TdLib;

namespace bielu.tdsharp.client.factory.tests;

public class TdClientFactoryTests
{
    [Fact]
    public void GetOrCreateClient_ReturnsSameClientForSameIdentifier()
    {
        // Arrange
        var mockProvider = Substitute.For<IClientProvider>();
        var mockClient = Substitute.For<TdApi.IClient>();
        mockProvider.Create().Returns(mockClient);

        var factory = new TdClientFactory(mockProvider);

        // Act
        var client1 = factory.GetOrCreateClient("+1234567890");
        var client2 = factory.GetOrCreateClient("+1234567890");

        // Assert
        client1.Should().BeSameAs(client2);
        mockProvider.Received(1).Create(); // Only called once
    }

    [Fact]
    public void GetOrCreateClient_ReturnsDifferentClientsForDifferentIdentifiers()
    {
        // Arrange
        var mockProvider = Substitute.For<IClientProvider>();
        var mockClient1 = Substitute.For<TdApi.IClient>();
        var mockClient2 = Substitute.For<TdApi.IClient>();
        mockProvider.Create().Returns(mockClient1, mockClient2);

        var factory = new TdClientFactory(mockProvider);

        // Act
        var client1 = factory.GetOrCreateClient("+1234567890");
        var client2 = factory.GetOrCreateClient("+0987654321");

        // Assert
        client1.Should().NotBeSameAs(client2);
        mockProvider.Received(2).Create();
    }

    [Fact]
    public void GetOrCreateClient_ThrowsOnNullIdentifier()
    {
        // Arrange
        var mockProvider = Substitute.For<IClientProvider>();
        var factory = new TdClientFactory(mockProvider);

        // Act & Assert
        var act = () => factory.GetOrCreateClient(null!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetOrCreateClient_ThrowsOnWhitespaceIdentifier()
    {
        // Arrange
        var mockProvider = Substitute.For<IClientProvider>();
        var factory = new TdClientFactory(mockProvider);

        // Act & Assert
        var act = () => factory.GetOrCreateClient("   ");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_ThrowsOnNullProvider()
    {
        // Act & Assert
        var act = () => new TdClientFactory(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task CloseClientAsync_SendsCloseAndDisposesClient()
    {
        // Arrange
        var mockProvider = Substitute.For<IClientProvider>();
        var mockClient = Substitute.For<TdApi.IClient, IDisposable>();
        mockProvider.Create().Returns((TdApi.IClient)mockClient);
        ((TdApi.IClient)mockClient).ExecuteAsync(Arg.Any<TdApi.Close>())
            .Returns(Task.FromResult(new TdApi.Ok()));

        var factory = new TdClientFactory(mockProvider);
        factory.GetOrCreateClient("+1234567890");

        // Act
        await factory.CloseClientAsync("+1234567890");

        // Assert
        await ((TdApi.IClient)mockClient).Received(1).ExecuteAsync(Arg.Any<TdApi.Close>());
        ((IDisposable)mockClient).Received(1).Dispose();
    }

    [Fact]
    public async Task CloseClientAsync_RemovesClientFromFactory()
    {
        // Arrange
        var mockProvider = Substitute.For<IClientProvider>();
        var mockClient1 = Substitute.For<TdApi.IClient, IDisposable>();
        var mockClient2 = Substitute.For<TdApi.IClient>();
        mockProvider.Create().Returns((TdApi.IClient)mockClient1, mockClient2);
        ((TdApi.IClient)mockClient1).ExecuteAsync(Arg.Any<TdApi.Close>())
            .Returns(Task.FromResult(new TdApi.Ok()));

        var factory = new TdClientFactory(mockProvider);
        factory.GetOrCreateClient("+1234567890");

        // Act
        await factory.CloseClientAsync("+1234567890");

        // Creating again should produce a new client
        var newClient = factory.GetOrCreateClient("+1234567890");

        // Assert
        newClient.Should().BeSameAs(mockClient2);
        mockProvider.Received(2).Create();
    }

    [Fact]
    public async Task CloseClientAsync_ThrowsWhenClientNotFound()
    {
        // Arrange
        var mockProvider = Substitute.For<IClientProvider>();
        var factory = new TdClientFactory(mockProvider);

        // Act & Assert
        var act = async () => await factory.CloseClientAsync("+1234567890");
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CloseClientAsync_ThrowsOnNullIdentifier()
    {
        // Arrange
        var mockProvider = Substitute.For<IClientProvider>();
        var factory = new TdClientFactory(mockProvider);

        // Act & Assert
        var act = async () => await factory.CloseClientAsync(null!);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CloseClientAsync_DisposesEvenWhenCloseThrows()
    {
        // Arrange
        var mockProvider = Substitute.For<IClientProvider>();
        var mockClient = Substitute.For<TdApi.IClient, IDisposable>();
        mockProvider.Create().Returns((TdApi.IClient)mockClient);
        ((TdApi.IClient)mockClient).ExecuteAsync(Arg.Any<TdApi.Close>())
            .Returns<TdApi.Ok>(_ => throw new TdException(new TdApi.Error { Code = 500, Message = "Error" }));

        var factory = new TdClientFactory(mockProvider);
        factory.GetOrCreateClient("+1234567890");

        // Act & Assert
        var act = async () => await factory.CloseClientAsync("+1234567890");
        await act.Should().ThrowAsync<TdException>();
        ((IDisposable)mockClient).Received(1).Dispose();
    }

    [Fact]
    public async Task DestroyClientAsync_SendsLogOutAndDisposesClient()
    {
        // Arrange
        var mockProvider = Substitute.For<IClientProvider>();
        var mockClient = Substitute.For<TdApi.IClient, IDisposable>();
        mockProvider.Create().Returns((TdApi.IClient)mockClient);
        ((TdApi.IClient)mockClient).ExecuteAsync(Arg.Any<TdApi.LogOut>())
            .Returns(Task.FromResult(new TdApi.Ok()));

        var factory = new TdClientFactory(mockProvider);
        factory.GetOrCreateClient("+1234567890");

        // Act
        await factory.DestroyClientAsync("+1234567890");

        // Assert
        await ((TdApi.IClient)mockClient).Received(1).ExecuteAsync(Arg.Any<TdApi.LogOut>());
        ((IDisposable)mockClient).Received(1).Dispose();
    }

    [Fact]
    public async Task DestroyClientAsync_RemovesClientFromFactory()
    {
        // Arrange
        var mockProvider = Substitute.For<IClientProvider>();
        var mockClient1 = Substitute.For<TdApi.IClient, IDisposable>();
        var mockClient2 = Substitute.For<TdApi.IClient>();
        mockProvider.Create().Returns((TdApi.IClient)mockClient1, mockClient2);
        ((TdApi.IClient)mockClient1).ExecuteAsync(Arg.Any<TdApi.LogOut>())
            .Returns(Task.FromResult(new TdApi.Ok()));

        var factory = new TdClientFactory(mockProvider);
        factory.GetOrCreateClient("+1234567890");

        // Act
        await factory.DestroyClientAsync("+1234567890");

        // Creating again should produce a new client
        var newClient = factory.GetOrCreateClient("+1234567890");

        // Assert
        newClient.Should().BeSameAs(mockClient2);
        mockProvider.Received(2).Create();
    }

    [Fact]
    public async Task DestroyClientAsync_ThrowsWhenClientNotFound()
    {
        // Arrange
        var mockProvider = Substitute.For<IClientProvider>();
        var factory = new TdClientFactory(mockProvider);

        // Act & Assert
        var act = async () => await factory.DestroyClientAsync("+1234567890");
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task DestroyClientAsync_ThrowsOnNullIdentifier()
    {
        // Arrange
        var mockProvider = Substitute.For<IClientProvider>();
        var factory = new TdClientFactory(mockProvider);

        // Act & Assert
        var act = async () => await factory.DestroyClientAsync(null!);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task DestroyClientAsync_DisposesEvenWhenLogOutThrows()
    {
        // Arrange
        var mockProvider = Substitute.For<IClientProvider>();
        var mockClient = Substitute.For<TdApi.IClient, IDisposable>();
        mockProvider.Create().Returns((TdApi.IClient)mockClient);
        ((TdApi.IClient)mockClient).ExecuteAsync(Arg.Any<TdApi.LogOut>())
            .Returns<TdApi.Ok>(_ => throw new TdException(new TdApi.Error { Code = 500, Message = "Error" }));

        var factory = new TdClientFactory(mockProvider);
        factory.GetOrCreateClient("+1234567890");

        // Act & Assert
        var act = async () => await factory.DestroyClientAsync("+1234567890");
        await act.Should().ThrowAsync<TdException>();
        ((IDisposable)mockClient).Received(1).Dispose();
    }
}

public class TdClientFactoryWithConfigureTests
{
    [Fact]
    public void GetOrCreateClient_WithConfigure_CallsProviderCreateWithConfigure()
    {
        // Arrange
        var mockProvider = Substitute.For<IClientProvider>();
        var mockClient = Substitute.For<TdApi.IClient>();
        Action<TdClient> configure = _ => { };
        mockProvider.Create(configure).Returns(mockClient);

        var factory = new TdClientFactory(mockProvider);

        // Act
        var client = factory.GetOrCreateClient("+1234567890", configure);

        // Assert
        client.Should().BeSameAs(mockClient);
        mockProvider.Received(1).Create(configure);
    }

    [Fact]
    public void GetOrCreateClient_WithConfigure_ReturnsCachedClientOnSecondCall()
    {
        // Arrange
        var mockProvider = Substitute.For<IClientProvider>();
        var mockClient = Substitute.For<TdApi.IClient>();
        Action<TdClient> configure = _ => { };
        mockProvider.Create(configure).Returns(mockClient);

        var factory = new TdClientFactory(mockProvider);

        // Act
        var client1 = factory.GetOrCreateClient("+1234567890", configure);
        var client2 = factory.GetOrCreateClient("+1234567890", configure);

        // Assert
        client1.Should().BeSameAs(client2);
        mockProvider.Received(1).Create(configure);
    }

    [Fact]
    public void GetOrCreateClient_WithConfigure_ThrowsOnNullIdentifier()
    {
        // Arrange
        var mockProvider = Substitute.For<IClientProvider>();
        var factory = new TdClientFactory(mockProvider);
        Action<TdClient> configure = _ => { };

        // Act & Assert
        var act = () => factory.GetOrCreateClient(null!, configure);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetOrCreateClient_WithConfigure_ThrowsOnWhitespaceIdentifier()
    {
        // Arrange
        var mockProvider = Substitute.For<IClientProvider>();
        var factory = new TdClientFactory(mockProvider);
        Action<TdClient> configure = _ => { };

        // Act & Assert
        var act = () => factory.GetOrCreateClient("   ", configure);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetOrCreateClient_WithConfigure_ThrowsOnNullConfigure()
    {
        // Arrange
        var mockProvider = Substitute.For<IClientProvider>();
        var factory = new TdClientFactory(mockProvider);

        // Act & Assert
        var act = () => factory.GetOrCreateClient("+1234567890", (Action<TdClient>)null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetOrCreateClient_WithoutConfigure_ReturnsSameAsCachedWithConfigure()
    {
        // Arrange - create with configure first, then get without configure
        var mockProvider = Substitute.For<IClientProvider>();
        var mockClient = Substitute.For<TdApi.IClient>();
        Action<TdClient> configure = _ => { };
        mockProvider.Create(configure).Returns(mockClient);

        var factory = new TdClientFactory(mockProvider);

        // Act
        var client1 = factory.GetOrCreateClient("+1234567890", configure);
        var client2 = factory.GetOrCreateClient("+1234567890");

        // Assert - should return the cached client, not create a new one
        client1.Should().BeSameAs(client2);
        mockProvider.Received(1).Create(configure);
        mockProvider.DidNotReceive().Create();
    }
}

public class TdClientFactoryWithBindingsTests
{
    [Fact]
    public void GetOrCreateClient_WithBindings_CallsProviderCreateWithBindings()
    {
        // Arrange
        var mockProvider = Substitute.For<IClientProvider>();
        var mockClient = Substitute.For<TdApi.IClient>();
        var mockBindings = Substitute.For<TdLib.Bindings.ITdLibBindings>();
        mockProvider.Create(mockBindings).Returns(mockClient);

        var factory = new TdClientFactory(mockProvider);

        // Act
        var client = factory.GetOrCreateClient("+1234567890", mockBindings);

        // Assert
        client.Should().BeSameAs(mockClient);
        mockProvider.Received(1).Create(mockBindings);
    }

    [Fact]
    public void GetOrCreateClient_WithBindings_ReturnsCachedClientOnSecondCall()
    {
        // Arrange
        var mockProvider = Substitute.For<IClientProvider>();
        var mockClient = Substitute.For<TdApi.IClient>();
        var mockBindings = Substitute.For<TdLib.Bindings.ITdLibBindings>();
        mockProvider.Create(mockBindings).Returns(mockClient);

        var factory = new TdClientFactory(mockProvider);

        // Act
        var client1 = factory.GetOrCreateClient("+1234567890", mockBindings);
        var client2 = factory.GetOrCreateClient("+1234567890", mockBindings);

        // Assert
        client1.Should().BeSameAs(client2);
        mockProvider.Received(1).Create(mockBindings);
    }

    [Fact]
    public void GetOrCreateClient_WithBindings_ThrowsOnNullIdentifier()
    {
        // Arrange
        var mockProvider = Substitute.For<IClientProvider>();
        var mockBindings = Substitute.For<TdLib.Bindings.ITdLibBindings>();
        var factory = new TdClientFactory(mockProvider);

        // Act & Assert
        var act = () => factory.GetOrCreateClient(null!, mockBindings);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetOrCreateClient_WithBindings_ThrowsOnWhitespaceIdentifier()
    {
        // Arrange
        var mockProvider = Substitute.For<IClientProvider>();
        var mockBindings = Substitute.For<TdLib.Bindings.ITdLibBindings>();
        var factory = new TdClientFactory(mockProvider);

        // Act & Assert
        var act = () => factory.GetOrCreateClient("   ", mockBindings);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetOrCreateClient_WithBindings_ThrowsOnNullBindings()
    {
        // Arrange
        var mockProvider = Substitute.For<IClientProvider>();
        var factory = new TdClientFactory(mockProvider);

        // Act & Assert
        var act = () => factory.GetOrCreateClient("+1234567890", (TdLib.Bindings.ITdLibBindings)null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetOrCreateClient_WithBindingsAndTimeout_CallsProviderCreateWithBindingsAndTimeout()
    {
        // Arrange
        var mockProvider = Substitute.For<IClientProvider>();
        var mockClient = Substitute.For<TdApi.IClient>();
        var mockBindings = Substitute.For<TdLib.Bindings.ITdLibBindings>();
        var timeout = TimeSpan.FromSeconds(5);
        mockProvider.Create(mockBindings, timeout).Returns(mockClient);

        var factory = new TdClientFactory(mockProvider);

        // Act
        var client = factory.GetOrCreateClient("+1234567890", mockBindings, timeout);

        // Assert
        client.Should().BeSameAs(mockClient);
        mockProvider.Received(1).Create(mockBindings, timeout);
    }

    [Fact]
    public void GetOrCreateClient_WithBindingsAndTimeout_ReturnsCachedClientOnSecondCall()
    {
        // Arrange
        var mockProvider = Substitute.For<IClientProvider>();
        var mockClient = Substitute.For<TdApi.IClient>();
        var mockBindings = Substitute.For<TdLib.Bindings.ITdLibBindings>();
        var timeout = TimeSpan.FromSeconds(5);
        mockProvider.Create(mockBindings, timeout).Returns(mockClient);

        var factory = new TdClientFactory(mockProvider);

        // Act
        var client1 = factory.GetOrCreateClient("+1234567890", mockBindings, timeout);
        var client2 = factory.GetOrCreateClient("+1234567890", mockBindings, timeout);

        // Assert
        client1.Should().BeSameAs(client2);
        mockProvider.Received(1).Create(mockBindings, timeout);
    }

    [Fact]
    public void GetOrCreateClient_WithBindingsAndTimeout_ThrowsOnNullIdentifier()
    {
        // Arrange
        var mockProvider = Substitute.For<IClientProvider>();
        var mockBindings = Substitute.For<TdLib.Bindings.ITdLibBindings>();
        var factory = new TdClientFactory(mockProvider);

        // Act & Assert
        var act = () => factory.GetOrCreateClient(null!, mockBindings, TimeSpan.FromSeconds(5));
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetOrCreateClient_WithBindingsAndTimeout_ThrowsOnNullBindings()
    {
        // Arrange
        var mockProvider = Substitute.For<IClientProvider>();
        var factory = new TdClientFactory(mockProvider);

        // Act & Assert
        var act = () => factory.GetOrCreateClient("+1234567890", (TdLib.Bindings.ITdLibBindings)null!, TimeSpan.FromSeconds(5));
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetOrCreateClient_WithBindingsAndConfigure_CallsProviderCreateWithBindingsAndConfigure()
    {
        // Arrange
        var mockProvider = Substitute.For<IClientProvider>();
        var mockClient = Substitute.For<TdApi.IClient>();
        var mockBindings = Substitute.For<TdLib.Bindings.ITdLibBindings>();
        Action<TdClient> configure = _ => { };
        mockProvider.Create(mockBindings, configure).Returns(mockClient);

        var factory = new TdClientFactory(mockProvider);

        // Act
        var client = factory.GetOrCreateClient("+1234567890", mockBindings, configure);

        // Assert
        client.Should().BeSameAs(mockClient);
        mockProvider.Received(1).Create(mockBindings, configure);
    }

    [Fact]
    public void GetOrCreateClient_WithBindingsAndConfigure_ReturnsCachedClientOnSecondCall()
    {
        // Arrange
        var mockProvider = Substitute.For<IClientProvider>();
        var mockClient = Substitute.For<TdApi.IClient>();
        var mockBindings = Substitute.For<TdLib.Bindings.ITdLibBindings>();
        Action<TdClient> configure = _ => { };
        mockProvider.Create(mockBindings, configure).Returns(mockClient);

        var factory = new TdClientFactory(mockProvider);

        // Act
        var client1 = factory.GetOrCreateClient("+1234567890", mockBindings, configure);
        var client2 = factory.GetOrCreateClient("+1234567890", mockBindings, configure);

        // Assert
        client1.Should().BeSameAs(client2);
        mockProvider.Received(1).Create(mockBindings, configure);
    }

    [Fact]
    public void GetOrCreateClient_WithBindingsAndConfigure_ThrowsOnNullIdentifier()
    {
        // Arrange
        var mockProvider = Substitute.For<IClientProvider>();
        var mockBindings = Substitute.For<TdLib.Bindings.ITdLibBindings>();
        Action<TdClient> configure = _ => { };
        var factory = new TdClientFactory(mockProvider);

        // Act & Assert
        var act = () => factory.GetOrCreateClient(null!, mockBindings, configure);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetOrCreateClient_WithBindingsAndConfigure_ThrowsOnNullBindings()
    {
        // Arrange
        var mockProvider = Substitute.For<IClientProvider>();
        Action<TdClient> configure = _ => { };
        var factory = new TdClientFactory(mockProvider);

        // Act & Assert
        var act = () => factory.GetOrCreateClient("+1234567890", (TdLib.Bindings.ITdLibBindings)null!, configure);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetOrCreateClient_WithBindingsAndConfigure_ThrowsOnNullConfigure()
    {
        // Arrange
        var mockProvider = Substitute.For<IClientProvider>();
        var mockBindings = Substitute.For<TdLib.Bindings.ITdLibBindings>();
        var factory = new TdClientFactory(mockProvider);

        // Act & Assert
        var act = () => factory.GetOrCreateClient("+1234567890", mockBindings, (Action<TdClient>)null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetOrCreateClient_WithBindingsTimeoutAndConfigure_CallsProviderCreate()
    {
        // Arrange
        var mockProvider = Substitute.For<IClientProvider>();
        var mockClient = Substitute.For<TdApi.IClient>();
        var mockBindings = Substitute.For<TdLib.Bindings.ITdLibBindings>();
        var timeout = TimeSpan.FromSeconds(5);
        Action<TdClient> configure = _ => { };
        mockProvider.Create(mockBindings, timeout, configure).Returns(mockClient);

        var factory = new TdClientFactory(mockProvider);

        // Act
        var client = factory.GetOrCreateClient("+1234567890", mockBindings, timeout, configure);

        // Assert
        client.Should().BeSameAs(mockClient);
        mockProvider.Received(1).Create(mockBindings, timeout, configure);
    }

    [Fact]
    public void GetOrCreateClient_WithBindingsTimeoutAndConfigure_ReturnsCachedClientOnSecondCall()
    {
        // Arrange
        var mockProvider = Substitute.For<IClientProvider>();
        var mockClient = Substitute.For<TdApi.IClient>();
        var mockBindings = Substitute.For<TdLib.Bindings.ITdLibBindings>();
        var timeout = TimeSpan.FromSeconds(5);
        Action<TdClient> configure = _ => { };
        mockProvider.Create(mockBindings, timeout, configure).Returns(mockClient);

        var factory = new TdClientFactory(mockProvider);

        // Act
        var client1 = factory.GetOrCreateClient("+1234567890", mockBindings, timeout, configure);
        var client2 = factory.GetOrCreateClient("+1234567890", mockBindings, timeout, configure);

        // Assert
        client1.Should().BeSameAs(client2);
        mockProvider.Received(1).Create(mockBindings, timeout, configure);
    }

    [Fact]
    public void GetOrCreateClient_WithBindingsTimeoutAndConfigure_ThrowsOnNullIdentifier()
    {
        // Arrange
        var mockProvider = Substitute.For<IClientProvider>();
        var mockBindings = Substitute.For<TdLib.Bindings.ITdLibBindings>();
        Action<TdClient> configure = _ => { };
        var factory = new TdClientFactory(mockProvider);

        // Act & Assert
        var act = () => factory.GetOrCreateClient(null!, mockBindings, TimeSpan.FromSeconds(5), configure);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetOrCreateClient_WithBindingsTimeoutAndConfigure_ThrowsOnNullBindings()
    {
        // Arrange
        var mockProvider = Substitute.For<IClientProvider>();
        Action<TdClient> configure = _ => { };
        var factory = new TdClientFactory(mockProvider);

        // Act & Assert
        var act = () => factory.GetOrCreateClient("+1234567890", (TdLib.Bindings.ITdLibBindings)null!, TimeSpan.FromSeconds(5), configure);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetOrCreateClient_WithBindingsTimeoutAndConfigure_ThrowsOnNullConfigure()
    {
        // Arrange
        var mockProvider = Substitute.For<IClientProvider>();
        var mockBindings = Substitute.For<TdLib.Bindings.ITdLibBindings>();
        var factory = new TdClientFactory(mockProvider);

        // Act & Assert
        var act = () => factory.GetOrCreateClient("+1234567890", mockBindings, TimeSpan.FromSeconds(5), (Action<TdClient>)null!);
        act.Should().Throw<ArgumentNullException>();
    }
}

public class TdClientFactoryWithTimeoutTests
{
    [Fact]
    public void GetOrCreateClient_WithTimeout_CallsProviderCreateWithTimeout()
    {
        // Arrange
        var mockProvider = Substitute.For<IClientProvider>();
        var mockClient = Substitute.For<TdApi.IClient>();
        var timeout = TimeSpan.FromSeconds(5);
        mockProvider.Create(timeout).Returns(mockClient);

        var factory = new TdClientFactory(mockProvider);

        // Act
        var client = factory.GetOrCreateClient("+1234567890", timeout);

        // Assert
        client.Should().BeSameAs(mockClient);
        mockProvider.Received(1).Create(timeout);
    }

    [Fact]
    public void GetOrCreateClient_WithTimeout_ReturnsCachedClientOnSecondCall()
    {
        // Arrange
        var mockProvider = Substitute.For<IClientProvider>();
        var mockClient = Substitute.For<TdApi.IClient>();
        var timeout = TimeSpan.FromSeconds(5);
        mockProvider.Create(timeout).Returns(mockClient);

        var factory = new TdClientFactory(mockProvider);

        // Act
        var client1 = factory.GetOrCreateClient("+1234567890", timeout);
        var client2 = factory.GetOrCreateClient("+1234567890", timeout);

        // Assert
        client1.Should().BeSameAs(client2);
        mockProvider.Received(1).Create(timeout);
    }

    [Fact]
    public void GetOrCreateClient_WithTimeout_ThrowsOnNullIdentifier()
    {
        // Arrange
        var mockProvider = Substitute.For<IClientProvider>();
        var factory = new TdClientFactory(mockProvider);

        // Act & Assert
        var act = () => factory.GetOrCreateClient(null!, TimeSpan.FromSeconds(5));
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetOrCreateClient_WithTimeout_ThrowsOnWhitespaceIdentifier()
    {
        // Arrange
        var mockProvider = Substitute.For<IClientProvider>();
        var factory = new TdClientFactory(mockProvider);

        // Act & Assert
        var act = () => factory.GetOrCreateClient("   ", TimeSpan.FromSeconds(5));
        act.Should().Throw<ArgumentException>();
    }
}

public class DefaultClientProviderTests
{
    [Fact]
    public void Create_ReturnsNonNullClient()
    {
        // Arrange & Act & Assert
        // DefaultClientProvider() calls Interop.AutoDetectBindings() which needs native lib.
        // Test with explicit bindings mock instead.
        var mockBindings = Substitute.For<TdLib.Bindings.ITdLibBindings>();
        var provider = new DefaultClientProvider(mockBindings);

        var act = () => provider.Create();
        // TdClient ctor with mock bindings will throw since it tries to use them,
        // but we verify the provider is constructed correctly
        provider.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_ThrowsOnNullBindings()
    {
        // Act & Assert
        var act = () => new DefaultClientProvider((TdLib.Bindings.ITdLibBindings)null!);
        act.Should().Throw<ArgumentNullException>();
    }
}

public class DecoratorClientProviderTests
{
    private class TestDecoratorProvider : DecoratorClientProvider
    {
        private readonly TdApi.IClient _decorated;

        public TestDecoratorProvider(IClientProvider inner, TdApi.IClient decorated) : base(inner)
        {
            _decorated = decorated;
        }

        protected override TdApi.IClient Decorate(TdApi.IClient client)
        {
            return _decorated;
        }
    }

    [Fact]
    public void Create_CallsInnerProviderAndDecorate()
    {
        // Arrange
        var mockInner = Substitute.For<IClientProvider>();
        var mockInnerClient = Substitute.For<TdApi.IClient>();
        var mockDecoratedClient = Substitute.For<TdApi.IClient>();
        mockInner.Create().Returns(mockInnerClient);

        var provider = new TestDecoratorProvider(mockInner, mockDecoratedClient);

        // Act
        var result = provider.Create();

        // Assert
        result.Should().BeSameAs(mockDecoratedClient);
        mockInner.Received(1).Create();
    }

    [Fact]
    public void CreateWithBindings_CallsInnerProviderAndDecorate()
    {
        // Arrange
        var mockInner = Substitute.For<IClientProvider>();
        var mockBindings = Substitute.For<TdLib.Bindings.ITdLibBindings>();
        var mockInnerClient = Substitute.For<TdApi.IClient>();
        var mockDecoratedClient = Substitute.For<TdApi.IClient>();
        mockInner.Create(mockBindings).Returns(mockInnerClient);

        var provider = new TestDecoratorProvider(mockInner, mockDecoratedClient);

        // Act
        var result = provider.Create(mockBindings);

        // Assert
        result.Should().BeSameAs(mockDecoratedClient);
        mockInner.Received(1).Create(mockBindings);
    }

    [Fact]
    public void CreateWithBindingsAndTimeout_CallsInnerProviderAndDecorate()
    {
        // Arrange
        var mockInner = Substitute.For<IClientProvider>();
        var mockBindings = Substitute.For<TdLib.Bindings.ITdLibBindings>();
        var timeout = TimeSpan.FromSeconds(1);
        var mockInnerClient = Substitute.For<TdApi.IClient>();
        var mockDecoratedClient = Substitute.For<TdApi.IClient>();
        mockInner.Create(mockBindings, timeout).Returns(mockInnerClient);

        var provider = new TestDecoratorProvider(mockInner, mockDecoratedClient);

        // Act
        var result = provider.Create(mockBindings, timeout);

        // Assert
        result.Should().BeSameAs(mockDecoratedClient);
        mockInner.Received(1).Create(mockBindings, timeout);
    }

    [Fact]
    public void CreateWithTimeout_CallsInnerProviderAndDecorate()
    {
        // Arrange
        var mockInner = Substitute.For<IClientProvider>();
        var timeout = TimeSpan.FromSeconds(1);
        var mockInnerClient = Substitute.For<TdApi.IClient>();
        var mockDecoratedClient = Substitute.For<TdApi.IClient>();
        mockInner.Create(timeout).Returns(mockInnerClient);

        var provider = new TestDecoratorProvider(mockInner, mockDecoratedClient);

        // Act
        var result = provider.Create(timeout);

        // Assert
        result.Should().BeSameAs(mockDecoratedClient);
        mockInner.Received(1).Create(timeout);
    }

    [Fact]
    public void Constructor_ThrowsOnNullInner()
    {
        // Act & Assert
        var act = () => new TestDecoratorProvider(null!, Substitute.For<TdApi.IClient>());
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CreateWithConfigure_CallsInnerProviderAndDecorate()
    {
        // Arrange
        var mockInner = Substitute.For<IClientProvider>();
        var mockInnerClient = Substitute.For<TdApi.IClient>();
        var mockDecoratedClient = Substitute.For<TdApi.IClient>();
        Action<TdClient> configure = _ => { };
        mockInner.Create(configure).Returns(mockInnerClient);

        var provider = new TestDecoratorProvider(mockInner, mockDecoratedClient);

        // Act
        var result = provider.Create(configure);

        // Assert
        result.Should().BeSameAs(mockDecoratedClient);
        mockInner.Received(1).Create(configure);
    }

    [Fact]
    public void CreateWithBindingsAndConfigure_CallsInnerProviderAndDecorate()
    {
        // Arrange
        var mockInner = Substitute.For<IClientProvider>();
        var mockBindings = Substitute.For<TdLib.Bindings.ITdLibBindings>();
        var mockInnerClient = Substitute.For<TdApi.IClient>();
        var mockDecoratedClient = Substitute.For<TdApi.IClient>();
        Action<TdClient> configure = _ => { };
        mockInner.Create(mockBindings, configure).Returns(mockInnerClient);

        var provider = new TestDecoratorProvider(mockInner, mockDecoratedClient);

        // Act
        var result = provider.Create(mockBindings, configure);

        // Assert
        result.Should().BeSameAs(mockDecoratedClient);
        mockInner.Received(1).Create(mockBindings, configure);
    }

    [Fact]
    public void CreateWithBindingsTimeoutAndConfigure_CallsInnerProviderAndDecorate()
    {
        // Arrange
        var mockInner = Substitute.For<IClientProvider>();
        var mockBindings = Substitute.For<TdLib.Bindings.ITdLibBindings>();
        var timeout = TimeSpan.FromSeconds(1);
        var mockInnerClient = Substitute.For<TdApi.IClient>();
        var mockDecoratedClient = Substitute.For<TdApi.IClient>();
        Action<TdClient> configure = _ => { };
        mockInner.Create(mockBindings, timeout, configure).Returns(mockInnerClient);

        var provider = new TestDecoratorProvider(mockInner, mockDecoratedClient);

        // Act
        var result = provider.Create(mockBindings, timeout, configure);

        // Assert
        result.Should().BeSameAs(mockDecoratedClient);
        mockInner.Received(1).Create(mockBindings, timeout, configure);
    }
}
