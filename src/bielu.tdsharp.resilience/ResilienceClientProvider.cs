// SPDX-FileCopyrightText: 2024 tdsharp contributors <https://github.com/egramtel/tdsharp>
//
// SPDX-License-Identifier: MIT

using bielu.tdsharp.abstractions;
using bielu.tdsharp.client.factory;
using Polly;
using TdLib;

namespace bielu.tdsharp.resilience;

/// <summary>
/// An <see cref="IClientProvider"/> that decorates created clients with a resilience pipeline
/// (retry + circuit breaker) powered by Polly.
/// </summary>
public class ResilienceClientProvider : DecoratorClientProvider
{
    private readonly ResiliencePipeline _pipeline;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResilienceClientProvider"/> class
    /// with default resilience options.
    /// </summary>
    /// <param name="inner">The inner provider whose clients will be decorated.</param>
    public ResilienceClientProvider(IClientProvider inner)
        : this(inner, new TdSharpResilienceOptions())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResilienceClientProvider"/> class.
    /// </summary>
    /// <param name="inner">The inner provider whose clients will be decorated.</param>
    /// <param name="options">The resilience options to configure retry and circuit breaker behaviour.</param>
    public ResilienceClientProvider(IClientProvider inner, TdSharpResilienceOptions options)
        : base(inner)
    {
        ArgumentNullException.ThrowIfNull(options);
        _pipeline = ResiliencePipelineFactory.Create(options);
    }

    /// <inheritdoc />
    protected override TdApi.IClient Decorate(TdApi.IClient client)
    {
        return new ResilienceTdClientDecorator(client, _pipeline);
    }
}
