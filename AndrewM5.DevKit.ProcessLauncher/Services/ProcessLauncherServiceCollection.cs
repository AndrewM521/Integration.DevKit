using AndrewM5.DevKit.ProcessLauncher.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AndrewM5.DevKit.ProcessLauncher.Services;

public static class ProcessLauncherServiceCollection
{
    public static IServiceCollection AddProcessLauncher(this IServiceCollection services, IConfiguration config)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        // Register the concrete class
        services.AddSingleton<IProcessManager, ProcessManager>();

        return services;
    }
}
