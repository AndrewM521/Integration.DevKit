# Task Management

`Integration.DevKit.TaskMgmt` runs recurring or long-running background work as **managed tasks**: you subclass an abstract base class, hand an instance to a task manager, and get back a handle for observing and cancelling it. The manager tracks retries, iteration counts, timeouts, and keeps a history of recent runs that you can query for observability.

> If you've seen an earlier version of this document, note that the task body API described here (subclassing `ManagedTask`) replaces an earlier example that referenced non-existent `ManagedTask`/`SimpleTask` constructors. The API below is verified directly against source and the working samples in `samples/TestApp`.

## Requirements

- .NET 8
- `Microsoft.Extensions.Hosting.Abstractions` (for `IHostApplicationLifetime`, used internally to stop tasks on shutdown)

## Installation

```bash
dotnet add reference src/Integration.DevKit.TaskMgmt/Integration.DevKit.TaskMgmt.csproj
```

Or from NuGet: [Integration.DevKit.TaskMgmt](https://www.nuget.org/packages/Integration.DevKit.TaskMgmt)

## Getting started

### 1. Define a task by subclassing `ManagedTask`

A task's body is **not** a delegate or an interface implementation — it's an abstract class you derive from and override one method on:

```csharp
using Integration.DevKit.TaskMgmt.Contracts;

internal class SendReportTask : ManagedTask
{
    public SendReportTask() : base("SendReportTask") { }

    public override async Task DoTaskWork(IManagedTaskIterationHandle iterationHandle)
    {
        // Do the work for one iteration here.
        // Use iterationHandle.CancelationToken to respect cancellation/timeouts.
        await Task.Delay(TimeSpan.FromSeconds(1), iterationHandle.CancelationToken);
    }
}
```

`DoTaskWork` runs once per iteration; the manager calls it again (subject to `MaxIterations`, retry settings, and any configured iteration-timing strategy) until the task completes, is cancelled, or exhausts its iteration/retry budget.

### 2. Register and initialize the module

```csharp
using Integration.DevKit.TaskMgmt;

services.AddTaskMgmt(configuration);
// ... build the host ...
Service_TaskMgmt.Initialize(app.Services);
```

### 3. Start the task

```csharp
using Integration.DevKit.TaskMgmt.Contracts;

var settings = new ManagedTaskSettings { MaxIterations = 5 };

var started = await Service_TaskMgmt.TaskManager.StartTask(
    new SendReportTask(),
    TaskExecutionMode.Asyncronous,   // note: this spelling is exact in the shipped API
    settings);

if (!started.MethodSuccess)
{
    // e.g. a task with the same TaskKey is already running
    logger.LogError(started.Exception, "Failed to start task");
    return;
}

var handle = started.Result;
await handle.RunningTask!;   // await the whole task's lifetime (Asyncronous mode)
```

## Configuration

`AddTaskMgmt` binds the config section `Integration.DevKit:TaskManagement` to `TaskManagerSettings` — this configures the manager as a whole, not any individual task:

```json
{
  "Integration.DevKit": {
    "TaskManagement": {
      "MaxConcurrentTasks": 2147483647,
      "MaxTaskRegistryCount": 2000,
      "MaxTaskIterationRegistryCount": 100
    }
  }
}
```

| Property | Default | Purpose |
| --- | --- | --- |
| `MaxConcurrentTasks` | `int.MaxValue` | Upper bound on tasks running at once across the manager. |
| `MaxTaskRegistryCount` | `2000` | How many completed task snapshots the registry retains before evicting the oldest (FIFO). |
| `MaxTaskIterationRegistryCount` | `100` | How many iteration snapshots are retained per task before older ones are evicted. |

Per-task behavior (retries, iteration limits, timing) is configured separately per call via `ManagedTaskSettings`, described next.

## `ManagedTaskSettings`

Passed to `StartTask` to control how that specific task instance runs:

| Property | Default | Notes |
| --- | --- | --- |
| `MaxIterations` | `1` | Values `<= 0` are normalized to `-1` (run forever, until cancelled). |
| `MaxRetryCount` | `1` | Values `<= 0` are normalized to `-1` (unlimited retries). |
| `RetryOnException` | `false` | Whether a failed iteration is retried. |
| `StopIteratingOnException` | `true` | Whether an exception ends the task entirely (after any retries) rather than continuing to the next iteration. |
| `StopIterationAfterMaxRetries` | `true` | Whether hitting `MaxRetryCount` stops the task rather than moving on. |
| `IterationExecutionMode` | `ManagedTaskExecutionMode.Sequential` | `Sequential` or `Parallel`. |
| `IterationStrategy` | `new BaseIterationStrategy()` | Governs the delay between iterations — see [Iteration strategies](#iteration-strategies) below. |
| `AllowParallelIterationExecution` | `false` | Allows more than one iteration of the same task to run concurrently. |
| `MaxConcurrentParallelTasks` | `2` | Cap when parallel iteration execution is enabled. |
| `Timeout` | `null` | Optional overall wall-clock timeout for the task; exceeding it cancels the task. |

## Iteration strategies

By default (`BaseIterationStrategy`), the manager starts the next iteration immediately with no delay. To run on a schedule, assign a `Time_IterationStrategy` subclass instead:

```csharp
using Integration.DevKit.TaskMgmt.Contracts;

var strategySettings = new TimeStrategySettings
{
    SkipFirstIterationWait = true,
    FastForwardToPresent = true
};

var settings = new ManagedTaskSettings
{
    MaxIterations = -1,
    IterationStrategy = new TimeStrategy_Interval(TimeSpan.FromMinutes(5), strategySettings)
};
```

Three concrete strategies ship out of the box, all constructed with a `TimeStrategySettings`:

- `TimeStrategy_Interval(TimeSpan interval, TimeStrategySettings settings)` — run every fixed interval.
- `TimeStrategy_Daily(TimeStrategySettings settings)` — once per day.
- `TimeStrategy_Hourly(TimeStrategySettings settings)` — once per hour.

`TimeStrategySettings` also lets you anchor a schedule to a specific starting point:

| Property | Default | Purpose |
| --- | --- | --- |
| `SkipFirstIterationWait` | `true` | Run the first iteration immediately instead of waiting one full interval first. |
| `FastForwardToPresent` | `true` | If the computed next run time is already in the past (e.g. after downtime), jump to now instead of running every missed interval back-to-back. |
| `CustomStartDate` | `null` | Anchor date for the schedule. |
| `CustomStartTime` | `null` | Anchor time-of-day for the schedule. |

## Starting and controlling tasks

```csharp
public interface ITaskManager
{
    TaskManagerSettings RuntimeSettings { get; }

    NullOperationResult Initialize();

    Task<OperationResult<IManagedTaskHandle>> StartTask(ManagedTask managedTask, TaskExecutionMode executionMode,
        ManagedTaskSettings settings, CancellationToken cancellationToken = default);

    NullOperationResult CancelTask(string taskKey);
    NullOperationResult CancelAllTasks();
    OperationResult<bool> IsTaskRunning(string taskKey);
    IEnumerable<string> GetAllRunningTaskKeys();
    Task AwaitAllTasksToFinish(List<Task> tasksList);
    void LogRuntimeSettings();
}
```

`StartTask` fails (as a failed `OperationResult`, not a thrown exception) if a task with the same `TaskKey` (`"{TaskName}_{TaskId}"`) is already running. `TaskExecutionMode` is `Asyncronous` or `Syncronous` (exact spelling as shipped) — `Asyncronous` returns a handle you can `await handle.RunningTask` on independently, while `Syncronous` runs the task to completion as part of the `StartTask` call itself.

### Re-initializing after mutating `RuntimeSettings`

`RuntimeSettings` is mutable in place, but `TaskManager` sizes its internal concurrent-task semaphore from `MaxConcurrentTasks` once, at construction. Changing `MaxConcurrentTasks` (or the two registry-count settings) on the `RuntimeSettings` object has no effect until you call `Initialize()`, which re-normalizes the settings and rebuilds the semaphore:

```csharp
Service_TaskMgmt.TaskManager.RuntimeSettings.MaxConcurrentTasks = 10;
Service_TaskMgmt.TaskManager.Initialize();
```

Tasks already waiting on the old semaphore when `Initialize()` runs will see it disposed out from under them — prefer calling this during a quiet period rather than under active load.

### `IManagedTaskHandle`

Returned by `StartTask`, and reachable from an iteration handle via `iterationHandle.TaskHandle`:

```csharp
public interface IManagedTaskHandle
{
    string TaskKey { get; }
    ManagedTaskState State { get; }
    Task? RunningTask { get; }        // the whole task's lifetime, not one iteration
    DateTime StartDTM { get; }
    DateTime EndDTM { get; }
    TimeSpan Runtime { get; }
    int CurrentIterationCount { get; }
    void Cancel();                    // cancels the task and all of its iterations
}
```

### `IManagedTaskIterationHandle`

Passed into `DoTaskWork` for the currently-running iteration:

```csharp
public interface IManagedTaskIterationHandle
{
    IManagedTaskHandle TaskHandle { get; }
    int IterationNumber { get; }
    DateTime StartDTM { get; }
    CancellationToken CancelationToken { get; }   // exact spelling as shipped — one 'l'
    TimeSpan Runtime { get; }
    bool IsRunning { get; }
    void Cancel();                                 // cancels only this iteration, not the whole task
}
```

`ManagedTaskState` is `Idle | Starting | Running | Completed | Canceled | Faulted | CancelRequested`.

> **Respect the cancellation token.** If `DoTaskWork` ignores `iterationHandle.CancelationToken` (e.g. calling `Task.Delay(1000)` instead of `Task.Delay(1000, iterationHandle.CancelationToken)`), the manager will still mark the task as cancelled at the handle/state level once cancellation or a timeout fires, but your code inside `DoTaskWork` keeps running in the background until it naturally finishes — cancellation becomes advisory rather than immediate. Always pass the token through to any awaited delay or I/O call inside `DoTaskWork`.

## Observability: task history

`ITaskRegistry` (available as `Service_TaskMgmt.TaskRegistry`) keeps a read-only history of task runs, separate from `ITaskManager`, which is for control:

```csharp
public interface ITaskRegistry
{
    ConcurrentDictionary<string, IManagedTaskSnapshot> Snapshots { get; }
    NullableOperationResult<IManagedTaskSnapshot?> TryGet(string taskKey);
    NullOperationResult Remove(string taskKey);
}
```

```csharp
var snapshot = Service_TaskMgmt.TaskRegistry.TryGet(handle.TaskKey);
if (snapshot.MethodSuccess && snapshot.Result != null)
{
    Console.WriteLine(snapshot.Result.GetSnapshotInfo(showIterations: true));
}
```

`IManagedTaskSnapshot` exposes `TaskKey`, `State`, `IterationCount`, `StartDTM`/`EndDTM`/`Runtime`, `Exception`, and a per-iteration `IterationHistory`; each `IManagedTaskIterationSnapshot` exposes the same shape (`State`, timing, `Exception`) for one iteration. Both are read-only, framework-constructed types — you retrieve them from the registry, you don't build them yourself.

## API Reference

### `Service_TaskMgmt` (static)

```csharp
public static IServiceCollection AddTaskMgmt(this IServiceCollection services, IConfiguration config);
public static void Initialize(IServiceProvider sp);
public static ITaskManager TaskManager { get; }    // throws InvalidOperationException before Initialize
public static ITaskRegistry TaskRegistry { get; }  // throws InvalidOperationException before Initialize
```

### `ManagedTask` (abstract)

```csharp
public abstract class ManagedTask : IDisposable
{
    public string TaskName { get; }
    public Guid TaskId { get; }
    public string TaskKey { get; }   // "{TaskName}_{TaskId}"

    protected ManagedTask(string taskName, Guid? id = null);
    public abstract Task DoTaskWork(IManagedTaskIterationHandle iterationHandle);
    public virtual void Dispose() { }
}
```

## Error Handling

`StartTask`, `CancelTask`, `CancelAllTasks`, and `IsTaskRunning` all return `OperationResult<T>`/`NullOperationResult` rather than throwing for expected failure conditions (task already running, task not found, etc.) — check `MethodSuccess` and inspect `Exception` before proceeding. An unhandled exception thrown from inside `DoTaskWork` is caught by the manager and reflected in the task's `ManagedTaskState`/snapshot rather than crashing the host process; whether that also stops the task depends on `StopIteratingOnException` and `RetryOnException`.

## Best Practices

- Always thread `iterationHandle.CancelationToken` through any `Task.Delay`/I/O call inside `DoTaskWork` so cancellation and timeouts actually take effect promptly.
- Make `DoTaskWork` idempotent where practical — a retried or restarted iteration should be safe to re-run.
- Use `ITaskRegistry` snapshots for status/health reporting rather than polling `IManagedTaskHandle` state from multiple places.
- Set `MaxIterations`/`Timeout` explicitly for anything that should not run forever by default.
- Keep `MaxTaskRegistryCount`/`MaxTaskIterationRegistryCount` in mind if you rely on history for auditing — older entries are evicted once the cap is reached.
