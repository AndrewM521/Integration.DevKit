/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using System.Diagnostics;

namespace AndrewM5.DevKit.Core;

/// <summary>
/// Provides throttled management of the .NET Garbage Collector to prevent excessive 
/// collection cycles while ensuring memory pressure remains within defined limits.
/// </summary>
/// <remarks>
/// This manager uses a combination of time-based and heap-growth-based triggers to 
/// determine if a forced collection is necessary. It is thread-safe and prevents 
/// concurrent collection attempts.
/// </remarks>
public static class GCManager
{
    private static readonly TimeSpan _maxElapsedFromLastCall = TimeSpan.FromMinutes(20);
    private static int _gcRunning = 0;

    private static DateTime _lastCallToCollect = DateTime.MinValue;
    private static long _lastHeapBaseline;

    /// <summary>
    /// The threshold for heap growth (20 MB) that triggers a collection.
    /// </summary>
    private const long _maxHeapDelta = 20L * 1024 * 1024; // 20 MB

    /// <summary>
    /// Evaluates memory pressure and elapsed time to decide whether to force a full Garbage Collection.
    /// </summary>
    /// <param name="description">An optional message to include in the debug logs if a collection occurs.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <remarks>
    /// <para>
    /// A collection is only performed if:
    /// <list type="number">
    /// <item>The heap has grown by more than 20 MB since the last managed collection.</item>
    /// <item>More than 20 minutes have passed since the last managed collection.</item>
    /// </list>
    /// </para>
    /// <para>
    /// If a collection is triggered, it performs a full collection across all generations 
    /// and waits for pending finalizers. Results are output to <see cref="Debug.WriteLine(string)"/>.
    /// </para>
    /// </remarks>
    public static async Task CallGC_Collect(string? description = null)
    {
        if (Interlocked.Exchange(ref _gcRunning, 1) == 1)
        {
            return; // already running
        }

        try
        {
            bool collectFromElapsedLastCall = false;
            bool collectFromMaxHeapDelta = false;

            var info = GC.GetGCMemoryInfo();
            long currentHeap = info.HeapSizeBytes;

            long heapDelta = currentHeap - _lastHeapBaseline;
            if (heapDelta >= _maxHeapDelta)
            {
                collectFromMaxHeapDelta = true;
            }

            TimeSpan timeElapsed = DateTime.UtcNow - _lastCallToCollect;
            if (timeElapsed >= _maxElapsedFromLastCall)
            {
                collectFromElapsedLastCall = true;
            }

            if (!collectFromMaxHeapDelta && !collectFromElapsedLastCall)
            {
                return;
            }

            // Capture memory before GC
            long memoryBefore = GC.GetTotalMemory(false);

            // Force a full collection of all generations
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // Capture memory after GC
            long memoryAfter = GC.GetTotalMemory(false);

            string headerMsg = "GC Force Collected.";

            string reason = "";

            if (collectFromElapsedLastCall && collectFromMaxHeapDelta)
            {
                reason = "Memory Preasure above threshold";
            }
            else if (collectFromMaxHeapDelta)
            {
                reason = "Memory Preasure above threshold";
            }
            else if (collectFromElapsedLastCall)
            {
                reason = "Reached max collect time";
            }

            headerMsg += $" Reason: {reason}";

            if (!string.IsNullOrWhiteSpace(description))
            {
                headerMsg += $", Message: {description}";
            }

            long reclaimed = memoryBefore - memoryAfter;
            double reclaimedPercentage = 0;

            if (memoryBefore > 0)
            {
                reclaimedPercentage = reclaimed * 100.0 / memoryBefore;
            }

            string msg = @$"
                {headerMsg}
                Time since last force GC: {timeElapsed.TotalMinutes:F1} minutes
                Heap growth since last GC: {heapDelta / (1024.0 * 1024.0):N2} MB
                    Baseline: {_lastHeapBaseline / (1024.0 * 1024.0):N2} MB
                    Current: {currentHeap / (1024.0 * 1024.0):N2} MB)
                Reclaimed by GC: {reclaimed / (1024.0 * 1024.0):N2} MB
                    {reclaimedPercentage:F1}% of pre-GC heap
            ";

            Debug.WriteLine(msg);

            // Update baselines for the next cycle
            _lastHeapBaseline = GC.GetGCMemoryInfo().HeapSizeBytes;
            _lastCallToCollect = DateTime.UtcNow;

            // Brief delay to allow system stabilization
            await Task.Delay(1000);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[GCManager] Exception during GC: {ex}");
        }
        finally
        {
            // Reset the interlock flag
            Volatile.Write(ref _gcRunning, 0);
        }
    }
}
