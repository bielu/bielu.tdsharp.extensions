# bielu.tdsharp.extensions

[![CI](https://github.com/bielu/bielu.tdsharp.extensions/actions/workflows/ci.yml/badge.svg)](https://github.com/bielu/bielu.tdsharp.extensions/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/bielu.tdsharp.aspnetcore.logger.svg)](https://www.nuget.org/packages/bielu.tdsharp.aspnetcore.logger/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/bielu.tdsharp.aspnetcore.logger.svg)](https://www.nuget.org/packages/bielu.tdsharp.aspnetcore.logger/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Extensions for TdSharp (Telegram TDLib .NET bindings) providing enhanced integration with .NET ecosystem.

## bielu.tdsharp.aspnetcore.logger

This library provides seamless integration between TDLib's internal logging system and .NET's `Microsoft.Extensions.Logging` framework.

### Key Features

- **TDLib → .NET Logging**: Route ALL TDLib internal logs to your .NET `ILoggerFactory`
- **Full Verbosity Support**: Captures all log levels (Fatal, Error, Warning, Info, Debug, Verbose)
- **Per-Source Category Logging**: Logs are categorized by their TDLib C++ source file (e.g., `TDLib.AuthData`, `TDLib.Td`, `TDLib.Client`)
- **Standard Integration**: Works with any logging provider (Console, File, Serilog, NLog, Application Insights, etc.)

### Installation

```bash
dotnet add package bielu.tdsharp.aspnetcore.logger
```

### Usage

#### Route TDLib Logs to .NET Logging (Main Feature)

This is the primary use case - injecting `ILoggerFactory` into TDLib's log stream:

```csharp
using bielu.tdsharp.aspnetcore.logger;
using Microsoft.Extensions.Logging;
using TdLib;
using TdLib.Bindings;

// Create your standard .NET LoggerFactory
using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Debug);
});

// Create TdClient
using var client = new TdClient();

// Configure TDLib to route ALL its logs to .NET's ILoggerFactory
// TDLib internal logs will now appear in your console/file/etc. through ILogger
client.UseTdLibLogging(loggerFactory, TdLogLevel.Info);

// Optional: Disable TDLib's default console/stderr output
client.UseTdLibLogging(loggerFactory, TdLogLevel.Info, disableDefaultLogging: true);
```

#### With ASP.NET Core Dependency Injection

```csharp
// In Program.cs or Startup.cs
builder.Services.AddSingleton<TdClient>(sp =>
{
    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
    var client = new TdClient();
    
    // Route TDLib logs to ASP.NET Core's logging infrastructure
    client.UseTdLibLogging(loggerFactory, TdLogLevel.Info, disableDefaultLogging: true);
    
    return client;
});
```

### How It Works

The library uses TDLib's native `td_set_log_message_callback` function to intercept all log messages from TDLib's internal logging system. These messages are then forwarded to your configured `ILoggerFactory`, allowing them to flow through your standard .NET logging pipeline.

**Logger categories are extracted from the TDLib source file** mentioned in each log message. For example:
- `[ 4][t 5][1771420471.389248132][AuthData.cpp:122]...` → Category: `TDLib.AuthData`
- `[ 3][t 2][1771414660.623883962][Td.cpp:1346]...` → Category: `TDLib.Td`
- `[ 3][t 0][1771414660.622062444][Client.cpp:600]...` → Category: `TDLib.Client`

This allows you to filter TDLib logs by component in your logging configuration.

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

The logging integration is thread-safe. The `UseTdLibLogging` method should be called once during application initialization, before using the TdClient.

### Cleanup

When disposing your application, you can optionally clear the logging callback:

```csharp
TdLoggerExtensions.DisableTdLibLogging();
```

## License

MIT License