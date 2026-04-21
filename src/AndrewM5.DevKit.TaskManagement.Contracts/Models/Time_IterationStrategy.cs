using AndrewM5.DevKit.Logging.Contracts.Interfaces;
using AndrewM5.DevKit.TaskManagement.Contracts.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;

namespace AndrewM5.DevKit.TaskManagement.Contracts.Models;

/// <summary>
/// Provides a base implementation for defining when a task should next execute.
/// Handles initial start times and tracking of the last execution target.
/// </summary>
public abstract class Time_IterationStrategy : BaseIterationStrategy
{
    private const string outputDTMFormat = "yyyy-MM-dd hh:mm:ss tt";
    
    internal TimeStrategySettings RuntimeSettings { get; }

    /// <summary>
    /// Gets or sets the timestamp of the last calculated execution target.
    /// Used as a reference point for calculating the subsequent run.
    /// </summary>
    public DateTime LastTargetDTM { get; set; } = default;

    /// <summary>
    /// Initializes a new instance of the <see cref="NextRunStrategy"/> class.
    /// </summary>
    protected Time_IterationStrategy(TimeStrategySettings settings)
    {
        RuntimeSettings = settings;      
    }

    /// <inheritdoc/>
    public override async Task WaitForReadyAsync(IManagedTaskHandle handle, CancellationToken cancellationToken, ICustomLogger? logger = null)
    {
        // Initial State Resolution
        var target = GetNextTargetDTM(handle.CurrentIterationCount);
        var utcTarget = new DateTimeOffset(target.ToUniversalTime());
        var remaining = utcTarget - DateTimeOffset.UtcNow;

        // The Catch-up Phase
        // If flag is true, we "burn" through past dates without returning to the Task Manager.
        if (RuntimeSettings.FastForwardToPresent && remaining <= TimeSpan.Zero)
        {
            logger?.LogInformation($"Task '{handle.TaskKey}' is behind schedule. Catching up to current time (Skipping missed iterations)...");

            // Keep advancing until 'target' is in the future
            while (remaining <= TimeSpan.Zero && !cancellationToken.IsCancellationRequested)
            {
                LastTargetDTM = target;

                // Advance target based on the strategy's math
                target = ComputeNextTargetDTM(handle.CurrentIterationCount);
                utcTarget = new DateTimeOffset(target.ToUniversalTime());
                remaining = utcTarget - DateTimeOffset.UtcNow;
            }

            logger?.LogInformation($"Task '{handle.TaskKey}' catch-up complete");
        }

        var localTarget = utcTarget.ToLocalTime();
        string runtimeStr = GetTargetRuntimeStr(localTarget, utcTarget);

        logger?.LogInformation($"Task '{handle.TaskKey}' next runtime <{runtimeStr}>");

        // The Execution/Wait Phase(External Relay)
        while (!cancellationToken.IsCancellationRequested)
        {
            // Re-sync local variables with the current state of 'target'
            remaining = utcTarget - DateTimeOffset.UtcNow;

            // Process Missed (Catchup was false, or we just hit a slot)
            if (remaining <= TimeSpan.Zero)
            {
                logger?.LogDebug($"Task '{handle.TaskKey}' target reached <{runtimeStr}>");
                LastTargetDTM = target;

                return; // Exit to let the Task Manager execute the logic
            }

            // Fresh Start / Skip Wait Policy
            if (handle.CurrentIterationCount == 0 && RuntimeSettings.SkipFirstIterationWait)
            {
                logger?.LogDebug($"Task '{handle.TaskKey}' skipping first runtime wait for <{runtimeStr}> due to strategy policy. ({nameof(RuntimeSettings.SkipFirstIterationWait)})");
                LastTargetDTM = target;

                return;
            }

            // Real-time Polling/Waiting
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
                LastTargetDTM = target;

                break; // Target reached exactly during calculation
            }

            // Log the next check for long-running waits
            if (wait > TimeSpan.FromMinutes(1))
            {
                logger?.LogDebug($"Task '{handle.TaskKey}' sleeping for {wait.TotalMinutes:F1} minutes until next check.");
            }

            await Task.Delay(wait, cancellationToken);
        }
    }

    private string GetTargetRuntimeStr(DateTimeOffset localTarget, DateTimeOffset utcTarget)
    {
        return $"Local: {localTarget.ToString(outputDTMFormat)} | UTC: {utcTarget.ToString(outputDTMFormat)}";
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

        if (RuntimeSettings.CustomStartDate.HasValue)
        {
            startDate = (DateOnly)RuntimeSettings.CustomStartDate;
        }

        if (RuntimeSettings.CustomStartTime.HasValue)
        {
            startTime = (TimeSpan)RuntimeSettings.CustomStartTime;
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
