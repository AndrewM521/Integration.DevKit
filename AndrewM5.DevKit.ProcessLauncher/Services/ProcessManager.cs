using AndrewM5.DevKit.Core.Results;
using AndrewM5.DevKit.Logging.Abstractions;
using AndrewM5.DevKit.ProcessLauncher.Abstractions;
using AndrewM5.DevKit.ProcessLauncher.Abstractions.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Reflection;

namespace AndrewM5.DevKit.ProcessLauncher.Services;

public class ProcessManager : IProcessManager
{
    public ProcessManagerSettings RuntimeSettings { get; init; }

    private readonly ConcurrentDictionary<string, ManagedProcess> _processes = new ConcurrentDictionary<string, ManagedProcess>();
    private readonly ICustomLogger? _logger;

    public ProcessManager (IOptions<ProcessManagerSettings> settings, ICustomLoggerManager loggerManager)
    {
        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        if (loggerManager == null)
        {
            throw new ArgumentNullException(nameof(loggerManager));
        }

        _logger = loggerManager.GetLogger("ProcessLauncherManager");

        RuntimeSettings = settings.Value.Clone();

        if (RuntimeSettings.MaxProcessesCount < 0)
        {
            RuntimeSettings.MaxProcessesCount = int.MaxValue;
        }
    }

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

            if (_processes.Count >= RuntimeSettings.MaxProcessesCount)
            {
                throw new InvalidOperationException($"Cannot store more than {RuntimeSettings.MaxProcessesCount} processes.");
            }

            var process = new ManagedProcess(config, _logger);
            
            var startProcess = process.Start();
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

    public void OutputRuntimeSettings()
    {
        _logger?.LogDebug($"--- Process Manager Settings ---");

        Type type = RuntimeSettings.GetType();
        PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            object? value = property.GetValue(RuntimeSettings);

            _logger?.LogDebug($"  {property.Name}: {value}");
        }
    }
}
