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
- **TDLib 1.8.60 Compatible**: Works with the latest TDLib versions

### Installation

```bash
dotnet add package bielu.tdsharp.aspnetcore.logger
```

### Usage

**Note:** Due to .NET native interop requirements, you need to define a simple P/Invoke in your application and pass it to the extension method.

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

// Step 4: Use the extension method - pass your P/Invoke as a parameter
using var loggingScope = client.UseTdLibLogging(
    loggerFactory, 
    TdLogLevel.Info, 
    td_set_log_message_callback);

// Now all TDLib logs will appear through your ILoggerFactory!
// Your app code here...

// loggingScope.Dispose() is called automatically when exiting the using block,
// which cleans up the callback and frees resources
```

### With ASP.NET Core Dependency Injection

```csharp
// In your application
public class TdClientService : IDisposable
{
    [DllImport("tdjson", CallingConvention = CallingConvention.Cdecl)]
    private static extern void td_set_log_message_callback(int maxVerbosityLevel, TdLogMessageCallback? callback);

    private readonly TdClient _client;
    private readonly IDisposable _loggingScope;

    public TdClientService(ILoggerFactory loggerFactory)
    {
        _client = new TdClient();
        _loggingScope = _client.UseTdLibLogging(
            loggerFactory, 
            TdLogLevel.Info, 
            td_set_log_message_callback);
    }

    public TdClient Client => _client;

    public void Dispose()
    {
        _loggingScope.Dispose();
        _client.Dispose();
    }
}

// Register in DI
builder.Services.AddSingleton<TdClientService>();
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

The logging integration is thread-safe. The callback registration should be done once during application initialization, before using the TdClient extensively.

### Why the P/Invoke Must Be in Your Application

Due to how .NET marshals callback delegates to native code, the P/Invoke declaration must be in the consumer's assembly for callbacks to work correctly. This is a .NET runtime requirement. The extension method handles all the complexity - you just need to define the one-line P/Invoke and pass it in.

## License

MIT License
