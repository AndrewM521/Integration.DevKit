using Microsoft.Extensions.DependencyInjection;

namespace AndrewM5.DevKit.ThreadSafeItems.Services;

public static class ThreadSafeItemsServiceCollection
{
    public static IServiceCollection AddThreadSafeItems(this IServiceCollection services)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        services.AddSingleton<ThreadSafeFileIO>();

        return services;
    }
}
