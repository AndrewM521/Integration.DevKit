using AndrewM5.DevKit.Core.Results;
using AndrewM5.DevKit.Logging.Contracts.Interfaces;
using AndrewM5.DevKit.ProcessLauncher.Contracts.Interfaces;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;

namespace AndrewM5.DevKit.ProcessLauncher;

/// <summary>
/// A concrete implementation of <see cref="IManagedProcess"/> that wraps a <see cref="System.Diagnostics.Process"/>.
/// Handles stream redirection, asynchronous monitoring, and lifecycle management.
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
    public Process? Process { get; private set; }

    /// <inheritdoc />
    public Task? MonitorTask { get; private set; }

    /// <inheritdoc />
    public DateTime StartTime { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedProcess"/> class.
    /// </summary>
    /// <param name="config">The configuration defining how the process should be launched.</param>
    /// <param name="logger">An optional logger for internal event tracking.</param>
    public ManagedProcess(IManagedProcessConfig config, ICustomLogger? logger = null)
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
    /// This method initializes the internal <see cref="Process"/> object, attaches 
    /// output data handlers if redirected, and kicks off the <see cref="MonitorTask"/>.
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
    /// If <paramref name="forceKill"/> is false, the implementation attempts a graceful 
    /// <see cref="Process.CloseMainWindow"/> with a 3-second timeout before falling back to a hard kill.
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
    private static async Task WaitForExitAsync(Process process, CancellationToken token)
    {
        while (!process.HasExited)
        {
            await Task.Delay(250, token);
        }
    }

    /// <summary>
    /// Triggers cancellation, waits for the monitor task to conclude, and releases all process and logging resources.
    /// </summary>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous disposal operation.</returns>
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
