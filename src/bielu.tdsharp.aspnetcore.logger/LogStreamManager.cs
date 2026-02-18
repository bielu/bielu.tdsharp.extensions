// SPDX-FileCopyrightText: 2024 tdsharp contributors <https://github.com/egramtel/tdsharp>
//
// SPDX-License-Identifier: MIT

using System.Runtime.InteropServices;
using TdLib;
using TdLib.Bindings;

namespace bielu.tdsharp.aspnetcore.logger;

/// <summary>
/// Manages the connection between TDLib's native logging and custom ILogStream implementations.
/// </summary>
/// <remarks>
/// <para>
/// This class sets up the native log message callback and routes all TDLib log messages
/// to a custom <see cref="ILogStream"/> implementation without intermediate files.
/// </para>
/// <para>
/// <b>Thread Safety:</b> This class is thread-safe for concurrent access.
/// </para>
/// <para>
/// <b>Lifetime:</b> Call <see cref="Dispose"/> when done to properly clean up native callbacks.
/// </para>
/// </remarks>
public sealed class LogStreamManager : IDisposable
{
    private static readonly object _globalLock = new();
    private static LogStreamManager? _currentInstance;

    private readonly object _lock = new();
    private readonly TdClient _client;
    private ILogStream? _logStream;
    private TdLogMessageCallback? _nativeCallback;
    private GCHandle _callbackHandle;
    private Callback? _fatalErrorCallback;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="LogStreamManager"/> class.
    /// </summary>
    /// <param name="client">The TdClient instance to manage logging for.</param>
    /// <exception cref="ArgumentNullException">Thrown when client is null.</exception>
    public LogStreamManager(TdClient client)
    {
        ArgumentNullException.ThrowIfNull(client);
        _client = client;
    }

    /// <summary>
    /// Sets the custom log stream to receive TDLib log messages.
    /// </summary>
    /// <param name="logStream">The custom log stream implementation.</param>
    /// <param name="logLevel">The TDLib log level to set (controls which messages TDLib generates).</param>
    /// <exception cref="ArgumentNullException">Thrown when logStream is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when another LogStreamManager is already active.</exception>
    public void SetLogStream(ILogStream logStream, TdLogLevel logLevel = TdLogLevel.Warning)
    {
        ArgumentNullException.ThrowIfNull(logStream);

        lock (_globalLock)
        {
            if (_currentInstance != null && _currentInstance != this)
            {
                throw new InvalidOperationException(
                    "Another LogStreamManager is already active. TDLib only supports one global log stream. " +
                    "Dispose the existing manager before creating a new one.");
            }

            lock (_lock)
            {
                // Clean up any existing callback
                CleanupCallback();

                _logStream = logStream;

                // Set the verbosity level
                _client.Bindings.SetLogVerbosityLevel((int)logLevel);

                // Disable default logging output - we capture everything via callback
                _client.Execute(new TdApi.SetLogStream
                {
                    LogStream = new TdApi.LogStream.LogStreamEmpty()
                });

                // Create and pin the callback delegate
                _nativeCallback = OnLogMessage;
                _callbackHandle = GCHandle.Alloc(_nativeCallback);

                // Set up the native callback
                TdNativeLogging.SetLogMessageCallback((int)logLevel, _nativeCallback);

                // Set up fatal error callback
                _fatalErrorCallback = OnFatalError;
                _client.Bindings.SetLogFatalErrorCallback(_fatalErrorCallback);

                _currentInstance = this;
            }
        }
    }

    /// <summary>
    /// Clears the current log stream and stops receiving log messages.
    /// </summary>
    public void ClearLogStream()
    {
        lock (_globalLock)
        {
            lock (_lock)
            {
                CleanupCallback();
                _logStream?.Dispose();
                _logStream = null;

                if (_currentInstance == this)
                {
                    _currentInstance = null;
                }
            }
        }
    }

    private void CleanupCallback()
    {
        TdNativeLogging.SetLogMessageCallback(0, null);

        if (_callbackHandle.IsAllocated)
        {
            _callbackHandle.Free();
        }

        _nativeCallback = null;
        _fatalErrorCallback = null;
    }

    private void OnLogMessage(int verbosityLevel, IntPtr messagePtr)
    {
        ILogStream? currentLogStream;
        lock (_lock)
        {
            currentLogStream = _logStream;
        }

        if (currentLogStream == null || messagePtr == IntPtr.Zero)
        {
            return;
        }

        try
        {
            var message = Marshal.PtrToStringAnsi(messagePtr);
            if (!string.IsNullOrEmpty(message))
            {
                currentLogStream.OnLogMessage((TdLogLevel)verbosityLevel, message);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in LogStreamManager.OnLogMessage: {ex}");
        }
    }

    private void OnFatalError(IntPtr messagePtr)
    {
        ILogStream? currentLogStream;
        lock (_lock)
        {
            currentLogStream = _logStream;
        }

        if (currentLogStream == null || messagePtr == IntPtr.Zero)
        {
            return;
        }

        try
        {
            var message = Marshal.PtrToStringAnsi(messagePtr);
            if (!string.IsNullOrEmpty(message))
            {
                currentLogStream.OnFatalError(message);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in LogStreamManager.OnFatalError: {ex}");
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        ClearLogStream();
    }
}
