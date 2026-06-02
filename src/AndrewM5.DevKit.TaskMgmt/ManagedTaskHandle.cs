/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using AndrewM5.DevKit.TaskMgmt.Contracts.Interfaces;

namespace AndrewM5.DevKit.TaskMgmt;

/// <summary>
/// Concrete Implementation of <see cref="IManagedTaskHandle"/> providing a public-facing handle for monitoring and controlling a managed task.
/// </summary>
public sealed class ManagedTaskHandle : IManagedTaskHandle
{
    /// <inheritdoc />
    public string TaskKey => _managedTaskRuntime.UserTask.TaskKey;

    /// <inheritdoc />
    public ManagedTaskState State => _managedTaskRuntime.State;

    /// <inheritdoc />
    public Task? RunningTask => _managedTaskRuntime.LifecycleTask;

    /// <inheritdoc />
    public int CurrentIterationCount => _managedTaskRuntime.IterationCount;

    /// <inheritdoc />
    public DateTime StartDTM => _managedTaskRuntime.StartDTM;

    /// <inheritdoc />
    public DateTime EndDTM => _managedTaskRuntime.EndDTM;

    /// <inheritdoc/>
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
