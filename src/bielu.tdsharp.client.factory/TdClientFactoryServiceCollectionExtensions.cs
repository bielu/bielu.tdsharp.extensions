// SPDX-FileCopyrightText: 2024 tdsharp contributors <https://github.com/egramtel/tdsharp>
//
// SPDX-License-Identifier: MIT

using bielu.tdsharp.abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace bielu.tdsharp.client.factory;

/// <summary>
/// Extension methods for registering <see cref="ITdClientFactory"/> in the DI container.
/// </summary>
public static class TdClientFactoryServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="ITdClientFactory"/> and <see cref="TdClientFactory"/> in the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddTdClientFactory(this IServiceCollection services)
    {
        services.AddSingleton<ITdClientFactory, TdClientFactory>();
        return services;
    }
}
