/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using Integration.DevKit.TaskMgmt.Interfaces;

namespace Integration.DevKit.TaskMgmt;

/// <summary>
/// Concrete Implementation of <see cref="IManagedTaskHandle"/> providing a public-facing handle for monitoring and controlling a managed task.
/// </summary>
public sealed class ManagedTaskHandle : IManagedTaskHandle
{
    /// <summary>
    /// Gets the unique key used to identify the managed task.
    /// </summary>
    public string TaskKey => _managedTaskRuntime.UserTask.TaskKey;

    /// <summary>
    /// Gets the current execution state of the managed task.
    /// </summary>
    public ManagedTaskState State => _managedTaskRuntime.State;

    /// <summary>
    /// Gets the task that is currently executing the managed task lifecycle, if any.
    /// </summary>
    public Task? RunningTask => _managedTaskRuntime.LifecycleTask;

    /// <summary>
    /// Gets the number of iterations completed or executed for this task so far.
    /// </summary>
    public int CurrentIterationCount => _managedTaskRuntime.IterationCount;

    /// <summary>
    /// Gets the UTC start time for the managed task.
    /// </summary>
    public DateTime StartDTM => _managedTaskRuntime.StartDTM;

    /// <summary>
    /// Gets the UTC end time for the managed task, if it has completed.
    /// </summary>
    public DateTime EndDTM => _managedTaskRuntime.EndDTM;

    /// <summary>
    /// Gets the total runtime of the managed task.
    /// </summary>
    public TimeSpan Runtime => _managedTaskRuntime.Runtime;

    private readonly ManagedTaskRuntime _managedTaskRuntime;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedTaskHandle"/> class.
    /// </summary>
    /// <param name="managedTaskRuntime">The internal runtime instance to wrap.</param>
    internal ManagedTaskHandle(ManagedTaskRuntime managedTaskRuntime)
    {
        _managedTaskRuntime = managedTaskRuntime;
    }

    /// <inheritdoc/>
    public void Cancel() => _managedTaskRuntime.Cancel();
}
