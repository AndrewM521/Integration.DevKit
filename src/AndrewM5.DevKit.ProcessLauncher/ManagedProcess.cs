using AndrewM5.DevKit.Core.Results;
using AndrewM5.DevKit.CustomLogger.Contracts.Interfaces;
using AndrewM5.DevKit.ProcessLauncher.Contracts.Interfaces;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;

namespace AndrewM5.DevKit.ProcessLauncher;

/// <summary>
/// Concrete Implementation of <see cref="IManagedProcess"/>
/// </summary>

public class ManagedProcess : IManagedProcess
{
    private readonly ICustomLogger? _logger;
    private readonly ProcessStartInfo _startInfo;
    private readonly CancellationTokenSource _cts = new CancellationTokenSource();
    private readonly StringBuilder _stdout = new StringBuilder();
    private readonly StringBuilder _stderr = new StringBuilder();
    private readonly TimeSpan? _timeout;

    /// <inheritdoc />
    public string ProcessKey { get; }

    /// <inheritdoc />
    /// <value>The underlying <see cref="Process"/>; remains available until the instance is disposed.</value>
    public Process? Process { get; private set; }

    /// <inheritdoc />
    /// <remarks>
    /// This task runs for the duration of the process lifetime. It completes when the process 
    /// exits naturally, times out, or is explicitly cancelled.
    /// </remarks>
    public Task? MonitorTask { get; private set; }

    /// <inheritdoc />
    public DateTime StartTime { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedProcess"/> class.
    /// </summary>
    /// <param name="config">The configuration defining how the process should be launched.</param>
    /// <param name="logger">An optional logger for internal event tracking.</param>
    internal ManagedProcess(IManagedProcessConfig config, ICustomLogger? logger = null)
    {
        ProcessKey = config.ProcessKey;
        _logger = logger;
        _timeout = TimeSpan.FromSeconds(config.TimeoutSeconds);

        _startInfo = new ProcessStartInfo
        {
            FileName = config.Command,
            Arguments = config.Arguments,
            UseShellExecute = config.ShowWindow,
            RedirectStandardOutput = !config.ShowWindow,
            RedirectStandardError = !config.ShowWindow,
            CreateNoWindow = !config.ShowWindow,
            WorkingDirectory = config.WorkingDirectory
        };

        if (config.ShowWindow)
        {
            _startInfo.WindowStyle = ProcessWindowStyle.Normal;
        }
        else
        {
            _startInfo.WindowStyle = ProcessWindowStyle.Hidden;
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Configures event handlers for asynchronous stream reading and begins process execution. 
    /// Once started, a background monitoring loop is initiated.
    /// </remarks>
    public NullOperationResult Start()
    {
        var result = new NullOperationResult();
        
        try
        {
            Process = new Process { StartInfo = _startInfo, EnableRaisingEvents = true };
            
            if (!_startInfo.UseShellExecute)
            {
                Process.OutputDataReceived += (_, e) => {
                    if (e.Data != null)
                    {
                        _stdout.AppendLine(e.Data);
                    }
                };
                Process.ErrorDataReceived += (_, e) => {
                    if (e.Data != null)
                    {
                        _stderr.AppendLine(e.Data);
                    }
                };
            }

            Process.Exited += (_, _) => {
                _logger?.LogInformation($"Process '{ProcessKey}' exited with code {Process?.ExitCode}");
            };

            Process.Start();

            if (!_startInfo.UseShellExecute)
            {
                Process.BeginOutputReadLine();
                Process.BeginErrorReadLine();
            }

            StartTime = DateTime.UtcNow;

            MonitorTask = Task.Run(async () =>
            {
                try
                {
                    if (_timeout.HasValue)
                    {
                        using var timeoutCts = new CancellationTokenSource(_timeout.Value);
                        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, timeoutCts.Token);
                        await WaitForExitAsync(Process, linkedCts.Token);
                    }
                    else
                    {
                        await WaitForExitAsync(Process, _cts.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                    if (_cts.IsCancellationRequested)
                    {
                        _logger?.LogInformation($"Process '{ProcessKey}' cancelled by user.");
                    }
                    else
                    {
                        _logger?.LogWarning($"Process '{ProcessKey}' timed out after {_timeout}.");
                    }

                    Cancel(true);
                }
            });

            return result.SetMethodSuccess();
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// When <paramref name="forceKill"/> is <see langword="false"/>, the method attempts 
    /// <see cref="Process.CloseMainWindow"/> and waits up to 3 seconds. If the process does 
    /// not exit within that window, a recursive <see cref="Process.Kill(bool)"/> is performed.
    /// </remarks>
    public NullOperationResult Cancel(bool forceKill)
    {
        var result = new NullOperationResult();

        try
        {
            _cts.Cancel();

            if (Process == null || Process.HasExited)
            {
                return result.SetMethodSuccess();
            }

            if (forceKill)
            {
                Process.Kill(true);

                _logger?.LogInformation($"Process '{ProcessKey}' force killed.");
            }
            else
            {
                TimeSpan waitTime = TimeSpan.FromSeconds(3);
                Process.CloseMainWindow();

                if (!Process.WaitForExit((int)waitTime.TotalMilliseconds))
                {
                    Process.Kill(true);

                    _logger?.LogWarning($"Failure to close proccess after waiting {waitTime.Seconds} seconds. Process '{ProcessKey}' force killed.");
                }
            }

            return result.SetMethodSuccess();
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }

    /// <inheritdoc />
    /// <returns>An <see cref="OperationResult{String}"/> containing the full contents of the STDOUT buffer.</returns>
    public OperationResult<string> GetOutput()
    {
        var result = new OperationResult<string>();

        try
        {
            return result.SetMethodSuccess(_stdout.ToString());
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }

    /// <inheritdoc />
    /// <returns>An <see cref="OperationResult{String}"/> containing the full contents of the STDERR buffer.</returns>
    public OperationResult<string> GetError() 
    {
        var result = new OperationResult<string>();

        try
        {
            return result.SetMethodSuccess(_stderr.ToString());
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }

    /// <summary>
    /// Periodically polls the process status until it exits or the cancellation token is triggered.
    /// </summary>
    /// <param name="process">The process to monitor.</param>
    /// <param name="token">A token to signal abandonment of the wait operation.</param>
    private static async Task WaitForExitAsync(Process process, CancellationToken token)
    {
        while (!process.HasExited)
        {
            await Task.Delay(250, token);
        }
    }

    /// <summary>
    /// Triggers cancellation, awaits the monitor task, and disposes of process handles and stream buffers.
    /// </summary>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous disposal.</returns>
    public async ValueTask DisposeAsync()
    {
        try
        {
            _cts.Cancel();

            if (MonitorTask != null)
            {
                await MonitorTask;
            }
        }
        catch (OperationCanceledException)
        {
            // Do Nothing, this is expected
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Error disposing proccess: {ex}");
        }
        finally
        {
            _cts.Dispose();
            Process?.Dispose();
        }
    }
}
