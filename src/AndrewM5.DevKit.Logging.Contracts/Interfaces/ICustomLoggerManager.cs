using AndrewM5.DevKit.Logging.Abstractions.Options;

namespace AndrewM5.DevKit.Logging.Contracts.Interfaces;

/// <summary>
/// Provides centralized management for creating and configuring custom logger instances 
/// and accessing global logging runtime settings.
/// </summary>
public interface ICustomLoggerManager
{
    /// <summary>
    /// Gets the current configuration and operational settings for the logger manager.
    /// </summary>
    public LoggerManagerSettings RuntimeSettings { get; }

    /// <summary>
    /// Retrieves an existing logger or creates a new one for the specified category name.
    /// </summary>
    /// <param name="categoryName">The name of the category (usually the fully qualified type name) for the logger.</param>
    /// <returns>An instance of <see cref="ICustomLogger"/> associated with the given category.</returns>
    public ICustomLogger GetLogger(string categoryName);

    /// <summary>
    /// Captures the current state of <see cref="RuntimeSettings"/> and outputs them to the Debug log.
    /// </summary>
    public void OutputRuntimeSettings();
}
