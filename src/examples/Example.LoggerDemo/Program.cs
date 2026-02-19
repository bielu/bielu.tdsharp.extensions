// SPDX-FileCopyrightText: 2024 tdsharp contributors <https://github.com/egramtel/tdsharp>
//
// SPDX-License-Identifier: MIT

using System.Runtime.InteropServices;
using bielu.tdsharp.aspnetcore.logger;
using Microsoft.Extensions.Logging;
using TdLib;
using TdLib.Bindings;

// Important: Define P/Invoke in your application for the callback to work correctly.
// This is required due to .NET native interop limitations with cross-assembly callbacks.
[DllImport("tdjson", CallingConvention = CallingConvention.Cdecl)]
static extern void td_set_log_message_callback(int maxVerbosityLevel, TdLogMessageCallback? callback);

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

// Use the extension method - pass your P/Invoke as a parameter
// This returns an IDisposable that handles cleanup
using var loggingScope = client.UseTdLibLogging(
    loggerFactory, 
    TdLogLevel.Info, 
    td_set_log_message_callback);

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
    
    // Give some time for background logs to be processed
    Thread.Sleep(500);
}
catch (Exception ex)
{
    appLogger.LogError(ex, "Error getting TDLib version");
}

Console.WriteLine();
Console.WriteLine("Demo complete. Press any key to exit...");
Console.ReadKey();

// loggingScope.Dispose() is called automatically when exiting the using block,
// which cleans up the callback and frees resources
