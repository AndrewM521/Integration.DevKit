using AndrewM5.DevKit.ProcessLauncher.Contracts.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace AndrewM5.DevKit.ProcessLauncher.Services;

/// <summary>
/// Provides a static entry point to access the <see cref="IProcessManager"/>. 
/// This class must be initialized during application startup to resolve the required services.
/// </summary>
public static class ProcessLauncherHost
{
    private const string NoInit = "ProcessLauncherHost has not been initialized.";

    private static IProcessManager? _processManager;

    /// <summary>
    /// Initializes the static host with the provided service provider.
    /// </summary>
    /// <param name="sp">The <see cref="IServiceProvider"/> used to resolve <see cref="IProcessManager"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="sp"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if <see cref="IProcessManager"/> is not registered in the service collection.
    /// </exception>
    public static void Initialize(IServiceProvider sp)
    {
        if (sp == null)
        {
            throw new ArgumentNullException(nameof(sp));
        }

        _processManager = sp.GetService<IProcessManager>();
        if (_processManager == null)
        {
            throw new InvalidOperationException($"{nameof(IProcessManager)} is not registered. Make sure you call AddProcessLauncher() when configuring services.");
        }
    }

    /// <summary>
    /// Gets the global instance of the <see cref="IProcessManager"/>.
    /// </summary>
    /// <value>The initialized <see cref="IProcessManager"/>.</value>
    /// <exception cref="InvalidOperationException">Thrown if accessed before <see cref="Initialize"/> is called.</exception>
    public static IProcessManager ProcessManager
    {
        get
        {
            if (_processManager == null)
            {
                throw new InvalidOperationException(NoInit);
            }

            return _processManager;
        }
    }
}
