using AndrewM5.DevKit.ProcessLauncher.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace AndrewM5.DevKit.ProcessLauncher;

public static class ProcessLauncherHost
{
    private const string NoInit = "ProcessLauncherHost has not been initialized.";

    private static IProcessManager? _processManager;

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
