// SPDX-FileCopyrightText: 2024 tdsharp contributors <https://github.com/egramtel/tdsharp>
//
// SPDX-License-Identifier: MIT

using TdLib;

namespace bielu.tdsharp.abstractions;

/// <summary>
/// Factory for creating or retrieving <see cref="TdApi.IClient"/> instances identified by a unique key (e.g. phone number).
/// Also provides methods to close and dispose of clients.
/// </summary>
public interface ITdClientFactory
{
    /// <summary>
    /// Gets an existing client or creates a new one for the given identifier.
    /// </summary>
    /// <param name="identifier">A unique identifier for the client (e.g. phone number).</param>
    /// <returns>A configured <see cref="TdApi.IClient"/> instance.</returns>
    TdApi.IClient GetOrCreateClient(string identifier);

    /// <summary>
    /// Gets an existing client or creates a new one for the given identifier,
    /// invoking the <paramref name="configure"/> callback on the underlying <see cref="TdClient"/>
    /// when a new client is created.
    /// </summary>
    /// <remarks>
    /// The <paramref name="configure"/> callback is only invoked when a new client is created,
    /// not when returning an existing cached client. Use this to configure the native
    /// <see cref="TdClient"/> before any decoration is applied (e.g. setting up TDLib logging).
    /// </remarks>
    /// <param name="identifier">A unique identifier for the client (e.g. phone number).</param>
    /// <param name="configure">An action invoked with the underlying <see cref="TdClient"/> when a new client is created.</param>
    /// <returns>A configured <see cref="TdApi.IClient"/> instance.</returns>
    TdApi.IClient GetOrCreateClient(string identifier, Action<TdClient> configure);

    /// <summary>
    /// Closes the client for the given identifier without logging out (non-permanent).
    /// Sends <see cref="TdApi.Close"/> and disposes the client.
    /// The user session is preserved; the client can be recreated later and resume the session.
    /// </summary>
    /// <param name="identifier">The identifier of the client to close.</param>
    /// <returns>A task that completes when the client has been closed and disposed.</returns>
    Task CloseClientAsync(string identifier);

    /// <summary>
    /// Permanently closes the client for the given identifier by logging out first.
    /// Sends <see cref="TdApi.LogOut"/> (which terminates the user session on the server),
    /// then disposes the client. The session cannot be resumed; a fresh login is required.
    /// </summary>
    /// <param name="identifier">The identifier of the client to destroy.</param>
    /// <returns>A task that completes when the client has been logged out and disposed.</returns>
    Task DestroyClientAsync(string identifier);
}
