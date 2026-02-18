// SPDX-FileCopyrightText: 2024 tdsharp contributors <https://github.com/egramtel/tdsharp>
//
// SPDX-License-Identifier: MIT

using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using TdLib.Bindings;

namespace bielu.tdsharp.aspnetcore.logger;

/// <summary>
/// A custom LogStream implementation that forwards TDLib log messages to an ILoggerFactory.
/// </summary>
/// <remarks>
/// <para>
/// This class receives TDLib log messages directly via the native callback and forwards them
/// to .NET's ILoggerFactory-based logging system without any intermediate files.
/// </para>
/// <para>
/// Each log message is routed through a logger with a category based on the TDLib source file
/// (e.g., "TDLib.AuthData" for messages from AuthData.cpp).
/// </para>
/// </remarks>
public sealed class LoggerLogStream : ILogStream
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger _fatalErrorLogger;
    private bool _disposed;

    /// <summary>
    /// Regex pattern to extract source file from TDLib log messages.
    /// Matches patterns like [AuthData.cpp:122] or [Td.cpp:1346]
    /// </summary>
    private static readonly Regex SourceFilePattern = new(
        @"\[([A-Za-z0-9_]+)\.cpp:\d+\]",
        RegexOptions.Compiled);

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggerLogStream"/> class.
    /// </summary>
    /// <param name="loggerFactory">The ILoggerFactory to use for creating loggers.</param>
    /// <exception cref="ArgumentNullException">Thrown when loggerFactory is null.</exception>
    public LoggerLogStream(ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);
        _loggerFactory = loggerFactory;
        _fatalErrorLogger = loggerFactory.CreateLogger("TDLib.FatalError");
    }

    /// <inheritdoc />
    public void OnLogMessage(TdLogLevel verbosityLevel, string message)
    {
        if (_disposed || string.IsNullOrEmpty(message))
        {
            return;
        }

        try
        {
            var category = ExtractLoggerCategory(message);
            var logger = _loggerFactory.CreateLogger(category);
            var logLevel = verbosityLevel.ToLogLevel();

            logger.Log(logLevel, "{Message}", message);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in LoggerLogStream.OnLogMessage: {ex}");
        }
    }

    /// <inheritdoc />
    public void OnFatalError(string message)
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            _fatalErrorLogger.LogCritical("{Message}", message);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in LoggerLogStream.OnFatalError: {ex}");
        }
    }

    /// <summary>
    /// Extracts the source file name from a TDLib log message to use as logger category.
    /// </summary>
    /// <param name="message">The raw TDLib log message</param>
    /// <returns>Logger category like "TDLib.AuthData" or "TDLib" if not found</returns>
    internal static string ExtractLoggerCategory(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return "TDLib";
        }

        var match = SourceFilePattern.Match(message);
        if (match.Success)
        {
            var sourceFile = match.Groups[1].Value;
            return $"TDLib.{sourceFile}";
        }

        return "TDLib";
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _disposed = true;
    }
}
