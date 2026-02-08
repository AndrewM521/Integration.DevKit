using AndrewM5.DevKit.Logging.Abstractions;
using Microsoft.Extensions.Logging;

namespace AndrewM5.DevKit.Core;

public static class GCManager
{
    private static readonly TimeSpan _maxElapsedFromLastCall = TimeSpan.FromMinutes(20);
    
    private static DateTime _lastCallToCollect = DateTime.MinValue;
    private static long _lastHeapBaseline;
    
    private const long _maxHeapDelta = 20L * 1024 * 1024; // 20 MB

    public static void CallGC_Collect(string? description = null, ICustomLogger? logger = null)
    {
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

            string msg = @$"{headerMsg}
                Time since last force GC: {timeElapsed.TotalMinutes:F1} minutes
                Heap growth since last GC: {heapDelta / (1024.0 * 1024.0):N2} MB
                    Baseline: {_lastHeapBaseline / (1024.0 * 1024.0):N2} MB
                    Current: {currentHeap / (1024.0 * 1024.0):N2} MB)
                Reclaimed by GC: {reclaimed / (1024.0 * 1024.0):N2} MB
                    {reclaimedPercentage:F1}% of pre-GC heap
            ";

            logger?.LogDebug(msg);

            _lastHeapBaseline = GC.GetGCMemoryInfo().HeapSizeBytes;
            _lastCallToCollect = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            logger?.LogError($"[GCManager] Exception during GC: {ex}");
        }
    }
}
