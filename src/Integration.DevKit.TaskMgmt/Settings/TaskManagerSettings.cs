/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

namespace Integration.DevKit.TaskMgmt.Settings;

/// <summary>
/// Provides global configuration settings for the Task Manager, 
/// controlling resource limits and registry capacity.
/// </summary>
public class TaskManagerSettings
{
    /// <summary>
    /// Gets or sets the maximum number of tasks allowed to run concurrently across the entire manager.
    /// </summary>
    /// <value>
    /// The concurrency limit. The default value is <see cref="int.MaxValue"/>.
    /// </value>
    public int MaxConcurrentTasks { get; set; } = int.MaxValue;

    /// <summary>
    /// Gets or sets the maximum number of task records allowed in the <see cref="TaskRegistry"/>.
    /// </summary>
    /// <remarks>
    /// This limit controls how many <see cref="ManagedTaskSnapshot"/> objects are 
    /// retained in memory. Once this limit is reached, the manager prunes old records
    /// </remarks>
    /// <value>Default value is 2000.</value>
    public int MaxTaskRegistryCount { get; set; } = 2000;

    /// <summary>
    /// Gets or sets the maximum number of historical iteration records kept for each individual task.
    /// </summary>
    /// <remarks>
    /// This property limits the size of the <see cref="ManagedTaskSnapshot.IterationHistory"/> 
    /// dictionary. It is essential for long-running tasks to prevent the snapshot from 
    /// growing indefinitely over time.
    /// </remarks>
    /// <value>Default value is 100.</value>
    public int MaxTaskIterationRegistryCount { get; set; } = 100;

    /// <summary>
    /// Gets or sets whether this module logs through the logger factory supplied at registration.
    /// Defaults to <see langword="true"/>. Can be flipped at runtime via the manager's
    /// <c>RuntimeSettings</c> to silence/resume this module's logging without removing the app's logger.
    /// </summary>
    public bool EnableLogging { get; set; } = true;

    /// <summary>
    /// Creates a member-wise deep copy of the current <see cref="TaskManagerSettings"/> instance.
    /// </summary>
    /// <returns>A new <see cref="TaskManagerSettings"/> object with identical property values.</returns>
    public TaskManagerSettings Clone()
    {
        return new TaskManagerSettings
        {
            MaxConcurrentTasks = MaxConcurrentTasks,
            MaxTaskRegistryCount = MaxTaskRegistryCount,
            MaxTaskIterationRegistryCount = MaxTaskIterationRegistryCount,
            EnableLogging = EnableLogging
        };
    }
}
