// SPDX-FileCopyrightText: 2024 tdsharp contributors <https://github.com/egramtel/tdsharp>
//
// SPDX-License-Identifier: MIT

using TdLib;

namespace bielu.tdsharp.abstractions;

/// <summary>
/// Factory for creating or retrieving <see cref="TdApi.IClient"/> instances identified by a unique key (e.g. phone number).
/// </summary>
public interface ITdClientFactory
{
    /// <summary>
    /// Gets an existing client or creates a new one for the given identifier.
    /// </summary>
    /// <param name="identifier">A unique identifier for the client (e.g. phone number).</param>
    /// <returns>A configured <see cref="TdApi.IClient"/> instance.</returns>
    TdApi.IClient GetOrCreateClient(string identifier);
}
