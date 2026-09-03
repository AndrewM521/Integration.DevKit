/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using Integration.DevKit.Core;
using Integration.DevKit.TaskMgmt;
using Integration.DevKit.TaskMgmt.Contracts;
using System.Diagnostics;

namespace TestApp.Demos;

/// <summary>
/// Same sub-test matrix as <see cref="TaskManagementSyncDemo"/>, mirrored for
/// <see cref="TaskExecutionMode.Asyncronous"/>, plus a polling/registry-snapshot loop at the end.
/// </summary>
public class TaskManagementAsyncDemo : IDemo
{
    public async Task RunAsync()
    {
        Console.WriteLine("----|----|Task Management|----|----");

        var _taskManager = Service_TaskMgmt.TaskManager;
        var _taskRegistry = Service_TaskMgmt.TaskRegistry;
        _taskManager.LogRuntimeSettings();

        OperationResult<IManagedTaskHandle> createTask;
        var settings = new ManagedTaskSettings();
        var strategySettings = new TimeStrategySettings();

        Console.WriteLine("\n==Test 1: Asynchronous (Default Settings)==");
        createTask = await _taskManager.StartTask(new SimpleShortTask(), TaskExecutionMode.Asyncronous, settings);
        if (!createTask.MethodSuccess)
        {
            Console.WriteLine("Error: " + createTask.Exception.Message);
        }

        Console.WriteLine("Finished creating task. Now waiting until its done.");

        await createTask.Result.RunningTask!;

        settings = new ManagedTaskSettings();

        Console.WriteLine("\n==Test 2: Asynchronous (Custom Settings)==");
        Console.WriteLine("NOTE: Look at output logs");
        settings.MaxIterations = 2;

        Console.WriteLine("Changed Interation Settings");
        Console.WriteLine("  MaxIterations = 2");

        createTask = await _taskManager.StartTask(new SimpleShortTask(), TaskExecutionMode.Asyncronous, settings);
        if (!createTask.MethodSuccess)
        {
            Console.WriteLine("Error: " + createTask.Exception.Message);
        }

        Console.WriteLine("Finished creating task. Now waiting until its done.");

        await createTask.Result.RunningTask!;

        settings = new ManagedTaskSettings();

        Console.WriteLine("\n==Test 3: Asynchronous (Custom Settings)==");
        Console.WriteLine("NOTE: Look at output logs");
        settings.MaxIterations = 2;
        settings.StopIteratingOnException = false;

        Console.WriteLine("Changed Interation Settings");
        Console.WriteLine("  MaxIterations = 2");
        Console.WriteLine("  StopIteratingOnException = false");

        createTask = await _taskManager.StartTask(new SimpleBrokenTask(), TaskExecutionMode.Asyncronous, settings);
        if (!createTask.MethodSuccess)
        {
            Console.WriteLine("Error: " + createTask.Exception.Message);
        }

        Console.WriteLine("Finished creating task. Now waiting until its done.");

        await createTask.Result.RunningTask!;

        settings = new ManagedTaskSettings();

        Console.WriteLine("\n==Test 4: Asynchronous (Custom Settings)==");
        Console.WriteLine("NOTE: Look at output logs");
        settings.RetryOnException = true;
        settings.MaxRetryCount = 3;

        Console.WriteLine("Changed Interation Settings");
        Console.WriteLine("  RetryOnException = true");
        Console.WriteLine("  MaxRetryCount = 3");

        createTask = await _taskManager.StartTask(new SimpleBrokenTask(), TaskExecutionMode.Asyncronous, settings);
        if (!createTask.MethodSuccess)
        {
            Console.WriteLine("Error: " + createTask.Exception.Message);
        }

        Console.WriteLine("Finished creating task. Now waiting until its done.");

        await createTask.Result.RunningTask!;

        settings = new ManagedTaskSettings();

        Console.WriteLine("\n==Test 5: Asynchronous (Custom Settings)==");
        Console.WriteLine("NOTE: Look at output logs");
        settings.MaxIterations = 2;
        settings.RetryOnException = true;
        settings.MaxRetryCount = 3;
        settings.StopIterationAfterMaxRetries = false;

        Console.WriteLine("Changed Interation Settings");
        Console.WriteLine("  MaxIterations = 2");
        Console.WriteLine("  RetryOnException = true");
        Console.WriteLine("  MaxRetryCount = 3");
        Console.WriteLine("  StopIterationAfterMaxRetries = false");

        createTask = await _taskManager.StartTask(new SimpleBrokenTask(), TaskExecutionMode.Asyncronous, settings);
        if (!createTask.MethodSuccess)
        {
            Console.WriteLine("Error: " + createTask.Exception.Message);
        }

        Console.WriteLine("Finished creating task. Now waiting until its done.");

        await createTask.Result.RunningTask!;

        settings = new ManagedTaskSettings();

        Console.WriteLine("\n==Test 6: Asynchronous (Custom Settings)==");
        Console.WriteLine("NOTE: Look at output logs");
        settings.MaxIterations = 2;
        settings.IterationStrategy = new TimeStrategy_Interval(TimeSpan.FromSeconds(10), strategySettings);

        Console.WriteLine("Changed Interation Settings");
        Console.WriteLine("  MaxIterations = 2");
        Console.WriteLine("  IterationStrategy = Interval (Every 10 seconds)");

        createTask = await _taskManager.StartTask(new SimpleShortTask(), TaskExecutionMode.Asyncronous, settings);
        if (!createTask.MethodSuccess)
        {
            Console.WriteLine("Error: " + createTask.Exception.Message);
        }

        Console.WriteLine("Finished creating task. Now waiting until its done.");

        await createTask.Result.RunningTask!;

        settings = new ManagedTaskSettings();
        strategySettings = new TimeStrategySettings();

        Console.WriteLine("\n==Test 7: Asynchronous (Custom Settings)==");
        Console.WriteLine("NOTE: Look at output logs");
        settings.MaxIterations = 2;

        strategySettings.SkipFirstIterationWait = false;

        settings.IterationStrategy = new TimeStrategy_Interval(TimeSpan.FromSeconds(10), strategySettings);

        Console.WriteLine("Changed Interation Settings");
        Console.WriteLine("  MaxIterations = 2");
        Console.WriteLine("  IterationStrategy = Interval (Every 10 seconds)");
        Console.WriteLine("Changed Strategy Settings");
        Console.WriteLine("  SkipFirstIterationWait = false");

        createTask = await _taskManager.StartTask(new SimpleShortTask(), TaskExecutionMode.Asyncronous, settings);
        if (!createTask.MethodSuccess)
        {
            Console.WriteLine("Error: " + createTask.Exception.Message);
        }

        Console.WriteLine("Finished creating task. Now waiting until its done.");

        await createTask.Result.RunningTask!;

        settings = new ManagedTaskSettings();
        strategySettings = new TimeStrategySettings();

        Console.WriteLine("\n==Test 8: Asynchronous (Custom Settings)==");
        Console.WriteLine("NOTE: Look at output logs");
        settings.MaxIterations = 2;

        strategySettings.CustomStartDate = DateOnly.FromDateTime(DateTime.Now);
        strategySettings.CustomStartTime = DateTime.Now.TimeOfDay.Add(TimeSpan.FromSeconds(-15));

        settings.IterationStrategy = new TimeStrategy_Interval(TimeSpan.FromSeconds(10), strategySettings);

        Console.WriteLine("Changed Interation Settings");
        Console.WriteLine("  MaxIterations = 2");
        Console.WriteLine("  IterationStrategy = Interval (Every 10 seconds)");
        Console.WriteLine("Changed Strategy Settings");
        Console.WriteLine($"  CustomStartDate = {strategySettings.CustomStartDate}");
        Console.WriteLine($"  CustomStartTime = {strategySettings.CustomStartTime}");
        Console.WriteLine("NOTE: This will look like its not abiding the custom start date/time but it is. Its just catching up to the current time");

        createTask = await _taskManager.StartTask(new SimpleShortTask(), TaskExecutionMode.Asyncronous, settings);
        if (!createTask.MethodSuccess)
        {
            Console.WriteLine("Error: " + createTask.Exception.Message);
        }

        Console.WriteLine("Finished creating task. Now waiting until its done.");

        await createTask.Result.RunningTask!;

        settings = new ManagedTaskSettings();
        strategySettings = new TimeStrategySettings();

        Console.WriteLine("\n==Test 9: Synchronous (Custom Settings)==");
        Console.WriteLine("NOTE: Look at output logs");
        settings.MaxIterations = 2;

        strategySettings.CustomStartDate = DateOnly.FromDateTime(DateTime.Now);
        strategySettings.CustomStartTime = DateTime.Now.TimeOfDay.Add(TimeSpan.FromSeconds(-15));
        strategySettings.FastForwardToPresent = false;

        settings.IterationStrategy = new TimeStrategy_Interval(TimeSpan.FromSeconds(10), strategySettings);

        Console.WriteLine("Changed Interation Settings");
        Console.WriteLine("  MaxIterations = 2");
        Console.WriteLine("  IterationStrategy = Interval (Every 10 seconds)");
        Console.WriteLine("Changed Strategy Settings");
        Console.WriteLine($"  CustomStartDate = {strategySettings.CustomStartDate}");
        Console.WriteLine($"  CustomStartTime = {strategySettings.CustomStartTime}");
        Console.WriteLine("  FastForwardToPresent = false");

        createTask = await _taskManager.StartTask(new SimpleShortTask(), TaskExecutionMode.Asyncronous, settings);
        if (!createTask.MethodSuccess)
        {
            Console.WriteLine("Error: " + createTask.Exception.Message);
        }

        Console.WriteLine("Finished creating task. Now waiting until its done.");

        await createTask.Result.RunningTask!;

        settings = new ManagedTaskSettings();
        strategySettings = new TimeStrategySettings();

        Console.WriteLine("\n==Test 10: Asynchronous (Custom Settings)==");
        Console.WriteLine("NOTE: Look at output logs");
        settings.MaxIterations = 3;
        settings.AllowParallelIterationExecution = true;
        settings.MaxConcurrentParallelTasks = 2;

        Console.WriteLine("Changed Interation Settings");
        Console.WriteLine("  MaxIterations = 3");
        Console.WriteLine("  AllowParallelIterationExecution = true");
        Console.WriteLine("  MaxConcurrentParallelTasks = 2");

        createTask = await _taskManager.StartTask(new SimpleLongTask(), TaskExecutionMode.Asyncronous, settings);
        if (!createTask.MethodSuccess)
        {
            Console.WriteLine("Error: " + createTask.Exception.Message);
        }

        Console.WriteLine("Finished creating task. Now waiting until its done.");

        await createTask.Result.RunningTask!;

        settings = new ManagedTaskSettings();

        Console.WriteLine("\n==Test 11: Asynchronous (Custom Settings)==");
        Console.WriteLine("NOTE: Look at output logs");
        settings.MaxIterations = 2;

        Console.WriteLine("Changed Interation Settings");
        Console.WriteLine("  MaxIterations = 2");

        createTask = await _taskManager.StartTask(new SimpleShortTask(), TaskExecutionMode.Asyncronous, settings);
        if (!createTask.MethodSuccess)
        {
            Console.WriteLine("Error: " + createTask.Exception.Message);
        }

        Console.WriteLine("Finished creating task. Now waiting until its done.");

        while (!createTask.Result.RunningTask!.IsCompleted)
        {
            var tryGet1 = _taskRegistry.TryGet(createTask.Result.TaskKey);
            if (!tryGet1.MethodSuccess)
            {
                throw tryGet1.Exception;
            }

            Debug.WriteLine(tryGet1.Result?.GetSnapshotInfo(true));

            await Task.Delay(2000);
        }

        var tryGetHistory = _taskRegistry.TryGet(createTask.Result.TaskKey);
        if (!tryGetHistory.MethodSuccess)
        {
            throw tryGetHistory.Exception;
        }

        Console.WriteLine("Task Snapshot in Registry");
        string history = tryGetHistory.Result?.GetSnapshotInfo(true)!;
        Console.WriteLine(history);

        settings = new ManagedTaskSettings();
    }
}
