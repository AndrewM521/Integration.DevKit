using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;
using Integration.DevKit.Core;

namespace Integration.DevKit.ProcessLauncher;

/// <summary>
/// Managed process wrapper, providing mechanisms to start,
/// monitor, and terminate an external system process.
/// </summary>

public class ManagedProcess : IAsyncDisposable
{
    private readonly ILogger? _logger;

    internal readonly ProcessStartInfo _startInfo;
    internal readonly StringBuilder _stdout = new StringBuilder();
    internal readonly StringBuilder _stderr = new StringBuilder();
    internal readonly TimeSpan? _timeout;
    internal readonly CancellationTokenSource _cts = new CancellationTokenSource();

    /// <summary>
    /// Gets a unique identifier associated with this managed process instance.
    /// </summary>
    /// <value>A string used to track or look up the process within a manager or collection.</value>
    public string ProcessKey { get; }

    /// <summary>
    /// Gets the underlying <see cref="System.Diagnostics.Process"/> instance.
    /// </summary>
    /// <value>The underlying <see cref="Process"/>; remains available until the instance is disposed.</value>
    public Process? Process { get; internal set; }

    /// <summary>
    /// Gets the task responsible for monitoring the process lifecycle.
    /// </summary>
    /// <remarks>
    /// This task runs for the duration of the process lifetime. It completes when the process
    /// exits naturally, times out, or is explicitly cancelled.
    /// </remarks>
    public Task? MonitorTask { get; internal set; }

    /// <summary>
    /// Gets the timestamp of when the process was officially started.
    /// </summary>
    public DateTime StartTime { get; internal set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedProcess"/> class.
    /// </summary>
    /// <param name="config">The configuration defining how the process should be launched.</param>
    /// <param name="logger">An optional logger for internal event tracking.</param>
    internal ManagedProcess(ManagedProcessConfig config, ILogger? logger = null)
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

    /// <summary>
    /// Cancels the running process.
    /// </summary>
    /// <param name="forceKill">
    /// If <see langword="true"/>, the process is terminated immediately via <see cref="Process.Kill()"/>;
    /// otherwise, a graceful shutdown is attempted (e.g., sending a close signal to the main window).
    /// </param>
    /// <returns>
    /// A <see cref="NullOperationResult"/> indicating the outcome of the cancellation request.
    /// </returns>
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

    /// <summary>
    /// Captures and returns the standard output (STDOUT) produced by the process.
    /// </summary>
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

    /// <summary>
    /// Captures and returns the standard error (STDERR) produced by the process.
    /// </summary>
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
