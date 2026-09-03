using Integration.DevKit.Core.Logging;
using Microsoft.Extensions.Logging;

namespace Integration.DevKit.Core.Tests;

public class LoggerFactoryExtensionsTests
{
    private sealed class RecordingLogger : ILogger
    {
        public int LogCount { get; private set; }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            LogCount++;
        }
    }

    private sealed class RecordingLoggerFactory : ILoggerFactory
    {
        public RecordingLogger Logger { get; } = new();
        public int CreateLoggerCallCount { get; private set; }

        public void AddProvider(ILoggerProvider provider) { }

        public ILogger CreateLogger(string categoryName)
        {
            CreateLoggerCallCount++;
            return Logger;
        }

        public void Dispose() { }
    }

    [Fact]
    public void CreateConditionalLogger_NullFactory_ReturnsNull()
    {
        ILoggerFactory? factory = null;

        var logger = factory.CreateConditionalLogger("Category", () => true);

        Assert.Null(logger);
    }

    [Fact]
    public void CreateConditionalLogger_EnabledPredicateFalse_SuppressesLogging()
    {
        var factory = new RecordingLoggerFactory();
        var enabled = true;

        var logger = factory.CreateConditionalLogger("Category", () => enabled)!;

        logger.LogInformation("first");
        enabled = false;
        logger.LogInformation("second");

        Assert.Equal(1, factory.Logger.LogCount);
    }

    [Fact]
    public void CreateConditionalLogger_TogglingBackOn_ResumesLoggingWithoutRecreatingLogger()
    {
        var factory = new RecordingLoggerFactory();
        var enabled = true;

        var logger = factory.CreateConditionalLogger("Category", () => enabled)!;

        logger.LogInformation("first");
        enabled = false;
        logger.LogInformation("suppressed");
        enabled = true;
        logger.LogInformation("resumed");

        Assert.Equal(2, factory.Logger.LogCount);
        Assert.Equal(1, factory.CreateLoggerCallCount);
    }
}
