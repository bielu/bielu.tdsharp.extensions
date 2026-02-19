// SPDX-FileCopyrightText: 2024 tdsharp contributors <https://github.com/egramtel/tdsharp>
//
// SPDX-License-Identifier: MIT

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace bielu.tdsharp.aspnetcore.logger;

/// <summary>
/// Delegate for TDLib log message callback that receives all log messages.
/// </summary>
/// <remarks>
/// <para>
/// This delegate is provided for compatibility and testing scenarios.
/// The actual callback implementation uses <see cref="UnmanagedCallersOnlyAttribute"/>
/// with function pointers for better reliability in modern .NET.
/// </para>
/// </remarks>
/// <param name="verbosityLevel">The verbosity level of the message (0=Fatal, 1=Error, 2=Warning, 3=Info, 4=Debug, 5+=Verbose)</param>
/// <param name="message">The log message as a pointer to a null-terminated ANSI string</param>
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void TdLogMessageCallback(int verbosityLevel, IntPtr message);

/// <summary>
/// Delegate for a function that sets the TDLib log message callback.
/// </summary>
/// <remarks>
/// <para>
/// Due to .NET native interop limitations, the P/Invoke for <c>td_set_log_message_callback</c>
/// must be defined in the consumer's assembly for callbacks to work correctly.
/// This delegate type allows passing that P/Invoke method to extension methods.
/// </para>
/// </remarks>
/// <param name="maxVerbosityLevel">The maximum verbosity level for which the callback will be invoked.</param>
/// <param name="callback">The callback delegate to invoke, or null to disable the callback.</param>
/// <example>
/// <code>
/// // Define P/Invoke in your application
/// [DllImport("tdjson", CallingConvention = CallingConvention.Cdecl)]
/// static extern void td_set_log_message_callback(int maxVerbosityLevel, TdLogMessageCallback? callback);
/// 
/// // Pass it to the extension method
/// client.UseTdLibLogging(loggerFactory, TdLogLevel.Info, td_set_log_message_callback);
/// </code>
/// </example>
public delegate void SetLogMessageCallbackDelegate(int maxVerbosityLevel, TdLogMessageCallback? callback);

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
/// <para>
/// <b>Implementation Note:</b> This class uses <see cref="UnmanagedCallersOnlyAttribute"/> with function pointers
/// for native callbacks. This is the recommended approach in .NET 5+ for native interop callbacks as it avoids
/// delegate marshaling issues and provides better performance and reliability.
/// </para>
/// </remarks>
public static unsafe class TdNativeLogging
{
    private const string TdJsonLib = "tdjson";

    /// <summary>
    /// Sets the callback that will be called when a TDLib log message is generated using a function pointer.
    /// This is the recommended approach for modern .NET (5+).
    /// </summary>
    /// <param name="maxVerbosityLevel">The maximum verbosity level for which the callback will be invoked.
    /// Use 0 for fatal errors only, up to 5+ for all messages.</param>
    /// <param name="callback">The function pointer to invoke, or null to disable the callback.</param>
    [DllImport(TdJsonLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "td_set_log_message_callback")]
    public static extern void SetLogMessageCallbackFunctionPointer(int maxVerbosityLevel, delegate* unmanaged[Cdecl]<int, IntPtr, void> callback);

    /// <summary>
    /// Sets the callback that will be called when a TDLib log message is generated using a delegate.
    /// </summary>
    /// <param name="maxVerbosityLevel">The maximum verbosity level for which the callback will be invoked.
    /// Use 0 for fatal errors only, up to 5+ for all messages.</param>
    /// <param name="callback">The callback delegate to invoke, or null to disable the callback.</param>
    /// <remarks>
    /// This method uses delegate marshaling. For better reliability in modern .NET,
    /// prefer using <see cref="SetLogMessageCallbackFunctionPointer"/> with <see cref="UnmanagedCallersOnlyAttribute"/> methods.
    /// </remarks>
    [DllImport(TdJsonLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "td_set_log_message_callback")]
    public static extern void SetLogMessageCallback(int maxVerbosityLevel, TdLogMessageCallback? callback);
}
