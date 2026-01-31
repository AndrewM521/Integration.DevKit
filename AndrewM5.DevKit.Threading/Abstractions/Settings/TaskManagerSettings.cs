namespace AndrewM5.DevKit.Threading.Abstractions.Settings;

public class TaskManagerSettings
{
    public int MaxConcurrentTasks { get; set; } = int.MaxValue;

    public TaskManagerSettings Clone()
    {
        return new TaskManagerSettings 
        { 
            MaxConcurrentTasks = this.MaxConcurrentTasks
        };
    }
}
