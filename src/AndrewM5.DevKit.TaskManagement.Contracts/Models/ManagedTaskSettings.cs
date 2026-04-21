namespace AndrewM5.DevKit.TaskManagement.Contracts.Models;

/// <summary>
/// Defines the execution behavior, retry policies, and iteration limits for an individual managed task.
/// </summary>
public class ManagedTaskSettings
{
    private int _maxIterations = 1;
    private int _maxRetryCount = 1;

    /// <summary>
    /// Gets or sets the maximum number of times the task should iterate. 
    /// Values less than or equal to 0 are set to -1 (infinite iterations).
    /// </summary>
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

    /// <summary>
    /// Gets or sets the maximum number of retry attempts allowed if a task fails. 
    /// Values less than -1 are set to -1 (infinite retries).
    /// </summary>
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

    /// <summary>
    /// Gets or sets a value indicating whether the task should be retried if an exception is thrown during execution.
    /// Default is <c>false</c>.
    /// </summary>
    public bool RetryOnException { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether the entire iteration loop should stop if there is an exception.
    /// Default is <c>true</c>.
    /// </summary>
    /// <remarks>
    /// If <see cref="RetryOnException"/> is true, this flag will be skipped. If you want the same results with 
    /// retries, use <see cref="StopIterationAfterMaxRetries"/> instead
    /// </remarks>
    public bool StopIteratingOnException { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the entire iteration loop should stop if the <see cref="MaxRetryCount"/> is reached.
    /// Default is <c>true</c>.
    /// </summary>
    public bool StopIterationAfterMaxRetries { get; set; } = true;

    public ManagedTaskExecutionMode IterationExecutionMode { get; set; } = ManagedTaskExecutionMode.Sequential;

    /// <summary>
    /// Gets or sets when you want the iteration loop to continue.
    /// Defaults to new instance of <see cref="BaseIterationStrategy"/>
    /// </summary>
    public BaseIterationStrategy IterationStrategy { get; set; } = new BaseIterationStrategy();


    public bool AllowParallelIterationExecution { get; set; } = false;

    public int MaxConcurrentParallelTasks { get; set; } = 1;


    /// <summary>
    /// Creates a deep copy clone of the current settings.
    /// </summary>
    /// <returns>A new instance of <see cref="ManagedTaskSettings"/> with the same configuration.</returns>
    public ManagedTaskSettings Clone()
    {
        return new ManagedTaskSettings
        {
            MaxIterations = _maxIterations,
            IterationStrategy = IterationStrategy,
            MaxRetryCount = _maxRetryCount,
            RetryOnException = RetryOnException,
            StopIterationAfterMaxRetries = StopIterationAfterMaxRetries,
            StopIteratingOnException = StopIteratingOnException,
            MaxConcurrentParallelTasks = MaxConcurrentParallelTasks,
            AllowParallelIterationExecution = AllowParallelIterationExecution,
            IterationExecutionMode = IterationExecutionMode
        };
    }
}

public enum ManagedTaskExecutionMode
{
    Parallel,
    Sequential
}
