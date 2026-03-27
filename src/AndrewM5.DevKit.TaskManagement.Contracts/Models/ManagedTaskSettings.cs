using AndrewM5.DevKit.TaskManagement.Abstractions.Interfaces;

namespace AndrewM5.DevKit.TaskManagement.Abstractions.Models;

public class ManagedTaskSettings
{
    private int _maxIterations = 1;
    public int MaxIterations 
    { 
        get => _maxIterations;
        set
        {
            if (value <= 0)
            {
                value = -1;
            }

            _maxIterations = value;
        }
    }

    private int _maxRetryCount = 1;

    public int MaxRetryCount 
    {
        get => _maxRetryCount;
        set 
        { 
            if (value < -1)
            {
                value = -1;
            }

            _maxRetryCount = value;
        }
    }

    public bool RetryOnException { get; set; } = false;
    public bool StopIterationAfterMaxRetries { get; set; } = true;

    public NextRunStrategy? NextRunStrategy { get; set; }

    public ManagedTaskSettings Clone()
    {
        return new ManagedTaskSettings
        {
            MaxIterations = _maxIterations,
            NextRunStrategy = NextRunStrategy,
            MaxRetryCount = _maxRetryCount,
            RetryOnException = RetryOnException
        };
    }
}
