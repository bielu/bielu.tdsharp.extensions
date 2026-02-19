// SPDX-FileCopyrightText: 2024 tdsharp contributors <https://github.com/egramtel/tdsharp>
//
// SPDX-License-Identifier: MIT

using System.Runtime.InteropServices;

namespace bielu.tdsharp.aspnetcore.logger;

/// <summary>
/// Represents an active TDLib logging configuration that cleans up when disposed.
/// </summary>
/// <remarks>
/// This class is returned by <see cref="TdLoggerExtensions.UseTdLibLogging"/> and handles
/// proper cleanup of the native callback registration and associated resources.
/// </remarks>
internal sealed class TdLibLoggingScope : IDisposable
{
    private readonly LogStreamCallback _logHandler;
    private readonly GCHandle _callbackHandle;
    private readonly SetLogMessageCallbackDelegate _setCallback;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="TdLibLoggingScope"/> class.
    /// </summary>
    /// <param name="logHandler">The log handler instance</param>
    /// <param name="callbackHandle">The GC handle pinning the callback delegate</param>
    /// <param name="setCallback">The P/Invoke method for setting/clearing the callback</param>
    internal TdLibLoggingScope(
        LogStreamCallback logHandler,
        GCHandle callbackHandle,
        SetLogMessageCallbackDelegate setCallback)
    {
        _logHandler = logHandler;
        _callbackHandle = callbackHandle;
        _setCallback = setCallback;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // Use try-finally to ensure all resources are cleaned up even if one step fails
        try
        {
            // Disable the native callback
            _setCallback(0, null);
        }
        finally
        {
            try
            {
                // Free the GC handle
                if (_callbackHandle.IsAllocated)
                {
                    _callbackHandle.Free();
                }
            }
            finally
            {
                // Dispose the log handler
                _logHandler.Dispose();
            }
        }
    }
}
