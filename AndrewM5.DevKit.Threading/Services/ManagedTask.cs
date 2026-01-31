using AndrewM5.DevKit.Core.Results;
using AndrewM5.DevKit.Logging.Abstractions;
using AndrewM5.DevKit.Threading.Abstractions;
using Microsoft.Extensions.Logging;
using System.Text;

namespace AndrewM5.DevKit.Threading.Services;

public abstract class ManagedTask : IDisposable
{
    public string TaskName => _taskName;
    public Guid TaskId => _taskID;
    public string TaskKey => _taskKey;
    public TimeSpan? Timeout { get; set; }
    public ManagedTaskState State
    {
        get => (ManagedTaskState)Volatile.Read(ref _state);
        internal set => Volatile.Write(ref _state, (int)value);
    }

    internal ITaskManager? TaskManager { get; private set; }
    internal IThreadLockManager? ThreadLockManager { get; private set; }
    internal ICustomLogger? Logger { get; private set; }
    
    internal readonly string _taskName;
    internal readonly Guid _taskID;
    internal readonly string _taskKey;
    internal bool _isLongRunningTask;
    
    internal Task? TaskToRun;
    internal CancellationTokenSource? _cancellationTokenSource;
    internal TaskCompletionSource<bool> _taskCompletionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
    //internal ManagedTask_Schedule? ScheduleAddon;

    internal TaskExecutionMode _executionMode = TaskExecutionMode.Syncronous;
    internal int _state = (int)ManagedTaskState.Idle;
    internal DateTime _taskStartTime = DateTime.MinValue;
    internal DateTime _taskEndTime = DateTime.MinValue;
    internal bool _forceCancelRequested = false;

    public ManagedTask(string taskName, Guid id, bool isLongRunningTask = false)
    {
        if (string.IsNullOrWhiteSpace(taskName))
        {
            throw new ArgumentException("Task name cannot be null or whitepace.");
        }

        if (id == Guid.Empty)
        {
            throw new ArgumentException("Task id cannot be empty.");
        }

        _taskName = taskName;
        _taskID = id;
        _isLongRunningTask = isLongRunningTask;

        _taskKey = $"{taskName}_{id}";
    }

    internal void AttachServices(ITaskManager taskManager, IThreadLockManager threadLockManager, ICustomLogger logger)
    {
        if (taskManager == null)
        {
            throw new ArgumentNullException(nameof(taskManager));
        }

        if (threadLockManager == null)
        {
            throw new ArgumentNullException(nameof(threadLockManager));
        }

        if (TaskManager != null || ThreadLockManager != null || Logger != null)
        {
            throw new InvalidOperationException("Services have already been attached to this task.");
        }

        TaskManager = taskManager;
        ThreadLockManager = threadLockManager;
        Logger = logger;
    }

    public OperationResult<TimeSpan> GetTaskRuntime()
    {
        var result = new OperationResult<TimeSpan>();

        try
        {
            CheckTaskManager();

            var getRunTime = TaskManager!.GetTaskRuntime(_taskKey);
            if (!getRunTime.MethodSuccess)
            {
                throw getRunTime.Exception;
            }

            return result.SetMethodSuccess(getRunTime.Result);
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }

    public Task? GetTaskObject()
    {
        return TaskToRun;
    }

    internal void ResetCancellationToken()
    {
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = new CancellationTokenSource();
    }

    #region ScheduleSpecifics
    //public OperationResult<Task> AddScheduleToTask(TimeSpan interval, TimeSpan? initalDelay = null, int maxRunCount = -1)
    //{
    //    var result = new OperationResult<Task>();

    //    try
    //    {
    //        CheckTaskManager();
    //        CheckThreadLockManager();

    //        if (ScheduleAddon == null)
    //        {
    //            ScheduleAddon = new ManagedTask_Schedule(TaskManager!, ThreadLockManager!, this, interval, maxRunCount, Logger);

    //            var startTimer = ScheduleAddon.StartTimer(interval, initalDelay);
    //            if (!startTimer.MethodSuccess)
    //            {
    //                throw startTimer.Exception;
    //            }
    //        }

    //        return result.SetMethodSuccess(ScheduleAddon._timerTask!);
    //    }
    //    catch (Exception ex)
    //    {
    //        return result.SetMethodFailure(ex);
    //    }
    //}

    //public OperationResult<bool> RemoveScheduleFromTask(bool cancelTask = false)
    //{
    //    var result = new OperationResult<bool>();

    //    try
    //    {
    //        CheckTaskManager();

    //        if (ScheduleAddon != null)
    //        {
    //            ScheduleAddon!._timerCts.Cancel();

    //            if (cancelTask)
    //            {
    //                TaskManager!.CancelTask(_taskKey, true);
    //            }

    //            ScheduleAddon = null;

    //            Logger?.LogInformation($"[TaskScheduler] Removed schedule from Task '{_taskKey}'.");
    //        }
    //        else
    //        {
    //            Logger?.LogWarning($"[TaskScheduler] Task '{_taskKey}' does not have a schedule.");
    //        }

    //        return result.SetMethodSuccess(true);
    //    }
    //    catch (Exception ex)
    //    {
    //        return result.SetMethodFailure(ex);
    //    }
    //}

    //public OperationResult<bool> UpdateScheduleInterval(TimeSpan newInterval)
    //{
    //    OperationResult<bool> result = new OperationResult<bool>();

    //    try
    //    {
    //        if (ScheduleAddon != null)
    //        {
    //            ScheduleAddon!.RestartTimer(newInterval);

    //            Logger?.LogInformation($"[TaskScheduler] Task '{_taskKey}' schedule interval updated to {newInterval.TotalMinutes:N1} minutes.");
    //        }
    //        else
    //        {
    //            Logger?.LogWarning($"[TaskScheduler] Task '{_taskKey}' does not have an attached scheduled.");
    //        }

    //        return result.SetMethodSuccess(true);
    //    }
    //    catch (Exception ex)
    //    {
    //        return result.SetMethodFailure(ex);
    //    }
    //}
    #endregion

    public abstract Task DoTaskWork(CancellationToken cancellationToken);

    public virtual void Dispose()
    {
        //if (ScheduleAddon != null)
        //{
        //    Logger?.LogWarning("Cannot dispose task while a schedule is attached.");
        //    return;
        //}

        try
        {
            _cancellationTokenSource?.Cancel();
        }
        catch {}

        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;

        TaskToRun = null;

        Logger = null;
        ThreadLockManager = null;
        TaskManager = null;
        //ScheduleAddon = null;
    }

    protected void CheckTaskManager()
    {
        if (TaskManager == null)
        {
            throw new InvalidOperationException("TaskManager has not been attached to this task.");
        }
    }
    protected void CheckThreadLockManager()
    {
        if (ThreadLockManager == null)
        {
            throw new InvalidOperationException("ThreadLockManager has not been attached to this task.");
        }
    }
}

public enum ManagedTaskState
{
    Idle,
    Starting,
    Running,
    Completed,
    Canceled,
    Faulted,
    CancelRequested
}