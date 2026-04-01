// SPDX-FileCopyrightText: 2024 tdsharp contributors <https://github.com/egramtel/tdsharp>
//
// SPDX-License-Identifier: MIT

using bielu.tdsharp.abstractions;
using TdLib;

namespace bielu.tdsharp.client.factory;

/// <summary>
/// Default provider that creates a plain <see cref="TdClient"/> instance.
/// </summary>
public class DefaultClientProvider : IClientProvider
{
    /// <inheritdoc />
    public TdApi.IClient Create()
    {
        return new TdClient();
    }
}
