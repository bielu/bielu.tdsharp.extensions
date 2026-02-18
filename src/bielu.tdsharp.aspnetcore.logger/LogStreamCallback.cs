// SPDX-FileCopyrightText: 2024 tdsharp contributors <https://github.com/egramtel/tdsharp>
//
// SPDX-License-Identifier: MIT

using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using TdLib;
using TdLib.Bindings;

namespace bielu.tdsharp.aspnetcore.logger;

/// <summary>
/// Captures TDLib log messages using td_set_log_message_callback and forwards them to an ILoggerFactory.
/// </summary>
/// <remarks>
/// <para>
/// This class uses TDLib's native <c>td_set_log_message_callback</c> function (added in TDLib 1.7.5)
/// to intercept ALL log messages and forward them to .NET's ILoggerFactory-based logging system.
/// </para>
/// <para>
/// The <c>td_set_log_message_callback</c> function is not exposed by TDSharp's standard bindings,
/// so this class uses P/Invoke via <see cref="TdNativeLogging"/> to access it directly.
/// See TDLib issue #794 for the history of this feature.
/// </para>
/// <para>
/// Each log message is routed through a logger with a category based on the TDLib source file
/// (e.g., "TDLib.AuthData" for messages from AuthData.cpp).
/// </para>
/// <para>
/// <b>Thread Safety:</b> The native callback is called from TDLib's internal threads.
/// The callback delegate is pinned using GCHandle to prevent garbage collection.
/// </para>
/// </remarks>
public sealed class LogStreamCallback : IDisposable
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger _fatalErrorLogger;
    private readonly TdLogMessageCallback _nativeCallback;
    private readonly GCHandle _callbackHandle;
    private readonly Callback _fatalErrorCallback;
    private bool _disposed;

    /// <summary>
    /// Regex pattern to extract source file from TDLib log messages.
    /// Matches patterns like [AuthData.cpp:122] or [Td.cpp:1346]
    /// </summary>
    private static readonly Regex SourceFilePattern = new(
        @"\[([A-Za-z0-9_]+)\.cpp:\d+\]",
        RegexOptions.Compiled);

    /// <summary>
    /// Initializes a new instance of the <see cref="LogStreamCallback"/> class.
    /// </summary>
    /// <param name="loggerFactory">The ILoggerFactory to use for creating loggers.</param>
    /// <exception cref="ArgumentNullException">Thrown when loggerFactory is null.</exception>
    public LogStreamCallback(ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);
        
        _loggerFactory = loggerFactory;
        _fatalErrorLogger = loggerFactory.CreateLogger("TDLib.FatalError");
        
        // Create and pin the callback delegates to prevent garbage collection
        _nativeCallback = OnLogMessage;
        _callbackHandle = GCHandle.Alloc(_nativeCallback);
        _fatalErrorCallback = OnFatalError;
    }

    /// <summary>
    /// Activates this log stream for the specified TdClient.
    /// </summary>
    /// <remarks>
    /// This method:
    /// <list type="number">
    /// <item>Sets the TDLib verbosity level</item>
    /// <item>Disables default logging output using LogStreamEmpty</item>
    /// <item>Registers the native log message callback via td_set_log_message_callback</item>
    /// <item>Registers the fatal error callback</item>
    /// </list>
    /// </remarks>
    /// <param name="client">The TdClient instance to configure logging for.</param>
    /// <param name="logLevel">The TDLib log level to set (controls which messages TDLib generates).</param>
    /// <exception cref="ArgumentNullException">Thrown when client is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when this instance has been disposed.</exception>
    public void Activate(TdClient client, TdLogLevel logLevel = TdLogLevel.Warning)
    {
        ArgumentNullException.ThrowIfNull(client);
        ObjectDisposedException.ThrowIf(_disposed, this);

        // Set the verbosity level to control what messages TDLib generates
        client.Bindings.SetLogVerbosityLevel((int)logLevel);

        // Disable default logging output (stderr/file) - we capture via callback instead
        client.Execute(new TdApi.SetLogStream
        {
            LogStream = new TdApi.LogStream.LogStreamEmpty()
        });

        // Register our callback using td_set_log_message_callback (TDLib 1.7.5+)
        // This is the key function that allows intercepting ALL log messages
        TdNativeLogging.SetLogMessageCallback((int)logLevel, _nativeCallback);

        // Also register fatal error callback for critical errors that bypass normal logging
        client.Bindings.SetLogFatalErrorCallback(_fatalErrorCallback);
    }

    /// <summary>
    /// Deactivates this log stream and clears the native callbacks.
    /// </summary>
    public void Deactivate()
    {
        TdNativeLogging.SetLogMessageCallback(0, null);
    }

    private void OnLogMessage(int verbosityLevel, IntPtr messagePtr)
    {
        if (_disposed || messagePtr == IntPtr.Zero)
        {
            return;
        }

        try
        {
            var message = Marshal.PtrToStringAnsi(messagePtr);
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            var category = ExtractLoggerCategory(message);
            var logger = _loggerFactory.CreateLogger(category);
            var logLevel = ((TdLogLevel)verbosityLevel).ToLogLevel();

            logger.Log(logLevel, "{Message}", message);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in LogStreamCallback.OnLogMessage: {ex}");
        }
    }

    private void OnFatalError(IntPtr messagePtr)
    {
        if (_disposed || messagePtr == IntPtr.Zero)
        {
            return;
        }

        try
        {
            var message = Marshal.PtrToStringAnsi(messagePtr);
            _fatalErrorLogger.LogCritical("{Message}", message);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in LogStreamCallback.OnFatalError: {ex}");
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
        {
            return "TDLib";
        }

        var match = SourceFilePattern.Match(message);
        if (match.Success)
        {
            var sourceFile = match.Groups[1].Value;
            return $"TDLib.{sourceFile}";
        }

        return "TDLib";
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // Clear the native callback
        TdNativeLogging.SetLogMessageCallback(0, null);

        // Free the pinned callback handle
        if (_callbackHandle.IsAllocated)
        {
            _callbackHandle.Free();
        }
    }
}
