// SPDX-FileCopyrightText: 2024 tdsharp contributors <https://github.com/egramtel/tdsharp>
//
// SPDX-License-Identifier: MIT

using bielu.tdsharp.abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace bielu.tdsharp.resilience;

/// <summary>
/// Extension methods for registering TDLib resilience (retry + circuit breaker) in the DI container.
/// </summary>
public static class ResilienceTdSharpExtensions
{
    /// <summary>
    /// Registers a resilience middleware (retry + circuit breaker) that is applied by
    /// <see cref="IClientProvider"/> implementations before their own outermost decorator.
    /// Uses the default <see cref="TdSharpResilienceOptions"/>.
    /// </summary>
    /// <remarks>
    /// Registration order does not matter. The middleware is resolved at runtime by providers
    /// such as <c>OpenTelemetryClientProvider</c> and <c>DefaultClientProvider</c>, which
    /// apply it before their own decorator so that OTel always observes the full operation
    /// including retries.
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddTdSharpResilience(this IServiceCollection services)
    {
        return services.AddTdSharpResilience(_ => { });
    }

    /// <summary>
    /// Registers a resilience middleware (retry + circuit breaker) that is applied by
    /// <see cref="IClientProvider"/> implementations before their own outermost decorator.
    /// Uses the specified <see cref="TdSharpResilienceOptions"/> configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">An action to configure the resilience options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddTdSharpResilience(
        this IServiceCollection services,
        Action<TdSharpResilienceOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var options = new TdSharpResilienceOptions();
        configure(options);

        services.AddSingleton<ITdClientMiddleware>(new ResilienceTdClientMiddleware(options));

        return services;
    }
}
