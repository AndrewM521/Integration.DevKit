/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using Microsoft.Extensions.Logging;

namespace Integration.DevKit.Core.Logging;

/// <summary>
/// An <see cref="ILogger"/> decorator that re-evaluates a caller-supplied predicate on every call,
/// letting a module's logging be turned on/off at runtime without recreating or discarding the
/// underlying logger.
/// </summary>
internal sealed class ConditionalLogger : ILogger
{
    private readonly ILogger _inner;
    private readonly Func<bool> _isEnabled;

    public ConditionalLogger(ILogger inner, Func<bool> isEnabled)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _isEnabled = isEnabled ?? throw new ArgumentNullException(nameof(isEnabled));
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => _inner.BeginScope(state);

    public bool IsEnabled(LogLevel logLevel) => _isEnabled() && _inner.IsEnabled(logLevel);

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (IsEnabled(logLevel))
        {
            _inner.Log(logLevel, eventId, state, exception, formatter);
        }
    }
}
