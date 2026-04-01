// SPDX-FileCopyrightText: 2024 tdsharp contributors <https://github.com/egramtel/tdsharp>
//
// SPDX-License-Identifier: MIT

using TdLib;

namespace bielu.tdsharp.abstractions;

/// <summary>
/// Provides a <see cref="TdApi.IClient"/> instance. Implementations may create a plain client
/// or wrap it with additional behavior (e.g. OpenTelemetry instrumentation).
/// </summary>
public interface IClientProvider
{
    /// <summary>
    /// Creates a new <see cref="TdApi.IClient"/> instance.
    /// </summary>
    /// <returns>A <see cref="TdApi.IClient"/> instance.</returns>
    TdApi.IClient Create();
}
