// SPDX-FileCopyrightText: 2024 tdsharp contributors <https://github.com/egramtel/tdsharp>
//
// SPDX-License-Identifier: MIT

using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TdLib;
using TdLib.Bindings;

namespace bielu.tdsharp.aspnetcore.logger.tests;

/// <summary>
/// Integration tests that verify the native td_set_log_message_callback actually works.
/// These tests require the tdlib.native package to be installed.
/// </summary>
public class LogStreamCallbackIntegrationTests
{
    [Fact]
    public void LogStreamCallback_WhenActivated_ShouldInvokeLoggerFactoryCreateLogger()
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

        // Act
        using var logStream = new LogStreamCallback(mockLoggerFactory);
        using var client = new TdClient();
        
        logStream.Activate(client, TdLogLevel.Info);
        
        // Wait for logs
        Thread.Sleep(1000);
        
        logStream.Deactivate();

        // Assert
        // The callback should have received messages and created loggers for them
        createdCategories.Should().NotBeEmpty("The callback should create loggers when TDLib generates log messages");
        createdCategories.Should().Contain(c => c.StartsWith("TDLib."),
            "Logger categories should be extracted from TDLib source files (e.g., TDLib.Client, TDLib.Td)");
    }

    [Fact]
    public void LogStreamCallback_ExtractLoggerCategory_ShouldCreateCorrectLoggerCategories()
    {
        // Arrange
        var createdCategories = new HashSet<string>();
        
        var mockLogger = Substitute.For<ILogger>();
        var mockLoggerFactory = Substitute.For<ILoggerFactory>();
        mockLoggerFactory
            .CreateLogger(Arg.Any<string>())
            .Returns(callInfo =>
            {
                createdCategories.Add(callInfo.ArgAt<string>(0));
                return mockLogger;
            });

        // Act
        using var logStream = new LogStreamCallback(mockLoggerFactory);
        using var client = new TdClient();
        
        logStream.Activate(client, TdLogLevel.Info);
        Thread.Sleep(500);
        logStream.Deactivate();

        // Assert
        createdCategories.Should().Contain(c => c.StartsWith("TDLib."),
            "Logger categories should be extracted from TDLib source files");
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
        var handle = System.Runtime.InteropServices.GCHandle.Alloc(callback);

        try
        {
            // Act
            TdNativeLogging.SetLogMessageCallback(5, callback);
            
            using var client = new TdClient();
            Thread.Sleep(500);

            // Assert
            callbackInvoked.Should().BeTrue("The native callback should be invoked when TDLib generates logs");
            messageCount.Should().BeGreaterThan(0, "Multiple log messages should be captured");
        }
        finally
        {
            TdNativeLogging.SetLogMessageCallback(0, null);
            handle.Free();
        }
    }

    [Fact]
    public void TdNativeLogging_SetLogMessageCallback_ExistsAndIsCallable()
    {
        // This test verifies that td_set_log_message_callback is exported by tdjson
        // and can be called without throwing EntryPointNotFoundException
        
        // Act & Assert - should not throw
        var act = () => TdNativeLogging.SetLogMessageCallback(0, null);
        act.Should().NotThrow<EntryPointNotFoundException>(
            "td_set_log_message_callback should be exported by tdjson (TDLib 1.7.5+)");
    }
}
