/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using Integration.DevKit.Core;
using Integration.DevKit.TaskMgmt;
using Integration.DevKit.TaskMgmt.Interfaces;
using Integration.DevKit.TaskMgmt.Models;

namespace TestApp.Demos;

/// <summary>
/// Async-mode task start/cancel/timeout smoke tests.
/// </summary>
public class TaskManagementDemo : IDemo
{
    public async Task RunAsync()
    {
        var _taskManager = Service_TaskMgmt.TaskManager;
        _taskManager.LogRuntimeSettings();

        OperationResult<IManagedTaskHandle> createTask;
        var settings = new ManagedTaskSettings();

        Console.WriteLine("\n==Test 1: Start Single Task==");
        Console.WriteLine("NOTE: Look at output logs");
        createTask = await _taskManager.StartTask(new SimpleLongTask(), TaskExecutionMode.Asyncronous, settings);
        if (!createTask.MethodSuccess)
        {
            Console.WriteLine("Error: " + createTask.Exception.Message);
        }

        Console.WriteLine("waiting 2 seconds for next task");
        await Task.Delay(2000);

        Console.WriteLine("\n==Test 2: Cancel Single Task==");
        Console.WriteLine("NOTE: Look at output logs");
        createTask.Result.Cancel();

        Console.WriteLine("waiting for task to finish");
        await createTask.Result.RunningTask!;
        Console.WriteLine("taks finished");

        settings = new ManagedTaskSettings();

        Console.WriteLine("\n==Test 3: Start Single Task==");
        Console.WriteLine("NOTE: Look at output logs");
        createTask = await _taskManager.StartTask(new SimpleLongTask_NoTokenChecking(), TaskExecutionMode.Asyncronous, settings);
        if (!createTask.MethodSuccess)
        {
            Console.WriteLine("Error: " + createTask.Exception.Message);
        }

        Console.WriteLine("waiting 2 seconds for next task");
        await Task.Delay(2000);

        Console.WriteLine("\n==Test 4: Cancel Single Task==");
        Console.WriteLine("This task does not check if the cancelation token is canceled so this will continue to run even after a cancel. ");
        Console.WriteLine("However the RunningTask will be canceled so you wont get stuck waiting for something that wont cancel");
        Console.WriteLine("NOTE: Look at output logs");
        createTask.Result.Cancel();

        Console.WriteLine("waiting for task to finish");
        await createTask.Result.RunningTask!;
        Console.WriteLine("task finished");

        settings = new ManagedTaskSettings();

        Console.WriteLine("\n==Test 5: Start Task with Timeout==");
        Console.WriteLine("NOTE: Look at output logs");
        settings.Timeout = TimeSpan.FromSeconds(3);

        createTask = await _taskManager.StartTask(new SimpleLongTask(), TaskExecutionMode.Asyncronous, settings);
        if (!createTask.MethodSuccess)
        {
            Console.WriteLine("Error: " + createTask.Exception.Message);
        }

        Console.WriteLine("waiting for task to finish");
        await createTask.Result.RunningTask!;
        Console.WriteLine("task finished");

        settings = new ManagedTaskSettings();
    }
}
