// SPDX-FileCopyrightText: 2024 tdsharp contributors <https://github.com/egramtel/tdsharp>
//
// SPDX-License-Identifier: MIT

using bielu.tdsharp.abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace bielu.tdsharp.resilience;

/// <summary>
/// Extension methods for registering TDLib resilience (retry + circuit breaker) in the DI container.
/// </summary>
public static class ResilienceTdSharpExtensions
{
    /// <summary>
    /// Decorates the registered <see cref="IClientProvider"/> with a resilience pipeline
    /// using the default <see cref="TdSharpResilienceOptions"/>.
    /// </summary>
    /// <remarks>
    /// This must be called <strong>after</strong> the base provider has been registered
    /// (e.g. after <c>AddTdSharpOpenTelemetry</c>). The resilience decorator wraps the
    /// existing provider so that every client it creates is protected by retry + circuit breaker.
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddTdSharpResilience(this IServiceCollection services)
    {
        return services.AddTdSharpResilience(_ => { });
    }

    /// <summary>
    /// Decorates the registered <see cref="IClientProvider"/> with a resilience pipeline
    /// using the specified <see cref="TdSharpResilienceOptions"/> configuration.
    /// </summary>
    /// <remarks>
    /// This must be called <strong>after</strong> the base provider has been registered
    /// (e.g. after <c>AddTdSharpOpenTelemetry</c>). The resilience decorator wraps the
    /// existing provider so that every client it creates is protected by retry + circuit breaker.
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">An action to configure the resilience options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddTdSharpResilience(
        this IServiceCollection services,
        Action<TdSharpResilienceOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        services.Decorate<IClientProvider>((inner, _) =>
        {
            var options = new TdSharpResilienceOptions();
            configure(options);
            return new ResilienceClientProvider(inner, options);
        });

        return services;
    }
}

/// <summary>
/// Internal helper to support the Decorate pattern for service collections.
/// </summary>
internal static class ServiceCollectionDecoratorExtensions
{
    /// <summary>
    /// Decorates an already-registered service with a new implementation that wraps the original.
    /// </summary>
    internal static IServiceCollection Decorate<TService>(
        this IServiceCollection services,
        Func<TService, IServiceProvider, TService> decorator)
        where TService : class
    {
        var wrappedDescriptor = services.LastOrDefault(s => s.ServiceType == typeof(TService));
        if (wrappedDescriptor is null)
        {
            throw new InvalidOperationException(
                $"No service of type {typeof(TService).FullName} has been registered. " +
                $"Register an IClientProvider (e.g. via AddTdSharpOpenTelemetry or DefaultClientProvider) before calling AddTdSharpResilience.");
        }

        services.Replace(ServiceDescriptor.Describe(
            typeof(TService),
            sp =>
            {
                var inner = wrappedDescriptor.ImplementationInstance as TService
                    ?? (wrappedDescriptor.ImplementationFactory?.Invoke(sp) as TService)
                    ?? (TService)ActivatorUtilities.GetServiceOrCreateInstance(sp, wrappedDescriptor.ImplementationType!);

                return decorator(inner, sp);
            },
            wrappedDescriptor.Lifetime));

        return services;
    }
}
