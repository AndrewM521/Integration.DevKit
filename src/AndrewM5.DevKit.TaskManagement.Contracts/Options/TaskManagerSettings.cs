namespace AndrewM5.DevKit.TaskManagement.Contracts.Options;

/// <summary>
/// Provides global configuration settings for the Task Manager, 
/// controlling resource limits and registry capacity.
/// </summary>
public class TaskManagerSettings
{
    /// <summary>
    /// Gets or sets the maximum number of tasks allowed to run concurrently.
    /// The default value is <see cref="int.MaxValue"/>.
    /// </summary>
    public int MaxConcurrentTasks { get; set; } = int.MaxValue;

    /// <summary>
    /// Gets or sets the maximum number of task records allowed in the <see cref="ITaskRegistry"/>.
    /// The default value is 2000.
    /// </summary>
    public int MaxRegistryCount { get; set; } = 2000;

    /// <summary>
    /// Creates a member-wise deep copy of the current <see cref="TaskManagerSettings"/> instance.
    /// </summary>
    /// <returns>A new <see cref="TaskManagerSettings"/> object with identical property values.</returns>
    public TaskManagerSettings Clone()
    {
        return new TaskManagerSettings 
        { 
            MaxConcurrentTasks = this.MaxConcurrentTasks,
            MaxRegistryCount = this.MaxRegistryCount
        };
    }
}
