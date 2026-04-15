// SPDX-FileCopyrightText: 2024 tdsharp contributors <https://github.com/egramtel/tdsharp>
//
// SPDX-License-Identifier: MIT

using TdLib;

namespace bielu.tdsharp.abstractions;

/// <summary>
/// Middleware that decorates a <see cref="TdApi.IClient"/> instance.
/// Implementations are applied by <see cref="IClientProvider"/> before the provider's own
/// outermost decorator (e.g. OpenTelemetry), ensuring the middleware sits closer to the
/// actual TDLib client.
/// </summary>
/// <remarks>
/// Register implementations in the DI container via
/// <c>services.AddSingleton&lt;ITdClientMiddleware&gt;(...)</c>.
/// Providers that support middleware (such as <c>OpenTelemetryClientProvider</c> and
/// <c>DefaultClientProvider</c>) will resolve all registered middleware and apply them
/// in registration order.
/// </remarks>
public interface ITdClientMiddleware
{
    /// <summary>
    /// Wraps the given <paramref name="client"/> with additional behaviour.
    /// </summary>
    /// <param name="client">The client to decorate.</param>
    /// <returns>The decorated client.</returns>
    TdApi.IClient Decorate(TdApi.IClient client);
}
