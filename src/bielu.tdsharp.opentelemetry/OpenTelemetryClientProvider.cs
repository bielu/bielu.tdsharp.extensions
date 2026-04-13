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
public class OpenTelemetryClientProvider : IClientProvider
{
    private readonly ITdLibBindings _bindings;
    private readonly TimeSpan _receiverTimeout;
    private static int _clientCounter;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenTelemetryClientProvider"/> class
    /// with auto-detected bindings and default receiver timeout.
    /// </summary>
    public OpenTelemetryClientProvider()
        : this(Interop.AutoDetectBindings(), TimeSpan.FromSeconds(0.1))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenTelemetryClientProvider"/> class.
    /// </summary>
    /// <param name="bindings">The TDLib native bindings to use.</param>
    /// <param name="receiverTimeout">The timeout for the receiver's polling loop.</param>
    public OpenTelemetryClientProvider(ITdLibBindings bindings, TimeSpan receiverTimeout)
    {
        ArgumentNullException.ThrowIfNull(bindings);
        _bindings = bindings;
        _receiverTimeout = receiverTimeout;
    }

    /// <inheritdoc />
    public TdApi.IClient Create()
    {
        return CreateInstrumented(_bindings, _receiverTimeout, configure: null);
    }

    /// <inheritdoc />
    public TdApi.IClient Create(ITdLibBindings bindings)
    {
        ArgumentNullException.ThrowIfNull(bindings);
        return CreateInstrumented(bindings, _receiverTimeout, configure: null);
    }

    /// <inheritdoc />
    public TdApi.IClient Create(TimeSpan receiverTimeout)
    {
        return CreateInstrumented(_bindings, receiverTimeout, configure: null);
    }

    /// <inheritdoc />
    public TdApi.IClient Create(ITdLibBindings bindings, TimeSpan receiverTimeout)
    {
        ArgumentNullException.ThrowIfNull(bindings);
        return CreateInstrumented(bindings, receiverTimeout, configure: null);
    }

    /// <inheritdoc />
    public TdApi.IClient Create(Action<TdClient> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        return CreateInstrumented(_bindings, _receiverTimeout, configure);
    }

    /// <inheritdoc />
    public TdApi.IClient Create(ITdLibBindings bindings, Action<TdClient> configure)
    {
        ArgumentNullException.ThrowIfNull(bindings);
        ArgumentNullException.ThrowIfNull(configure);
        return CreateInstrumented(bindings, _receiverTimeout, configure);
    }

    /// <inheritdoc />
    public TdApi.IClient Create(ITdLibBindings bindings, TimeSpan receiverTimeout, Action<TdClient> configure)
    {
        ArgumentNullException.ThrowIfNull(bindings);
        ArgumentNullException.ThrowIfNull(configure);
        return CreateInstrumented(bindings, receiverTimeout, configure);
    }

    private static TdApi.IClient CreateInstrumented(ITdLibBindings bindings, TimeSpan receiverTimeout, Action<TdClient>? configure)
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

        // 7. Wrap the client with OTel instrumentation
        return new OpenTelemetryTdClientDecorator(client);
    }
}
