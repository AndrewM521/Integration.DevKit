namespace AndrewM5.DevKit.TaskManagement.Abstractions.Options;

public class TaskManagerSettings
{
    public int MaxConcurrentTasks { get; set; } = int.MaxValue;
    public int MaxRegistryCount { get; set; } = 2000;

    public TaskManagerSettings Clone()
    {
        return new TaskManagerSettings 
        { 
            MaxConcurrentTasks = this.MaxConcurrentTasks,
            MaxRegistryCount = this.MaxRegistryCount
        };
    }
}
