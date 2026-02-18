// SPDX-FileCopyrightText: 2024 tdsharp contributors <https://github.com/egramtel/tdsharp>
//
// SPDX-License-Identifier: MIT

using System.Runtime.InteropServices;

namespace bielu.tdsharp.aspnetcore.logger;

/// <summary>
/// Delegate for TDLib log message callback that receives all log messages.
/// </summary>
/// <param name="verbosityLevel">The verbosity level of the message (0=Fatal, 1=Error, 2=Warning, 3=Info, 4=Debug, 5+=Verbose)</param>
/// <param name="message">The log message (null-terminated string)</param>
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void TdLogMessageCallback(int verbosityLevel, IntPtr message);

/// <summary>
/// Provides direct access to TDLib's native logging functions that are not exposed through the standard TDLib bindings.
/// </summary>
/// <remarks>
/// <para>
/// This class provides P/Invoke access to <c>td_set_log_message_callback</c>, which allows capturing
/// ALL TDLib log messages (not just fatal errors) and routing them to custom handlers.
/// </para>
/// <para>
/// <b>Important:</b> The callback is called from TDLib's internal threads, so implementations must be thread-safe.
/// The callback should return as quickly as possible to avoid blocking TDLib's operation.
/// </para>
/// </remarks>
internal static class TdNativeLogging
{
    private const string TdJsonLib = "tdjson";

    /// <summary>
    /// Sets the callback that will be called when a TDLib log message is generated.
    /// </summary>
    /// <param name="maxVerbosityLevel">The maximum verbosity level for which the callback will be invoked.
    /// Use 0 for fatal errors only, up to 5+ for all messages.</param>
    /// <param name="callback">The callback to invoke, or null to disable the callback.</param>
    [DllImport(TdJsonLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "td_set_log_message_callback")]
    public static extern void SetLogMessageCallback(int maxVerbosityLevel, TdLogMessageCallback? callback);
}
