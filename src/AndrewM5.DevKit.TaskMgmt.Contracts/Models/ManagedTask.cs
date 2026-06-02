/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using AndrewM5.DevKit.TaskMgmt.Contracts.Interfaces;

namespace AndrewM5.DevKit.TaskMgmt.Contracts.Models;

/// <summary>
/// Provides a base implementation for a task that can be managed by the task management system.
/// Implements basic identification and validation logic.
/// </summary>
/// <remarks>
/// Inherit from this class to define specific units of work. The task manager handles 
/// the lifecycle, but the derived class defines the actual functional logic within 
/// the <see cref="DoTaskWork"/> method.
/// </remarks>
public abstract class ManagedTask : IDisposable
{
    /// <summary>
    /// Gets the friendly display name of the task.
    /// </summary>
    public string TaskName { get; }

    /// <summary>
    /// Gets the unique global identifier for this specific task instance.
    /// </summary>
    public Guid TaskId { get; }

    /// <summary>
    /// Gets a unique string key used for lookups within the task registry. 
    /// </summary>
    /// <value>
    /// A string formatted as "{TaskName}_{TaskId}".
    /// </value>
    public string TaskKey { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedTask"/> class.
    /// </summary>
    /// <param name="taskName">The friendly display name of the task.</param>
    /// <param name="id">A unique identifier for this specific task instance.</param>
    /// <exception cref="ArgumentException">Thrown if <paramref name="taskName"/> is null or whitespace, or if <paramref name="id"/> is <see cref="Guid.Empty"/>.</exception>
    protected ManagedTask(string taskName, Guid id)
    {
        if (string.IsNullOrWhiteSpace(taskName))
        {
            throw new ArgumentException("Task name cannot be null or whitespace.");
        }

        if (id == Guid.Empty)
        {
            throw new ArgumentException("Task id cannot be empty.");
        }

        TaskName = taskName;
        TaskId = id;
        TaskKey = $"{taskName}_{id}";
    }

    /// <summary>
    /// Contains the core logic to be executed by the task manager during a single iteration.
    /// </summary>
    /// <param name="iterationHandle">The handle providing context, telemetry, and cancellation support for the current cycle.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <remarks>
    /// When this method completes, the manager will update terminal timestamps (like EndDTM) 
    /// and proceed to the <see cref="IIterationStrategy"/> to determine when to run the next cycle.
    /// <para/>
    /// Accessing <see cref="IManagedTaskIterationHandle.Runtime"/> within this method will 
    /// provide the elapsed time from the start of the current iteration to the present moment.
    /// </remarks>
    public abstract Task DoTaskWork(IManagedTaskIterationHandle iterationHandle);

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    /// <remarks>
    /// Derived classes should override this if they utilize <see cref="CancellationTokenSource"/>, 
    /// file handles, or other disposable objects within their task logic.
    /// </remarks>
    public virtual void Dispose() {}
}