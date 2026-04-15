// SPDX-FileCopyrightText: 2024 tdsharp contributors <https://github.com/egramtel/tdsharp>
//
// SPDX-License-Identifier: MIT

using bielu.tdsharp.abstractions;
using Polly;
using TdLib;

namespace bielu.tdsharp.resilience;

/// <summary>
/// An <see cref="ITdClientMiddleware"/> that wraps a <see cref="TdApi.IClient"/>
/// with a Polly resilience pipeline (retry + circuit breaker).
/// </summary>
public class ResilienceTdClientMiddleware : ITdClientMiddleware
{
    private readonly ResiliencePipeline _pipeline;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResilienceTdClientMiddleware"/> class
    /// with default resilience options.
    /// </summary>
    public ResilienceTdClientMiddleware()
        : this(new TdSharpResilienceOptions())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResilienceTdClientMiddleware"/> class.
    /// </summary>
    /// <param name="options">The resilience options to configure retry and circuit breaker behaviour.</param>
    public ResilienceTdClientMiddleware(TdSharpResilienceOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _pipeline = ResiliencePipelineFactory.Create(options);
    }

    /// <inheritdoc />
    public TdApi.IClient Decorate(TdApi.IClient client)
    {
        ArgumentNullException.ThrowIfNull(client);
        return new ResilienceTdClientDecorator(client, _pipeline);
    }
}
