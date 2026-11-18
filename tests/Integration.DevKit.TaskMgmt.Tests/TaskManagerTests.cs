using System.Collections.Concurrent;
using Integration.DevKit.TaskMgmt.Abstractions;
using Integration.DevKit.TaskMgmt.Implementations;
using Integration.DevKit.TaskMgmt.Models;
using Integration.DevKit.TaskMgmt.Settings;
using Integration.DevKit.TaskMgmt.Tests.TestSupport;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Integration.DevKit.TaskMgmt.Tests;

public class TaskManagerTests
{
    private static TaskManager CreateManager(TaskManagerSettings? settings = null)
    {
        var lifetime = new Mock<IHostApplicationLifetime>();
        lifetime.SetupGet(l => l.ApplicationStopping).Returns(CancellationToken.None);

        return new TaskManager(lifetime.Object, new TaskRegistry(), Options.Create(settings ?? new TaskManagerSettings()));
    }

    private static async Task WaitUntilAsync(Func<bool> condition, int timeoutMs = 2000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < deadline)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(10);
        }

        Assert.True(condition(), "Timed out waiting for condition.");
    }

    [Fact]
    public void Constructor_NullAppLifetime_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new TaskManager(null!, new TaskRegistry(), Options.Create(new TaskManagerSettings())));
    }

    [Fact]
    public void Constructor_NullTaskRegistry_Throws()
    {
        var lifetime = new Mock<IHostApplicationLifetime>();
        lifetime.SetupGet(l => l.ApplicationStopping).Returns(CancellationToken.None);

        Assert.Throws<ArgumentNullException>(() =>
            new TaskManager(lifetime.Object, null!, Options.Create(new TaskManagerSettings())));
    }

    [Fact]
    public void Initialize_NegativeSettings_CoercedToIntMaxValue()
    {
        var manager = CreateManager(new TaskManagerSettings
        {
            MaxConcurrentTasks = -1,
            MaxTaskRegistryCount = -1,
            MaxTaskIterationRegistryCount = -1
        });

        Assert.Equal(int.MaxValue, manager.RuntimeSettings.MaxConcurrentTasks);
        Assert.Equal(int.MaxValue, manager.RuntimeSettings.MaxTaskRegistryCount);
        Assert.Equal(int.MaxValue, manager.RuntimeSettings.MaxTaskIterationRegistryCount);
    }

    [Fact]
    public async Task StartTask_Synchronous_RunsWorkAndReturnsHandle()
    {
        var manager = CreateManager();
        var task = new FakeManagedTask("sync-task");

        var result = await manager.StartTask(task, TaskExecutionMode.Syncronous, new ManagedTaskSettings { MaxIterations = 1 });

        Assert.True(result.MethodSuccess);
        Assert.Equal(task.TaskKey, result.Result.TaskKey);
        Assert.Equal(1, task.RunCount);
    }

    [Fact]
    public async Task StartTask_DuplicateKey_FailsWithInvalidOperationException()
    {
        var manager = CreateManager();
        var task = new FakeManagedTask("dup-task");
        task.Work = async handle =>
        {
            try { await Task.Delay(Timeout.Infinite, handle.CancelationToken); }
            catch (OperationCanceledException) { }
        };

        var first = await manager.StartTask(task, TaskExecutionMode.Asyncronous, new ManagedTaskSettings());
        Assert.True(first.MethodSuccess);

        var second = await manager.StartTask(task, TaskExecutionMode.Asyncronous, new ManagedTaskSettings());

        Assert.False(second.MethodSuccess);
        Assert.IsType<InvalidOperationException>(second.Exception);

        // The rejected start used to remove the running task's entry, so it could no longer be found or canceled
        Assert.True(manager.IsTaskRunning(task.TaskKey).Result);
        Assert.True(manager.CancelTask(task.TaskKey).MethodSuccess);

        var finished = await Task.WhenAny(first.Result.RunningTask!, Task.Delay(5000));
        Assert.Same(first.Result.RunningTask, finished);
    }

    [Fact]
    public async Task IsTaskRunning_ReflectsRunningState()
    {
        var manager = CreateManager();
        var task = new FakeManagedTask("running-task");
        task.Work = async handle =>
        {
            try { await Task.Delay(Timeout.Infinite, handle.CancelationToken); }
            catch (OperationCanceledException) { }
        };

        Assert.False(manager.IsTaskRunning(task.TaskKey).Result);

        var startResult = await manager.StartTask(task, TaskExecutionMode.Asyncronous, new ManagedTaskSettings());
        Assert.True(startResult.MethodSuccess);

        await WaitUntilAsync(() => manager.IsTaskRunning(task.TaskKey).Result);
        Assert.Contains(task.TaskKey, manager.GetAllRunningTaskKeys());

        manager.CancelTask(task.TaskKey);
        await startResult.Result.RunningTask!;

        Assert.False(manager.IsTaskRunning(task.TaskKey).Result);
        Assert.DoesNotContain(task.TaskKey, manager.GetAllRunningTaskKeys());
    }

    [Fact]
    public async Task CancelTask_StopsTheRunningIteration()
    {
        var manager = CreateManager();
        var task = new FakeManagedTask("cancel-task");
        task.Work = async handle =>
        {
            try { await Task.Delay(Timeout.Infinite, handle.CancelationToken); }
            catch (OperationCanceledException) { }
        };

        var startResult = await manager.StartTask(task, TaskExecutionMode.Asyncronous, new ManagedTaskSettings());
        await WaitUntilAsync(() => task.RunCount > 0);

        var cancelResult = manager.CancelTask(task.TaskKey);
        Assert.True(cancelResult.MethodSuccess);

        await startResult.Result.RunningTask!;

        Assert.False(manager.IsTaskRunning(task.TaskKey).Result);
        Assert.Equal(1, task.RunCount);
    }

    [Fact]
    public async Task CancelAllTasks_StopsEveryRunningTask()
    {
        var manager = CreateManager();
        var task1 = new FakeManagedTask("cancel-all-1");
        var task2 = new FakeManagedTask("cancel-all-2");

        Func<ManagedTaskIterationHandle, Task> longRunningWork = async handle =>
        {
            try { await Task.Delay(Timeout.Infinite, handle.CancelationToken); }
            catch (OperationCanceledException) { }
        };
        task1.Work = longRunningWork;
        task2.Work = longRunningWork;

        var start1 = await manager.StartTask(task1, TaskExecutionMode.Asyncronous, new ManagedTaskSettings());
        var start2 = await manager.StartTask(task2, TaskExecutionMode.Asyncronous, new ManagedTaskSettings());

        await WaitUntilAsync(() => manager.IsTaskRunning(task1.TaskKey).Result && manager.IsTaskRunning(task2.TaskKey).Result);

        var cancelAllResult = manager.CancelAllTasks();
        Assert.True(cancelAllResult.MethodSuccess);

        await Task.WhenAll(start1.Result.RunningTask!, start2.Result.RunningTask!);

        Assert.False(manager.IsTaskRunning(task1.TaskKey).Result);
        Assert.False(manager.IsTaskRunning(task2.TaskKey).Result);
    }

    [Fact]
    public void CancelTask_UnknownKey_StillSucceeds()
    {
        var manager = CreateManager();

        var result = manager.CancelTask("does-not-exist");

        Assert.True(result.MethodSuccess);
    }

    [Fact]
    public void CancelTask_NullOrWhitespaceKey_Fails()
    {
        var manager = CreateManager();

        var result = manager.CancelTask("   ");

        Assert.False(result.MethodSuccess);
        Assert.IsType<ArgumentException>(result.Exception);
    }

    [Fact]
    public void IsTaskRunning_UnknownKey_ReturnsFalse()
    {
        var manager = CreateManager();

        var result = manager.IsTaskRunning("does-not-exist");

        Assert.True(result.MethodSuccess);
        Assert.False(result.Result);
    }

    [Fact]
    public async Task StartTask_WorkThrowsSynchronously_IterationIsRecordedAsFaulted()
    {
        var lifetime = new Mock<IHostApplicationLifetime>();
        lifetime.SetupGet(l => l.ApplicationStopping).Returns(CancellationToken.None);
        var registry = new TaskRegistry();
        var manager = new TaskManager(lifetime.Object, registry, Options.Create(new TaskManagerSettings()));

        var task = new SyncThrowingTask("sync-throw");
        await manager.StartTask(task, TaskExecutionMode.Syncronous, new ManagedTaskSettings { MaxIterations = 1 });

        var snapshot = registry.Snapshots[task.TaskKey];
        Assert.Equal(ManagedTaskState.Faulted, snapshot.State);
        var iteration = Assert.Single(snapshot.GetIterationHistory());
        Assert.Equal(ManagedTaskState.Faulted, iteration.State);
        Assert.IsType<InvalidOperationException>(iteration.Exception);
    }

    private sealed class SyncThrowingTask : ManagedTask
    {
        public SyncThrowingTask(string taskName) : base(taskName)
        {
        }

        // Not async on purpose: the exception escapes before a Task is returned.
        public override Task DoTaskWork(ManagedTaskIterationHandle iterationHandle)
        {
            throw new InvalidOperationException("boom");
        }
    }

    [Fact]
    public async Task ZeroConcurrencyLimits_AreTreatedAsUnlimited()
    {
        // A semaphore with zero slots used to make the task wait forever
        var manager = CreateManager(new TaskManagerSettings { MaxConcurrentTasks = 0 });
        var task = new FakeManagedTask("zero-limits");

        var run = manager.StartTask(task, TaskExecutionMode.Syncronous, new ManagedTaskSettings { MaxIterations = 1, MaxConcurrentParallelTasks = 0 });
        var finished = await Task.WhenAny(run, Task.Delay(5000));

        Assert.Same(run, finished);
        Assert.Equal(1, task.RunCount);
    }

    private static (TaskManager Manager, TaskRegistry Registry) CreateManagerWithRegistry(TaskManagerSettings settings)
    {
        var lifetime = new Mock<IHostApplicationLifetime>();
        lifetime.SetupGet(l => l.ApplicationStopping).Returns(CancellationToken.None);
        var registry = new TaskRegistry();

        return (new TaskManager(lifetime.Object, registry, Options.Create(settings)), registry);
    }

    [Fact]
    public async Task RegistryOverLimit_DropsOnlyTheOldestSnapshot()
    {
        // The count wasn't re-read in the trim loop, so going one over the limit removed every snapshot
        var (manager, registry) = CreateManagerWithRegistry(new TaskManagerSettings { MaxTaskRegistryCount = 2 });
        var tasks = new[] { new FakeManagedTask("reg-1"), new FakeManagedTask("reg-2"), new FakeManagedTask("reg-3") };

        foreach (var task in tasks)
        {
            await manager.StartTask(task, TaskExecutionMode.Syncronous, new ManagedTaskSettings { MaxIterations = 1 });
        }

        Assert.Equal(2, registry.Snapshots.Count);
        Assert.False(registry.Snapshots.ContainsKey(tasks[0].TaskKey));
        Assert.True(registry.Snapshots.ContainsKey(tasks[2].TaskKey));
    }

    [Fact]
    public async Task RepeatedFailures_KeepOnlyAsManyExceptionsAsTheIterationHistory()
    {
        var (manager, registry) = CreateManagerWithRegistry(new TaskManagerSettings { MaxTaskIterationRegistryCount = 2 });
        var task = new FakeManagedTask("many-faults");
        task.Work = _ => throw new InvalidOperationException("boom");

        await manager.StartTask(task, TaskExecutionMode.Syncronous, new ManagedTaskSettings { MaxIterations = 5, StopIteratingOnException = false });

        var snapshot = registry.Snapshots[task.TaskKey];
        Assert.Equal(5, task.RunCount);
        Assert.Equal(ManagedTaskState.Faulted, snapshot.State);
        Assert.Equal(2, Assert.IsType<AggregateException>(snapshot.Exception).InnerExceptions.Count);
    }

    [Fact]
    public async Task Retry_WaitsBeforeTheNextAttempt()
    {
        // Retries used to run back to back about every 50 ms
        var manager = CreateManager();
        var task = new FakeManagedTask("retry-wait");
        var attempts = new List<DateTime>();
        task.Work = _ =>
        {
            attempts.Add(DateTime.UtcNow);
            throw new InvalidOperationException("boom");
        };

        await manager.StartTask(task, TaskExecutionMode.Syncronous, new ManagedTaskSettings { MaxIterations = 1, RetryOnException = true, MaxRetryCount = 1 });

        Assert.Equal(2, attempts.Count);
        Assert.True(attempts[1] - attempts[0] >= TimeSpan.FromMilliseconds(900));
    }

    [Fact]
    public async Task ParallelWithoutIterationLimit_ReturnsAtOnceAndRespectsTheConcurrencyLimit()
    {
        // The iteration loop never waited for a slot, so StartTask spun forever on the caller's thread and queued iterations without end
        var manager = CreateManager();
        int running = 0;
        int maxSeen = 0;
        var task = new FakeManagedTask("parallel-forever")
        {
            Work = async handle =>
            {
                int now = Interlocked.Increment(ref running);
                int seen;
                do
                {
                    seen = maxSeen;
                }
                while (now > seen && Interlocked.CompareExchange(ref maxSeen, now, seen) != seen);

                try
                {
                    await Task.Delay(50, handle.CancelationToken);
                }
                finally
                {
                    Interlocked.Decrement(ref running);
                }
            }
        };

        var start = Task.Run(() => manager.StartTask(task, TaskExecutionMode.Asyncronous, new ManagedTaskSettings
        {
            MaxIterations = -1,
            IterationExecutionMode = ManagedTaskExecutionMode.Parallel,
            MaxConcurrentParallelTasks = 2
        }));

        var first = await Task.WhenAny(start, Task.Delay(TimeSpan.FromSeconds(2)));
        Assert.True(first == start, "StartTask did not return");
        var handle = (await start).Result;

        await Task.Delay(500);
        handle.Cancel();
        await handle.RunningTask!;

        Assert.True(task.RunCount >= 2, $"Only {task.RunCount} iterations ran");
        Assert.True(maxSeen <= 2, $"{maxSeen} iterations ran at once");
    }

    [Fact]
    public async Task ParallelWithNothingToMakeTheLoopWait_IsRejected()
    {
        var manager = CreateManager();

        var result = await manager.StartTask(new FakeManagedTask("unbounded"), TaskExecutionMode.Asyncronous, new ManagedTaskSettings
        {
            MaxIterations = -1,
            IterationExecutionMode = ManagedTaskExecutionMode.Parallel,
            MaxConcurrentParallelTasks = 0
        });

        Assert.False(result.MethodSuccess);
        Assert.IsType<ArgumentException>(result.Exception);
    }

    [Fact]
    public async Task IterationTimeout_CancelsWorkThatRunsTooLong()
    {
        var (manager, registry) = CreateManagerWithRegistry(new TaskManagerSettings());
        var task = new FakeManagedTask("timeout")
        {
            Work = handle => Task.Delay(TimeSpan.FromSeconds(10), handle.CancelationToken)
        };

        var timer = System.Diagnostics.Stopwatch.StartNew();
        await manager.StartTask(task, TaskExecutionMode.Syncronous, new ManagedTaskSettings { MaxIterations = 1, Timeout = TimeSpan.FromMilliseconds(200) });
        timer.Stop();

        Assert.True(timer.Elapsed < TimeSpan.FromSeconds(3), $"Run took {timer.Elapsed}");
        var iteration = Assert.Single(registry.Snapshots[task.TaskKey].GetIterationHistory());
        Assert.Equal(ManagedTaskState.Canceled, iteration.State);
    }

    [Fact]
    public async Task RerunOfAKey_StartsAFreshSnapshot()
    {
        // A second run used to keep the first run's settings and update its iteration records
        var (manager, registry) = CreateManagerWithRegistry(new TaskManagerSettings());
        var task = new FakeManagedTask("rerun");

        await manager.StartTask(task, TaskExecutionMode.Syncronous, new ManagedTaskSettings { MaxIterations = 2 });
        var secondStart = DateTime.UtcNow;
        await manager.StartTask(task, TaskExecutionMode.Syncronous, new ManagedTaskSettings { MaxIterations = 1 });

        var snapshot = registry.Snapshots[task.TaskKey];
        Assert.Equal(1, snapshot.Settings!.MaxIterations);
        var iteration = Assert.Single(snapshot.GetIterationHistory());
        Assert.True(iteration.StartDTM >= secondStart);
    }

    [Fact]
    public async Task SharedTimeStrategy_SecondRunningTaskIsRejected()
    {
        // Both tasks would move the same LastTargetDTM, so each would skip every other slot
        var manager = CreateManager();
        var strategy = new TimeStrategy_Interval(TimeSpan.FromHours(1), new TimeStrategySettings());
        var first = new FakeManagedTask("shared-1");
        var second = new FakeManagedTask("shared-2");

        var start1 = await manager.StartTask(first, TaskExecutionMode.Asyncronous, new ManagedTaskSettings { MaxIterations = -1, IterationStrategy = strategy });
        var start2 = await manager.StartTask(second, TaskExecutionMode.Asyncronous, new ManagedTaskSettings { MaxIterations = -1, IterationStrategy = strategy });

        Assert.True(start1.MethodSuccess);
        Assert.False(start2.MethodSuccess);
        Assert.IsType<InvalidOperationException>(start2.Exception);
        Assert.True(manager.IsTaskRunning(first.TaskKey).Result);

        manager.CancelTask(first.TaskKey);
        await start1.Result.RunningTask!;
    }

    [Fact]
    public async Task Cancel_WorkThatStopsWithinASecond_LogsNoStillRunningWarning()
    {
        // The watchdog's grace delay never ran, so every cancel logged "running longer than 1 second"
        var logs = new CapturingLoggerFactory();
        var lifetime = new Mock<IHostApplicationLifetime>();
        lifetime.SetupGet(l => l.ApplicationStopping).Returns(CancellationToken.None);
        var manager = new TaskManager(lifetime.Object, new TaskRegistry(), Options.Create(new TaskManagerSettings()), logs);
        var task = new FakeManagedTask("graceful-cancel");
        task.Work = async handle =>
        {
            try { await Task.Delay(Timeout.Infinite, handle.CancelationToken); }
            catch (OperationCanceledException) { await Task.Delay(200); }
        };

        var start = await manager.StartTask(task, TaskExecutionMode.Asyncronous, new ManagedTaskSettings());
        await WaitUntilAsync(() => task.RunCount > 0);
        manager.CancelTask(task.TaskKey);
        await start.Result.RunningTask!;

        Assert.DoesNotContain(logs.Messages, m => m.Contains("after being canceled"));
    }

    [Fact]
    public async Task ParallelIterations_AllRunAndTheTaskCompletes()
    {
        var (manager, registry) = CreateManagerWithRegistry(new TaskManagerSettings());
        var task = new FakeManagedTask("parallel");
        task.Work = _ => Task.Delay(50);

        await manager.StartTask(task, TaskExecutionMode.Syncronous, new ManagedTaskSettings { MaxIterations = 3, IterationExecutionMode = ManagedTaskExecutionMode.Parallel });

        Assert.Equal(3, task.RunCount);
        Assert.Equal(ManagedTaskState.Completed, registry.Snapshots[task.TaskKey].State);
    }

    private sealed class CapturingLoggerFactory : ILoggerFactory, ILogger
    {
        public ConcurrentQueue<string> Messages { get; } = new ConcurrentQueue<string>();

        public ILogger CreateLogger(string categoryName)
        {
            return this;
        }

        public void AddProvider(ILoggerProvider provider)
        {
        }

        public void Dispose()
        {
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Messages.Enqueue(formatter(state, exception));
        }
    }
}
