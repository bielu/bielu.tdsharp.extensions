# bielu.tdsharp.extensions

[![CI](https://github.com/bielu/bielu.tdsharp.extensions/actions/workflows/ci.yml/badge.svg)](https://github.com/bielu/bielu.tdsharp.extensions/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Extensions for [TdSharp](https://github.com/egramtel/tdsharp) (Telegram TDLib .NET bindings) providing enhanced integration with the .NET ecosystem.

## Packages

| Package | Description |
|---------|-------------|
| `bielu.tdsharp.abstractions` | Core interfaces (`IClientProvider`, `ITdClientFactory`) |
| `bielu.tdsharp.client.factory` | Client factory with DI support and decorator pattern |
| `bielu.tdsharp.opentelemetry` | Full-stack OpenTelemetry instrumentation (traces + metrics) |
| `bielu.tdsharp.aspnetcore.logger` | TDLib → .NET `ILogger` bridge |

---

## bielu.tdsharp.abstractions

Core interfaces for client creation and management.

### `IClientProvider`

Provides `TdApi.IClient` instances. Implementations may create a plain client or wrap it with additional behavior (e.g. OpenTelemetry instrumentation).

```csharp
public interface IClientProvider
{
    TdApi.IClient Create();
    TdApi.IClient Create(ITdLibBindings bindings);
    TdApi.IClient Create(ITdLibBindings bindings, TimeSpan receiverTimeout);
}
```

### `ITdClientFactory`

Factory for creating or retrieving clients identified by a unique key (e.g. phone number).

```csharp
public interface ITdClientFactory
{
    TdApi.IClient GetOrCreateClient(string identifier);
}
```

---

## bielu.tdsharp.client.factory

Concrete implementations of the abstractions with DI support.

### Key Types

- **`DefaultClientProvider`** — Creates plain `TdClient` instances. Supports configurable `ITdLibBindings` (defaults to `Interop.AutoDetectBindings()`).
- **`DecoratorClientProvider`** — Abstract base for decorating another `IClientProvider`. Accepts only `IClientProvider` in its constructor.
- **`TdClientFactory`** — Thread-safe factory using `ConcurrentDictionary` to cache clients by identifier.

### Registration

```csharp
services.AddTdClientFactory(); // Registers ITdClientFactory + TdClientFactory
```

---

## bielu.tdsharp.opentelemetry

Full-stack OpenTelemetry instrumentation for TDLib. Instruments **every layer** of the TDLib client stack:

```
TdJsonClient(bindings)
  → OpenTelemetryTdJsonClientDecorator  (traces/metrics: Send, Execute, Receive)
    → Receiver(jsonClient, timeout)
      → OpenTelemetryReceiverDecorator  (traces/metrics: Received, AuthStateChanged, ExceptionThrown)
        → TdClient(jsonClient, receiver)
          → OpenTelemetryTdClientDecorator  (traces/metrics: Send, Execute, ExecuteAsync)
```

### Metrics Emitted

| Metric | Layer |
|--------|-------|
| `tdsharp.client.operations.count` | IClient |
| `tdsharp.client.operations.duration` | IClient |
| `tdsharp.client.operations.errors` | IClient |
| `tdsharp.receiver.events.count` | IReceiver |
| `tdsharp.receiver.errors` | IReceiver |
| `tdsharp.json_client.operations.count` | ITdJsonClient |
| `tdsharp.json_client.operations.duration` | ITdJsonClient |
| `tdsharp.json_client.operations.errors` | ITdJsonClient |

### Registration

```csharp
// Register the instrumented client provider (auto-detected bindings, default timeout)
services.AddTdSharpOpenTelemetry();

// Or with custom bindings and timeout
services.AddTdSharpOpenTelemetry(myBindings, TimeSpan.FromSeconds(0.5));
```

### OpenTelemetry SDK Configuration

```csharp
services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddTdSharpInstrumentation())
    .WithMetrics(metrics => metrics
        .AddTdSharpInstrumentation());
```

### Using with the Aspire Dashboard

The [.NET Aspire Dashboard](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/dashboard/standalone) provides a local UI for viewing traces, metrics, and logs. Run it with Docker and point the OTLP exporter at it:

```bash
docker run --rm -it -d -p 18888:18888 -p 4317:18889 \
  --name aspire-dashboard mcr.microsoft.com/dotnet/aspire-dashboard:latest
```

Then configure your app to export via OTLP (defaults to `http://localhost:4317`):

```csharp
using bielu.tdsharp.client.factory;
using bielu.tdsharp.opentelemetry;
using OpenTelemetry.Resources;

var builder = Host.CreateApplicationBuilder(args);

// Register TDLib services
builder.Services.AddTdSharpOpenTelemetry();
builder.Services.AddTdClientFactory();

// Configure OpenTelemetry with OTLP exporter → Aspire Dashboard
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("MyTelegramBot"))
    .WithTracing(t => t
        .AddTdSharpInstrumentation()
        .AddOtlpExporter())
    .WithMetrics(m => m
        .AddTdSharpInstrumentation()
        .AddOtlpExporter());

using var host = builder.Build();

// Resolve and use the factory
var factory = host.Services.GetRequiredService<ITdClientFactory>();
var client = factory.GetOrCreateClient("+1234567890");
```

Open `http://localhost:18888` to view traces and metrics in the Aspire Dashboard.

---

## bielu.tdsharp.aspnetcore.logger

Routes TDLib's internal C++ logs to .NET's `Microsoft.Extensions.Logging` framework.

### Key Features

- **TDLib → .NET Logging**: Route ALL TDLib internal logs to your .NET `ILoggerFactory`
- **Full Verbosity Support**: Captures all log levels (Fatal, Error, Warning, Info, Debug, Verbose)
- **Per-Source Category Logging**: Logs are categorized by their TDLib C++ source file (e.g., `TDLib.AuthData`, `TDLib.Td`, `TDLib.Client`)
- **Standard Integration**: Works with any logging provider (Console, Serilog, NLog, Application Insights, etc.)

### Usage

**Note:** Due to .NET native interop requirements, you need to define a P/Invoke in your application and pass it to the extension method.

```csharp
using System.Runtime.InteropServices;
using bielu.tdsharp.aspnetcore.logger;
using Microsoft.Extensions.Logging;
using TdLib;
using TdLib.Bindings;

// Step 1: Define P/Invoke in your application (one line)
[DllImport("tdjson", CallingConvention = CallingConvention.Cdecl)]
static extern void td_set_log_message_callback(int maxVerbosityLevel, TdLogMessageCallback? callback);

// Step 2: Create your standard .NET LoggerFactory
using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Debug);
});

// Step 3: Create TdClient
using var client = new TdClient();

// Step 4: Use the extension method
using var loggingScope = client.UseTdLibLogging(
    loggerFactory, 
    TdLogLevel.Info, 
    td_set_log_message_callback);

// All TDLib logs now appear through your ILoggerFactory!
```

### Log Level Mapping

| TDLib Level | .NET LogLevel |
|-------------|---------------|
| Fatal (0)   | Critical      |
| Error (1)   | Error         |
| Warning (2) | Warning       |
| Info (3)    | Information   |
| Debug (4)   | Debug         |
| Verbose (5) | Trace         |

### Thread Safety

The logging integration is thread-safe. The callback registration should be done once during application initialization, before using the TdClient extensively.

### Why the P/Invoke Must Be in Your Application

Due to how .NET marshals callback delegates to native code, the P/Invoke declaration must be in the consumer's assembly for callbacks to work correctly. This is a .NET runtime requirement. The extension method handles all the complexity — you just need to define the one-line P/Invoke and pass it in.

---

## Full Example: Client Factory + OTel + Aspire + Logging

See [`examples/Example.OTelDemo`](src/examples/Example.OTelDemo/Program.cs) for a complete working example that combines all packages:

```csharp
using System.Runtime.InteropServices;
using bielu.tdsharp.abstractions;
using bielu.tdsharp.aspnetcore.logger;
using bielu.tdsharp.client.factory;
using bielu.tdsharp.opentelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Resources;
using TdLib;
using TdLib.Bindings;

[DllImport("tdjson", CallingConvention = CallingConvention.Cdecl)]
static extern void td_set_log_message_callback(int maxVerbosityLevel, TdLogMessageCallback? callback);

var builder = Host.CreateApplicationBuilder(args);

// Register all TDLib services
builder.Services.AddTdSharpOpenTelemetry();   // IClientProvider with OTel instrumentation
builder.Services.AddTdClientFactory();         // ITdClientFactory

// Configure OpenTelemetry → Aspire Dashboard
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("MyTelegramBot"))
    .WithTracing(t => t.AddTdSharpInstrumentation().AddOtlpExporter())
    .WithMetrics(m => m.AddTdSharpInstrumentation().AddOtlpExporter());

using var host = builder.Build();

var factory = host.Services.GetRequiredService<ITdClientFactory>();
var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();

// Create an instrumented client
var client = factory.GetOrCreateClient("my-bot");

// Bridge TDLib native logs → ILogger
if (client is TdClient tdClient)
{
    using var loggingScope = tdClient.UseTdLibLogging(
        loggerFactory, TdLogLevel.Info, td_set_log_message_callback);

    var version = tdClient.Execute(new TdApi.GetOption { Name = "version" });
    Console.WriteLine($"TDLib version: {version}");
}
```

## License

MIT License
