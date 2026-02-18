// SPDX-FileCopyrightText: 2024 tdsharp contributors <https://github.com/egramtel/tdsharp>
//
// SPDX-License-Identifier: MIT

using Microsoft.Extensions.Logging;
using TdLib;

namespace bielu.tdsharp.aspnetcore.logger;

/// <summary>
/// ILogger implementation that writes log messages to TDLib via AddLogMessage.
/// </summary>
/// <remarks>
/// <para>
/// This logger sends application logs through TDLib's logging system using the AddLogMessage method.
/// Each logger instance is associated with a specific category name and uses a logger factory
/// to allow different loggers for different classes.
/// </para>
/// <para>
/// <b>Thread Safety:</b> This class is thread-safe and can be used concurrently from multiple threads.
/// </para>
/// </remarks>
public class TdLibLogger : ILogger
{
    private readonly string _categoryName;
    private readonly TdClient _client;
    private readonly TdLogLevel _minLevel;

    /// <summary>
    /// Initializes a new instance of the <see cref="TdLibLogger"/> class.
    /// </summary>
    /// <param name="categoryName">The category name for messages produced by the logger.</param>
    /// <param name="client">The TdClient instance to use for logging.</param>
    /// <param name="minLevel">The minimum log level to write.</param>
    public TdLibLogger(string categoryName, TdClient client, TdLogLevel minLevel)
    {
        _categoryName = categoryName ?? throw new ArgumentNullException(nameof(categoryName));
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _minLevel = minLevel;
    }

    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel)
    {
        if (logLevel == LogLevel.None)
            return false;

        var tdLogLevel = logLevel.ToTdLogLevel();
        // In TDLib, lower values = more severe (Fatal=0, Error=1, etc.)
        // So we should log if tdLogLevel <= minLevel
        return (int)tdLogLevel <= (int)_minLevel;
    }

    /// <inheritdoc />
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        ArgumentNullException.ThrowIfNull(formatter);

        var message = formatter(state, exception);

        if (string.IsNullOrEmpty(message) && exception == null)
            return;

        var tdLogLevel = logLevel.ToTdLogLevel();

        // Format the message with category name
        var formattedMessage = $"[{_categoryName}] {message}";

        if (exception != null)
        {
            formattedMessage += Environment.NewLine + exception;
        }

        try
        {
            // Send the log message to TDLib
            _client.Execute(new TdApi.AddLogMessage
            {
                VerbosityLevel = (int)tdLogLevel,
                Text = formattedMessage
            });
        }
        catch (Exception ex)
        {
            // Swallow exceptions to prevent logging from breaking the application
            System.Diagnostics.Debug.WriteLine($"Error writing to TDLib log: {ex}");
        }
    }
}
