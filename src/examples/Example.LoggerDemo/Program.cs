// SPDX-FileCopyrightText: 2024 tdsharp contributors <https://github.com/egramtel/tdsharp>
//
// SPDX-License-Identifier: MIT

using bielu.tdsharp.aspnetcore.logger;
using Microsoft.Extensions.Logging;
using TdLib;
using TdLib.Bindings;

// Create a standard .NET LoggerFactory with console output
using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Debug);
});

Console.WriteLine("=== TDLib to .NET Logging Demo ===");
Console.WriteLine();

// Create TdClient
using var client = new TdClient();

// Configure TDLib to route ALL its logs to .NET's ILoggerFactory
// This is the key feature - TDLib internal logs will appear in your .NET logging output
// Note: Some initial logs may still appear on stderr if they occur before this call
client.UseTdLibLogging(loggerFactory, TdLogLevel.Info, disableDefaultLogging: true);

Console.WriteLine("TDLib logging has been configured to route to .NET logging.");
Console.WriteLine("All subsequent TDLib logs will appear through the console logger.");
Console.WriteLine();

// You can also create a custom logger for your application
var appLogger = loggerFactory.CreateLogger("MyApp");
appLogger.LogInformation("Application started - TDLib logging is active");

// Demonstrate that TdClient operations will generate logs routed through ILogger
try
{
    // This will trigger some TDLib internal logging
    var version = client.Execute(new TdApi.GetOption { Name = "version" });
    appLogger.LogInformation("TDLib version retrieved: {Version}", version);
    
    // Trigger more TDLib activity to demonstrate callback receiving messages
    client.Execute(new TdApi.GetOption { Name = "commit_hash" });
    
    // Give some time for any background logs to be processed
    Thread.Sleep(100);
}
catch (Exception ex)
{
    appLogger.LogError(ex, "Error getting TDLib version");
}

Console.WriteLine();
Console.WriteLine("Demo complete. Press any key to exit...");
Console.ReadKey();

// Clean up - disable the callback before disposing
TdLoggerExtensions.DisableTdLibLogging();
