using AndrewM5.DevKit.Core.Results;
using AndrewM5.DevKit.CustomLogger.Contracts.Interfaces;
using AndrewM5.DevKit.ProcessLauncher.Contracts.Interfaces;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

namespace AndrewM5.DevKit.ProcessLauncher;


/// <summary>
/// Concrete Implementation of <see cref="IProcessManager"/>
/// </summary>
public class ProcessManager : IProcessManager
{
    private readonly ConcurrentDictionary<string, ManagedProcess> _processes = new ConcurrentDictionary<string, ManagedProcess>();

    private readonly ICustomLogger? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessManager"/> class.
    /// </summary>
    /// <param name="loggerManager">An optional logger manager to provide contextual logging for the launcher.</param>
    public ProcessManager (ICustomLoggerManager? loggerManager = null)
    {
        _logger = loggerManager?.GetLogger("ProcessLauncherManager");
    }

    /// <inheritdoc/>
    /// <remarks>
    /// This method validates the command path and ensures the <paramref name="config.ProcessKey"/> 
    /// is not already in use before instantiating a <see cref="ManagedProcess"/>. 
    /// If the startup fails, the exception is caught and returned within the <see cref="OperationResult{T}"/>.
    /// </remarks>
    public OperationResult<IManagedProcess> StartProcess(IManagedProcessConfig config)
    {
        var result = new OperationResult<IManagedProcess>();

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

    /// <inheritdoc/>
    /// <remarks>
    /// Attempts to remove the process from the internal tracking dictionary. If found, 
    /// the process's own Cancel method is invoked. 
    /// </remarks>
    /// <exception cref="KeyNotFoundException">Returned inside the result if the key does not exist.</exception>
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

    /// <inheritdoc/>
    /// <remarks>
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

    /// <inheritdoc/>
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

    /// <inheritdoc/>
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
