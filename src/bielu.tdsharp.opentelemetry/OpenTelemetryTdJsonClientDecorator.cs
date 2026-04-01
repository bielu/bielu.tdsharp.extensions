// SPDX-FileCopyrightText: 2024 tdsharp contributors <https://github.com/egramtel/tdsharp>
//
// SPDX-License-Identifier: MIT

using System.Diagnostics;
using TdLib;
using TdLib.Bindings;

namespace bielu.tdsharp.opentelemetry;

/// <summary>
/// An OpenTelemetry-instrumented decorator for <see cref="ITdJsonClient"/> that adds
/// tracing and metrics to JSON-level TDLib operations (Send, Execute, Receive).
/// </summary>
public sealed class OpenTelemetryTdJsonClientDecorator(ITdJsonClient inner) : ITdJsonClient, IDisposable
{
    private readonly ITdJsonClient _inner = inner ?? throw new ArgumentNullException(nameof(inner));

    /// <inheritdoc />
    public ITdLibBindings Bindings => _inner.Bindings;

    /// <inheritdoc />
    public void Send(string data)
    {
        using var activity = TdSharpInstrumentation.ActivitySource.StartActivity(
            "TdLib.JsonClient.Send",
            ActivityKind.Client);

        activity?.SetTag("tdsharp.json_client.method", "Send");

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _inner.Send(data);
            stopwatch.Stop();
            RecordSuccess("Send", stopwatch.Elapsed.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            RecordError(activity, "Send", stopwatch.Elapsed.TotalMilliseconds, ex);
            throw;
        }
    }

    /// <inheritdoc />
    public string Execute(string data)
    {
        using var activity = TdSharpInstrumentation.ActivitySource.StartActivity(
            "TdLib.JsonClient.Execute",
            ActivityKind.Client);

        activity?.SetTag("tdsharp.json_client.method", "Execute");

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = _inner.Execute(data);
            stopwatch.Stop();
            RecordSuccess("Execute", stopwatch.Elapsed.TotalMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            RecordError(activity, "Execute", stopwatch.Elapsed.TotalMilliseconds, ex);
            throw;
        }
    }

    /// <inheritdoc />
    public string Receive(double timeout)
    {
        using var activity = TdSharpInstrumentation.ActivitySource.StartActivity(
            "TdLib.JsonClient.Receive",
            ActivityKind.Client);

        activity?.SetTag("tdsharp.json_client.method", "Receive");

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = _inner.Receive(timeout);
            stopwatch.Stop();
            RecordSuccess("Receive", stopwatch.Elapsed.TotalMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            RecordError(activity, "Receive", stopwatch.Elapsed.TotalMilliseconds, ex);
            throw;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_inner is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    private static void RecordSuccess(string method, double durationMs)
    {
        var tags = new TagList
        {
            { "tdsharp.json_client.method", method },
            { "tdsharp.status", "ok" }
        };

        TdSharpMetrics.JsonClientOperationCount.Add(1, tags);
        TdSharpMetrics.JsonClientOperationDuration.Record(durationMs, tags);
    }

    private static void RecordError(Activity? activity, string method, double durationMs, Exception ex)
    {
        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        activity?.SetTag("error.type", ex.GetType().FullName);
        activity?.SetTag("error.message", ex.Message);

        var tags = new TagList
        {
            { "tdsharp.json_client.method", method },
            { "tdsharp.status", "error" },
            { "error.type", ex.GetType().FullName }
        };

        TdSharpMetrics.JsonClientOperationCount.Add(1, tags);
        TdSharpMetrics.JsonClientOperationDuration.Record(durationMs, tags);
        TdSharpMetrics.JsonClientOperationErrors.Add(1, tags);
    }
}
