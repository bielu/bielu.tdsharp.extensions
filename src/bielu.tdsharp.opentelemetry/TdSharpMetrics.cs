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

    /// <summary>
    /// Tracks the number of currently in-flight (pending) async operations.
    /// Incremented before an async call starts and decremented when it completes.
    /// </summary>
    internal static readonly UpDownCounter<long> OperationsInflight =
        Meter.CreateUpDownCounter<long>(
            "tdsharp.client.operations.inflight",
            description: "Number of currently in-flight TDLib client async operations");

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

    /// <summary>
    /// Counts authorization state transitions, tagged by <c>tdsharp.auth_state.from</c> and <c>tdsharp.auth_state.to</c>.
    /// </summary>
    internal static readonly Counter<long> AuthStateTransitions =
        Meter.CreateCounter<long>(
            "tdsharp.client.auth_state.transitions",
            description: "Total number of TDLib client authorization state transitions");

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

    // --- Factory-level metrics ---

    /// <summary>
    /// Tracks the number of currently active (non-disposed) TDLib clients.
    /// </summary>
    internal static readonly UpDownCounter<long> ActiveClients =
        Meter.CreateUpDownCounter<long>(
            "tdsharp.factory.clients.active",
            description: "Number of currently active TDLib clients managed by the factory");

    /// <summary>
    /// Counts the total number of TDLib clients created by the factory.
    /// </summary>
    internal static readonly Counter<long> ClientsCreated =
        Meter.CreateCounter<long>(
            "tdsharp.factory.clients.created",
            description: "Total number of TDLib clients created");

    /// <summary>
    /// Counts the total number of TDLib clients closed by the factory (both permanent and non-permanent).
    /// Tagged with <c>tdsharp.disposal_type</c> ("permanent" or "temporary").
    /// </summary>
    internal static readonly Counter<long> ClientsClosed =
        Meter.CreateCounter<long>(
            "tdsharp.factory.clients.closed",
            description: "Total number of TDLib clients closed");

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
