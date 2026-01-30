using AndrewM5.DevKit.ProcessLauncher.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace AndrewM5.DevKit.ProcessLauncher;

public static class ProcessLauncherHost
{
    private const string NotInitializedMsg = "ProcessLauncherHost has not been initialized.";

    private static IServiceProvider? _serviceProvider;
    private static IProcessManager? _processManager;

    public static void Initialize(IServiceProvider serviceProvider)
    {
        if (serviceProvider == null)
        {
            throw new ArgumentNullException(nameof(serviceProvider));
        }

        _serviceProvider = serviceProvider;

        _processManager = _serviceProvider.GetService<IProcessManager>();
        if (_processManager == null)
        {
            throw new InvalidOperationException($"{nameof(IProcessManager)} is not registered. Make sure you call AddProcessLauncher() when configuring services before initializing {nameof(ProcessLauncherHost)}.");
        }
    }

    public static IServiceProvider ServiceProvider
    {
        get
        {
            if (_serviceProvider == null)
            {
                throw new InvalidOperationException(NotInitializedMsg);
            }

            return _serviceProvider;
        }
    }

    public static IProcessManager ProcessManager
    {
        get
        {
            if (_processManager == null)
            {
                throw new InvalidOperationException(NotInitializedMsg);
            }

            return _processManager;
        }
    }

    internal static void Reset()
    {
        _serviceProvider = null;
        _processManager = null;
    }
}
