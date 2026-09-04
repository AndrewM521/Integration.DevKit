
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Diagnostics;
using Integration.DevKit.Core;
using Integration.DevKit.Core.Logging;
using Integration.DevKit.ProcessLauncher.Settings;

namespace Integration.DevKit.ProcessLauncher;


/// <summary>
/// Orchestrator responsible for spawning, tracking, and terminating managed processes.
/// </summary>
/// <remarks>
/// This manager maintains an internal registry of <see cref="ManagedProcess"/> instances,
/// keyed by their <see cref="ManagedProcessConfig.ProcessKey"/>. It acts as a single point of
/// control for aggregate operations and process lookups.
/// </remarks>
public class ProcessManager
{
    /// <summary>
    /// Gets or sets the current runtime settings for this manager, initialized from the bound
    /// <see cref="ProcessLauncherSettings"/>. Mutate this in place (e.g. <c>RuntimeSettings.EnableLogging = false</c>)
    /// to change behavior, including logging, at runtime.
    /// </summary>
    public ProcessLauncherSettings RuntimeSettings { get; set; }

    private readonly ConcurrentDictionary<string, ManagedProcess> _processes = new ConcurrentDictionary<string, ManagedProcess>();

    private readonly ILogger? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessManager"/> class.
    /// </summary>
    /// <param name="settings">The initial configuration settings injected via the Options pattern.</param>
    /// <param name="loggerFactory">An optional logger factory to provide contextual logging for the launcher.</param>
    public ProcessManager(IOptions<ProcessLauncherSettings> settings, ILoggerFactory? loggerFactory = null)
    {
        RuntimeSettings = settings?.Value.Clone() ?? new ProcessLauncherSettings();

        _logger = loggerFactory?.CreateConditionalLogger("ProcessLauncherManager", () => RuntimeSettings.EnableLogging);
    }

    /// <summary>
    /// Initializes and starts a new process based on the provided configuration.
    /// </summary>
    /// <param name="config">The configuration settings for the process, including command, arguments, and monitoring rules.</param>
    /// <returns>
    /// An <see cref="OperationResult{ManagedProcess}"/> containing the managed process instance if successful;
    /// otherwise, a failure result containing error details.
    /// </returns>
    /// <remarks>
    /// This method validates the command path and ensures <paramref name="config"/>'s <c>ProcessKey</c>
    /// is not already in use before instantiating a <see cref="ManagedProcess"/>.
    /// If the startup fails, the exception is caught and returned within the <see cref="OperationResult{T}"/>.
    /// </remarks>
    public OperationResult<ManagedProcess> StartProcess(ManagedProcessConfig config)
    {
        var result = new OperationResult<ManagedProcess>();

        try
        {
            if (string.IsNullOrWhiteSpace(config.Command))
            {
                throw new ArgumentException("Command must be specified.");
            }

            if (_processes.ContainsKey(config.ProcessKey))
            {
                throw new InvalidOperationException($"Process '{config.ProcessKey}' is already running.");
            }

            var process = new ManagedProcess(config, _logger);
            
            var startProcess = StartProcess(process);
            if (!startProcess.MethodSuccess)
            {
                throw startProcess.Exception;
            }

            _processes[config.ProcessKey] = process;

            return result.SetMethodSuccess(process);
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Failed to start process {config.ProcessKey}");

            return result.SetMethodFailure(ex);
        }
    }

    /// <summary>
    /// Attempts to cancel and stop a specific process identified by its unique key.
    /// </summary>
    /// <param name="processKey">The unique identifier associated with the process to be cancelled.</param>
    /// <param name="forceKill">
    /// If <see langword="true"/>, the process is terminated immediately (SIGKILL);
    /// otherwise, a graceful shutdown is attempted. Defaults to <see langword="false"/>.
    /// </param>
    /// <returns>
    /// A <see cref="NullOperationResult"/> indicating whether the cancellation was successful.
    /// Returns a failure if the <paramref name="processKey"/> is not found.
    /// </returns>
    /// <remarks>
    /// Attempts to remove the process from the internal tracking dictionary. If found,
    /// the process's own Cancel method is invoked. If <paramref name="processKey"/> is not found, a
    /// failed <see cref="NullOperationResult"/> wrapping a <see cref="KeyNotFoundException"/> is
    /// returned (the exception is not thrown to the caller).
    /// </remarks>
    public NullOperationResult CancelProcess(string processKey, bool forceKill = false)
    {
        var result = new NullOperationResult();

        try
        {
            if (_processes.TryRemove(processKey, out var baseManagedProcess))
            {
                var cancelProcess = baseManagedProcess.Cancel(forceKill);
                if (!cancelProcess.MethodSuccess)
                {
                    throw cancelProcess.Exception;
                }

                return result.SetMethodSuccess();
            }
            else
            {
                throw new KeyNotFoundException($"Process '{processKey}' not found.");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Failed to cancel process {processKey}");

            return result.SetMethodFailure(ex);
        }
    }

    /// <summary>
    /// Attempts to cancel and stop all currently active processes managed by this instance.
    /// </summary>
    /// <param name="forceKill">
    /// If <see langword="true"/>, all processes are terminated immediately;
    /// otherwise, graceful shutdowns are attempted. Defaults to <see langword="false"/>.
    /// </param>
    /// <returns>
    /// A <see cref="NullOperationResult"/> indicating the overall success of the mass cancellation.
    /// </returns>
    /// <remarks>
    /// In the event of a partial failure (some processes stopped while others failed to terminate),
    /// the returned result should aggregate these errors.
    /// Iterates through all active keys and attempts to cancel each.
    /// Any caught errors are aggregated into a single <see cref="AggregateException"/>.
    /// </remarks>
    public NullOperationResult CancelAllProcesses(bool forceKill = false)
    {
        var result = new NullOperationResult();
        List<Exception> errors = new List<Exception>();

        foreach (var key in _processes.Keys)
        {
            var cancelProcess = CancelProcess(key, forceKill);
            if (!cancelProcess.MethodSuccess)
            {
                errors.Add(cancelProcess.Exception);
            }
        }

        if (errors.Count > 0)
        {
            _logger?.LogError($"Failed to cancel all processes.");

            return result.SetMethodFailure(new AggregateException(errors));
        }

        return result.SetMethodSuccess();
    }

    /// <summary>
    /// Checks the current status of a managed process to determine if it is still executing.
    /// </summary>
    /// <param name="processKey">The unique identifier of the process to check.</param>
    /// <returns>
    /// An <see cref="OperationResult{Boolean}"/> where the value is <see langword="true"/> if the process is
    /// found and currently running; otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// Performs a high-performance lookup in the internal <see cref="ConcurrentDictionary{TKey, TValue}"/>.
    /// </remarks>
    public OperationResult<bool> IsRunning(string processKey)
    {
        var result = new OperationResult<bool>();

        try
        {
            return result.SetMethodSuccess(_processes.ContainsKey(processKey));
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Failed to check if proccess {processKey} is running.");

            return result.SetMethodFailure(ex);
        }
    }

    /// <summary>
    /// Periodically polls the process status until it exits or the cancellation token is triggered.
    /// </summary>
    /// <param name="process">The process to monitor.</param>
    /// <param name="token">A token to signal abandonment of the wait operation.</param>
    public async Task WaitForExitAsync(Process process, CancellationToken token = default)
    {
        while (process != null && !process.HasExited)
        {
            await Task.Delay(250, token);
        }
    }


    /// <inheritdoc />
    /// <remarks>
    /// Configures event handlers for asynchronous stream reading and begins process execution. 
    /// Once started, a background monitoring loop is initiated.
    /// </remarks>
    private NullOperationResult StartProcess(ManagedProcess managedProcess)
    {
        var result = new NullOperationResult();

        try
        {
            managedProcess.Process = new Process { StartInfo = managedProcess._startInfo, EnableRaisingEvents = true };

            if (!managedProcess._startInfo.UseShellExecute)
            {
                managedProcess.Process.OutputDataReceived += (_, e) => {
                    if (e.Data != null)
                    {
                        managedProcess._stdout.AppendLine(e.Data);
                    }
                };
                managedProcess.Process.ErrorDataReceived += (_, e) => {
                    if (e.Data != null)
                    {
                        managedProcess._stderr.AppendLine(e.Data);
                    }
                };
            }

            managedProcess.Process.Exited += (_, _) => {
                _logger?.LogInformation($"Process '{managedProcess.ProcessKey}' exited with code {managedProcess.Process?.ExitCode}");

                if (_processes.TryGetValue(managedProcess.ProcessKey, out var foundProcess))
                {
                    _processes.Remove(foundProcess.ProcessKey, out ManagedProcess? _);
                }
            };

            managedProcess.Process.Start();

            if (!managedProcess._startInfo.UseShellExecute)
            {
                managedProcess.Process.BeginOutputReadLine();
                managedProcess.Process.BeginErrorReadLine();
            }

            managedProcess.StartTime = DateTime.UtcNow;

            managedProcess.MonitorTask = Task.Run(async () =>
            {
                try
                {
                    if (managedProcess._timeout.HasValue)
                    {
                        using var timeoutCts = new CancellationTokenSource(managedProcess._timeout.Value);
                        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(managedProcess._cts.Token, timeoutCts.Token);


                        await WaitForExitAsync(managedProcess.Process, linkedCts.Token);
                    }
                    else
                    {
                        await WaitForExitAsync(managedProcess.Process, managedProcess._cts.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                    if (managedProcess._cts.IsCancellationRequested)
                    {
                        _logger?.LogInformation($"Process '{managedProcess.ProcessKey}' cancelled by user.");
                    }
                    else
                    {
                        _logger?.LogWarning($"Process '{managedProcess.ProcessKey}' timed out after {managedProcess._timeout}.");
                    }

                    managedProcess.Cancel(true);
                }
            });

            return result.SetMethodSuccess();
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }
}
