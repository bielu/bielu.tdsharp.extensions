// SPDX-FileCopyrightText: 2024 tdsharp contributors <https://github.com/egramtel/tdsharp>
//
// SPDX-License-Identifier: MIT

using System.Collections.Concurrent;
using bielu.tdsharp.abstractions;
using TdLib;

namespace bielu.tdsharp.client.factory;

/// <summary>
/// Factory that creates or retrieves <see cref="TdApi.IClient"/> instances by identifier
/// using an <see cref="IClientProvider"/> to create new clients.
/// </summary>
public class TdClientFactory : ITdClientFactory
{
    private readonly IClientProvider _clientProvider;
    private readonly ConcurrentDictionary<string, TdApi.IClient> _clients = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="TdClientFactory"/> class.
    /// </summary>
    /// <param name="clientProvider">The provider used to create new client instances.</param>
    public TdClientFactory(IClientProvider clientProvider)
    {
        ArgumentNullException.ThrowIfNull(clientProvider);
        _clientProvider = clientProvider;
    }

    /// <inheritdoc />
    public TdApi.IClient GetOrCreateClient(string identifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);

        return _clients.GetOrAdd(identifier, _ => _clientProvider.Create());
    }
}
