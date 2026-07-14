/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using Integration.DevKit.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using Integration.DevKit.CustomLogger.Contracts;

namespace Integration.DevKit.CustomLogger.Flusher;

/// <summary>
/// Concrete Implementation of <see cref="ILogFlusher"/> that periodically flushes buffered log messages from 
/// <see cref="ILogFileRegistry"/> to a persistent file destination.
/// </summary>
public class LogFlusher : BackgroundService, ILogFlusher
{
    /// <inheritdoc />
    public LogFlushServiceSettings RuntimeSettings { get; init; }

    private readonly ILogFileRegistry _logRegistry;
    private readonly ICustomLogger? _logger;

    private readonly bool _runningInContainer;

    /// <summary>
    /// Initializes a new instance of the <see cref="LogFlusher"/> class.
    /// </summary>
    /// <param name="settings">The configuration settings for flushing behavior.</param>
    /// <param name="loggerManager">The manager used to create an internal logger for this service.</param>
    /// <param name="logRegistry">The registry containing the message buffer to be flushed.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="settings"/> is null.</exception>
    public LogFlusher(IOptions<LogFlushServiceSettings> settings, ICustomLoggerManager loggerManager, ILogFileRegistry logRegistry)
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

    /// <summary>
    /// The main execution loop of the background service. 
    /// Monitors buffer size and elapsed time to trigger log flushes.
    /// </summary>
    /// <param name="cancellationToken">Triggered when the application host is shutting down.</param>
    /// <returns>A <see cref="Task"/> representing the background operation.</returns>
    /// <remarks>
    /// The loop checks the buffer state every 500ms. A flush is triggered if 
    /// <see cref="LogFlushServiceSettings.MaxBufferCount"/> is reached or 
    /// <see cref="LogFlushServiceSettings.FlushIntervalSeconds"/> has elapsed.
    /// </remarks>
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        DateTime lastFlushTime = DateTime.UtcNow;

        while (!cancellationToken.IsCancellationRequested)
        {
            TimeSpan elapsedTime = DateTime.UtcNow - lastFlushTime;

            if (_logRegistry.Count >= RuntimeSettings.MaxBufferCount || elapsedTime.TotalSeconds >= RuntimeSettings.FlushIntervalSeconds)
            {
                FlushBuffer();
                lastFlushTime = DateTime.UtcNow;
            }

            await Task.Delay(500, cancellationToken); // Short delay to avoid busy while loop
        }

        FlushBuffer(); //Final flush when task is canceled.
    }

    /// <summary>
    /// Dequeues messages from the registry and writes them to the configured log file.
    /// </summary>
    /// <remarks>
    /// Respects the <see cref="LogFlushServiceSettings.CreateLogFile"/> and 
    /// <see cref="LogFlushServiceSettings.AllowCreateFileInContainer"/> configurations. 
    /// If an error occurs during the write process, it is captured and sent to <see cref="Debug"/>.
    /// </remarks>
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
                var writeToFile = FileUtils.WriteToFile(RuntimeSettings.LogFilePath, output, true);
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

    /// <inheritdoc />
    public void LogRuntimeSettings()
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
