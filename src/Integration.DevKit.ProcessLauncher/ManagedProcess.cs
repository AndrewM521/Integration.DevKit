using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;
using Integration.DevKit.ProcessLauncher.Contracts;
using Integration.DevKit.CustomLogger.Contracts;
using Integration.DevKit.Core;

namespace Integration.DevKit.ProcessLauncher;

/// <summary>
/// Concrete Implementation of <see cref="IManagedProcess"/>
/// </summary>

public class ManagedProcess : IManagedProcess
{
    private readonly ICustomLogger? _logger;

    internal readonly ProcessStartInfo _startInfo;
    internal readonly StringBuilder _stdout = new StringBuilder();
    internal readonly StringBuilder _stderr = new StringBuilder();
    internal readonly TimeSpan? _timeout;
    internal readonly CancellationTokenSource _cts = new CancellationTokenSource();

    /// <inheritdoc />
    public string ProcessKey { get; }

    /// <inheritdoc />
    /// <value>The underlying <see cref="Process"/>; remains available until the instance is disposed.</value>
    public Process? Process { get; internal set; }

    /// <inheritdoc />
    /// <remarks>
    /// This task runs for the duration of the process lifetime. It completes when the process 
    /// exits naturally, times out, or is explicitly cancelled.
    /// </remarks>
    public Task? MonitorTask { get; internal set; }

    /// <inheritdoc />
    public DateTime StartTime { get; internal set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedProcess"/> class.
    /// </summary>
    /// <param name="config">The configuration defining how the process should be launched.</param>
    /// <param name="logger">An optional logger for internal event tracking.</param>
    internal ManagedProcess(IManagedProcessConfig config, ICustomLogger? logger = null)
    {
        _logger = logger;

        ProcessKey = config.ProcessKey;
        _timeout = config.TimeoutSeconds <= 0 ? null : TimeSpan.FromSeconds(config.TimeoutSeconds);

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
    /// When <paramref name="forceKill"/> is <see langword="false"/>, the method attempts 
    /// <see cref="Process.CloseMainWindow"/> and waits up to 3 seconds. If the process does 
    /// not exit within that window, a recursive <see cref="Process.Kill(bool)"/> is performed.
    /// </remarks>
    public NullOperationResult Cancel(bool forceKill = false)
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

                try
                {
                    if (!Process.WaitForExit((int)waitTime.TotalMilliseconds))
                    {
                        Process.Kill(true);

                        _logger?.LogWarning($"Failure to close proccess after waiting {waitTime.Seconds} seconds. Process '{ProcessKey}' force killed.");
                    }
                }
                catch
                {
                    _logger?.LogError($"Failed to cancel, trying to force cancel now..");

                    Process.Kill(true);
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
