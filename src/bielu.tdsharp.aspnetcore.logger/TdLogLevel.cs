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
    All = 1024
}
