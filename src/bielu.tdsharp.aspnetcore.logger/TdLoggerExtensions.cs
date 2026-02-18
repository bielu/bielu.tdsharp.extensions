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
/// <b>Implementation:</b> This class uses a custom <see cref="ILogStream"/> implementation 
/// (<see cref="LoggerLogStream"/>) that captures log messages directly via TDLib's native callback
/// and forwards them to ILogger without intermediate files.
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
    private static LogStreamManager? _logStreamManager;
    private static LoggerLogStream? _loggerLogStream;

    /// <summary>
    /// Configures TDLib to route all log messages to the specified ILoggerFactory.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method sets up a custom <see cref="LoggerLogStream"/> that captures TDLib log messages 
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
            // Clean up any existing resources
            _logStreamManager?.Dispose();
            _loggerLogStream?.Dispose();

            // Create the custom log stream that forwards to ILogger
            _loggerLogStream = new LoggerLogStream(loggerFactory);

            // Create and configure the log stream manager
            _logStreamManager = new LogStreamManager(client);
            _logStreamManager.SetLogStream(_loggerLogStream, logLevel);
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
    /// Configures TDLib to route all log messages to a custom <see cref="ILogStream"/> implementation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method allows you to provide your own custom log stream implementation
    /// that receives TDLib log messages directly without intermediate files.
    /// </para>
    /// <para>
    /// This method is not thread-safe and should be called once during application initialization.
    /// </para>
    /// </remarks>
    /// <param name="client">The TdClient instance</param>
    /// <param name="logStream">The custom log stream implementation</param>
    /// <param name="logLevel">The TDLib log level to set (controls which messages TDLib generates)</param>
    public static void UseTdLibLogging(this TdClient client, ILogStream logStream, TdLogLevel logLevel = TdLogLevel.Warning)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(logStream);

        lock (_lock)
        {
            // Clean up any existing resources
            _logStreamManager?.Dispose();
            _loggerLogStream?.Dispose();
            _loggerLogStream = null;

            // Create and configure the log stream manager with the custom stream
            _logStreamManager = new LogStreamManager(client);
            _logStreamManager.SetLogStream(logStream, logLevel);
        }
    }
        UseTdLibLogging(client, loggerFactory, logLevel);
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
            
            // Free the pinned callback handle
            if (_callbackHandle.IsAllocated)
            {
                _callbackHandle.Free();
            }

            _nativeCallback = null;
            _fatalErrorCallback = null;
            _loggerFactory = null;
        }
    }

    /// <summary>
    /// Extracts the source file name from a TDLib log message to use as logger category.
    /// </summary>
    /// <param name="message">The raw TDLib log message</param>
    /// <returns>Logger category like "TDLib.AuthData" or "TDLib" if not found</returns>
    internal static string ExtractLoggerCategory(string message)
    {
        if (string.IsNullOrEmpty(message))
            return "TDLib";

        var match = SourceFilePattern.Match(message);
        if (match.Success)
        {
            var sourceFile = match.Groups[1].Value;
            return $"TDLib.{sourceFile}";
        }

        return "TDLib";
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

            // Extract the source file from the message to use as logger category
            var category = ExtractLoggerCategory(message);
            var logger = currentLoggerFactory.CreateLogger(category);
            var logLevel = ((TdLogLevel)verbosityLevel).ToLogLevel();

            // Log the message at the appropriate level
            logger.Log(logLevel, "{Message}", message);
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
            logger.LogCritical("{Message}", message);
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
