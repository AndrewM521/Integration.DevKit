/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using Integration.DevKit.Core;
using Integration.DevKit.ThreadSafeItems;

namespace TestApp.Demos;

/// <summary>
/// Exercises <see cref="ThreadSafeFileIO"/> under contention: basic read/write, async-only,
/// sync-only, and hybrid sync+async concurrent access.
/// </summary>
public class ThreadSafeItemsDemo : IDemo
{
    public async Task RunAsync()
    {
        Console.WriteLine("----|----|Thread Safe FileIO |----|----");

        var _threadSafeFileIO = Service_ThreadSafeItems.ThreadSafeFileIOClass;
        string filePath = "C:\\Users\\andre\\Projects\\Junk\\ThreadSafeTest.txt";
        string filePath_AsyncLock = "C:\\Users\\andre\\Projects\\Junk\\ThreadSafeTest_AsyncLock.txt";
        string filePath_SyncLock = "C:\\Users\\andre\\Projects\\Junk\\ThreadSafeTest_SyncLock.txt";
        string filePath_HybridLock = "C:\\Users\\andre\\Projects\\Junk\\ThreadSafeTest_HybridLock.txt";

        NullOperationResult writeFile;
        OperationResult<string[]> readFile;

        Console.WriteLine("==Test 1: Control==");
        Console.WriteLine("FilePath: " + filePath);

        Console.WriteLine("Deleting File...");
        var deleteFile = FileUtils.DeleteFile(filePath);
        if (!deleteFile.MethodSuccess)
        {
            Console.WriteLine(deleteFile.Exception);
        }

        Console.WriteLine("File Exists: " + FileUtils.DoesFileExist(filePath));

        Console.WriteLine("\n==Test 2: Create File==");
        writeFile = await _threadSafeFileIO.WriteToFileAsync(filePath, "This is a async test message");
        if (!writeFile.MethodSuccess)
        {
            Console.WriteLine(writeFile.Exception);
        }

        Console.WriteLine("File Exists: " + FileUtils.DoesFileExist(filePath));

        readFile = await _threadSafeFileIO.ReadFileLinesAsync(filePath);
        if (!readFile.MethodSuccess)
        {
            Console.WriteLine(readFile.Exception);
        }

        Console.WriteLine("Read Result: ");
        Console.WriteLine(string.Join("\r\n", readFile.Result));

        Console.WriteLine("\n==Test 3: Write File (append)==");
        writeFile = await _threadSafeFileIO.WriteToFileAsync(filePath, "This is a async test message with appending", true);
        if (!writeFile.MethodSuccess)
        {
            Console.WriteLine(writeFile.Exception);
        }

        Console.WriteLine("File Exists: " + FileUtils.DoesFileExist(filePath));

        readFile = await _threadSafeFileIO.ReadFileLinesAsync(filePath);
        if (!readFile.MethodSuccess)
        {
            Console.WriteLine(readFile.Exception);
        }

        Console.WriteLine("Read Result: ");
        Console.WriteLine(string.Join("\r\n", readFile.Result));

        Console.WriteLine("\n==Test 4: Write File (overwrite)==");
        writeFile = await _threadSafeFileIO.WriteToFileAsync(filePath, "This is a async test message with overwrite");
        if (!writeFile.MethodSuccess)
        {
            Console.WriteLine(writeFile.Exception);
        }

        Console.WriteLine("File Exists: " + FileUtils.DoesFileExist(filePath));

        readFile = await _threadSafeFileIO.ReadFileLinesAsync(filePath);
        if (!readFile.MethodSuccess)
        {
            Console.WriteLine(readFile.Exception);
        }

        Console.WriteLine("Read Result: ");
        Console.WriteLine(string.Join("\r\n", readFile.Result));

        Console.WriteLine("\n==Test 5: Write File (multi-line)==");
        writeFile = await _threadSafeFileIO.WriteToFileAsync(filePath, new string[] { "This is a async test message", "This is a async test message 1" });
        if (!writeFile.MethodSuccess)
        {
            Console.WriteLine(writeFile.Exception);
        }

        Console.WriteLine("File Exists: " + FileUtils.DoesFileExist(filePath));

        readFile = await _threadSafeFileIO.ReadFileLinesAsync(filePath);
        if (!readFile.MethodSuccess)
        {
            Console.WriteLine(readFile.Exception);
        }

        Console.WriteLine("Read Result: ");
        Console.WriteLine(string.Join("\r\n", readFile.Result));

        Console.WriteLine("\n==Test 6: Async-Only Contention (Race Condition)==");

        // Clean start
        FileUtils.DeleteFile(filePath_AsyncLock);

        int numberOfThreads = 10;
        int writesPerThread = 20;
        var tasks = new List<Task<NullOperationResult>>();

        Console.WriteLine($"Starting {numberOfThreads} parallel tasks, each writing {writesPerThread} times...");

        for (int i = 0; i < numberOfThreads; i++)
        {
            int threadId = i; // Capture local variable for the closure
            tasks.Add(Task.Run(async () =>
            {
                NullOperationResult lastResult = new NullOperationResult().SetMethodSuccess();

                for (int j = 0; j < writesPerThread; j++)
                {
                    var writeResult = await _threadSafeFileIO.WriteToFileAsync(
                        filePath_AsyncLock,
                        $"Thread {threadId} - Entry {j}",
                        append: true,
                        lockTimeoutMs: 10000 // Higher timeout to handle the queue
                    );

                    if (!writeResult.MethodSuccess) return writeResult;
                    lastResult = writeResult;
                }
                return lastResult;
            }));
        }

        // Wait for all "threads" to finish
        var results = await Task.WhenAll(tasks);

        // Check for failures
        int failureCount = results.Count(r => !r.MethodSuccess);
        if (failureCount > 0)
        {
            Console.WriteLine($"FAILURE: {failureCount} operations failed during contention!");
            foreach (var fail in results.Where(r => !r.MethodSuccess))
                Console.WriteLine($" -> {fail.Exception.Message}");
        }
        else
        {
            Console.WriteLine("SUCCESS: All parallel writes completed without file-in-use errors.");
        }

        // Final Verification: Count the lines in the file
        var finalRead = await _threadSafeFileIO.ReadFileLinesAsync(filePath_AsyncLock);
        int expectedLines = numberOfThreads * writesPerThread;

        if (finalRead.MethodSuccess && finalRead.Result.Length == expectedLines)
        {
            Console.WriteLine($"INTEGRITY PASSED: Found {finalRead.Result.Length} lines as expected.");
        }
        else
        {
            Console.WriteLine($"INTEGRITY FAILED: Expected {expectedLines} lines, but found {finalRead.Result?.Length ?? 0}.");
        }

        Console.WriteLine("\n==Test 7: Sync-Only Contention (Parallel.For)==");

        FileUtils.DeleteFile(filePath_SyncLock);

        int totalWrites = 50;

        // Parallel.For uses the thread pool to slam the sync methods
        Parallel.For(0, totalWrites, i =>
        {
            _threadSafeFileIO.WriteToFile(filePath_SyncLock, $"Sync-Write-{i}", true);
        });

        var finalRead_sync = _threadSafeFileIO.ReadFileLines(filePath_SyncLock);

        if (finalRead.MethodSuccess && finalRead_sync.Result.Length == totalWrites)
        {
            Console.WriteLine($"SYNC SUCCESS: {finalRead_sync.Result.Length}/{totalWrites} lines written.");
        }
        else
        {
            Console.WriteLine("SYNC FAILURE: Monitor lock failed to prevent collision.");
        }

        Console.WriteLine("\n==Test 8: Hybrid (Sync + Async) Contention==");
        Console.WriteLine("NOTE: This should fail and produce exceptions since you should not combine Sync and Async locking");

        FileUtils.DeleteFile(filePath_HybridLock);

        int iterations = 15;

        // Task for Async writes
        var asyncTask = Task.Run(async () =>
        {
            for (int i = 0; i < iterations; i++)
            {
                await _threadSafeFileIO.WriteToFileAsync(filePath_HybridLock, $"Async-Write-{i}", true);
            }
        });

        // Task for Sync writes
        var syncTask = Task.Run(() =>
        {
            for (int i = 0; i < iterations; i++)
            {
                _threadSafeFileIO.WriteToFile(filePath_HybridLock, $"Sync-Write-{i}", true);
            }
        });

        await Task.WhenAll(asyncTask, syncTask);

        var finalRead_hybrid = await _threadSafeFileIO.ReadFileLinesAsync(filePath_HybridLock);
        int expectedLines_hybrid = iterations * 2;

        if (finalRead.MethodSuccess && finalRead_hybrid.Result.Length == expectedLines_hybrid)
        {
            Console.WriteLine($"HYBRID SUCCESS: {finalRead_hybrid.Result.Length} total lines written.");
        }
        else
        {
            Console.WriteLine("HYBRID COLLISION: Some writes were lost or an exception occurred.");
            // If this fails, it means your IThreadLockManager uses different
            // locks for Sync vs Async internally.
        }
    }
}
