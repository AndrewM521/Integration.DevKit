using AndrewM5.DevKit.TaskManagement.Abstractions.Interfaces;

namespace AndrewM5.DevKit.TaskManagement.Abstractions.Models;

public class ManagedTaskSettings
{
    private int _maxIterations = 1;

    public TaskExecutionMode ExecutionMode { get; set; } = TaskExecutionMode.Syncronous;
    public int MaxIterations 
    { 
        get
        {
            return _maxIterations;
        }
        set
        {
            if (value <= 0)
            {
                value = -1;
            }

            _maxIterations = value;
        }
    }

    public bool StopIteratingOnException { get; set; } = true;

    public NextTargetDTMStrategy? NextRunDelayStrategy { get; set; }
}
