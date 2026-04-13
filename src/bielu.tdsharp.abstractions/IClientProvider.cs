// SPDX-FileCopyrightText: 2024 tdsharp contributors <https://github.com/egramtel/tdsharp>
//
// SPDX-License-Identifier: MIT

using TdLib;
using TdLib.Bindings;

namespace bielu.tdsharp.abstractions;

/// <summary>
/// Provides a <see cref="TdApi.IClient"/> instance. Implementations may create a plain client
/// or wrap it with additional behavior (e.g. OpenTelemetry instrumentation).
/// </summary>
public interface IClientProvider
{
    /// <summary>
    /// Creates a new <see cref="TdApi.IClient"/> instance using the default bindings.
    /// </summary>
    /// <returns>A <see cref="TdApi.IClient"/> instance.</returns>
    TdApi.IClient Create();

    /// <summary>
    /// Creates a new <see cref="TdApi.IClient"/> instance using the specified bindings.
    /// </summary>
    /// <param name="bindings">The TDLib native bindings to use.</param>
    /// <returns>A <see cref="TdApi.IClient"/> instance.</returns>
    TdApi.IClient Create(ITdLibBindings bindings);

    /// <summary>
    /// Creates a new <see cref="TdApi.IClient"/> instance using the default bindings and the specified receiver timeout.
    /// </summary>
    /// <param name="receiverTimeout">The timeout for the receiver's polling loop.</param>
    /// <returns>A <see cref="TdApi.IClient"/> instance.</returns>
    TdApi.IClient Create(TimeSpan receiverTimeout);

    /// <summary>
    /// Creates a new <see cref="TdApi.IClient"/> instance using the specified bindings and receiver timeout.
    /// </summary>
    /// <param name="bindings">The TDLib native bindings to use.</param>
    /// <param name="receiverTimeout">The timeout for the receiver's polling loop.</param>
    /// <returns>A <see cref="TdApi.IClient"/> instance.</returns>
    TdApi.IClient Create(ITdLibBindings bindings, TimeSpan receiverTimeout);

    /// <summary>
    /// Creates a new <see cref="TdApi.IClient"/> instance using the default bindings,
    /// invoking the <paramref name="configure"/> callback on the underlying <see cref="TdClient"/>
    /// before any decoration is applied.
    /// </summary>
    /// <remarks>
    /// Use this overload when you need access to the native <see cref="TdClient"/> for configuration
    /// that is not available through the <see cref="TdApi.IClient"/> interface (e.g. setting up logging).
    /// The callback is invoked after the <see cref="TdClient"/> is created but before it is wrapped
    /// with any decorators (such as OpenTelemetry instrumentation).
    /// </remarks>
    /// <param name="configure">An action invoked with the underlying <see cref="TdClient"/> before decoration.</param>
    /// <returns>A <see cref="TdApi.IClient"/> instance.</returns>
    TdApi.IClient Create(Action<TdClient> configure);

    /// <summary>
    /// Creates a new <see cref="TdApi.IClient"/> instance using the specified bindings,
    /// invoking the <paramref name="configure"/> callback on the underlying <see cref="TdClient"/>
    /// before any decoration is applied.
    /// </summary>
    /// <param name="bindings">The TDLib native bindings to use.</param>
    /// <param name="configure">An action invoked with the underlying <see cref="TdClient"/> before decoration.</param>
    /// <returns>A <see cref="TdApi.IClient"/> instance.</returns>
    TdApi.IClient Create(ITdLibBindings bindings, Action<TdClient> configure);

    /// <summary>
    /// Creates a new <see cref="TdApi.IClient"/> instance using the specified bindings and receiver timeout,
    /// invoking the <paramref name="configure"/> callback on the underlying <see cref="TdClient"/>
    /// before any decoration is applied.
    /// </summary>
    /// <param name="bindings">The TDLib native bindings to use.</param>
    /// <param name="receiverTimeout">The timeout for the receiver's polling loop.</param>
    /// <param name="configure">An action invoked with the underlying <see cref="TdClient"/> before decoration.</param>
    /// <returns>A <see cref="TdApi.IClient"/> instance.</returns>
    TdApi.IClient Create(ITdLibBindings bindings, TimeSpan receiverTimeout, Action<TdClient> configure);
}
