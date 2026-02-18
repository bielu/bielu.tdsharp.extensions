// SPDX-FileCopyrightText: 2024 tdsharp contributors <https://github.com/egramtel/tdsharp>
//
// SPDX-License-Identifier: MIT

using System.Runtime.InteropServices;
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
/// <b>Thread Safety:</b> The UseTdLibLogging methods are not thread-safe.
/// They should be called once during application initialization before using the TdClient.
/// </para>
/// <para>
/// <b>Multi-Client Scenarios:</b> TDLib uses a global log message callback, so only one
/// logger configuration can be active at a time.
/// </para>
/// </remarks>
public static class TdLoggerExtensions
{
    private static readonly object _lock = new();
    private static ILoggerFactory? _loggerFactory;
    private static TdLogMessageCallback? _nativeCallback;
    private static Callback? _fatalErrorCallback;

    /// <summary>
    /// Configures TDLib to route all log messages to the specified ILoggerFactory.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method sets up TDLib's log message callback to forward all TDLib internal logs
    /// to .NET's ILoggerFactory. Each log message is routed through a logger with category "TDLib".
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
            _loggerFactory = loggerFactory;

            // Set the verbosity level to control what messages TDLib generates
            client.Bindings.SetLogVerbosityLevel((int)logLevel);

            // Set up the log message callback to route ALL log messages to ILogger
            // Keep a reference to prevent garbage collection
            _nativeCallback = OnLogMessage;
            TdNativeLogging.SetLogMessageCallback((int)logLevel, _nativeCallback);

            // Also set up fatal error callback for critical errors that bypass normal logging
            _fatalErrorCallback = OnFatalError;
            client.Bindings.SetLogFatalErrorCallback(_fatalErrorCallback);
        }
    }

    /// <summary>
    /// Configures TDLib to route all log messages to the specified ILoggerFactory,
    /// with an option to disable default logging output.
    /// </summary>
    /// <param name="client">The TdClient instance</param>
    /// <param name="loggerFactory">The ILoggerFactory to use for creating loggers</param>
    /// <param name="logLevel">The TDLib log level to set</param>
    /// <param name="disableDefaultLogging">Whether to disable default console/stderr logging</param>
    public static void UseTdLibLogging(this TdClient client, ILoggerFactory loggerFactory, TdLogLevel logLevel, bool disableDefaultLogging)
    {
        UseTdLibLogging(client, loggerFactory, logLevel);

        if (disableDefaultLogging)
        {
            // Disable default logging by setting an empty log stream
            client.Execute(new TdApi.SetLogStream
            {
                LogStream = new TdApi.LogStream.LogStreamEmpty()
            });
        }
    }

    /// <summary>
    /// Disables TDLib logging integration and clears the callback.
    /// </summary>
    /// <remarks>
    /// Call this method when disposing your application or when you want to stop
    /// routing TDLib logs to .NET logging.
    /// </remarks>
    public static void DisableTdLibLogging()
    {
        lock (_lock)
        {
            // Clear the callback by setting null
            TdNativeLogging.SetLogMessageCallback(0, null);
            _nativeCallback = null;
            _fatalErrorCallback = null;
            _loggerFactory = null;
        }
    }

    /// <summary>
    /// Callback handler for ALL TDLib log messages.
    /// </summary>
    private static void OnLogMessage(int verbosityLevel, IntPtr messagePtr)
    {
        ILoggerFactory? currentLoggerFactory;
        lock (_lock)
        {
            currentLoggerFactory = _loggerFactory;
        }

        if (currentLoggerFactory == null || messagePtr == IntPtr.Zero)
            return;

        try
        {
            var message = Marshal.PtrToStringAnsi(messagePtr);
            if (string.IsNullOrEmpty(message))
                return;

            // Create a logger for TDLib messages
            var logger = currentLoggerFactory.CreateLogger("TDLib");
            var logLevel = ToLogLevel(verbosityLevel);

            // Log the message at the appropriate level
            logger.Log(logLevel, "[TDLib] {Message}", message);
        }
        catch (Exception ex)
        {
            // Swallow exceptions in callback to prevent native crashes
            System.Diagnostics.Debug.WriteLine($"Error in TDLib log callback: {ex}");
        }
    }

    /// <summary>
    /// Callback handler for TDLib fatal errors.
    /// </summary>
    private static void OnFatalError(IntPtr messagePtr)
    {
        ILoggerFactory? currentLoggerFactory;
        lock (_lock)
        {
            currentLoggerFactory = _loggerFactory;
        }

        if (currentLoggerFactory == null || messagePtr == IntPtr.Zero)
            return;

        try
        {
            var message = Marshal.PtrToStringAnsi(messagePtr);
            // Create a logger specifically for TDLib fatal errors
            var logger = currentLoggerFactory.CreateLogger("TDLib.FatalError");
            logger.LogCritical("[TDLib Fatal] {Message}", message);
        }
        catch (Exception ex)
        {
            // Swallow exceptions in callback to prevent native crashes
            System.Diagnostics.Debug.WriteLine($"Error in TDLib fatal error callback: {ex}");
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
    /// <param name="tdLogLevel">TDLib verbosity level (0-5+)</param>
    /// <returns>Corresponding Microsoft.Extensions.Logging LogLevel</returns>
    public static LogLevel ToLogLevel(this TdLogLevel tdLogLevel)
    {
        return tdLogLevel switch
        {
            TdLogLevel.Fatal => LogLevel.Critical,
            TdLogLevel.Error => LogLevel.Error,
            TdLogLevel.Warning => LogLevel.Warning,
            TdLogLevel.Info => LogLevel.Information,
            TdLogLevel.Debug => LogLevel.Debug,
            TdLogLevel.Verbose => LogLevel.Trace,
            TdLogLevel.All => LogLevel.Trace,
            _ => LogLevel.Information
        };
    }

    /// <summary>
    /// Maps TDLib verbosity level integer to Microsoft.Extensions.Logging LogLevel
    /// </summary>
    /// <param name="verbosityLevel">TDLib verbosity level (0-5+)</param>
    /// <returns>Corresponding Microsoft.Extensions.Logging LogLevel</returns>
    public static LogLevel ToLogLevel(int verbosityLevel)
    {
        return verbosityLevel switch
        {
            <= 0 => LogLevel.Critical,
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
