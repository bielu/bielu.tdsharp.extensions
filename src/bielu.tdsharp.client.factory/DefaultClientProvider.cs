// SPDX-FileCopyrightText: 2024 tdsharp contributors <https://github.com/egramtel/tdsharp>
//
// SPDX-License-Identifier: MIT

using bielu.tdsharp.abstractions;
using TdLib;
using TdLib.Bindings;

namespace bielu.tdsharp.client.factory;

/// <summary>
/// Default provider that creates a plain <see cref="TdClient"/> instance.
/// </summary>
public class DefaultClientProvider : IClientProvider
{
    private readonly ITdLibBindings _bindings;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultClientProvider"/> class
    /// with auto-detected bindings.
    /// </summary>
    public DefaultClientProvider()
        : this(Interop.AutoDetectBindings())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultClientProvider"/> class.
    /// </summary>
    /// <param name="bindings">The TDLib native bindings to use.</param>
    public DefaultClientProvider(ITdLibBindings bindings)
    {
        ArgumentNullException.ThrowIfNull(bindings);
        _bindings = bindings;
    }

    /// <inheritdoc />
    public TdApi.IClient Create()
    {
        return new TdClient(_bindings);
    }

    /// <inheritdoc />
    public TdApi.IClient Create(ITdLibBindings bindings)
    {
        ArgumentNullException.ThrowIfNull(bindings);
        return new TdClient(bindings);
    }

    /// <inheritdoc />
    public TdApi.IClient Create(ITdLibBindings bindings, TimeSpan receiverTimeout)
    {
        ArgumentNullException.ThrowIfNull(bindings);
        return new TdClient(new TdJsonClient(bindings), receiverTimeout);
    }
}
