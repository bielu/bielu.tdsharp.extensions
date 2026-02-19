// SPDX-FileCopyrightText: 2024 tdsharp contributors <https://github.com/egramtel/tdsharp>
//
// SPDX-License-Identifier: MIT

using System.Runtime.InteropServices;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TdLib;
using TdLib.Bindings;

namespace bielu.tdsharp.aspnetcore.logger.tests;

/// <summary>
/// Integration tests that verify TDLib logging integration works correctly.
/// These tests require the tdlib.native package to be installed.
/// </summary>
public class LogStreamCallbackIntegrationTests
{
    // Define P/Invoke in test assembly (required for callbacks to work)
    [DllImport("tdjson", CallingConvention = CallingConvention.Cdecl)]
    private static extern void td_set_log_message_callback(int maxVerbosityLevel, TdLogMessageCallback? callback);

    [Fact]
    public void LogStreamCallback_WhenCallbackInvoked_ShouldRouteToLoggerFactory()
    {
        // Arrange
        var createdCategories = new List<string>();
        
        var mockLogger = Substitute.For<ILogger>();
        var mockLoggerFactory = Substitute.For<ILoggerFactory>();
        mockLoggerFactory
            .CreateLogger(Arg.Any<string>())
            .Returns(callInfo =>
            {
                var category = callInfo.ArgAt<string>(0);
                createdCategories.Add(category);
                return mockLogger;
            });

        using var logHandler = new LogStreamCallback(mockLoggerFactory);
        using var client = new TdClient();

        // Create callback in THIS assembly
        TdLogMessageCallback callback = (verbosity, msgPtr) => logHandler.HandleLogMessage(verbosity, msgPtr);
        var handle = GCHandle.Alloc(callback);

        try
        {
            // Act
            client.Bindings.SetLogVerbosityLevel((int)TdLogLevel.Info);
            td_set_log_message_callback((int)TdLogLevel.Info, callback);
            client.Execute(new TdApi.SetLogStream { LogStream = new TdApi.LogStream.LogStreamEmpty() });
            
            // Wait for logs
            Thread.Sleep(500);

            // Assert
            // The callback should have received messages and created loggers for them
            // Note: TDLib.FatalError is created in constructor, so we check for additional categories
            var tdlibCategories = createdCategories.Where(c => c.StartsWith("TDLib.") && c != "TDLib.FatalError").ToList();
            tdlibCategories.Should().NotBeEmpty("The callback should create loggers when TDLib generates log messages");
        }
        finally
        {
            td_set_log_message_callback(0, null);
            handle.Free();
        }
    }

    [Fact]
    public void LogStreamCallback_ExtractLoggerCategory_ShouldExtractCorrectCategories()
    {
        // Test the category extraction logic directly
        
        // Arrange & Act & Assert
        LogStreamCallback.ExtractLoggerCategory("[ 3][t 0][1234567890.123456789][Client.cpp:600]Create client 1")
            .Should().Be("TDLib.Client");
            
        LogStreamCallback.ExtractLoggerCategory("[ 3][t 4][1234567890.123456789][Td.cpp:138][#1][!MultiTd]Create Td")
            .Should().Be("TDLib.Td");
            
        LogStreamCallback.ExtractLoggerCategory("Some message without source file")
            .Should().Be("TDLib");
            
        LogStreamCallback.ExtractLoggerCategory("")
            .Should().Be("TDLib");
    }

    [Fact]
    public void TdNativeLogging_SetLogMessageCallback_ShouldInvokeCallback()
    {
        // Arrange
        var callbackInvoked = false;
        var messageCount = 0;

        TdLogMessageCallback callback = (verbosity, msgPtr) =>
        {
            callbackInvoked = true;
            Interlocked.Increment(ref messageCount);
        };

        // Pin callback to prevent GC
        var handle = GCHandle.Alloc(callback);

        try
        {
            // Act - using our local P/Invoke (important for callback to work!)
            td_set_log_message_callback(5, callback);
            
            using var client = new TdClient();
            Thread.Sleep(500);

            // Assert
            callbackInvoked.Should().BeTrue("The native callback should be invoked when TDLib generates logs");
            messageCount.Should().BeGreaterThan(0, "Multiple log messages should be captured");
        }
        finally
        {
            td_set_log_message_callback(0, null);
            handle.Free();
        }
    }

    [Fact]
    public void TdNativeLogging_SetLogMessageCallback_ExistsAndIsCallable()
    {
        // This test verifies that td_set_log_message_callback is exported by tdjson
        // and can be called without throwing EntryPointNotFoundException
        
        // Act & Assert - should not throw
        var act = () => td_set_log_message_callback(0, null);
        act.Should().NotThrow<EntryPointNotFoundException>(
            "td_set_log_message_callback should be exported by tdjson (TDLib 1.7.5+)");
    }

    [Fact]
    public void LogStreamCallback_HandleLogMessage_ShouldLogWithCorrectLevel()
    {
        // Arrange
        var loggedLevels = new List<LogLevel>();
        
        var mockLogger = Substitute.For<ILogger>();
        mockLogger
            .When(x => x.Log(
                Arg.Any<LogLevel>(),
                Arg.Any<EventId>(),
                Arg.Any<object>(),
                Arg.Any<Exception?>(),
                Arg.Any<Func<object, Exception?, string>>()))
            .Do(callInfo => loggedLevels.Add(callInfo.ArgAt<LogLevel>(0)));

        var mockLoggerFactory = Substitute.For<ILoggerFactory>();
        mockLoggerFactory.CreateLogger(Arg.Any<string>()).Returns(mockLogger);

        using var logHandler = new LogStreamCallback(mockLoggerFactory);

        // Simulate a log message
        var testMessage = "[ 3][t 0][1234567890.123456][Client.cpp:600]Test message";
        var messagePtr = Marshal.StringToHGlobalAnsi(testMessage);

        try
        {
            // Act
            logHandler.HandleLogMessage(3, messagePtr);  // 3 = Info

            // Assert
            loggedLevels.Should().Contain(LogLevel.Information);
        }
        finally
        {
            Marshal.FreeHGlobal(messagePtr);
        }
    }
}
