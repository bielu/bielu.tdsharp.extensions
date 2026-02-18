# bielu.tdsharp.extensions

Extensions and utilities for TDSharp, providing enhanced logging and integration capabilities.

## bielu.tdsharp.aspnetcore.logger

A logging library that integrates TDLib with Microsoft.Extensions.Logging, featuring:

- **ILoggerFactory Support**: Create logger instances per class using `ILoggerFactory`
- **Bidirectional Logging**: Route TDLib logs to ILogger and .NET logs through TDLib
- **Thread-Safe**: Safe for concurrent use across multiple threads
- **.NET 10**: Built for the latest .NET platform

### Installation

```bash
dotnet add package bielu.tdsharp.aspnetcore.logger
```

### Usage

#### Route TDLib logs to ILogger using ILoggerFactory

```csharp
using bielu.tdsharp.aspnetcore.logger;
using Microsoft.Extensions.Logging;

var loggerFactory = LoggerFactory.Create(builder => 
{
    builder.AddConsole();
});

var client = new TdClient();
client.UseTdLibLogging(loggerFactory, TdLogLevel.Info, disableDefaultLogging: true);
```

#### Route .NET logs through TDLib

```csharp
var loggerFactory = LoggerFactory.Create(builder => 
{
    builder.AddTdLib(client, TdLogLevel.Debug);
});

// Each class gets its own logger with appropriate category name
var logger = loggerFactory.CreateLogger<MyApp>();
logger.LogInformation("This will be logged through TDLib");
```

### Features

- **Logger Factory Pattern**: Uses `ILoggerFactory` to create separate logger instances for each class
- **Per-Class Logging**: Each logger instance includes category name in log messages
- **Log Level Mapping**: Automatic conversion between TDLib and Microsoft.Extensions.Logging log levels
- **Centralized Package Management**: Uses Directory.Packages.props for version management

### Building

```bash
cd src
dotnet restore
dotnet build
```

### Testing

```bash
cd src
dotnet test
```

## License

MIT License - see LICENSE file for details
