/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

namespace Integration.DevKit.TaskMgmt.Contracts;

/// <summary>
/// Defines the execution behavior, retry policies, and iteration limits for an individual managed task.
/// </summary>
/// <remarks>
/// This class controls the flow of iterations, determining if they run one-by-one or in parallel, 
/// and how the manager should react when a specific iteration encounters an error.
/// </remarks>
public class ManagedTaskSettings
{
    private int _maxIterations = 1;
    private int _maxRetryCount = 1;

    /// <summary>
    /// Gets or sets the maximum number of times the task should iterate. 
    /// </summary>
    /// <value>
    /// The iteration limit. Values less than or equal to 0 are normalized to -1 (infinite retries).
    /// </value>
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
    /// Gets or sets the maximum number of retry attempts allowed if a single iteration fails. 
    /// </summary>
    /// <value>
    /// The retry limit. Values less than or equal to 0 are normalized to -1 (infinite retries).
    /// </value>
    public int MaxRetryCount 
    {
        get => _maxRetryCount;
        set 
        { 
            if (value <= 0)
            {
                value = -1;
            }

            _maxRetryCount = value;
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the current iteration should be retried if an exception is thrown.
    /// </summary>
    /// <value>Default is <see langword="false"/>.</value>
    public bool RetryOnException { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether the entire task loop should stop if an iteration throws an exception.
    /// </summary>
    /// <value>Default is <see langword="true"/>.</value>
    public bool StopIteratingOnException { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the task should stop spawning new iterations 
    /// if the <see cref="MaxRetryCount"/> is reached for a failing iteration.
    /// </summary>
    /// <value>Default is <see langword="true"/>.</value>
    public bool StopIterationAfterMaxRetries { get; set; } = true;
    
    /// <summary>
    /// Gets or sets the execution flow for task iterations.
    /// </summary>
    /// <value>
    /// <see cref="ManagedTaskExecutionMode.Sequential"/> for one-at-a-time, 
    /// or <see cref="ManagedTaskExecutionMode.Parallel"/> for concurrent execution.
    /// </value>
    public ManagedTaskExecutionMode IterationExecutionMode { get; set; } = ManagedTaskExecutionMode.Sequential;

    /// <summary>
    /// Gets or sets the strategy used to determine the delay or timing between iterations.
    /// </summary>
    /// <value>Defaults to a new instance of <see cref="BaseIterationStrategy"/> (no delay).</value>
    public BaseIterationStrategy IterationStrategy { get; set; } = new BaseIterationStrategy();

    /// <summary>
    /// Gets or sets a value indicating whether multiple iterations of this task can run at the same time.
    /// </summary>
    /// <remarks>
    /// When <see langword="true"/>, the task manager will not wait for the previous iteration 
    /// to finish before starting the next one, provided the <see cref="IterationStrategy"/> is met.
    /// </remarks>
    public bool AllowParallelIterationExecution { get; set; } = false;

    /// <summary>
    /// Gets or sets the maximum number of concurrent iterations allowed when 
    /// <see cref="AllowParallelIterationExecution"/> is enabled.
    /// </summary>
    /// <value>Default is 2.</value>
    public int MaxConcurrentParallelTasks { get; set; } = 2;

    /// <summary>
    /// Gets or sets the maximum amount of time the task is allowed to run before being automatically canceled.
    /// </summary>
    /// <value>
    /// A <see cref="TimeSpan"/> representing the timeout limit. If <see langword="null"/>, 
    /// the task will run indefinitely until completion or manual cancellation.
    /// </value>
    public TimeSpan? Timeout { get; set; } = null;

    /// <summary>
    /// Creates a deep copy clone of the current settings.
    /// </summary>
    /// <returns>A new instance of <see cref="ManagedTaskSettings"/> with the same configuration values.</returns>
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
            IterationExecutionMode = IterationExecutionMode,
            Timeout = Timeout
        };
    }
}

/// <summary>
/// Specifies the orchestration mode for task iterations.
/// </summary>
public enum ManagedTaskExecutionMode
{
    /// <summary>
    /// Iterations are executed concurrently according to concurrency limits.
    /// </summary>
    Parallel,
    /// <summary>
    /// Iterations are executed one after another, waiting for the previous to complete.
    /// </summary>
    Sequential
}
