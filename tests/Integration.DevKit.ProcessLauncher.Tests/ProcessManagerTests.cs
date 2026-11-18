using Integration.DevKit.ProcessLauncher.Settings;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace Integration.DevKit.ProcessLauncher.Tests;

public class ProcessManagerTests
{
    // About 30 seconds of work, long enough that a test always has to stop it
    private const string LongRunningWindowsArgs = "/c ping 127.0.0.1 -n 30";
    private const string LongRunningUnixArgs = "-c \"sleep 30\"";

    /// <summary>
    /// Builds a config that runs a shell command: cmd.exe on Windows, /bin/sh everywhere else.
    /// </summary>
    private static ManagedProcessConfig ShellCommand(string key, string windowsArguments, string unixArguments)
    {
        string command = "/bin/sh";
        string arguments = unixArguments;

        if (OperatingSystem.IsWindows())
        {
            command = "cmd.exe";
            arguments = windowsArguments;
        }

        return new ManagedProcessConfig
        {
            ProcessKey = key,
            Command = command,
            Arguments = arguments
        };
    }

    private static ManagedProcessConfig ExitImmediately(string key)
    {
        return ShellCommand(key, "/c exit 0", "-c \"exit 0\"");
    }

    [Fact]
    public async Task StartProcess_FastExitingProcess_IsNotLeftRegisteredAndKeyCanBeReused()
    {
        // A process that exits before Start() returns used to stay registered as running forever
        var manager = new ProcessManager(Options.Create(new ProcessLauncherSettings()));

        var first = manager.StartProcess(ExitImmediately("fast"));
        Assert.True(first.MethodSuccess);
        await first.Result.MonitorTask!;

        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (manager.IsRunning("fast").Result && DateTime.UtcNow < deadline)
        {
            await Task.Delay(20);
        }

        Assert.False(manager.IsRunning("fast").Result);

        var second = manager.StartProcess(ExitImmediately("fast"));
        Assert.True(second.MethodSuccess);
        await second.Result.MonitorTask!;
    }

    [Fact]
    public async Task Output_IsCappedAtMaxOutputLines_KeepingTheNewest()
    {
        var manager = new ProcessManager(Options.Create(new ProcessLauncherSettings()));
        var config = ShellCommand("capped", "/c echo one& echo two& echo three", "-c \"echo one; echo two; echo three\"");
        config.MaxOutputLines = 2;

        var start = manager.StartProcess(config);
        Assert.True(start.MethodSuccess);
        await start.Result.MonitorTask!;
        start.Result.Process!.WaitForExit(); // flushes the async output handlers

        List<string> lines = new List<string>();
        foreach (string line in start.Result.GetOutput().Result.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries))
        {
            // cmd's echo keeps the space before "&", so trim each line
            lines.Add(line.Trim());
        }

        Assert.Equal(new List<string> { "two", "three" }, lines);
    }

    [Fact]
    public async Task DisposeAsync_CancelsRunningProcesses()
    {
        var manager = new ProcessManager(Options.Create(new ProcessLauncherSettings()));
        var start = manager.StartProcess(ShellCommand("long", LongRunningWindowsArgs, LongRunningUnixArgs));
        Assert.True(start.MethodSuccess);
        Assert.False(start.Result.Process!.HasExited);

        await manager.DisposeAsync();

        Assert.True(start.Result.Process!.WaitForExit(10_000));
    }

    [Fact]
    public async Task TimeoutSeconds_KillsAProcessThatRunsTooLong()
    {
        var manager = new ProcessManager(Options.Create(new ProcessLauncherSettings()));
        var config = ShellCommand("timeout", LongRunningWindowsArgs, LongRunningUnixArgs);
        config.TimeoutSeconds = 1;

        var start = manager.StartProcess(config);
        Assert.True(start.MethodSuccess);

        var timer = Stopwatch.StartNew();
        await start.Result.MonitorTask!;

        // Killed by the monitor after ~1 second, not left running for 30
        Assert.True(start.Result.Process!.WaitForExit(5_000), $"Process still running after {timer.Elapsed}");
        Assert.True(timer.Elapsed < TimeSpan.FromSeconds(10));

        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (manager.IsRunning("timeout").Result && DateTime.UtcNow < deadline)
        {
            await Task.Delay(20);
        }

        Assert.False(manager.IsRunning("timeout").Result);
    }

    [Fact]
    public void GracefulCancel_WaitsBeforeKillingTheProcess()
    {
        // The monitor used to force kill as soon as the cancel token fired, so the 3-second graceful wait never happened
        var manager = new ProcessManager(Options.Create(new ProcessLauncherSettings()));
        var start = manager.StartProcess(ShellCommand("graceful", LongRunningWindowsArgs, LongRunningUnixArgs));
        Assert.True(start.MethodSuccess);

        var timer = Stopwatch.StartNew();
        Assert.True(manager.CancelProcess("graceful").MethodSuccess);
        timer.Stop();

        // No window to close, so it waits the full grace period and then kills
        Assert.True(timer.Elapsed >= TimeSpan.FromSeconds(2.5), $"Cancel returned after {timer.Elapsed}");
        Assert.True(start.Result.Process!.WaitForExit(10_000));
    }
}
