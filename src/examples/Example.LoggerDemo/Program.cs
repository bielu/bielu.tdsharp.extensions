// SPDX-FileCopyrightText: 2024 tdsharp contributors
//
// SPDX-License-Identifier: MIT

using bielu.tdsharp.aspnetcore.logger;
using Microsoft.Extensions.Logging;

Console.WriteLine("=== bielu.tdsharp.aspnetcore.logger Example ===\n");

// This example demonstrates how to use the logger library
// Note: This is a demonstration of the API - it won't actually run without TDLib native binaries

Console.WriteLine("Example 1: Using ILoggerFactory with TDLib");
Console.WriteLine("-------------------------------------------");
Console.WriteLine(@"
// Create a logger factory with console logging
var loggerFactory = LoggerFactory.Create(builder => 
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Debug);
});

// Create TdClient and configure it to use the logger factory
// var client = new TdClient();
// client.UseTdLibLogging(loggerFactory, TdLogLevel.Info, disableDefaultLogging: true);

// The logger factory creates separate loggers for each category
var logger = loggerFactory.CreateLogger(""MyApp"");
logger.LogInformation(""Application started"");
");

Console.WriteLine("\nExample 2: Adding TDLib logger to factory");
Console.WriteLine("-------------------------------------------");
Console.WriteLine(@"
// Route .NET logs through TDLib
var loggerFactory = LoggerFactory.Create(builder => 
{
    // var client = new TdClient();
    // builder.AddTdLib(client, TdLogLevel.Debug);
});

// Each class gets its own logger with appropriate category name
var logger = loggerFactory.CreateLogger<MyService>();
logger.LogInformation(""Service method called"");

// The log message will be: [MyService] Service method called
");

Console.WriteLine("\nExample 3: Log Level Conversion");
Console.WriteLine("-------------------------------------------");
Console.WriteLine(@"
// Convert between TDLib and Microsoft.Extensions.Logging levels
var tdLevel = TdLogLevel.Warning;
var msLevel = tdLevel.ToLogLevel();
Console.WriteLine($""TDLib {tdLevel} -> MS {msLevel}"");

var msLevel2 = LogLevel.Error;
var tdLevel2 = msLevel2.ToTdLogLevel();
Console.WriteLine($""MS {msLevel2} -> TDLib {tdLevel2}"");
");

// Demonstrate actual conversion
Console.WriteLine("\nActual conversion examples:");
var tdLevel = TdLogLevel.Warning;
var msLevel = tdLevel.ToLogLevel();
Console.WriteLine($"  TDLib {tdLevel} -> MS {msLevel}");

var msLevel2 = LogLevel.Error;
var tdLevel2 = msLevel2.ToTdLogLevel();
Console.WriteLine($"  MS {msLevel2} -> TDLib {tdLevel2}");

Console.WriteLine("\n=== End of Examples ===");
Console.WriteLine("\nNote: To actually use TdClient, you need to install the tdlib.native package");
Console.WriteLine("and have the TDLib native libraries available at runtime.");
