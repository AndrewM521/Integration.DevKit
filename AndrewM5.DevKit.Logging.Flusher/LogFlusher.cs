using AndrewM5.DevKit.Core;
using AndrewM5.DevKit.Logging.Abstractions;
using AndrewM5.DevKit.Logging.Abstractions.Settings;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace AndrewM5.DevKit.Logging.Flusher;

public class LogFlusher : BackgroundService, ILogFlusher
{
    public LogFlushServiceSettings RuntimeSettings { get; init; }

    private readonly ILogRegistry _logRegistry;
    private readonly ICustomLogger? _logger;

    private readonly bool _runningInContainer;

    public LogFlusher(IOptions<LogFlushServiceSettings> settings, ICustomLoggerManager loggerManager, ILogRegistry logRegistry)
    {
        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        RuntimeSettings = settings.Value.Clone();

        _logRegistry = logRegistry;
        _logger = loggerManager.GetLogger("LogFlushService");
        _runningInContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        DateTime lastFlushTime = DateTime.UtcNow;

        while (!cancellationToken.IsCancellationRequested)
        {
            TimeSpan elapsedTime = DateTime.UtcNow - lastFlushTime;

            if (_logRegistry.GetLogFileQueueCount() >= RuntimeSettings.MaxBufferCount || elapsedTime.TotalSeconds >= RuntimeSettings.FlushIntervalSeconds)
            {
                FlushBuffer();
                lastFlushTime = DateTime.UtcNow;
            }

            await Task.Delay(100, cancellationToken); // Short delay to avoid busy while loop
        }

        FlushBuffer(); //Final flush when task is canceled.
    }

    private void FlushBuffer()
    {
        try
        {
            if (!RuntimeSettings.CreateLogFile)
            {
                return;
            }

            var messages = _logRegistry.DequeueFromLogFileBuffer();
            if (messages.Length == 0)
            {
                return;
            }

            StringBuilder stringBuilder = new StringBuilder();
            foreach (var msg in messages)
            {
                stringBuilder.AppendLine(msg);
            }

            string output = stringBuilder.ToString();

            bool allowCreateFile = !_runningInContainer || RuntimeSettings.AllowCreateFileInContainer;

            if (allowCreateFile && !string.IsNullOrWhiteSpace(RuntimeSettings.LogFilePath))
            {
                var writeToFile = FileExtension.WriteToFile(RuntimeSettings.LogFilePath, output, true);
                if (!writeToFile.MethodSuccess)
                {
                    throw writeToFile.Exception;
                }
            }
        }
        catch (Exception ex) 
        {
            Debug.WriteLine(LogFormatter.Format(true, nameof(LogFlusher),"Failed to flush logs", LogLevel.Error, ex));
        }
    }

    public void OutputRuntimeSettings()
    {
        _logger?.LogDebug($"--- Log Flush Service Settings ---");

        Type type = RuntimeSettings.GetType();
        PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            object? value = property.GetValue(RuntimeSettings);
            _logger?.LogDebug($"  {property.Name}: {value}");
        }
    }
}
