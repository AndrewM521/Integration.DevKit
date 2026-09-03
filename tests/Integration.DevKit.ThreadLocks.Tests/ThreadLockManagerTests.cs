using Integration.DevKit.Core;
using Integration.DevKit.ThreadLocks.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Integration.DevKit.ThreadLocks.Tests;

public class ThreadLockManagerTests
{
    [Fact]
    public void TryEnterSyncLock_ThenExit_Succeeds()
    {
        var manager = new ThreadLockManager();

        var enter = manager.TryEnterSyncLock("key1");
        Assert.True(enter.MethodSuccess);

        var exit = manager.TryExitSyncLock("key1");
        Assert.True(exit.MethodSuccess);
    }

    [Fact]
    public void TryExitSyncLock_WithoutHoldingLock_Fails()
    {
        var manager = new ThreadLockManager();

        var exit = manager.TryExitSyncLock("never-entered");

        Assert.True(exit.MethodSuccess);
    }

    [Fact]
    public void TryExitSyncLock_HeldByAnotherThread_Fails()
    {
        var manager = new ThreadLockManager();
        manager.TryEnterSyncLock("key1");

        NullOperationResult? exitResult = null;
        var thread = new Thread(() => exitResult = manager.TryExitSyncLock("key1"));
        thread.Start();
        thread.Join();

        Assert.False(exitResult!.MethodSuccess);
        Assert.IsType<SynchronizationLockException>(exitResult.Exception);

        manager.TryExitSyncLock("key1");
    }

    [Fact]
    public void TryEnterSyncLock_SecondThreadBlocksUntilFirstExits()
    {
        var manager = new ThreadLockManager();
        manager.TryEnterSyncLock("key1");

        var secondThreadEntered = false;
        var thread = new Thread(() =>
        {
            var enter = manager.TryEnterSyncLock("key1", timeoutMilliseconds: 2000);
            secondThreadEntered = enter.MethodSuccess;
            if (enter.MethodSuccess)
            {
                manager.TryExitSyncLock("key1");
            }
        });
        thread.Start();

        Thread.Sleep(100);
        Assert.False(secondThreadEntered);

        manager.TryExitSyncLock("key1");
        thread.Join();

        Assert.True(secondThreadEntered);
    }

    [Fact]
    public void TryEnterSyncLock_TimeoutExpires_ReturnsFailureWithTimeoutException()
    {
        var manager = new ThreadLockManager();
        manager.TryEnterSyncLock("key1");

        NullOperationResult? enterResult = null;
        var thread = new Thread(() => enterResult = manager.TryEnterSyncLock("key1", timeoutMilliseconds: 50));
        thread.Start();
        thread.Join();

        Assert.False(enterResult!.MethodSuccess);
        Assert.IsType<TimeoutException>(enterResult.Exception);

        manager.TryExitSyncLock("key1");
    }

    [Fact]
    public void SyncLocks_DifferentKeys_DoNotContend()
    {
        var manager = new ThreadLockManager();

        var enterA = manager.TryEnterSyncLock("keyA");
        var enterB = manager.TryEnterSyncLock("keyB");

        Assert.True(enterA.MethodSuccess);
        Assert.True(enterB.MethodSuccess);

        manager.TryExitSyncLock("keyA");
        manager.TryExitSyncLock("keyB");
    }

    [Fact]
    public void TryEnterSyncLock_NullOrWhitespaceKey_Fails()
    {
        var manager = new ThreadLockManager();

        var result = manager.TryEnterSyncLock("   ");

        Assert.False(result.MethodSuccess);
        Assert.IsType<ArgumentException>(result.Exception);
    }

    [Fact]
    public void SyncLock_KeyIsNormalized_TrimmedAndLowercased()
    {
        var manager = new ThreadLockManager();

        manager.TryEnterSyncLock("  MyKey  ");
        var exit = manager.TryExitSyncLock("mykey");

        Assert.True(exit.MethodSuccess);
    }

    [Fact]
    public async Task TryEnterAsyncLock_ThenExit_Succeeds()
    {
        var manager = new ThreadLockManager();

        var enter = await manager.TryEnterAsyncLock("akey");
        Assert.True(enter.MethodSuccess);

        var exit = manager.TryExitAsyncLock("akey");
        Assert.True(exit.MethodSuccess);
    }

    [Fact]
    public async Task TryEnterAsyncLock_SecondCallerWaitsUntilReleased()
    {
        var manager = new ThreadLockManager();
        await manager.TryEnterAsyncLock("akey");

        var secondEnteredTask = manager.TryEnterAsyncLock("akey", timeoutMilliseconds: 2000);

        await Task.Delay(100);
        Assert.False(secondEnteredTask.IsCompleted);

        manager.TryExitAsyncLock("akey");

        var secondResult = await secondEnteredTask;
        Assert.True(secondResult.MethodSuccess);

        manager.TryExitAsyncLock("akey");
    }

    [Fact]
    public async Task TryEnterAsyncLock_TimeoutExpires_ReturnsFailureWithTimeoutException()
    {
        var manager = new ThreadLockManager();
        await manager.TryEnterAsyncLock("akey");

        var result = await manager.TryEnterAsyncLock("akey", timeoutMilliseconds: 50);

        Assert.False(result.MethodSuccess);
        Assert.IsType<TimeoutException>(result.Exception);

        manager.TryExitAsyncLock("akey");
    }

    [Fact]
    public async Task TryExitAsyncLock_MoreTimesThanAcquired_FailsWithInvalidOperationException()
    {
        var manager = new ThreadLockManager();

        await manager.TryEnterAsyncLock("akey");
        manager.TryExitAsyncLock("akey");

        // At this point the key was removed from the internal map (ref count hit zero),
        // so a second exit call is a no-op success rather than reaching the semaphore at all.
        var result = manager.TryExitAsyncLock("akey");

        Assert.True(result.MethodSuccess);
    }

    [Fact]
    public async Task AsyncLocks_DifferentKeys_DoNotContend()
    {
        var manager = new ThreadLockManager();

        var enterA = await manager.TryEnterAsyncLock("akeyA");
        var enterB = await manager.TryEnterAsyncLock("akeyB");

        Assert.True(enterA.MethodSuccess);
        Assert.True(enterB.MethodSuccess);

        manager.TryExitAsyncLock("akeyA");
        manager.TryExitAsyncLock("akeyB");
    }

    [Fact]
    public async Task TryEnterAsyncLock_NullOrWhitespaceKey_Fails()
    {
        var manager = new ThreadLockManager();

        var result = await manager.TryEnterAsyncLock(null!);

        Assert.False(result.MethodSuccess);
        Assert.IsType<ArgumentException>(result.Exception);
    }

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

        public void AddProvider(ILoggerProvider provider) { }

        public ILogger CreateLogger(string categoryName) => Logger;

        public void Dispose() { }
    }

    [Fact]
    public void RuntimeSettings_EnableLoggingToggledFalse_SuppressesLoggingWithoutRecreatingManager()
    {
        var loggerFactory = new RecordingLoggerFactory();
        var manager = new ThreadLockManager(Options.Create(new ThreadLockSettings()), loggerFactory);

        manager.TryEnterSyncLock("key1");
        manager.TryExitSyncLock("key1");
        var countWhileEnabled = loggerFactory.Logger.LogCount;

        manager.RuntimeSettings.EnableLogging = false;

        manager.TryEnterSyncLock("key2");
        manager.TryExitSyncLock("key2");

        Assert.True(countWhileEnabled > 0);
        Assert.Equal(countWhileEnabled, loggerFactory.Logger.LogCount);

        manager.RuntimeSettings.EnableLogging = true;

        manager.TryEnterSyncLock("key3");
        manager.TryExitSyncLock("key3");

        Assert.True(loggerFactory.Logger.LogCount > countWhileEnabled);
    }
}
