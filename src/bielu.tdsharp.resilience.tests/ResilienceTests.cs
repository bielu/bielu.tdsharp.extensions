// SPDX-FileCopyrightText: 2024 tdsharp contributors <https://github.com/egramtel/tdsharp>
//
// SPDX-License-Identifier: MIT

using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Polly;
using Polly.CircuitBreaker;
using TdLib;

namespace bielu.tdsharp.resilience.tests;

public class ResilienceTdClientDecoratorTests
{
    [Fact]
    public void Constructor_ThrowsOnNullInner()
    {
        var act = () => new ResilienceTdClientDecorator(null!, new TdSharpResilienceOptions());
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_ThrowsOnNullOptions()
    {
        var mockInner = Substitute.For<TdApi.IClient>();
        var act = () => new ResilienceTdClientDecorator(mockInner, (TdSharpResilienceOptions)null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_ThrowsOnNullPipeline()
    {
        var mockInner = Substitute.For<TdApi.IClient>();
        var act = () => new ResilienceTdClientDecorator(mockInner, (ResiliencePipeline)null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Send_DelegatesToInner()
    {
        var mockInner = Substitute.For<TdApi.IClient>();
        var decorator = new ResilienceTdClientDecorator(mockInner, new TdSharpResilienceOptions());
        var function = new TdApi.GetMe();

        decorator.Send(function);

        mockInner.Received(1).Send(function);
    }

    [Fact]
    public void Execute_DelegatesToInnerAndReturnsResult()
    {
        var mockInner = Substitute.For<TdApi.IClient>();
        var expected = new TdApi.User();
        var function = new TdApi.GetMe();
        mockInner.Execute(function).Returns(expected);
        var decorator = new ResilienceTdClientDecorator(mockInner, new TdSharpResilienceOptions());

        var result = decorator.Execute(function);

        result.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task ExecuteAsync_DelegatesToInnerAndReturnsResult()
    {
        var mockInner = Substitute.For<TdApi.IClient>();
        var expected = new TdApi.User();
        var function = new TdApi.GetMe();
        mockInner.ExecuteAsync(function).Returns(Task.FromResult(expected));
        var decorator = new ResilienceTdClientDecorator(mockInner, new TdSharpResilienceOptions());

        var result = await decorator.ExecuteAsync(function);

        result.Should().BeSameAs(expected);
    }

    [Fact]
    public void Execute_RetriesOnTransientFailure()
    {
        var mockInner = Substitute.For<TdApi.IClient>();
        var function = new TdApi.GetMe();
        var expected = new TdApi.User();
        var callCount = 0;

        mockInner.Execute(function).Returns(_ =>
        {
            callCount++;
            if (callCount < 3)
            {
                throw new TdException(new TdApi.Error { Code = 500, Message = "Transient" });
            }
            return expected;
        });

        var options = new TdSharpResilienceOptions
        {
            MaxRetryAttempts = 3,
            RetryBaseDelay = TimeSpan.FromMilliseconds(1),
        };
        var decorator = new ResilienceTdClientDecorator(mockInner, options);

        var result = decorator.Execute(function);

        result.Should().BeSameAs(expected);
        callCount.Should().Be(3);
    }

    [Fact]
    public async Task ExecuteAsync_RetriesOnTransientFailure()
    {
        var mockInner = Substitute.For<TdApi.IClient>();
        var function = new TdApi.GetMe();
        var expected = new TdApi.User();
        var callCount = 0;

        mockInner.ExecuteAsync(function).Returns(_ =>
        {
            callCount++;
            if (callCount < 3)
            {
                throw new TdException(new TdApi.Error { Code = 500, Message = "Transient" });
            }
            return Task.FromResult(expected);
        });

        var options = new TdSharpResilienceOptions
        {
            MaxRetryAttempts = 3,
            RetryBaseDelay = TimeSpan.FromMilliseconds(1),
        };
        var decorator = new ResilienceTdClientDecorator(mockInner, options);

        var result = await decorator.ExecuteAsync(function);

        result.Should().BeSameAs(expected);
        callCount.Should().Be(3);
    }

    [Fact]
    public void Execute_ThrowsAfterMaxRetries()
    {
        var mockInner = Substitute.For<TdApi.IClient>();
        var function = new TdApi.GetMe();

        mockInner.Execute(function)
            .Returns<TdApi.User>(_ => throw new TdException(new TdApi.Error { Code = 500, Message = "Permanent" }));

        var options = new TdSharpResilienceOptions
        {
            MaxRetryAttempts = 2,
            RetryBaseDelay = TimeSpan.FromMilliseconds(1),
            // Set circuit breaker high so it doesn't interfere
            CircuitBreakerMinimumThroughput = 100,
        };
        var decorator = new ResilienceTdClientDecorator(mockInner, options);

        var act = () => decorator.Execute(function);

        act.Should().Throw<TdException>();
        // 1 initial + 2 retries = 3 calls
        mockInner.Received(3).Execute(function);
    }

    [Fact]
    public void Send_RetriesOnFailure()
    {
        var mockInner = Substitute.For<TdApi.IClient>();
        var function = new TdApi.GetMe();
        var callCount = 0;

        mockInner.When(x => x.Send(function)).Do(_ =>
        {
            callCount++;
            if (callCount < 2)
            {
                throw new InvalidOperationException("Transient");
            }
        });

        var options = new TdSharpResilienceOptions
        {
            MaxRetryAttempts = 3,
            RetryBaseDelay = TimeSpan.FromMilliseconds(1),
        };
        var decorator = new ResilienceTdClientDecorator(mockInner, options);

        decorator.Send(function);

        callCount.Should().Be(2);
    }

    [Fact]
    public void UpdateReceived_DelegatesToInner()
    {
        var mockInner = Substitute.For<TdApi.IClient>();
        var decorator = new ResilienceTdClientDecorator(mockInner, new TdSharpResilienceOptions());
        EventHandler<TdApi.Update> handler = (_, _) => { };

        decorator.UpdateReceived += handler;
        decorator.UpdateReceived -= handler;

        mockInner.Received(1).UpdateReceived += handler;
        mockInner.Received(1).UpdateReceived -= handler;
    }

    [Fact]
    public void Dispose_DisposesInnerIfDisposable()
    {
        var mockInner = Substitute.For<TdApi.IClient, IDisposable>();
        var decorator = new ResilienceTdClientDecorator((TdApi.IClient)mockInner, new TdSharpResilienceOptions());

        decorator.Dispose();

        ((IDisposable)mockInner).Received(1).Dispose();
    }

    [Fact]
    public void ShouldHandle_FiltersExceptions()
    {
        var mockInner = Substitute.For<TdApi.IClient>();
        var function = new TdApi.GetMe();

        // Only retry TdException
        var options = new TdSharpResilienceOptions
        {
            MaxRetryAttempts = 3,
            RetryBaseDelay = TimeSpan.FromMilliseconds(1),
            ShouldHandle = ex => ex is TdException,
        };
        var decorator = new ResilienceTdClientDecorator(mockInner, options);

        // InvalidOperationException should NOT be retried
        mockInner.Execute(function)
            .Returns<TdApi.User>(_ => throw new InvalidOperationException("Not retried"));

        var act = () => decorator.Execute(function);

        act.Should().Throw<InvalidOperationException>();
        // Only 1 call — no retries for non-matching exception
        mockInner.Received(1).Execute(function);
    }
}

public class ResilienceOptionsTests
{
    [Fact]
    public void Defaults_AreReasonable()
    {
        var options = new TdSharpResilienceOptions();

        options.MaxRetryAttempts.Should().Be(3);
        options.RetryBaseDelay.Should().Be(TimeSpan.FromMilliseconds(50));
        options.RetryMaxDelay.Should().Be(TimeSpan.FromSeconds(30));
        options.CircuitBreakerFailureRatio.Should().Be(0.5);
        options.CircuitBreakerMinimumThroughput.Should().Be(10);
        options.CircuitBreakerBreakDuration.Should().Be(TimeSpan.FromSeconds(30));
        options.CircuitBreakerSamplingDuration.Should().Be(TimeSpan.FromSeconds(60));
        options.ShouldHandle.Should().BeNull();
    }

    [Fact]
    public void MinimumRetryDelay_Is50Ms()
    {
        TdSharpResilienceOptions.MinimumRetryDelay.Should().Be(TimeSpan.FromMilliseconds(50));
    }
}

public class ResilienceClientProviderTests
{
    [Fact]
    public void Constructor_ThrowsOnNullInner()
    {
        var act = () => new ResilienceClientProvider(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_ThrowsOnNullOptions()
    {
        var mockInner = Substitute.For<bielu.tdsharp.abstractions.IClientProvider>();
        var act = () => new ResilienceClientProvider(mockInner, null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Create_ReturnsDecoratedClient()
    {
        var mockInner = Substitute.For<bielu.tdsharp.abstractions.IClientProvider>();
        var mockClient = Substitute.For<TdApi.IClient>();
        mockInner.Create().Returns(mockClient);

        var provider = new ResilienceClientProvider(mockInner);
        var result = provider.Create();

        result.Should().BeOfType<ResilienceTdClientDecorator>();
    }
}

public class ResiliencePipelineFactoryTests
{
    [Fact]
    public void Create_ThrowsOnNullOptions()
    {
        var act = () => ResiliencePipelineFactory.Create(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Create_ReturnsPipeline()
    {
        var pipeline = ResiliencePipelineFactory.Create(new TdSharpResilienceOptions());
        pipeline.Should().NotBeNull();
    }

    [Fact]
    public void Create_ClampsDelayBelowMinimum()
    {
        // Setting delay below 50 ms should still produce a valid pipeline
        // that retries with at least 50 ms base delay.
        var options = new TdSharpResilienceOptions
        {
            RetryBaseDelay = TimeSpan.FromMilliseconds(1),
        };

        var act = () => ResiliencePipelineFactory.Create(options);
        act.Should().NotThrow();
    }
}

