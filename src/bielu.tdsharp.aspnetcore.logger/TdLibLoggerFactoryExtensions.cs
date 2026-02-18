// SPDX-FileCopyrightText: 2024 tdsharp contributors <https://github.com/egramtel/tdsharp>
//
// SPDX-License-Identifier: MIT

using Microsoft.Extensions.Logging;
using TdLib;

namespace bielu.tdsharp.aspnetcore.logger;

/// <summary>
/// Extension methods for ILoggerFactory to add TDLib logging support.
/// </summary>
public static class TdLibLoggerFactoryExtensions
{
    /// <summary>
    /// Adds a TDLib logger provider to the factory, allowing .NET logs to be written through TDLib.
    /// </summary>
    /// <param name="factory">The logger factory to add the provider to.</param>
    /// <param name="client">The TdClient instance to use for logging.</param>
    /// <param name="minLevel">The minimum log level to write.</param>
    /// <returns>The logger factory for chaining.</returns>
    public static ILoggerFactory AddTdLib(this ILoggerFactory factory, TdClient client, TdLogLevel minLevel = TdLogLevel.Info)
    {
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(client);

        factory.AddProvider(new TdLibLoggerProvider(client, minLevel));
        return factory;
    }
}
