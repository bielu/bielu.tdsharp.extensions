// SPDX-FileCopyrightText: 2024 tdsharp contributors <https://github.com/egramtel/tdsharp>
//
// SPDX-License-Identifier: MIT

using TdLib.Bindings;

namespace bielu.tdsharp.aspnetcore.logger;

/// <summary>
/// Represents a custom log stream that receives TDLib log messages.
/// </summary>
/// <remarks>
/// Implement this interface to create custom log handlers that receive
/// TDLib log messages directly without intermediate files.
/// </remarks>
public interface ILogStream : IDisposable
{
    /// <summary>
    /// Called when a log message is received from TDLib.
    /// </summary>
    /// <param name="verbosityLevel">The verbosity level of the message (0=Fatal, 1=Error, 2=Warning, 3=Info, 4=Debug, 5+=Verbose)</param>
    /// <param name="message">The log message content</param>
    void OnLogMessage(TdLogLevel verbosityLevel, string message);

    /// <summary>
    /// Called when a fatal error occurs in TDLib.
    /// </summary>
    /// <param name="message">The fatal error message</param>
    void OnFatalError(string message);
}
