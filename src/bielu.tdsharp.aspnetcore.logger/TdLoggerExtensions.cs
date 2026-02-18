// SPDX-FileCopyrightText: 2024 tdsharp contributors <https://github.com/egramtel/tdsharp>
//
// SPDX-License-Identifier: MIT

using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using TdLib;
using TdLib.Bindings;

namespace bielu.tdsharp.aspnetcore.logger;

/// <summary>
/// Delegate for TDLib log message callback
/// </summary>
/// <param name="verbosityLevel">The verbosity level of the message</param>
/// <param name="message">The log message</param>
public delegate void LogMessageCallback(int verbosityLevel, string message);

/// <summary>
/// Provides extension methods for integrating TDLib logging with Microsoft.Extensions.Logging.
/// </summary>
/// <remarks>
/// <para>
/// <b>Thread Safety:</b> The UseTdLibLogging methods are not thread-safe.
/// They should be called once during application initialization before using the TdClient.
/// </para>
/// <para>
/// <b>Multi-Client Scenarios:</b> TDLib uses a global fatal error callback, so only one
/// logger can be configured for fatal errors at a time. If you need different loggers
/// for different TdClient instances, use the <see cref="TdLibLoggerProvider"/> instead
/// for application-to-TDLib logging.
/// </para>
/// </remarks>
public static class TdLoggerExtensions
{
    private static readonly object _lock = new();
    private static ILoggerFactory? _loggerFactory;
    private static Callback? _nativeCallback;

    /// <summary>
    /// Configures TDLib to use the specified ILoggerFactory for logging fatal errors.
    /// This allows creating a new logger instance for each class where logging is called.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method is not thread-safe and should be called once during application initialization.
    /// </para>
    /// <para>
    /// TDLib native bindings only support a global fatal error callback.
    /// For full logging integration where TDLib logs are routed to ILogger, consider
    /// using a file-based log stream and monitoring the log file.
    /// </para>
    /// </remarks>
    /// <param name="client">The TdClient instance</param>
    /// <param name="loggerFactory">The ILoggerFactory to use for creating loggers</param>
    /// <param name="logLevel">The TDLib log level to set</param>
    public static void UseTdLibLogging(this TdClient client, ILoggerFactory loggerFactory, TdLogLevel logLevel = TdLogLevel.Warning)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        lock (_lock)
        {
            _loggerFactory = loggerFactory;

            // Set the verbosity level
            client.Bindings.SetLogVerbosityLevel((int)logLevel);

            // Set up the fatal error callback to route to ILogger
            // Keep a reference to prevent garbage collection
            _nativeCallback = OnFatalError;
            client.Bindings.SetLogFatalErrorCallback(_nativeCallback);
        }
    }

    /// <summary>
    /// Configures TDLib to use the specified ILogger for logging fatal errors.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method is not thread-safe and should be called once during application initialization.
    /// </para>
    /// <para>
    /// TDLib native bindings only support a global fatal error callback.
    /// For full logging integration where TDLib logs are routed to ILogger, consider
    /// using a file-based log stream and monitoring the log file.
    /// </para>
    /// </remarks>
    /// <param name="client">The TdClient instance</param>
    /// <param name="logger">The ILogger to use for logging</param>
    /// <param name="logLevel">The TDLib log level to set</param>
    public static void UseTdLibLogging(this TdClient client, ILogger logger, TdLogLevel logLevel = TdLogLevel.Warning)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(logger);

        lock (_lock)
        {
            // Create a factory that always returns the provided logger
            _loggerFactory = new SingleLoggerFactory(logger);

            // Set the verbosity level
            client.Bindings.SetLogVerbosityLevel((int)logLevel);

            // Set up the fatal error callback to route to ILogger
            // Keep a reference to prevent garbage collection
            _nativeCallback = OnFatalError;
            client.Bindings.SetLogFatalErrorCallback(_nativeCallback);
        }
    }

    /// <summary>
    /// Configures TDLib to use the specified ILoggerFactory for logging,
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
            client.Execute(new TdLib.TdApi.SetLogStream
            {
                LogStream = new TdLib.TdApi.LogStream.LogStreamEmpty()
            });
        }
    }

    /// <summary>
    /// Configures TDLib to use the specified ILogger for logging,
    /// with an option to disable default logging output.
    /// </summary>
    /// <param name="client">The TdClient instance</param>
    /// <param name="logger">The ILogger to use for logging</param>
    /// <param name="logLevel">The TDLib log level to set</param>
    /// <param name="disableDefaultLogging">Whether to disable default console/stderr logging</param>
    public static void UseTdLibLogging(this TdClient client, ILogger logger, TdLogLevel logLevel, bool disableDefaultLogging)
    {
        UseTdLibLogging(client, logger, logLevel);

        if (disableDefaultLogging)
        {
            // Disable default logging by setting an empty log stream
            client.Execute(new TdLib.TdApi.SetLogStream
            {
                LogStream = new TdLib.TdApi.LogStream.LogStreamEmpty()
            });
        }
    }

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
            // Use Debug.WriteLine for diagnostics as we cannot use the logger here
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

    /// <summary>
    /// Simple logger factory implementation that always returns a single logger instance
    /// </summary>
    private class SingleLoggerFactory : ILoggerFactory
    {
        private readonly ILogger _logger;

        public SingleLoggerFactory(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public ILogger CreateLogger(string categoryName)
        {
            return _logger;
        }

        public void AddProvider(ILoggerProvider provider)
        {
            // No-op for single logger factory
        }

        public void Dispose()
        {
            // No-op for single logger factory
        }
    }
}
