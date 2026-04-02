// SPDX-FileCopyrightText: 2024 tdsharp contributors <https://github.com/egramtel/tdsharp>
//
// SPDX-License-Identifier: MIT

namespace bielu.tdsharp.aspnetcore.logger;

/// <summary>
/// A holder that wraps an inner <see cref="IDisposable"/> logging scope.
/// Used by <see cref="TdLoggerExtensions.CreateTdLibLoggingAction"/> to provide
/// an <see cref="IDisposable"/> that is available immediately (as an out parameter)
/// but delegates disposal to the actual scope created when the action is invoked.
/// </summary>
internal sealed class LoggingScopeHolder : IDisposable
{
    /// <summary>
    /// Gets or sets the inner logging scope. Set when the configure action is invoked.
    /// </summary>
    internal IDisposable? Scope { get; set; }

    /// <inheritdoc />
    public void Dispose()
    {
        Scope?.Dispose();
    }
}
