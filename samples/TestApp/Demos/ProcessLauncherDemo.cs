/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using Integration.DevKit.ProcessLauncher;

namespace TestApp.Demos;

/// <summary>
/// Demonstrates <see cref="ProcessManager"/> starting, waiting on, and canceling managed OS processes.
/// </summary>
public class ProcessLauncherDemo : IDemo
{
    public async Task RunAsync()
    {
        Console.WriteLine("----|----|Process Launcher|----|----");
        var _processManager = Service_ProcessLauncher.ProcessManager;

        Console.WriteLine("Console Command Ping Test (Limited)");
        var processConfig_Limited = new ManagedProcessConfig
        {
            ProcessKey = "PingTest",
            Command = "cmd.exe",
            Arguments = "/c ping 127.0.0.1 -n 4",
            ShowWindow = true,
            TimeoutSeconds = 10,
            WorkingDirectory = Environment.CurrentDirectory
        };

        var startResult = _processManager.StartProcess(processConfig_Limited);
        if (!startResult.MethodSuccess)
        {
            Console.WriteLine($"Failed to start process: {startResult.Exception}");
            return;
        }

        await _processManager.WaitForExitAsync(startResult.Result.Process);

        Console.WriteLine("Console Command Ping Test");
        var processConfig = new ManagedProcessConfig
        {
            ProcessKey = "PingTest",
            Command = "cmd.exe",
            Arguments = "/c ping 127.0.0.1 -t",
            ShowWindow = true,
            TimeoutSeconds = 10,
            WorkingDirectory = Environment.CurrentDirectory
        };

        var startResult1 = _processManager.StartProcess(processConfig);
        if (!startResult1.MethodSuccess)
        {
            Console.WriteLine($"Failed to start process: {startResult1.Exception}");
            return;
        }

        await Task.Delay(3000);

        Console.WriteLine("Canceling Console Command Ping Test");
        startResult.Result.Cancel();

        await _processManager.WaitForExitAsync(startResult1.Result.Process);
    }
}
