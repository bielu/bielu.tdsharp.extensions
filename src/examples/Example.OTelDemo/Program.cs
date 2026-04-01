// SPDX-FileCopyrightText: 2024 tdsharp contributors <https://github.com/egramtel/tdsharp>
//
// SPDX-License-Identifier: MIT

using System.Runtime.InteropServices;
using bielu.tdsharp.abstractions;
using bielu.tdsharp.aspnetcore.logger;
using bielu.tdsharp.client.factory;
using bielu.tdsharp.opentelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using TdLib;
using TdLib.Bindings;

// Important: Define P/Invoke in your application for the callback to work correctly.
[DllImport("tdjson", CallingConvention = CallingConvention.Cdecl)]
static extern void td_set_log_message_callback(int maxVerbosityLevel, TdLogMessageCallback? callback);

// --------------------------------------------------------------------------
// Build the host with DI, OpenTelemetry, client factory, and TDLib logging
// --------------------------------------------------------------------------
var builder = Host.CreateApplicationBuilder(args);

// 1. Configure logging
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

// 2. Register TDLib client factory with OpenTelemetry instrumentation
//    This sets up IClientProvider → OpenTelemetryClientProvider (instruments JSON client, receiver, and client)
builder.Services.AddTdSharpOpenTelemetry();
builder.Services.AddTdClientFactory();

// 3. Configure OpenTelemetry with OTLP exporter (works with Aspire Dashboard)
//
//    To use the Aspire Dashboard for visualizing traces and metrics, run:
//      docker run --rm -it -d -p 18888:18888 -p 4317:18889 --name aspire-dashboard \
//        mcr.microsoft.com/dotnet/aspire-dashboard:latest
//    Then open http://localhost:18888 in your browser.
//
//    The OTLP exporter defaults to http://localhost:4317 which maps to the dashboard.
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(
            serviceName: "Example.OTelDemo",
            serviceVersion: "1.0.0"))
    .WithTracing(tracing => tracing
        .AddTdSharpInstrumentation()
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddTdSharpInstrumentation()
        .AddOtlpExporter());

using var host = builder.Build();

// Resolve services from DI
var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
var clientFactory = host.Services.GetRequiredService<ITdClientFactory>();
var appLogger = loggerFactory.CreateLogger("Example.OTelDemo");

Console.WriteLine("=== TDLib + OpenTelemetry + Aspire Dashboard Demo ===");
Console.WriteLine();
Console.WriteLine("Tip: Run the Aspire Dashboard to view traces and metrics:");
Console.WriteLine("  docker run --rm -it -d -p 18888:18888 -p 4317:18889 \\");
Console.WriteLine("    --name aspire-dashboard mcr.microsoft.com/dotnet/aspire-dashboard:latest");
Console.WriteLine("  Then open http://localhost:18888");
Console.WriteLine();

// 4. Create a client via the factory (fully instrumented with OTel)
var client = clientFactory.GetOrCreateClient("demo-user");

// 5. Optionally set up TDLib native logging → .NET ILogger bridge
if (client is TdClient tdClient)
{
    using var loggingScope = tdClient.UseTdLibLogging(
        loggerFactory,
        TdLogLevel.Info,
        td_set_log_message_callback);

    appLogger.LogInformation("TDLib client created with OpenTelemetry instrumentation");

    // 6. Execute some operations — these will produce OTel traces and metrics
    try
    {
        var version = tdClient.Execute(new TdApi.GetOption { Name = "version" });
        appLogger.LogInformation("TDLib version: {Version}", version);

        var commitHash = tdClient.Execute(new TdApi.GetOption { Name = "commit_hash" });
        appLogger.LogInformation("TDLib commit hash: {CommitHash}", commitHash);

        // Creating another client with the same ID returns the cached one
        var sameClient = clientFactory.GetOrCreateClient("demo-user");
        appLogger.LogInformation("Same client returned: {IsSame}", ReferenceEquals(client, sameClient));

        // Wait for background telemetry to flush
        Thread.Sleep(1000);
    }
    catch (Exception ex)
    {
        appLogger.LogError(ex, "Error executing TDLib operation");
    }
}

Console.WriteLine();
Console.WriteLine("Demo complete. Check the Aspire Dashboard for traces and metrics.");
Console.WriteLine("Press any key to exit...");
Console.ReadKey();
