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
/// Handles TDLib log messages and routes them to an ILoggerFactory.
/// </summary>
/// <remarks>
/// <para>
/// This class processes TDLib log messages and forwards them to .NET's ILoggerFactory-based logging system.
/// Each log message is routed through a logger with a category based on the TDLib source file
/// (e.g., "TDLib.AuthData" for messages from AuthData.cpp).
/// </para>
/// <para>
/// <b>Important:</b> Due to .NET native interop limitations, the callback registration must be performed
/// from the consumer's assembly, not from this library. Use the <see cref="HandleLogMessage"/> method
/// to route messages from your callback to ILogger.
/// </para>
/// <para>
/// <b>Thread Safety:</b> The callback may be called from TDLib's internal threads.
/// The implementation is thread-safe.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // In your application code (important: P/Invoke must be in your assembly)
/// [DllImport("tdjson", CallingConvention = CallingConvention.Cdecl)]
/// static extern void td_set_log_message_callback(int maxVerbosityLevel, TdLogMessageCallback? callback);
/// 
/// // Create the log handler
/// using var logHandler = new LogStreamCallback(loggerFactory);
/// 
/// // Create and pin your callback delegate
/// TdLogMessageCallback callback = (verbosity, msgPtr) => logHandler.HandleLogMessage(verbosity, msgPtr);
/// var handle = GCHandle.Alloc(callback);
/// 
/// // Register the callback
/// td_set_log_message_callback(5, callback);
/// 
/// // When done:
/// td_set_log_message_callback(0, null);
/// handle.Free();
/// </code>
/// </example>
public sealed class LogStreamCallback : IDisposable
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger _fatalErrorLogger;
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
    }

    /// <summary>
    /// Gets the ILoggerFactory being used by this instance.
    /// </summary>
    public ILoggerFactory LoggerFactory => _loggerFactory;

    /// <summary>
    /// Handles a log message from TDLib. Call this from your native callback.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method should be called from the native callback that you register with
    /// <c>td_set_log_message_callback</c>. It extracts the logger category from the message
    /// and routes it to the appropriate ILogger.
    /// </para>
    /// <para>
    /// This method is thread-safe and can be called from multiple threads simultaneously.
    /// </para>
    /// </remarks>
    /// <param name="verbosityLevel">The TDLib verbosity level of the message (0=Fatal, 1=Error, 2=Warning, 3=Info, 4=Debug, 5+=Verbose).</param>
    /// <param name="messagePtr">Pointer to the null-terminated ANSI string message from TDLib.</param>
    public void HandleLogMessage(int verbosityLevel, IntPtr messagePtr)
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
            var logLevel = TdLoggerExtensions.ToLogLevel((TdLogLevel)verbosityLevel);

            logger.Log(logLevel, "{Message}", message);
        }
        catch (Exception ex)
        {
            // Log to console stderr so exceptions are visible during debugging
            Console.Error.WriteLine($"Error in LogStreamCallback.HandleLogMessage: {ex}");
        }
    }

    /// <summary>
    /// Handles a fatal error message from TDLib.
    /// </summary>
    /// <param name="messagePtr">Pointer to the null-terminated ANSI string message.</param>
    public void HandleFatalError(IntPtr messagePtr)
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
            Console.Error.WriteLine($"Error in LogStreamCallback.HandleFatalError: {ex}");
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
        _disposed = true;
    }
}
