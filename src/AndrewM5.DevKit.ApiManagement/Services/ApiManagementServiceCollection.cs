using AndrewM5.DevKit.ApiManagement.Abstractions.Settings;
using AndrewM5.DevKit.ApiManagement.Contracts.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AndrewM5.DevKit.ApiManagement.Services;

public static class ApiManagementServiceCollection
{
    public static IServiceCollection AddApiManagement(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ApiManagerSettings>(configuration.GetSection("AndrewM5.DevKit:ApiManager"));

        services.AddSingleton<IApiManager, ApiManager>();

        services.AddHttpClient();

        return services;
    }
}
