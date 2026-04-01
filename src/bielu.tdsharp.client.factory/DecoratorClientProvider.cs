// SPDX-FileCopyrightText: 2024 tdsharp contributors <https://github.com/egramtel/tdsharp>
//
// SPDX-License-Identifier: MIT

using bielu.tdsharp.abstractions;
using TdLib;

namespace bielu.tdsharp.client.factory;

/// <summary>
/// A client provider that wraps another <see cref="IClientProvider"/> and applies a decorator.
/// </summary>
public class DecoratorClientProvider : IClientProvider
{
    private readonly IClientProvider _inner;
    private readonly TdClientDecorator _decorator;

    /// <summary>
    /// Initializes a new instance of the <see cref="DecoratorClientProvider"/> class.
    /// </summary>
    /// <param name="inner">The inner provider to wrap.</param>
    /// <param name="decorator">The decorator to apply.</param>
    public DecoratorClientProvider(IClientProvider inner, TdClientDecorator decorator)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(decorator);
        _inner = inner;
        _decorator = decorator;
    }

    /// <inheritdoc />
    public TdApi.IClient Create()
    {
        var client = _inner.Create();
        return _decorator(client);
    }
}
