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
    /// Creates a new <see cref="TdApi.IClient"/> instance using the specified bindings and receiver timeout.
    /// </summary>
    /// <param name="bindings">The TDLib native bindings to use.</param>
    /// <param name="receiverTimeout">The timeout for the receiver's polling loop.</param>
    /// <returns>A <see cref="TdApi.IClient"/> instance.</returns>
    TdApi.IClient Create(ITdLibBindings bindings, TimeSpan receiverTimeout);
}
