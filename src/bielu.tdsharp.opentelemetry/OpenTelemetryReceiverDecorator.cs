// SPDX-FileCopyrightText: 2024 tdsharp contributors <https://github.com/egramtel/tdsharp>
//
// SPDX-License-Identifier: MIT

using System.Diagnostics;
using TdLib;
using TdLib.Bindings;

namespace bielu.tdsharp.opentelemetry;

/// <summary>
/// An OpenTelemetry-instrumented decorator for <see cref="IReceiver"/> that adds
/// tracing and metrics to receiver events (updates, auth state changes, exceptions).
/// </summary>
public sealed class OpenTelemetryReceiverDecorator : IReceiver, IDisposable
{
    private readonly IReceiver _inner;
    private readonly string _clientId;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenTelemetryReceiverDecorator"/> class.
    /// </summary>
    /// <param name="inner">The inner receiver to decorate.</param>
    /// <param name="clientId">An identifier for the client, used as a tag on auth-state metrics.</param>
    public OpenTelemetryReceiverDecorator(IReceiver inner, string clientId)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        _inner = inner;
        _clientId = clientId;

        _inner.Received += OnReceived;
        _inner.AuthorizationStateChanged += OnAuthorizationStateChanged;
        _inner.ExceptionThrown += OnExceptionThrown;
    }

    /// <inheritdoc />
    public event EventHandler<TdApi.Object>? Received;

    /// <inheritdoc />
    public event EventHandler<TdApi.AuthorizationState>? AuthorizationStateChanged;

    /// <inheritdoc />
    public event EventHandler<Exception>? ExceptionThrown;

    /// <inheritdoc />
    public void Start()
    {
        using var activity = TdSharpInstrumentation.ActivitySource.StartActivity(
            "TdLib.Receiver.Start",
            ActivityKind.Client);

        _inner.Start();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _inner.Received -= OnReceived;
        _inner.AuthorizationStateChanged -= OnAuthorizationStateChanged;
        _inner.ExceptionThrown -= OnExceptionThrown;

        // Remove this client from the auth-state tracking so the gauge no longer reports it.
        TdSharpMetrics.ClientAuthStates.TryRemove(_clientId, out _);

        if (_inner is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    private void OnReceived(object? sender, TdApi.Object obj)
    {
        using var activity = TdSharpInstrumentation.ActivitySource.StartActivity(
            "TdLib.Receiver.Received",
            ActivityKind.Client);

        var objectType = obj.GetType().Name;
        activity?.SetTag("tdsharp.receiver.event", "Received");
        activity?.SetTag("tdsharp.receiver.object_type", objectType);

        var tags = new TagList
        {
            { "tdsharp.receiver.event", "Received" },
            { "tdsharp.receiver.object_type", objectType }
        };

        TdSharpMetrics.ReceiverEventsCount.Add(1, tags);

        Received?.Invoke(this, obj);
    }

    private void OnAuthorizationStateChanged(object? sender, TdApi.AuthorizationState state)
    {
        using var activity = TdSharpInstrumentation.ActivitySource.StartActivity(
            "TdLib.Receiver.AuthorizationStateChanged",
            ActivityKind.Client);

        var stateType = state.GetType().Name;
        activity?.SetTag("tdsharp.receiver.event", "AuthorizationStateChanged");
        activity?.SetTag("tdsharp.receiver.auth_state", stateType);
        activity?.SetTag("tdsharp.client_id", _clientId);

        var tags = new TagList
        {
            { "tdsharp.receiver.event", "AuthorizationStateChanged" },
            { "tdsharp.receiver.auth_state", stateType }
        };

        TdSharpMetrics.ReceiverEventsCount.Add(1, tags);

        // Record the current auth state for this client.
        // The ObservableGauge callback in TdSharpMetrics groups by state on each collection.
        TdSharpMetrics.ClientAuthStates[_clientId] = stateType;

        AuthorizationStateChanged?.Invoke(this, state);
    }

    private void OnExceptionThrown(object? sender, Exception ex)
    {
        using var activity = TdSharpInstrumentation.ActivitySource.StartActivity(
            "TdLib.Receiver.ExceptionThrown",
            ActivityKind.Client);

        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        activity?.SetTag("tdsharp.receiver.event", "ExceptionThrown");
        activity?.SetTag("error.type", ex.GetType().FullName);
        activity?.SetTag("error.message", ex.Message);

        var tags = new TagList
        {
            { "tdsharp.receiver.event", "ExceptionThrown" },
            { "error.type", ex.GetType().FullName }
        };

        TdSharpMetrics.ReceiverEventsCount.Add(1, tags);
        TdSharpMetrics.ReceiverErrors.Add(1, tags);

        ExceptionThrown?.Invoke(this, ex);
    }
}
