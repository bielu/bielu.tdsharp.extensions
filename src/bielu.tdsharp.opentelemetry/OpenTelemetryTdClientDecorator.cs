// SPDX-FileCopyrightText: 2024 tdsharp contributors <https://github.com/egramtel/tdsharp>
//
// SPDX-License-Identifier: MIT

using System.Diagnostics;
using TdLib;

namespace bielu.tdsharp.opentelemetry;

/// <summary>
/// An OpenTelemetry-instrumented decorator for <see cref="TdApi.IClient"/> that adds
/// distributed tracing and metrics to all TDLib operations.
/// </summary>
public sealed class OpenTelemetryTdClientDecorator : TdApi.IClient, IDisposable
{
    private readonly TdApi.IClient _inner;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenTelemetryTdClientDecorator"/> class.
    /// </summary>
    /// <param name="inner">The inner client to decorate.</param>
    public OpenTelemetryTdClientDecorator(TdApi.IClient inner)
    {
        ArgumentNullException.ThrowIfNull(inner);
        _inner = inner;
    }

    /// <inheritdoc />
    public event EventHandler<TdApi.Update> UpdateReceived
    {
        add => _inner.UpdateReceived += value;
        remove => _inner.UpdateReceived -= value;
    }

    /// <inheritdoc />
    public void Send<TResult>(TdApi.Function<TResult> function)
    {
        var functionName = GetFunctionName(function);

        using var activity = TdSharpInstrumentation.ActivitySource.StartActivity(
            $"TdLib.Send {functionName}",
            ActivityKind.Client);

        SetActivityTags(activity, functionName, "Send");

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _inner.Send(function);
            stopwatch.Stop();
            RecordSuccess(functionName, "Send", stopwatch.Elapsed.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            RecordError(activity, functionName, "Send", stopwatch.Elapsed.TotalMilliseconds, ex);
            throw;
        }
    }

    /// <inheritdoc />
    public TResult Execute<TResult>(TdApi.Function<TResult> function)
        where TResult : TdApi.Object
    {
        var functionName = GetFunctionName(function);

        using var activity = TdSharpInstrumentation.ActivitySource.StartActivity(
            $"TdLib.Execute {functionName}",
            ActivityKind.Client);

        SetActivityTags(activity, functionName, "Execute");

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = _inner.Execute(function);
            stopwatch.Stop();
            RecordSuccess(functionName, "Execute", stopwatch.Elapsed.TotalMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            RecordError(activity, functionName, "Execute", stopwatch.Elapsed.TotalMilliseconds, ex);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<TResult> ExecuteAsync<TResult>(TdApi.Function<TResult> function)
        where TResult : TdApi.Object
    {
        var functionName = GetFunctionName(function);

        using var activity = TdSharpInstrumentation.ActivitySource.StartActivity(
            $"TdLib.ExecuteAsync {functionName}",
            ActivityKind.Client);

        SetActivityTags(activity, functionName, "ExecuteAsync");

        var inflightTags = new TagList
        {
            { "tdsharp.function", functionName }
        };

        TdSharpMetrics.OperationsInflight.Add(1, inflightTags);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await _inner.ExecuteAsync(function).ConfigureAwait(false);
            stopwatch.Stop();
            RecordSuccess(functionName, "ExecuteAsync", stopwatch.Elapsed.TotalMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            RecordError(activity, functionName, "ExecuteAsync", stopwatch.Elapsed.TotalMilliseconds, ex);
            throw;
        }
        finally
        {
            TdSharpMetrics.OperationsInflight.Add(-1, inflightTags);
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

    private static string GetFunctionName<TResult>(TdApi.Function<TResult> function)
    {
        return function.GetType().Name;
    }

    private static void SetActivityTags(Activity? activity, string functionName, string method)
    {
        activity?.SetTag("tdsharp.function", functionName);
        activity?.SetTag("tdsharp.method", method);
    }

    private static void RecordSuccess(string functionName, string method, double durationMs)
    {
        var tags = new TagList
        {
            { "tdsharp.function", functionName },
            { "tdsharp.method", method },
            { "tdsharp.status", "ok" }
        };

        TdSharpMetrics.OperationCount.Add(1, tags);
        TdSharpMetrics.OperationDuration.Record(durationMs, tags);
    }

    private static void RecordError(Activity? activity, string functionName, string method, double durationMs, Exception ex)
    {
        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        activity?.SetTag("error.type", ex.GetType().FullName);
        activity?.SetTag("error.message", ex.Message);

        var tags = new TagList
        {
            { "tdsharp.function", functionName },
            { "tdsharp.method", method },
            { "tdsharp.status", "error" },
            { "error.type", ex.GetType().FullName }
        };

        TdSharpMetrics.OperationCount.Add(1, tags);
        TdSharpMetrics.OperationDuration.Record(durationMs, tags);
        TdSharpMetrics.OperationErrors.Add(1, tags);
    }
}
