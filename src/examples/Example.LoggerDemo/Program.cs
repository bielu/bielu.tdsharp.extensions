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

// Create the log handler that routes TDLib messages to ILoggerFactory
using var logHandler = new LogStreamCallback(loggerFactory);

// Create TdClient
using var client = new TdClient();

// Set verbosity level to control what messages TDLib generates
client.Bindings.SetLogVerbosityLevel((int)TdLogLevel.Info);

// Create the callback delegate (must be in your application assembly)
TdLogMessageCallback callback = (verbosity, msgPtr) => logHandler.HandleLogMessage(verbosity, msgPtr);

// Pin the delegate to prevent garbage collection while it's registered with native code
var callbackHandle = GCHandle.Alloc(callback);

try
{
    // Register the callback with TDLib
    td_set_log_message_callback((int)TdLogLevel.Info, callback);
    
    // Disable default logging output (stderr) to prevent duplicate logs
    client.Execute(new TdApi.SetLogStream { LogStream = new TdApi.LogStream.LogStreamEmpty() });

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
}
finally
{
    // Clean up - disable the callback and free the handle
    td_set_log_message_callback(0, null);
    callbackHandle.Free();
}
