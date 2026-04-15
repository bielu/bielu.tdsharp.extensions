// SPDX-FileCopyrightText: 2024 tdsharp contributors <https://github.com/egramtel/tdsharp>
//
// SPDX-License-Identifier: MIT

using bielu.tdsharp.abstractions;
using TdLib;
using TdLib.Bindings;

namespace bielu.tdsharp.client.factory;

/// <summary>
/// Default provider that creates a plain <see cref="TdClient"/> instance.
/// Any registered <see cref="ITdClientMiddleware"/> instances are applied after client creation.
/// </summary>
public class DefaultClientProvider : IClientProvider
{
    private readonly ITdLibBindings _bindings;
    private readonly IEnumerable<ITdClientMiddleware> _middleware;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultClientProvider"/> class
    /// with auto-detected bindings.
    /// </summary>
    public DefaultClientProvider()
        : this(Interop.AutoDetectBindings(), [])
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultClientProvider"/> class.
    /// </summary>
    /// <param name="bindings">The TDLib native bindings to use.</param>
    public DefaultClientProvider(ITdLibBindings bindings)
        : this(bindings, [])
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultClientProvider"/> class
    /// with auto-detected bindings and DI-resolved middleware.
    /// </summary>
    /// <param name="middleware">Client middleware to apply after client creation.</param>
    public DefaultClientProvider(IEnumerable<ITdClientMiddleware> middleware)
        : this(Interop.AutoDetectBindings(), middleware)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultClientProvider"/> class.
    /// </summary>
    /// <param name="bindings">The TDLib native bindings to use.</param>
    /// <param name="middleware">Client middleware to apply after client creation.</param>
    public DefaultClientProvider(ITdLibBindings bindings, IEnumerable<ITdClientMiddleware> middleware)
    {
        ArgumentNullException.ThrowIfNull(bindings);
        _bindings = bindings;
        _middleware = middleware ?? [];
    }

    /// <inheritdoc />
    public TdApi.IClient Create()
    {
        return ApplyMiddleware(new TdClient(_bindings));
    }

    /// <inheritdoc />
    public TdApi.IClient Create(ITdLibBindings bindings)
    {
        ArgumentNullException.ThrowIfNull(bindings);
        return ApplyMiddleware(new TdClient(bindings));
    }

    /// <inheritdoc />
    public TdApi.IClient Create(TimeSpan receiverTimeout)
    {
        return ApplyMiddleware(new TdClient(new TdJsonClient(_bindings), receiverTimeout));
    }

    /// <inheritdoc />
    public TdApi.IClient Create(ITdLibBindings bindings, TimeSpan receiverTimeout)
    {
        ArgumentNullException.ThrowIfNull(bindings);
        return ApplyMiddleware(new TdClient(new TdJsonClient(bindings), receiverTimeout));
    }

    /// <inheritdoc />
    public TdApi.IClient Create(Action<TdClient> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var client = new TdClient(_bindings);
        configure(client);
        return ApplyMiddleware(client);
    }

    /// <inheritdoc />
    public TdApi.IClient Create(ITdLibBindings bindings, Action<TdClient> configure)
    {
        ArgumentNullException.ThrowIfNull(bindings);
        ArgumentNullException.ThrowIfNull(configure);
        var client = new TdClient(bindings);
        configure(client);
        return ApplyMiddleware(client);
    }

    /// <inheritdoc />
    public TdApi.IClient Create(ITdLibBindings bindings, TimeSpan receiverTimeout, Action<TdClient> configure)
    {
        ArgumentNullException.ThrowIfNull(bindings);
        ArgumentNullException.ThrowIfNull(configure);
        var client = new TdClient(new TdJsonClient(bindings), receiverTimeout);
        configure(client);
        return ApplyMiddleware(client);
    }

    private TdApi.IClient ApplyMiddleware(TdApi.IClient client)
    {
        TdApi.IClient current = client;
        foreach (var mw in _middleware)
        {
            current = mw.Decorate(current);
        }
        return current;
    }
}
