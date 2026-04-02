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
/// you must define the P/Invoke for <c>td_set_log_message_callback</c> in your application
/// and pass it to the <see cref="UseTdLibLogging"/> method.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Step 1: Define P/Invoke in your application (required for callback to work)
/// [DllImport("tdjson", CallingConvention = CallingConvention.Cdecl)]
/// static extern void td_set_log_message_callback(int maxVerbosityLevel, TdLogMessageCallback? callback);
/// 
/// // Step 2: Create logger factory and TdClient
/// using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
/// using var client = new TdClient();
/// 
/// // Step 3: Use the extension method, passing your P/Invoke
/// var cleanup = client.UseTdLibLogging(loggerFactory, TdLogLevel.Info, td_set_log_message_callback);
/// 
/// // ... use client ...
/// 
/// // Step 4: Cleanup when done
/// cleanup.Dispose();
/// </code>
/// </example>
public static class TdLoggerExtensions
{
    /// <summary>
    /// Configures TDLib to route all log messages to the specified ILoggerFactory.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method sets up the complete logging pipeline:
    /// </para>
    /// <list type="bullet">
    /// <item>Creates a <see cref="LogStreamCallback"/> to handle log messages</item>
    /// <item>Sets the TDLib verbosity level</item>
    /// <item>Registers the native callback using your provided P/Invoke</item>
    /// <item>Disables default TDLib logging to prevent duplicate output</item>
    /// </list>
    /// <para>
    /// <b>Important:</b> You must define the P/Invoke for <c>td_set_log_message_callback</c>
    /// in your application and pass it as the <paramref name="setCallback"/> parameter.
    /// This is required due to .NET native interop limitations with cross-assembly callbacks.
    /// </para>
    /// </remarks>
    /// <param name="client">The TdClient instance</param>
    /// <param name="loggerFactory">The ILoggerFactory to use for creating loggers</param>
    /// <param name="logLevel">The TDLib log level to set (controls which messages TDLib generates)</param>
    /// <param name="setCallback">Your P/Invoke method for <c>td_set_log_message_callback</c></param>
    /// <param name="disableDefaultLogging">Whether to disable default console/stderr logging (default: true)</param>
    /// <returns>An <see cref="IDisposable"/> that cleans up the logging when disposed</returns>
    /// <example>
    /// <code>
    /// [DllImport("tdjson", CallingConvention = CallingConvention.Cdecl)]
    /// static extern void td_set_log_message_callback(int maxVerbosityLevel, TdLogMessageCallback? callback);
    /// 
    /// var cleanup = client.UseTdLibLogging(loggerFactory, TdLogLevel.Info, td_set_log_message_callback);
    /// // ... use client ...
    /// cleanup.Dispose();
    /// </code>
    /// </example>
    public static IDisposable UseTdLibLogging(
        this TdClient client,
        ILoggerFactory loggerFactory,
        TdLogLevel logLevel,
        SetLogMessageCallbackDelegate setCallback,
        bool disableDefaultLogging = true)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(loggerFactory);
        ArgumentNullException.ThrowIfNull(setCallback);

        // Create the log handler
        var logHandler = new LogStreamCallback(loggerFactory);

        // Set the verbosity level
        client.Bindings.SetLogVerbosityLevel((int)logLevel);

        // Create and pin the callback
        TdLogMessageCallback callback = (verbosity, msgPtr) => logHandler.HandleLogMessage(verbosity, msgPtr);
        var callbackHandle = GCHandle.Alloc(callback);

        // Register the callback using the consumer's P/Invoke
        setCallback((int)logLevel, callback);

        // Disable default logging if requested
        if (disableDefaultLogging)
        {
            client.Execute(new TdApi.SetLogStream
            {
                LogStream = new TdApi.LogStream.LogStreamEmpty()
            });
        }

        // Return a disposable that handles cleanup
        return new TdLibLoggingScope(logHandler, callbackHandle, setCallback);
    }

    /// <summary>
    /// Configures TDLib to route all log messages to the specified ILoggerFactory with default settings.
    /// </summary>
    /// <remarks>
    /// This overload uses <see cref="TdLogLevel.Warning"/> as the default log level.
    /// </remarks>
    /// <param name="client">The TdClient instance</param>
    /// <param name="loggerFactory">The ILoggerFactory to use for creating loggers</param>
    /// <param name="setCallback">Your P/Invoke method for <c>td_set_log_message_callback</c></param>
    /// <returns>An <see cref="IDisposable"/> that cleans up the logging when disposed</returns>
    public static IDisposable UseTdLibLogging(
        this TdClient client,
        ILoggerFactory loggerFactory,
        SetLogMessageCallbackDelegate setCallback)
    {
        return UseTdLibLogging(client, loggerFactory, TdLogLevel.Warning, setCallback);
    }

    /// <summary>
    /// Configures TDLib verbosity level and disables default logging output.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method prepares TDLib for custom logging by setting the verbosity level
    /// and disabling the default stderr/file logging. Use this if you want more control
    /// over the callback registration process.
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
    /// Creates an <see cref="Action{TdClient}"/> that configures TDLib logging when invoked.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method returns an action suitable for passing to <see cref="bielu.tdsharp.abstractions.IClientProvider.Create(Action{TdClient})"/>
    /// or <see cref="bielu.tdsharp.abstractions.ITdClientFactory.GetOrCreateClient(string, Action{TdClient})"/>.
    /// When invoked, it sets up the complete logging pipeline on the underlying <see cref="TdClient"/>:
    /// </para>
    /// <list type="bullet">
    /// <item>Creates a <see cref="LogStreamCallback"/> to handle log messages</item>
    /// <item>Sets the TDLib verbosity level</item>
    /// <item>Registers the native callback using your provided P/Invoke</item>
    /// <item>Disables default TDLib logging to prevent duplicate output</item>
    /// </list>
    /// <para>
    /// This is especially useful when the client is created through a provider that applies decorators
    /// (such as OpenTelemetry instrumentation), where the returned <see cref="TdLib.TdApi.IClient"/> cannot
    /// be cast to <see cref="TdClient"/>. The configure action is invoked on the native client
    /// <em>before</em> any decoration is applied.
    /// </para>
    /// <para>
    /// <b>Important:</b> The returned <see cref="IDisposable"/> must be stored and disposed to clean up
    /// the native callback. It is returned via the <paramref name="loggingScope"/> output parameter.
    /// </para>
    /// </remarks>
    /// <param name="loggerFactory">The ILoggerFactory to use for creating loggers.</param>
    /// <param name="logLevel">The TDLib log level to set (controls which messages TDLib generates).</param>
    /// <param name="setCallback">Your P/Invoke method for <c>td_set_log_message_callback</c>.</param>
    /// <param name="loggingScope">
    /// When the returned action is invoked, this will be set to an <see cref="IDisposable"/> that cleans up
    /// the logging when disposed. Dispose it when you are done using the client.
    /// </param>
    /// <param name="disableDefaultLogging">Whether to disable default console/stderr logging (default: true).</param>
    /// <returns>An <see cref="Action{TdClient}"/> that configures TDLib logging on the client.</returns>
    /// <example>
    /// <code>
    /// [DllImport("tdjson", CallingConvention = CallingConvention.Cdecl)]
    /// static extern void td_set_log_message_callback(int maxVerbosityLevel, TdLogMessageCallback? callback);
    ///
    /// IDisposable? loggingScope = null;
    /// var configure = TdLoggerExtensions.CreateTdLibLoggingAction(
    ///     loggerFactory, TdLogLevel.Info, td_set_log_message_callback, out loggingScope);
    ///
    /// var client = clientFactory.GetOrCreateClient("demo-user", configure);
    /// // ... use client ...
    /// loggingScope?.Dispose();
    /// </code>
    /// </example>
    public static Action<TdClient> CreateTdLibLoggingAction(
        ILoggerFactory loggerFactory,
        TdLogLevel logLevel,
        SetLogMessageCallbackDelegate setCallback,
        out IDisposable? loggingScope,
        bool disableDefaultLogging = true)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);
        ArgumentNullException.ThrowIfNull(setCallback);

        IDisposable? scope = null;
        loggingScope = null;

        Action<TdClient> action = client =>
        {
            scope = client.UseTdLibLogging(loggerFactory, logLevel, setCallback, disableDefaultLogging);
        };

        // We need a way to expose the scope. We use a wrapper that delays until after invocation.
        // The caller gets the scope via the out parameter after the action is invoked.
        // To solve this, we use a holder pattern.
        var holder = new LoggingScopeHolder();

        Action<TdClient> wrappedAction = client =>
        {
            holder.Scope = client.UseTdLibLogging(loggerFactory, logLevel, setCallback, disableDefaultLogging);
        };

        loggingScope = holder;
        return wrappedAction;
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
