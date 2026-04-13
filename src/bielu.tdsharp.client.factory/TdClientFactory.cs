// SPDX-FileCopyrightText: 2024 tdsharp contributors <https://github.com/egramtel/tdsharp>
//
// SPDX-License-Identifier: MIT

using System.Collections.Concurrent;
using bielu.tdsharp.abstractions;
using TdLib;
using TdLib.Bindings;

namespace bielu.tdsharp.client.factory;

/// <summary>
/// Factory that creates or retrieves <see cref="TdApi.IClient"/> instances by identifier
/// using an <see cref="IClientProvider"/> to create new clients.
/// Also supports closing and permanently destroying clients.
/// </summary>
public class TdClientFactory(IClientProvider clientProvider) : ITdClientFactory
{
    private readonly IClientProvider _clientProvider = clientProvider ?? throw new ArgumentNullException(nameof(clientProvider));
    private readonly ConcurrentDictionary<string, TdApi.IClient> _clients = new();

    /// <inheritdoc />
    public TdApi.IClient GetOrCreateClient(string identifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);

        return _clients.GetOrAdd(identifier, _ => _clientProvider.Create());
    }

    /// <inheritdoc />
    public TdApi.IClient GetOrCreateClient(string identifier, Action<TdClient> configure)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        ArgumentNullException.ThrowIfNull(configure);

        return _clients.GetOrAdd(identifier, _ => _clientProvider.Create(configure));
    }

    /// <inheritdoc />
    public TdApi.IClient GetOrCreateClient(string identifier, ITdLibBindings bindings)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        ArgumentNullException.ThrowIfNull(bindings);

        return _clients.GetOrAdd(identifier, _ => _clientProvider.Create(bindings));
    }

    /// <inheritdoc />
    public TdApi.IClient GetOrCreateClient(string identifier, ITdLibBindings bindings, TimeSpan receiverTimeout)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        ArgumentNullException.ThrowIfNull(bindings);

        return _clients.GetOrAdd(identifier, _ => _clientProvider.Create(bindings, receiverTimeout));
    }

    /// <inheritdoc />
    public TdApi.IClient GetOrCreateClient(string identifier, ITdLibBindings bindings, Action<TdClient> configure)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        ArgumentNullException.ThrowIfNull(bindings);
        ArgumentNullException.ThrowIfNull(configure);

        return _clients.GetOrAdd(identifier, _ => _clientProvider.Create(bindings, configure));
    }

    /// <inheritdoc />
    public TdApi.IClient GetOrCreateClient(string identifier, ITdLibBindings bindings, TimeSpan receiverTimeout, Action<TdClient> configure)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        ArgumentNullException.ThrowIfNull(bindings);
        ArgumentNullException.ThrowIfNull(configure);

        return _clients.GetOrAdd(identifier, _ => _clientProvider.Create(bindings, receiverTimeout, configure));
    }

    /// <inheritdoc />
    public async Task CloseClientAsync(string identifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);

        if (!_clients.TryRemove(identifier, out var client))
        {
            throw new InvalidOperationException($"No client found for identifier '{identifier}'.");
        }

        try
        {
            // Send Close to gracefully shut down the TDLib instance without logging out.
            await client.ExecuteAsync(new TdApi.Close()).ConfigureAwait(false);
        }
        finally
        {
            if (client is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    /// <inheritdoc />
    public async Task DestroyClientAsync(string identifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);

        if (!_clients.TryRemove(identifier, out var client))
        {
            throw new InvalidOperationException($"No client found for identifier '{identifier}'.");
        }

        try
        {
            // LogOut terminates the user session on the server (permanent).
            await client.ExecuteAsync(new TdApi.LogOut()).ConfigureAwait(false);
        }
        finally
        {
            if (client is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
