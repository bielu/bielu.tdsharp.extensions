// SPDX-FileCopyrightText: 2024 tdsharp contributors <https://github.com/egramtel/tdsharp>
//
// SPDX-License-Identifier: MIT

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace bielu.tdsharp.opentelemetry;

/// <summary>
/// Provides OpenTelemetry metrics for TDLib operations.
/// </summary>
internal static class TdSharpMetrics
{
    internal static readonly Meter Meter = new(TdSharpInstrumentation.Name, TdSharpInstrumentation.Version);

    // --- Client-level metrics ---

    /// <summary>
    /// Counts the total number of TDLib client operations executed.
    /// </summary>
    internal static readonly Counter<long> OperationCount =
        Meter.CreateCounter<long>(
            "tdsharp.client.operations.count",
            description: "Total number of TDLib client operations executed");

    /// <summary>
    /// Records the duration of TDLib client operations in milliseconds.
    /// </summary>
    internal static readonly Histogram<double> OperationDuration =
        Meter.CreateHistogram<double>(
            "tdsharp.client.operations.duration",
            unit: "ms",
            description: "Duration of TDLib client operations in milliseconds");

    /// <summary>
    /// Counts the number of TDLib client operation errors.
    /// </summary>
    internal static readonly Counter<long> OperationErrors =
        Meter.CreateCounter<long>(
            "tdsharp.client.operations.errors",
            description: "Total number of TDLib client operation errors");

    // --- Receiver-level metrics ---

    /// <summary>
    /// Counts the total number of receiver events (Received, AuthorizationStateChanged, ExceptionThrown).
    /// </summary>
    internal static readonly Counter<long> ReceiverEventsCount =
        Meter.CreateCounter<long>(
            "tdsharp.receiver.events.count",
            description: "Total number of TDLib receiver events");

    /// <summary>
    /// Counts the number of receiver errors (exceptions thrown by the receiver).
    /// </summary>
    internal static readonly Counter<long> ReceiverErrors =
        Meter.CreateCounter<long>(
            "tdsharp.receiver.errors",
            description: "Total number of TDLib receiver errors");

    // --- Authorization state metrics ---

    /// <summary>
    /// Thread-safe mapping of client ID → current authorization state type name.
    /// Updated by <see cref="OpenTelemetryReceiverDecorator"/> on auth state changes.
    /// </summary>
    internal static readonly ConcurrentDictionary<string, string> ClientAuthStates = new();

    /// <summary>
    /// Reports the current number of TDLib clients grouped by authorization state.
    /// Each measurement is tagged with <c>tdsharp.auth_state</c>.
    /// Observed on each metrics collection cycle.
    /// </summary>
    internal static readonly ObservableGauge<int> AuthorizedClientsGauge =
        Meter.CreateObservableGauge(
            "tdsharp.client.auth_state.count",
            observeValues: ObserveAuthorizedClients,
            description: "Current number of TDLib clients by authorization state");

    private static IEnumerable<Measurement<int>> ObserveAuthorizedClients()
    {
        // Snapshot the dictionary and group by auth state.
        var snapshot = ClientAuthStates.ToArray();

        foreach (var group in snapshot.GroupBy(kvp => kvp.Value))
        {
            yield return new Measurement<int>(
                group.Count(),
                new TagList { { "tdsharp.auth_state", group.Key } });
        }
    }

    // --- JSON client-level metrics ---

    /// <summary>
    /// Counts the total number of JSON client operations.
    /// </summary>
    internal static readonly Counter<long> JsonClientOperationCount =
        Meter.CreateCounter<long>(
            "tdsharp.json_client.operations.count",
            description: "Total number of TDLib JSON client operations");

    /// <summary>
    /// Records the duration of JSON client operations in milliseconds.
    /// </summary>
    internal static readonly Histogram<double> JsonClientOperationDuration =
        Meter.CreateHistogram<double>(
            "tdsharp.json_client.operations.duration",
            unit: "ms",
            description: "Duration of TDLib JSON client operations in milliseconds");

    /// <summary>
    /// Counts the number of JSON client operation errors.
    /// </summary>
    internal static readonly Counter<long> JsonClientOperationErrors =
        Meter.CreateCounter<long>(
            "tdsharp.json_client.operations.errors",
            description: "Total number of TDLib JSON client operation errors");
}
