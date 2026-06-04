/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using Integration.DevKit.TaskMgmt.Contracts.Interfaces;
using Integration.DevKit.TaskMgmt.Contracts.Models;

namespace TestApp;

internal class SimpleDynamicTask : ManagedTask
{
    public SimpleDynamicTask() : base("SimpleDynamicTask") {}

    public override async Task DoTaskWork(IManagedTaskIterationHandle iterationHandle)
    {
        Console.WriteLine($"\n-----Iteration {iterationHandle.IterationNumber}-----");
        Console.WriteLine($"Start Time: {iterationHandle.TaskHandle.StartDTM}");
        Console.WriteLine($"Start Iteration Time: {iterationHandle.StartDTM}");

        if (iterationHandle.IterationNumber == 1)
        {
            Console.WriteLine($"Iteration {iterationHandle.IterationNumber}. Waiting for 5 seconds");

            await Task.Delay(TimeSpan.FromSeconds(5));

            Console.WriteLine($"Iteration {iterationHandle.IterationNumber} Done");
        }
        else if (iterationHandle.IterationNumber == 2)
        {
            Console.WriteLine($"Iteration {iterationHandle.IterationNumber}. Waiting for 30 seconds");

            await Task.Delay(TimeSpan.FromSeconds(30));

            Console.WriteLine($"Iteration {iterationHandle.IterationNumber} Done");
        }
        else if (iterationHandle.IterationNumber == 3)
        {
            Console.WriteLine($"Iteration {iterationHandle.IterationNumber}. Waiting for 1 Minute");

            await Task.Delay(TimeSpan.FromMinutes(1));

            Console.WriteLine($"Iteration {iterationHandle.IterationNumber} Done");
        }

        Console.WriteLine($"End Time: {DateTime.Now}. Elapsed Iteration Time: {iterationHandle.Runtime}");
        Console.WriteLine($"End Time: {DateTime.Now}. Elapsed Time: {iterationHandle.TaskHandle.Runtime}");
    }
}
