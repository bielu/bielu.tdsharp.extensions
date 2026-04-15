// SPDX-FileCopyrightText: 2024 tdsharp contributors <https://github.com/egramtel/tdsharp>
//
// SPDX-License-Identifier: MIT

using bielu.tdsharp.abstractions;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using TdLib.Bindings;

namespace bielu.tdsharp.opentelemetry;

/// <summary>
/// Extension methods for configuring OpenTelemetry instrumentation for TDLib.
/// </summary>
public static class OpenTelemetryTdSharpExtensions
{
    /// <summary>
    /// Registers an <see cref="IClientProvider"/> with OpenTelemetry instrumentation
    /// using auto-detected bindings and default receiver timeout.
    /// Any registered <see cref="ITdClientMiddleware"/> services (e.g. resilience) are applied
    /// before the OTel client decorator.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddTdSharpOpenTelemetry(this IServiceCollection services)
    {
        services.AddSingleton<IClientProvider, OpenTelemetryClientProvider>();
        return services;
    }

    /// <summary>
    /// Registers an <see cref="IClientProvider"/> with OpenTelemetry instrumentation
    /// using the specified bindings and receiver timeout.
    /// Any registered <see cref="ITdClientMiddleware"/> services (e.g. resilience) are applied
    /// before the OTel client decorator.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="bindings">The TDLib native bindings to use.</param>
    /// <param name="receiverTimeout">The timeout for the receiver's polling loop.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddTdSharpOpenTelemetry(
        this IServiceCollection services,
        ITdLibBindings bindings,
        TimeSpan receiverTimeout)
    {
        services.AddSingleton<IClientProvider>(sp =>
            new OpenTelemetryClientProvider(bindings, receiverTimeout, sp.GetServices<ITdClientMiddleware>()));
        return services;
    }

    /// <summary>
    /// Adds TDLib tracing instrumentation to the OpenTelemetry <see cref="TracerProviderBuilder"/>.
    /// </summary>
    /// <param name="builder">The tracer provider builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static TracerProviderBuilder AddTdSharpInstrumentation(this TracerProviderBuilder builder)
    {
        return builder.AddSource(TdSharpInstrumentation.Name);
    }

    /// <summary>
    /// Adds TDLib metrics instrumentation to the OpenTelemetry <see cref="MeterProviderBuilder"/>.
    /// </summary>
    /// <param name="builder">The meter provider builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static MeterProviderBuilder AddTdSharpInstrumentation(this MeterProviderBuilder builder)
    {
        return builder.AddMeter(TdSharpInstrumentation.Name);
    }
}
