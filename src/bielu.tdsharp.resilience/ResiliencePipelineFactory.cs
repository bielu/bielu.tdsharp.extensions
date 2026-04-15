// SPDX-FileCopyrightText: 2024 tdsharp contributors <https://github.com/egramtel/tdsharp>
//
// SPDX-License-Identifier: MIT

using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace bielu.tdsharp.resilience;

/// <summary>
/// Creates the Polly resilience pipeline (retry + circuit breaker) from <see cref="TdSharpResilienceOptions"/>.
/// </summary>
internal static class ResiliencePipelineFactory
{
    /// <summary>
    /// Builds an <see cref="ResiliencePipeline"/> configured with retry and circuit breaker strategies.
    /// </summary>
    internal static ResiliencePipeline Create(TdSharpResilienceOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var builder = new ResiliencePipelineBuilder();

        builder.AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = options.MaxRetryAttempts,
            BackoffType = DelayBackoffType.Exponential,
            Delay = options.RetryBaseDelay,
            MaxDelay = options.RetryMaxDelay,
            ShouldHandle = options.ShouldHandle is not null
                ? new PredicateBuilder().Handle<Exception>(options.ShouldHandle)
                : new PredicateBuilder().Handle<Exception>(),
        });

        builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions
        {
            FailureRatio = options.CircuitBreakerFailureRatio,
            MinimumThroughput = options.CircuitBreakerMinimumThroughput,
            BreakDuration = options.CircuitBreakerBreakDuration,
            SamplingDuration = options.CircuitBreakerSamplingDuration,
            ShouldHandle = options.ShouldHandle is not null
                ? new PredicateBuilder().Handle<Exception>(options.ShouldHandle)
                : new PredicateBuilder().Handle<Exception>(),
        });

        return builder.Build();
    }
}
