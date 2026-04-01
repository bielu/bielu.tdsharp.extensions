// SPDX-FileCopyrightText: 2024 tdsharp contributors <https://github.com/egramtel/tdsharp>
//
// SPDX-License-Identifier: MIT

using bielu.tdsharp.client.factory;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace bielu.tdsharp.opentelemetry;

/// <summary>
/// Extension methods for configuring OpenTelemetry instrumentation for TDLib.
/// </summary>
public static class OpenTelemetryTdSharpExtensions
{
    /// <summary>
    /// Registers the OpenTelemetry decorator for TDLib clients in the service collection.
    /// This adds tracing and metrics to all TDLib operations performed through the client factory.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddTdSharpOpenTelemetry(this IServiceCollection services)
    {
        services.AddSingleton<TdClientDecorator>(inner =>
            new OpenTelemetryTdClientDecorator(inner));

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
