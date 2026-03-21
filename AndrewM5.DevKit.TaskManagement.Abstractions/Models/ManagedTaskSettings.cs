using AndrewM5.DevKit.TaskManagement.Abstractions.Interfaces;

namespace AndrewM5.DevKit.TaskManagement.Abstractions.Models;

public class ManagedTaskSettings
{
    private int _maxIterations = 1;
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

    public NextRunStrategy? NextRunStrategy { get; set; }

    public ManagedTaskSettings Clone()
    {
        return new ManagedTaskSettings
        {
            MaxIterations = _maxIterations,
            NextRunStrategy = NextRunStrategy,
            StopIteratingOnException = StopIteratingOnException
        };
    }
}
