// SPDX-FileCopyrightText: 2024 tdsharp contributors <https://github.com/egramtel/tdsharp>
//
// SPDX-License-Identifier: MIT

using Microsoft.Extensions.Logging;
using TdLib;

namespace bielu.tdsharp.aspnetcore.logger;

/// <summary>
/// ILoggerProvider implementation that creates TdLibLogger instances.
/// </summary>
/// <remarks>
/// <para>
/// This provider creates logger instances that write to TDLib via AddLogMessage.
/// Each logger is created with a specific category name, allowing for per-class logging.
/// </para>
/// <para>
/// <b>Thread Safety:</b> This class is thread-safe and can be used concurrently from multiple threads.
/// </para>
/// </remarks>
public class TdLibLoggerProvider : ILoggerProvider
{
    private readonly TdClient _client;
    private readonly TdLogLevel _minLevel;

    /// <summary>
    /// Initializes a new instance of the <see cref="TdLibLoggerProvider"/> class.
    /// </summary>
    /// <param name="client">The TdClient instance to use for logging.</param>
    /// <param name="minLevel">The minimum log level to write.</param>
    public TdLibLoggerProvider(TdClient client, TdLogLevel minLevel = TdLogLevel.Info)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _minLevel = minLevel;
    }

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName)
    {
        return new TdLibLogger(categoryName, _client, _minLevel);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // No resources to dispose
    }
}
