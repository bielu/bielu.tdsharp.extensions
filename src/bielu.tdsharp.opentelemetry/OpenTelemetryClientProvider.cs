// SPDX-FileCopyrightText: 2024 tdsharp contributors <https://github.com/egramtel/tdsharp>
//
// SPDX-License-Identifier: MIT

using bielu.tdsharp.abstractions;
using TdLib;
using TdLib.Bindings;

namespace bielu.tdsharp.opentelemetry;

/// <summary>
/// An <see cref="IClientProvider"/> that creates a fully instrumented TDLib client stack
/// with OpenTelemetry tracing and metrics at every layer (JSON client, receiver, and client).
/// </summary>
/// <remarks>
/// Any registered <see cref="ITdClientMiddleware"/> instances (e.g. resilience) are applied
/// after the native <see cref="TdClient"/> is created but before the outermost
/// <see cref="OpenTelemetryTdClientDecorator"/>, ensuring that OTel observes the full
/// operation including retries.
/// </remarks>
public class OpenTelemetryClientProvider : IClientProvider
{
    private readonly ITdLibBindings _bindings;
    private readonly TimeSpan _receiverTimeout;
    private readonly IEnumerable<ITdClientMiddleware> _middleware;
    private static int _clientCounter;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenTelemetryClientProvider"/> class
    /// with auto-detected bindings and default receiver timeout.
    /// </summary>
    public OpenTelemetryClientProvider()
        : this(Interop.AutoDetectBindings(), TimeSpan.FromSeconds(0.1), [])
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenTelemetryClientProvider"/> class
    /// with auto-detected bindings, default receiver timeout, and DI-resolved middleware.
    /// </summary>
    /// <param name="middleware">Client middleware to apply before the OTel client decorator.</param>
    public OpenTelemetryClientProvider(IEnumerable<ITdClientMiddleware> middleware)
        : this(Interop.AutoDetectBindings(), TimeSpan.FromSeconds(0.1), middleware)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenTelemetryClientProvider"/> class.
    /// </summary>
    /// <param name="bindings">The TDLib native bindings to use.</param>
    /// <param name="receiverTimeout">The timeout for the receiver's polling loop.</param>
    public OpenTelemetryClientProvider(ITdLibBindings bindings, TimeSpan receiverTimeout)
        : this(bindings, receiverTimeout, [])
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenTelemetryClientProvider"/> class.
    /// </summary>
    /// <param name="bindings">The TDLib native bindings to use.</param>
    /// <param name="receiverTimeout">The timeout for the receiver's polling loop.</param>
    /// <param name="middleware">Client middleware to apply before the OTel client decorator.</param>
    public OpenTelemetryClientProvider(ITdLibBindings bindings, TimeSpan receiverTimeout, IEnumerable<ITdClientMiddleware> middleware)
    {
        ArgumentNullException.ThrowIfNull(bindings);
        _bindings = bindings;
        _receiverTimeout = receiverTimeout;
        _middleware = middleware ?? [];
    }

    /// <inheritdoc />
    public TdApi.IClient Create()
    {
        return CreateInstrumented(_bindings, _receiverTimeout, configure: null, _middleware);
    }

    /// <inheritdoc />
    public TdApi.IClient Create(ITdLibBindings bindings)
    {
        ArgumentNullException.ThrowIfNull(bindings);
        return CreateInstrumented(bindings, _receiverTimeout, configure: null, _middleware);
    }

    /// <inheritdoc />
    public TdApi.IClient Create(TimeSpan receiverTimeout)
    {
        return CreateInstrumented(_bindings, receiverTimeout, configure: null, _middleware);
    }

    /// <inheritdoc />
    public TdApi.IClient Create(ITdLibBindings bindings, TimeSpan receiverTimeout)
    {
        ArgumentNullException.ThrowIfNull(bindings);
        return CreateInstrumented(bindings, receiverTimeout, configure: null, _middleware);
    }

    /// <inheritdoc />
    public TdApi.IClient Create(Action<TdClient> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        return CreateInstrumented(_bindings, _receiverTimeout, configure, _middleware);
    }

    /// <inheritdoc />
    public TdApi.IClient Create(ITdLibBindings bindings, Action<TdClient> configure)
    {
        ArgumentNullException.ThrowIfNull(bindings);
        ArgumentNullException.ThrowIfNull(configure);
        return CreateInstrumented(bindings, _receiverTimeout, configure, _middleware);
    }

    /// <inheritdoc />
    public TdApi.IClient Create(ITdLibBindings bindings, TimeSpan receiverTimeout, Action<TdClient> configure)
    {
        ArgumentNullException.ThrowIfNull(bindings);
        ArgumentNullException.ThrowIfNull(configure);
        return CreateInstrumented(bindings, receiverTimeout, configure, _middleware);
    }

    private static TdApi.IClient CreateInstrumented(
        ITdLibBindings bindings,
        TimeSpan receiverTimeout,
        Action<TdClient>? configure,
        IEnumerable<ITdClientMiddleware> middleware)
    {
        var clientId = $"client-{Interlocked.Increment(ref _clientCounter)}";

        // 1. Create the raw JSON client with specified bindings
        var jsonClient = new TdJsonClient(bindings);

        // 2. Wrap with OTel instrumentation
        var instrumentedJsonClient = new OpenTelemetryTdJsonClientDecorator(jsonClient);

        // 3. Create receiver with the instrumented JSON client and specified timeout
        var receiver = new Receiver(instrumentedJsonClient, receiverTimeout);

        // 4. Wrap receiver with OTel instrumentation, passing client ID for auth-state tracking
        var instrumentedReceiver = new OpenTelemetryReceiverDecorator(receiver, clientId);

        // 5. Create TdClient with instrumented JSON client + receiver
        var client = new TdClient(instrumentedJsonClient, instrumentedReceiver);

        // 6. Invoke the configure callback on the native TdClient before decoration
        configure?.Invoke(client);

        // 7. Apply registered middleware (e.g. resilience) between the raw client and the OTel decorator
        TdApi.IClient current = client;
        foreach (var mw in middleware)
        {
            current = mw.Decorate(current);
        }

        // 8. Wrap with OTel instrumentation as the outermost decorator
        return new OpenTelemetryTdClientDecorator(current);
    }
}
