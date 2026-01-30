using AndrewM5.DevKit.ProcessLauncher.Abstractions;
using AndrewM5.DevKit.ProcessLauncher.Abstractions.Settings;
using AndrewM5.DevKit.ProcessLauncher.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AndrewM5.DevKit.ProcessLauncher;

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

        // Bind LoggerManagerSettings
        services.Configure<ProcessManagerSettings>(config.GetSection("ProcessManagerSettings"));

        // Register the concrete class
        services.AddSingleton<IProcessManager, ProcessManager>();

        return services;
    }
}
