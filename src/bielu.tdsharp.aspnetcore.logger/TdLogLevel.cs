// SPDX-FileCopyrightText: 2024 tdsharp contributors <https://github.com/egramtel/tdsharp>
//
// SPDX-License-Identifier: MIT

namespace bielu.tdsharp.aspnetcore.logger;

/// <summary>
/// TDLib log verbosity levels
/// </summary>
public enum TdLogLevel
{
    /// <summary>
    /// Fatal errors that require application termination
    /// </summary>
    Fatal = 0,

    /// <summary>
    /// Errors that need attention
    /// </summary>
    Error = 1,

    /// <summary>
    /// Warning messages
    /// </summary>
    Warning = 2,

    /// <summary>
    /// Informational messages
    /// </summary>
    Info = 3,

    /// <summary>
    /// Debug messages
    /// </summary>
    Debug = 4,

    /// <summary>
    /// Verbose debug messages
    /// </summary>
    Verbose = 5,

    /// <summary>
    /// Maximum verbosity level
    /// </summary>
    /// <remarks>
    /// This represents TDLib's maximum verbosity setting (value 1024).
    /// While TDLib's standard verbosity levels are 0-5, this special value
    /// enables all possible diagnostic output. In practice, it behaves similarly
    /// to Verbose (5) but may include additional internal diagnostics.
    /// Maps to LogLevel.Trace in Microsoft.Extensions.Logging.
    /// </remarks>
    All = 1024
}
