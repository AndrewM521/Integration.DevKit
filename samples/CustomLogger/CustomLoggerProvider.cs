using Microsoft.Extensions.Logging;

namespace CustomLogger;


/// <summary>
/// Adapts an <see cref="ICustomLoggerManager"/> to the standard <see cref="ILoggerProvider"/>
/// contract, so <see cref="CustomLogger"/> can be registered with any application's
/// <c>Microsoft.Extensions.Logging</c> pipeline (e.g. via <c>services.AddLogging(builder => ...)</c>)
/// exactly like a third-party provider such as Serilog or NLog.
/// </summary>
public sealed class CustomLoggerProvider : ILoggerProvider
{
    private readonly CustomLoggerManager _manager;

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomLoggerProvider"/> class.
    /// </summary>
    /// <param name="manager">The manager used to resolve category-specific loggers.</param>
    public CustomLoggerProvider(CustomLoggerManager manager)
    {
        _manager = manager ?? throw new ArgumentNullException(nameof(manager));
    }

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName) => _manager.GetLogger(categoryName);

    /// <inheritdoc />
    public void Dispose()
    {
    }
}
