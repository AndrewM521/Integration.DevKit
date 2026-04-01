using AndrewM5.DevKit.Logging.Abstractions.Options;

namespace AndrewM5.DevKit.Logging.Contracts.Interfaces;

/// <summary>
/// Defines a service responsible for managing the flushing of log buffers 
/// to their respective persistent storage or destinations.
/// </summary>
public interface ILogFlusher
{
    /// <summary>
    /// Gets the current operational settings for the log flushing service, 
    /// such as intervals or batch sizes.
    /// </summary>
    public LogFlushServiceSettings RuntimeSettings { get; }

    /// <summary>
    /// Captures the current state of the <see cref="RuntimeSettings"/> and 
    /// outputs them to the Debug log.
    /// </summary>
    public void OutputRuntimeSettings();
}
