// SPDX-FileCopyrightText: 2024 tdsharp contributors <https://github.com/egramtel/tdsharp>
//
// SPDX-License-Identifier: MIT

using Polly;
using TdLib;

namespace bielu.tdsharp.resilience;

/// <summary>
/// A decorator for <see cref="TdApi.IClient"/> that applies a Polly resilience pipeline
/// (retry with exponential backoff + circuit breaker) to all TDLib operations.
/// </summary>
public sealed class ResilienceTdClientDecorator : TdApi.IClient, IDisposable
{
    private readonly TdApi.IClient _inner;
    private readonly ResiliencePipeline _pipeline;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResilienceTdClientDecorator"/> class.
    /// </summary>
    /// <param name="inner">The inner client to decorate.</param>
    /// <param name="options">The resilience options to configure retry and circuit breaker behaviour.</param>
    public ResilienceTdClientDecorator(TdApi.IClient inner, TdSharpResilienceOptions options)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(options);
        _inner = inner;
        _pipeline = ResiliencePipelineFactory.Create(options);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResilienceTdClientDecorator"/> class
    /// using a pre-built resilience pipeline.
    /// </summary>
    /// <param name="inner">The inner client to decorate.</param>
    /// <param name="pipeline">The resilience pipeline to apply.</param>
    public ResilienceTdClientDecorator(TdApi.IClient inner, ResiliencePipeline pipeline)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(pipeline);
        _inner = inner;
        _pipeline = pipeline;
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
        _pipeline.Execute(_ => _inner.Send(function), CancellationToken.None);
    }

    /// <inheritdoc />
    public TResult Execute<TResult>(TdApi.Function<TResult> function)
        where TResult : TdApi.Object
    {
        return _pipeline.Execute(_ => _inner.Execute(function), CancellationToken.None);
    }

    /// <inheritdoc />
    public async Task<TResult> ExecuteAsync<TResult>(TdApi.Function<TResult> function)
        where TResult : TdApi.Object
    {
        return await _pipeline.ExecuteAsync(
            async ct => await _inner.ExecuteAsync(function).ConfigureAwait(false),
            CancellationToken.None).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_inner is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
