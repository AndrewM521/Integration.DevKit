/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using Integration.DevKit.TaskMgmt.Contracts.Interfaces;
using Integration.DevKit.TaskMgmt.Contracts.Models;
using System.Diagnostics;

namespace TestApp;

internal class SimpleBrokenTask : ManagedTask
{
    public SimpleBrokenTask() : base("SimpleBrokenTask") {}

    public override async Task DoTaskWork(IManagedTaskIterationHandle iterationHandle)
    {
        Console.WriteLine($"\n-----Iteration {iterationHandle.IterationNumber}-----");
        Console.WriteLine($"Start Time: {iterationHandle.TaskHandle.StartDTM}");
        Console.WriteLine($"Start Iteration Time: {iterationHandle.StartDTM}");

        for (int i = 0; i < 5; i++)
        {
            Console.WriteLine(i);

            await Task.Delay(1000, iterationHandle.CancelationToken);
        }

        Console.WriteLine($"End Time: {DateTime.Now}. Elapsed Iteration Time: {iterationHandle.Runtime}");
        Console.WriteLine($"End Time: {DateTime.Now}. Elapsed Time: {iterationHandle.TaskHandle.Runtime}");

        throw new NotImplementedException();
    }
}
