// SPDX-FileCopyrightText: 2024 tdsharp contributors <https://github.com/egramtel/tdsharp>
//
// SPDX-License-Identifier: MIT

namespace bielu.tdsharp.resilience;

/// <summary>
/// Configuration options for the TDLib resilience pipeline (retry + circuit breaker).
/// </summary>
public class TdSharpResilienceOptions
{
    /// <summary>
    /// Gets or sets the maximum number of retry attempts. Default is 3.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets the base delay between retry attempts. Default is 500 ms.
    /// Exponential backoff is applied: delay × 2^(attempt-1).
    /// </summary>
    public TimeSpan RetryBaseDelay { get; set; } = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// Gets or sets the maximum delay between retry attempts (delay cap). Default is 30 seconds.
    /// </summary>
    public TimeSpan RetryMaxDelay { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the failure ratio threshold at which the circuit breaker opens.
    /// Must be between 0.0 and 1.0. Default is 0.5 (50 %).
    /// </summary>
    public double CircuitBreakerFailureRatio { get; set; } = 0.5;

    /// <summary>
    /// Gets or sets the minimum number of calls that must be made within the sampling duration
    /// before the circuit breaker can trip. Default is 10.
    /// </summary>
    public int CircuitBreakerMinimumThroughput { get; set; } = 10;

    /// <summary>
    /// Gets or sets how long the circuit breaker stays open before transitioning to half-open.
    /// Default is 30 seconds.
    /// </summary>
    public TimeSpan CircuitBreakerBreakDuration { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the rolling window used to calculate the failure ratio.
    /// Default is 60 seconds.
    /// </summary>
    public TimeSpan CircuitBreakerSamplingDuration { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Gets or sets an optional predicate to decide which exceptions should be retried / counted
    /// by the circuit breaker. When <c>null</c> (the default) all exceptions are handled.
    /// </summary>
    public Func<Exception, bool>? ShouldHandle { get; set; }
}
