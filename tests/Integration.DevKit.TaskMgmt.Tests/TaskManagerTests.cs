using Integration.DevKit.TaskMgmt.Models;
using Integration.DevKit.TaskMgmt.Settings;
using Integration.DevKit.TaskMgmt.Tests.TestSupport;
using Microsoft.Extensions.Hosting;
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
        task.Work = _ => Task.Delay(200);

        var first = await manager.StartTask(task, TaskExecutionMode.Asyncronous, new ManagedTaskSettings());
        Assert.True(first.MethodSuccess);

        var second = await manager.StartTask(task, TaskExecutionMode.Asyncronous, new ManagedTaskSettings());

        Assert.False(second.MethodSuccess);
        Assert.IsType<InvalidOperationException>(second.Exception);

        // Let the still-running first task's background work finish naturally rather than
        // relying on CancelTask, since StartTask's failure path removes the (shared) task key
        // from the manager's internal registry even though the first task keeps running.
        await first.Result.RunningTask!;
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
    public async Task AwaitAllTasksToFinish_CompletesWhenAllProvidedTasksComplete()
    {
        var manager = CreateManager();
        var task1 = Task.Delay(10);
        var task2 = Task.Delay(20);

        await manager.AwaitAllTasksToFinish(new List<Task> { task1, task2 });

        Assert.True(task1.IsCompleted);
        Assert.True(task2.IsCompleted);
    }
}
