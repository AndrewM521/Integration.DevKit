using AndrewM5.DevKit.Logging.Contracts.Interfaces;
using AndrewM5.DevKit.TaskManagement.Contracts.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;

namespace AndrewM5.DevKit.TaskManagement.Contracts.Models;

/// <summary>
/// Provides a base implementation for iteration strategies that determine execution 
/// cycles based on specific points in time (clock-based scheduling).
/// </summary>
/// <remarks>
/// This abstract class handles the complex logic of time synchronization, including 
/// scheduling catch-up (fast-forwarding), smart polling intervals to reduce CPU usage, 
/// and initial start-time resolution. 
/// </remarks>
public abstract class Time_IterationStrategy : BaseIterationStrategy
{
    private const string outputDTMFormat = "yyyy-MM-dd hh:mm:ss tt";

    /// <summary>
    /// Gets the internal configuration settings used to drive the time-based logic.
    /// </summary>
    internal TimeStrategySettings RuntimeSettings { get; }

    /// <summary>
    /// Gets or sets the timestamp of the last calculated execution target.
    /// </summary>
    /// <remarks>
    /// This serves as the anchor point for calculating the next run. It is updated 
    /// every time a scheduled slot is successfully reached and handed off to the manager.
    /// </remarks>
    public DateTime LastTargetDTM { get; set; } = default;

    /// <summary>
    /// Initializes a new instance of the <see cref="Time_IterationStrategy"/> class.
    /// </summary>
    /// <param name="settings">The time-specific configuration</param>
    protected Time_IterationStrategy(TimeStrategySettings settings)
    {
        RuntimeSettings = settings;      
    }

    /// <inheritdoc/>
    /// <remarks>
    /// This implementation performs a multi-phase wait:
    /// <list type="number">
    /// <item><description><b>Resolution:</b> Determines the next target <see cref="DateTime"/>.</description></item>
    /// <item><description><b>Catch-up:</b> If enabled, bypasses past dates until reaching the current time.</description></item>
    /// <item><description><b>Waiting:</b> Performs a "Smart Wait" using tiered polling to reach the target with high precision.</description></item>
    /// </list>
    /// </remarks>
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

    /// <summary>
    /// Determines the next scheduled execution time, initializing the start point if necessary.
    /// </summary>
    /// <param name="currentIteration">The current iteration count of the task.</param>
    /// <returns>The <see cref="DateTime"/> representing the next scheduled run.</returns>
    public DateTime GetNextTargetDTM(int currentIteration)
    {
        if (LastTargetDTM == default)
        {
            LastTargetDTM = GetStartDTM();
        }

        return ComputeNextTargetDTM(currentIteration);
    }

    /// <summary>
    /// When implemented in a derived class, calculates the next execution time based on specific recurrence rules.
    /// </summary>
    /// <param name="currentIteration">The current iteration count of the task.</param>
    /// <returns>The calculated <see cref="DateTime"/> for the subsequent run.</returns>
    /// <remarks>
    /// Derived classes (e.g., Cron or Interval strategies) should use <see cref="LastTargetDTM"/> 
    /// as the basis for this calculation.
    /// </remarks>
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
    /// This implementation uses a tiered approach to optimize resource usage:
    /// <list type="bullet">
    /// <item><description><b>Critical (&lt; 5m):</b> 1-second precision heartbeat for accurate triggering.</description></item>
    /// <item><description><b>Near (&lt; 30m):</b> 5-minute polling intervals.</description></item>
    /// <item><description><b>Medium (&lt; 1h):</b> 20-minute polling intervals.</description></item>
    /// <item><description><b>Distant (&gt; 1h):</b> 1-hour sleep cycles to minimize thread/timer activity.</description></item>
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

    /// <summary>
    /// Formats a target time into a human-readable string for logging.
    /// </summary>
    /// <param name="localTarget">The target time in the local timezone.</param>
    /// <param name="utcTarget">The target time in UTC.</param>
    /// <returns>A formatted string containing both local and UTC representations.</returns>
    private string GetTargetRuntimeStr(DateTimeOffset localTarget, DateTimeOffset utcTarget)
    {
        return $"Local: {localTarget.ToString(outputDTMFormat)} | UTC: {utcTarget.ToString(outputDTMFormat)}";
    }
}
