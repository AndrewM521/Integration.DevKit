/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using Microsoft.Extensions.Logging;

namespace Integration.DevKit.Core.Logging;

/// <summary>
/// Extension methods for creating loggers whose logging can be toggled at runtime.
/// </summary>
public static class LoggerFactoryExtensions
{
    /// <summary>
    /// Creates a logger for <paramref name="categoryName"/> whose <see cref="ILogger.IsEnabled(LogLevel)"/>
    /// also checks <paramref name="isEnabled"/> on every call. This lets a module keep a single logger
    /// instance for its lifetime while still being able to turn its logging on/off at runtime (e.g. via a
    /// mutable settings flag read by <paramref name="isEnabled"/>), instead of only being able to decide
    /// once at construction time whether to create a logger at all.
    /// </summary>
    /// <param name="factory">The factory to create the underlying logger from. If null, no logger is created.</param>
    /// <param name="categoryName">The category name for messages produced by the logger.</param>
    /// <param name="isEnabled">Invoked on every logging call to decide whether it should be written.</param>
    /// <returns>A conditional logger, or null if <paramref name="factory"/> is null.</returns>
    public static ILogger? CreateConditionalLogger(this ILoggerFactory? factory, string categoryName, Func<bool> isEnabled)
    {
        return factory == null ? null : new ConditionalLogger(factory.CreateLogger(categoryName), isEnabled);
    }
}
