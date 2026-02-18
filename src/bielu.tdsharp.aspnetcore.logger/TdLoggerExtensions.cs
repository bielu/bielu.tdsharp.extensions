// SPDX-FileCopyrightText: 2024 tdsharp contributors <https://github.com/egramtel/tdsharp>
//
// SPDX-License-Identifier: MIT

using Microsoft.Extensions.Logging;
using TdLib;
using TdLib.Bindings;

namespace bielu.tdsharp.aspnetcore.logger;

/// <summary>
/// Provides extension methods for integrating TDLib logging with Microsoft.Extensions.Logging.
/// </summary>
/// <remarks>
/// <para>
/// This class enables routing TDLib internal log messages to .NET's ILoggerFactory-based logging system.
/// This allows TDLib logs to appear alongside your application logs using whatever logging providers
/// you have configured (Console, File, Application Insights, etc.).
/// </para>
/// <para>
/// <b>Implementation:</b> This class uses a custom <see cref="LogStreamCallback"/> class that inherits
/// from <see cref="TdApi.LogStream"/> to capture log messages directly and forward them to ILogger
/// without intermediate files.
/// </para>
/// <para>
/// <b>Thread Safety:</b> The UseTdLibLogging methods are not thread-safe.
/// They should be called once during application initialization before using the TdClient.
/// </para>
/// <para>
/// <b>Multi-Client Scenarios:</b> TDLib uses a global log stream, so only one
/// logger configuration can be active at a time.
/// </para>
/// </remarks>
public static class TdLoggerExtensions
{
    private static readonly object _lock = new();
    private static LogStreamCallback? _logStreamCallback;

    /// <summary>
    /// Configures TDLib to route all log messages to the specified ILoggerFactory.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method sets up a custom <see cref="LogStreamCallback"/> that captures TDLib log messages 
    /// directly via the native log message callback and forwards them to .NET's ILoggerFactory.
    /// Each log message is routed through a logger with a category
    /// based on the TDLib source file (e.g., "TDLib.AuthData" for messages from AuthData.cpp).
    /// </para>
    /// <para>
    /// This method is not thread-safe and should be called once during application initialization.
    /// </para>
    /// </remarks>
    /// <param name="client">The TdClient instance</param>
    /// <param name="loggerFactory">The ILoggerFactory to use for creating loggers</param>
    /// <param name="logLevel">The TDLib log level to set (controls which messages TDLib generates)</param>
    public static void UseTdLibLogging(this TdClient client, ILoggerFactory loggerFactory, TdLogLevel logLevel = TdLogLevel.Warning)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        lock (_lock)
        {
            // Clean up any existing log stream
            _logStreamCallback?.Dispose();

            // Create the custom log stream that inherits from TdApi.LogStream
            _logStreamCallback = new LogStreamCallback(loggerFactory);
            
            // Activate the log stream
            _logStreamCallback.Activate(client, logLevel);
        }
    }

    /// <summary>
    /// Configures TDLib to route all log messages to the specified ILoggerFactory,
    /// with an option to disable default logging output.
    /// </summary>
    /// <param name="client">The TdClient instance</param>
    /// <param name="loggerFactory">The ILoggerFactory to use for creating loggers</param>
    /// <param name="logLevel">The TDLib log level to set</param>
    /// <param name="disableDefaultLogging">Whether to disable default console/stderr logging. 
    /// Note: Default logging is always disabled when using this integration to capture logs directly via the custom stream.</param>
    public static void UseTdLibLogging(this TdClient client, ILoggerFactory loggerFactory, TdLogLevel logLevel, bool disableDefaultLogging)
    {
        // Default logging is always disabled since we capture logs via custom stream
        // The disableDefaultLogging parameter is kept for API compatibility
        UseTdLibLogging(client, loggerFactory, logLevel);
    }

    /// <summary>
    /// Configures TDLib to route all log messages to a custom <see cref="LogStreamCallback"/> instance.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method allows you to provide your own <see cref="LogStreamCallback"/> instance
    /// for custom log handling configuration.
    /// </para>
    /// <para>
    /// This method is not thread-safe and should be called once during application initialization.
    /// </para>
    /// </remarks>
    /// <param name="client">The TdClient instance</param>
    /// <param name="logStreamCallback">The custom log stream callback instance</param>
    /// <param name="logLevel">The TDLib log level to set (controls which messages TDLib generates)</param>
    public static void UseTdLibLogging(this TdClient client, LogStreamCallback logStreamCallback, TdLogLevel logLevel = TdLogLevel.Warning)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(logStreamCallback);

        lock (_lock)
        {
            // Clean up any existing log stream (but don't dispose the one passed in)
            if (_logStreamCallback != null && _logStreamCallback != logStreamCallback)
            {
                _logStreamCallback.Dispose();
            }

            _logStreamCallback = logStreamCallback;
            
            // Activate the log stream
            _logStreamCallback.Activate(client, logLevel);
        }
    }

    /// <summary>
    /// Disables TDLib logging integration and clears the log stream.
    /// </summary>
    /// <remarks>
    /// Call this method when disposing your application or when you want to stop
    /// routing TDLib logs to .NET logging.
    /// </remarks>
    public static void DisableTdLibLogging()
    {
        lock (_lock)
        {
            _logStreamCallback?.Dispose();
            _logStreamCallback = null;
        }
    }

    /// <summary>
    /// Maps TDLib verbosity level to Microsoft.Extensions.Logging LogLevel.
    /// </summary>
    /// <remarks>
    /// Both <see cref="TdLogLevel.Verbose"/> and <see cref="TdLogLevel.All"/> map to
    /// <see cref="LogLevel.Trace"/> because .NET's LogLevel has fewer granularity levels
    /// than TDLib's verbosity system. TDLib's All (1024) represents maximum verbosity,
    /// which semantically aligns with Trace in the .NET logging hierarchy.
    /// </remarks>
    /// <param name="tdLogLevel">TDLib verbosity level</param>
    /// <returns>Corresponding Microsoft.Extensions.Logging LogLevel</returns>
    public static LogLevel ToLogLevel(this TdLogLevel tdLogLevel)
    {
        return (int)tdLogLevel switch
        {
            0 => LogLevel.Critical,
            1 => LogLevel.Error,
            2 => LogLevel.Warning,
            3 => LogLevel.Information,
            4 => LogLevel.Debug,
            _ => LogLevel.Trace
        };
    }

    /// <summary>
    /// Maps Microsoft.Extensions.Logging LogLevel to TDLib verbosity level
    /// </summary>
    /// <param name="logLevel">Microsoft.Extensions.Logging LogLevel</param>
    /// <returns>Corresponding TDLib verbosity level</returns>
    public static TdLogLevel ToTdLogLevel(this LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Critical => TdLogLevel.Fatal,
            LogLevel.Error => TdLogLevel.Error,
            LogLevel.Warning => TdLogLevel.Warning,
            LogLevel.Information => TdLogLevel.Info,
            LogLevel.Debug => TdLogLevel.Debug,
            LogLevel.Trace => TdLogLevel.Verbose,
            LogLevel.None => TdLogLevel.Fatal,
            _ => TdLogLevel.Info
        };
    }
}
