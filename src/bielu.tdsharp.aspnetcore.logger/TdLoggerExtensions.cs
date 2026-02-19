// SPDX-FileCopyrightText: 2024 tdsharp contributors <https://github.com/egramtel/tdsharp>
//
// SPDX-License-Identifier: MIT

using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using TdLib;
using TdLib.Bindings;

namespace bielu.tdsharp.aspnetcore.logger;

/// <summary>
/// Provides extension methods and helpers for integrating TDLib logging with Microsoft.Extensions.Logging.
/// </summary>
/// <remarks>
/// <para>
/// This class enables routing TDLib internal log messages to .NET's ILoggerFactory-based logging system.
/// This allows TDLib logs to appear alongside your application logs using whatever logging providers
/// you have configured (Console, File, Application Insights, etc.).
/// </para>
/// <para>
/// <b>Important:</b> Due to .NET native interop limitations with cross-assembly callbacks,
/// the callback registration must be performed from your application code using the
/// <see cref="TdLogMessageCallback"/> delegate and your own P/Invoke declaration.
/// See the example below for proper usage.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Step 1: Define P/Invoke in your application (required for callback to work)
/// [DllImport("tdjson", CallingConvention = CallingConvention.Cdecl)]
/// static extern void td_set_log_message_callback(int maxVerbosityLevel, TdLogMessageCallback? callback);
/// 
/// // Step 2: Create logger factory and log handler
/// using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
/// using var logHandler = new LogStreamCallback(loggerFactory);
/// 
/// // Step 3: Create TdClient
/// using var client = new TdClient();
/// 
/// // Step 4: Set verbosity level and create callback
/// client.Bindings.SetLogVerbosityLevel((int)TdLogLevel.Info);
/// TdLogMessageCallback callback = (verbosity, msgPtr) => logHandler.HandleLogMessage(verbosity, msgPtr);
/// var handle = GCHandle.Alloc(callback);
/// 
/// // Step 5: Register callback and disable default logging
/// td_set_log_message_callback((int)TdLogLevel.Info, callback);
/// client.Execute(new TdApi.SetLogStream { LogStream = new TdApi.LogStream.LogStreamEmpty() });
/// 
/// // ... use client ...
/// 
/// // Step 6: Cleanup
/// td_set_log_message_callback(0, null);
/// handle.Free();
/// </code>
/// </example>
public static class TdLoggerExtensions
{
    /// <summary>
    /// Configures TDLib verbosity level and disables default logging output.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method prepares TDLib for custom logging by setting the verbosity level
    /// and disabling the default stderr/file logging. After calling this method,
    /// you must register your callback using your own P/Invoke declaration.
    /// </para>
    /// <para>
    /// <b>Important:</b> Due to .NET native interop limitations, you must define
    /// the P/Invoke for <c>td_set_log_message_callback</c> in your own application
    /// and register the callback yourself. See class documentation for an example.
    /// </para>
    /// </remarks>
    /// <param name="client">The TdClient instance</param>
    /// <param name="logLevel">The TDLib log level to set (controls which messages TDLib generates)</param>
    public static void PrepareTdLibLogging(this TdClient client, TdLogLevel logLevel = TdLogLevel.Warning)
    {
        ArgumentNullException.ThrowIfNull(client);

        // Set the verbosity level to control what messages TDLib generates
        client.Bindings.SetLogVerbosityLevel((int)logLevel);

        // Disable default logging output (stderr/file) to prevent duplicate logs
        client.Execute(new TdApi.SetLogStream
        {
            LogStream = new TdApi.LogStream.LogStreamEmpty()
        });
    }

    /// <summary>
    /// Creates a <see cref="TdLogMessageCallback"/> delegate that routes messages to the specified handler.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is a convenience method to create a callback delegate. You still need to:
    /// </para>
    /// <list type="number">
    /// <item>Pin the delegate with <see cref="GCHandle.Alloc(object)"/></item>
    /// <item>Register it using your own P/Invoke declaration</item>
    /// <item>Free the handle when done</item>
    /// </list>
    /// </remarks>
    /// <param name="handler">The LogStreamCallback instance to route messages to</param>
    /// <returns>A delegate that can be passed to <c>td_set_log_message_callback</c></returns>
    public static TdLogMessageCallback CreateCallback(LogStreamCallback handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        return (verbosity, msgPtr) => handler.HandleLogMessage(verbosity, msgPtr);
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
