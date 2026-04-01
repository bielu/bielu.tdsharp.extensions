// SPDX-FileCopyrightText: 2024 tdsharp contributors <https://github.com/egramtel/tdsharp>
//
// SPDX-License-Identifier: MIT

using System.Diagnostics;

namespace bielu.tdsharp.opentelemetry;

/// <summary>
/// Shared OpenTelemetry instrumentation constants for TDLib.
/// </summary>
internal static class TdSharpInstrumentation
{
    /// <summary>
    /// The instrumentation name used for the ActivitySource and Meter.
    /// </summary>
    internal const string Name = "bielu.tdsharp";

    /// <summary>
    /// The instrumentation version.
    /// </summary>
    internal const string Version = "1.0.0";

    /// <summary>
    /// The ActivitySource used for creating traces.
    /// </summary>
    internal static readonly ActivitySource ActivitySource = new(Name, Version);
}
