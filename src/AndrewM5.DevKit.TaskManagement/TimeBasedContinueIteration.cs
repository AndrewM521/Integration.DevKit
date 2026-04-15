using AndrewM5.DevKit.Logging.Contracts.Interfaces;
using AndrewM5.DevKit.TaskManagement.Contracts.Interfaces;
using AndrewM5.DevKit.TaskManagement.Contracts.Models;
using Microsoft.Extensions.Logging;

namespace AndrewM5.DevKit.TaskManagement;

/// <summary>
/// Provides a base implementation for defining when a task should next execute.
/// Handles initial start times and tracking of the last execution target.
/// </summary>
public abstract class TimeBasedContinueIteration : ContinueIterationBase
{
    /// <summary>
    /// Gets the specific date the strategy should begin from. If null, defaults to the current date.
    /// </summary>
    public DateOnly? CustomStartDate { get; }

    /// <summary>
    /// Gets the specific time of day the strategy should begin from. If null, defaults to the current time.
    /// </summary>
    public TimeSpan? CustomStartTime { get; }

    /// <summary>
    /// Gets or sets the timestamp of the last calculated execution target.
    /// Used as a reference point for calculating the subsequent run.
    /// </summary>
    public DateTime LastTargetDTM { get; set; } = default;

    /// <summary>
    /// Initializes a new instance of the <see cref="NextRunStrategy"/> class.
    /// </summary>
    /// <param name="startDate">An optional starting date.</param>
    /// <param name="startTime">An optional starting time of day.</param>
    protected TimeBasedContinueIteration(DateOnly? startDate = null, TimeSpan? startTime = null)
    {
        CustomStartDate = startDate;
        CustomStartTime = startTime;
    }

    /// <inheritdoc/>
    public override async Task WaitForReadyAsync(ITaskHandle handle, CancellationToken cancellationToken, ICustomLogger? logger = null)
    {
        var target = GetNextTargetDTM(handle.CurrentIterationCount);
        var utcTarget = new DateTimeOffset(target.ToUniversalTime());
        var localTarget = utcTarget.ToLocalTime();

        string format = "yyyy-MM-dd hh:mm:ss tt";
        string msg = $"Task '{handle.TaskKey}' next runtime <UTC: {utcTarget.ToString(format)} | Local: {localTarget.ToString(format)}";
        logger?.LogDebug(msg);

        while (!cancellationToken.IsCancellationRequested)
        {
            var utcNow = DateTimeOffset.UtcNow;
            var remaining = utcTarget - utcNow;

            // Target reached, record the target and let the next iteration start.
            if (remaining <= TimeSpan.Zero)
            {
                LastTargetDTM = target;
                break;
            }

            // Determine our sleep time
            TimeSpan wait = CalculateSmartWait(remaining);

            // If our desired sleep (e.g., 5 mins) is longer than 
            // the time left (e.g., 2 mins), clip it to exactly the time left.
            if (wait > remaining)
            {
                wait = remaining;
            }

            // Ensure we don't pass a negative or zero to Task.Delay if time ticked forward during calculation.
            if (wait <= TimeSpan.Zero)
            {
                break;
            }

            await Task.Delay(wait, cancellationToken);
        }
    }

    /// <summary>
    /// Determines the next scheduled execution time. 
    /// If no previous run has occurred, it returns the start time; otherwise, it triggers the inherited computation logic.
    /// </summary>
    /// <param name="currentIteration">The current iteration count of the task.</param>
    /// <returns>The <see cref="DateTime"/> for the next execution.</returns>
    public DateTime GetNextTargetDTM(int currentIteration)
    {
        if (LastTargetDTM == default)
        {
            LastTargetDTM = GetStartDTM();
        }

        return ComputeNextTargetDTM(currentIteration);
    }

    /// <summary>
    /// When implemented in a derived class, calculates the next execution time based on the strategy's specific recurrence rules.
    /// </summary>
    /// <param name="currentIteration">The current iteration count of the task.</param>
    /// <returns>The calculated <see cref="DateTime"/> for the next run.</returns>
    protected abstract DateTime ComputeNextTargetDTM(int currentIteration);

    /// <summary>
    /// Resolves the effective starting <see cref="DateTime"/> by combining custom or default date and time values.
    /// </summary>
    /// <returns>A local <see cref="DateTime"/> representing the absolute start point.</returns>
    private protected DateTime GetStartDTM()
    {
        var now = DateTime.Now;
        var startDate = DateOnly.FromDateTime(now);
        var startTime = now.TimeOfDay;

        if (CustomStartDate.HasValue)
        {
            startDate = (DateOnly)CustomStartDate;
        }

        if (CustomStartTime.HasValue)
        {
            startTime = (TimeSpan)CustomStartTime;
        }

        var localDTM = startDate.ToDateTime(TimeOnly.FromTimeSpan(startTime));

        return DateTime.SpecifyKind(localDTM, DateTimeKind.Local);
    }

    /// <summary>
    /// Calculates a variable sleep interval based on the time remaining until the next execution target.
    /// </summary>
    /// <remarks>
    /// This implemention uses a tiered approach to optimize resource usage:
    /// <list type="bullet">
    /// <item><description>Critical (&lt; 5m): 1-second precision heartbeat.</description></item>
    /// <item><description>Near (&lt; 30m): 5-minute polling.</description></item>
    /// <item><description>Medium (&lt; 1h): 20-minute polling.</description></item>
    /// <item><description>Distant (&gt; 1h): 1-hour sleep cycles.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="remaining">The duration of time left until the scheduled execution.</param>
    /// <returns>A <see cref="TimeSpan"/> representing the recommended duration to wait before the next check.</returns>
    private TimeSpan CalculateSmartWait(TimeSpan remaining)
    {
        if (remaining <= TimeSpan.FromMinutes(5))
        {
            return TimeSpan.FromSeconds(1);
        }
        else if (remaining <= TimeSpan.FromMinutes(30))
        {
            return TimeSpan.FromMinutes(5);
        }
        else if (remaining <= TimeSpan.FromHours(1))
        {
            return TimeSpan.FromMinutes(20);
        }

        return TimeSpan.FromHours(1);
    }
}
