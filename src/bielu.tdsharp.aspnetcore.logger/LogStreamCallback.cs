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
/// Delegate for the native TDLib log message callback.
/// </summary>
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void NativeLogCallback(int verbosityLevel, IntPtr message);

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
/// so this class uses P/Invoke to access it directly.
/// See TDLib issue #794 for the history of this feature.
/// </para>
/// <para>
/// Each log message is routed through a logger with a category based on the TDLib source file
/// (e.g., "TDLib.AuthData" for messages from AuthData.cpp).
/// </para>
/// <para>
/// <b>Thread Safety:</b> The native callback is called from TDLib's internal threads.
/// The callback delegate is stored in a static field to prevent garbage collection.
/// </para>
/// </remarks>
public sealed class LogStreamCallback : IDisposable
{
    /// <summary>
    /// Direct P/Invoke to TDLib's td_set_log_message_callback function.
    /// </summary>
    [DllImport("tdjson", CallingConvention = CallingConvention.Cdecl)]
    private static extern void td_set_log_message_callback(int maxVerbosityLevel, NativeLogCallback? callback);

    /// <summary>
    /// Alternative P/Invoke that takes a function pointer as IntPtr.
    /// </summary>
    [DllImport("tdjson", CallingConvention = CallingConvention.Cdecl, EntryPoint = "td_set_log_message_callback")]
    private static extern void td_set_log_message_callback_ptr(int maxVerbosityLevel, IntPtr callback);

    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger _fatalErrorLogger;
    private readonly Callback _fatalErrorCallback;
    private bool _disposed;

    /// <summary>
    /// Static reference to the current active instance for routing callbacks.
    /// TDLib only supports a single global callback, so we use a static reference.
    /// Access is synchronized using Volatile.Read/Write for thread safety without locks
    /// (locks should be avoided in native callbacks).
    /// </summary>
    private static LogStreamCallback? _currentInstance;

    /// <summary>
    /// Static reference to the callback delegate to prevent garbage collection.
    /// The delegate must remain alive as long as the callback is registered with native code.
    /// </summary>
    private static NativeLogCallback? _callbackDelegate;

    /// <summary>
    /// GC handle to pin the callback delegate, preventing it from being collected.
    /// </summary>
    private static GCHandle _callbackHandle;

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

        Console.WriteLine($"[DEBUG-ACTIVATE] Starting with logLevel={logLevel} ({(int)logLevel})");
        
        // Set the verbosity level to control what messages TDLib generates
        client.Bindings.SetLogVerbosityLevel((int)logLevel);
        Console.WriteLine($"[DEBUG-ACTIVATE] SetLogVerbosityLevel called");

        // IMPORTANT: Set the current instance BEFORE registering the callback
        // This ensures the callback has a valid instance to route messages to
        Volatile.Write(ref _currentInstance, this);
        Console.WriteLine($"[DEBUG-ACTIVATE] _currentInstance set");

        // Free any previous callback handle to prevent memory leak
        if (_callbackHandle.IsAllocated)
        {
            _callbackHandle.Free();
            Console.WriteLine($"[DEBUG-ACTIVATE] Previous handle freed");
        }

        // Create and pin the callback delegate to prevent garbage collection
        // The delegate must remain alive as long as the callback is registered with native code
        _callbackDelegate = OnNativeLogMessage;
        _callbackHandle = GCHandle.Alloc(_callbackDelegate);
        Console.WriteLine($"[DEBUG-ACTIVATE] Callback delegate created and pinned");

        // Get the function pointer for the delegate
        var callbackPtr = Marshal.GetFunctionPointerForDelegate(_callbackDelegate);
        Console.WriteLine($"[DEBUG-ACTIVATE] Function pointer: {callbackPtr}");

        // Register our callback using td_set_log_message_callback (TDLib 1.7.5+)
        Console.WriteLine($"[DEBUG-ACTIVATE] About to call td_set_log_message_callback_ptr");
        td_set_log_message_callback_ptr((int)logLevel, callbackPtr);
        Console.WriteLine($"[DEBUG-ACTIVATE] td_set_log_message_callback_ptr returned");

        // TEMPORARILY SKIP LogStreamEmpty to debug
        // Disable default logging output (stderr/file) to prevent duplicate logs.
        // Note: LogStreamEmpty and td_set_log_message_callback operate independently.
        // LogStreamEmpty prevents TDLib from writing to stderr/file, while
        // td_set_log_message_callback intercepts messages before they would be written.
        // Using both ensures logs only go through our callback.
        // We do this AFTER registering the callback to avoid losing any messages
        // client.Execute(new TdApi.SetLogStream
        // {
        //     LogStream = new TdApi.LogStream.LogStreamEmpty()
        // });
        Console.WriteLine($"[DEBUG-ACTIVATE] SetLogStream(LogStreamEmpty) SKIPPED for debugging");

        // Also register fatal error callback for critical errors that bypass normal logging
        client.Bindings.SetLogFatalErrorCallback(_fatalErrorCallback);
        Console.WriteLine($"[DEBUG-ACTIVATE] Done");
    }

    /// <summary>
    /// Deactivates this log stream and clears the native callbacks.
    /// </summary>
    public void Deactivate()
    {
        // First disable the callback to stop receiving messages
        td_set_log_message_callback(0, null);
        
        // Free the callback handle
        if (_callbackHandle.IsAllocated)
        {
            _callbackHandle.Free();
        }
        _callbackDelegate = null;
        
        // Atomically clear the instance reference only if it still points to this instance
        Interlocked.CompareExchange(ref _currentInstance, null, this);
    }

    /// <summary>
    /// Callback method that is invoked by native TDLib code when a log message is generated.
    /// This method is called from TDLib's internal threads.
    /// </summary>
    private static void OnNativeLogMessage(int verbosityLevel, IntPtr messagePtr)
    {
        Console.WriteLine($"[DEBUG-CALLBACK] Entered! verbosity={verbosityLevel}");
        
        // Use volatile read to avoid lock in native callback
        // This is safe because we only need eventual consistency for the instance reference
        var instance = Volatile.Read(ref _currentInstance);
        
        if (instance == null)
        {
            Console.WriteLine($"[DEBUG-CALLBACK] ERROR: instance is null!");
            return;
        }

        // Route to the instance method if available
        instance.OnLogMessage(verbosityLevel, messagePtr);
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
            // Log to console stderr so exceptions are visible during debugging
            Console.Error.WriteLine($"Error in LogStreamCallback.OnLogMessage: {ex}");
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
            // Log to console stderr so exceptions are visible during debugging
            Console.Error.WriteLine($"Error in LogStreamCallback.OnFatalError: {ex}");
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

        // First disable the callback to stop receiving messages
        td_set_log_message_callback(0, null);

        // Free the callback handle
        if (_callbackHandle.IsAllocated)
        {
            _callbackHandle.Free();
        }
        _callbackDelegate = null;

        // Atomically clear the instance reference only if it still points to this instance
        Interlocked.CompareExchange(ref _currentInstance, null, this);
    }
}
