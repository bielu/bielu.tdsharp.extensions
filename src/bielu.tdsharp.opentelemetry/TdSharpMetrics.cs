// SPDX-FileCopyrightText: 2024 tdsharp contributors <https://github.com/egramtel/tdsharp>
//
// SPDX-License-Identifier: MIT

using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace bielu.tdsharp.opentelemetry;

/// <summary>
/// Provides OpenTelemetry metrics for TDLib operations.
/// </summary>
internal sealed class TdSharpMetrics
{
    internal static readonly Meter Meter = new(TdSharpInstrumentation.Name, TdSharpInstrumentation.Version);

    /// <summary>
    /// Counts the total number of TDLib operations executed.
    /// </summary>
    internal static readonly Counter<long> OperationCount =
        Meter.CreateCounter<long>(
            "tdsharp.operations.count",
            description: "Total number of TDLib operations executed");

    /// <summary>
    /// Records the duration of TDLib operations in milliseconds.
    /// </summary>
    internal static readonly Histogram<double> OperationDuration =
        Meter.CreateHistogram<double>(
            "tdsharp.operations.duration",
            unit: "ms",
            description: "Duration of TDLib operations in milliseconds");

    /// <summary>
    /// Counts the number of TDLib operation errors.
    /// </summary>
    internal static readonly Counter<long> OperationErrors =
        Meter.CreateCounter<long>(
            "tdsharp.operations.errors",
            description: "Total number of TDLib operation errors");
}
