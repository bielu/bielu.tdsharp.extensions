// SPDX-FileCopyrightText: 2024 tdsharp contributors <https://github.com/egramtel/tdsharp>
//
// SPDX-License-Identifier: MIT

using bielu.tdsharp.abstractions;
using TdLib;
using TdLib.Bindings;

namespace bielu.tdsharp.client.factory;

/// <summary>
/// An abstract client provider that wraps another <see cref="IClientProvider"/>
/// and allows subclasses to decorate the created client.
/// </summary>
public abstract class DecoratorClientProvider : IClientProvider
{
    private readonly IClientProvider _inner;

    /// <summary>
    /// Initializes a new instance of the <see cref="DecoratorClientProvider"/> class.
    /// </summary>
    /// <param name="inner">The inner provider to wrap.</param>
    protected DecoratorClientProvider(IClientProvider inner)
    {
        ArgumentNullException.ThrowIfNull(inner);
        _inner = inner;
    }

    /// <inheritdoc />
    public TdApi.IClient Create()
    {
        var client = _inner.Create();
        return Decorate(client);
    }

    /// <inheritdoc />
    public TdApi.IClient Create(ITdLibBindings bindings)
    {
        var client = _inner.Create(bindings);
        return Decorate(client);
    }

    /// <inheritdoc />
    public TdApi.IClient Create(TimeSpan receiverTimeout)
    {
        var client = _inner.Create(receiverTimeout);
        return Decorate(client);
    }

    /// <inheritdoc />
    public TdApi.IClient Create(ITdLibBindings bindings, TimeSpan receiverTimeout)
    {
        var client = _inner.Create(bindings, receiverTimeout);
        return Decorate(client);
    }

    /// <inheritdoc />
    public TdApi.IClient Create(Action<TdClient> configure)
    {
        var client = _inner.Create(configure);
        return Decorate(client);
    }

    /// <inheritdoc />
    public TdApi.IClient Create(ITdLibBindings bindings, Action<TdClient> configure)
    {
        var client = _inner.Create(bindings, configure);
        return Decorate(client);
    }

    /// <inheritdoc />
    public TdApi.IClient Create(ITdLibBindings bindings, TimeSpan receiverTimeout, Action<TdClient> configure)
    {
        var client = _inner.Create(bindings, receiverTimeout, configure);
        return Decorate(client);
    }

    /// <summary>
    /// Applies decoration to the client created by the inner provider.
    /// </summary>
    /// <param name="client">The client to decorate.</param>
    /// <returns>The decorated client.</returns>
    protected abstract TdApi.IClient Decorate(TdApi.IClient client);
}
