using AndrewM5.DevKit.ApiClientManagement.Abstractions.Options;
using AndrewM5.DevKit.ApiClientManagement.Contracts.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AndrewM5.DevKit.ApiClientManagement.Services;

/// <summary>
/// Extension methods for setting up API management services in an <see cref="IServiceCollection"/>.
/// </summary>
public static class ApiManagementServiceCollection
{
    /// <summary>
    /// Adds the API management infrastructure to the service collection, including configuration 
    /// binding and the <see cref="IApiManager"/> singleton.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configuration">The application configuration used to bind <see cref="ApiManagerSettings"/>.</param>
    /// <returns>The original <see cref="IServiceCollection"/> for chaining calls.</returns>
    public static IServiceCollection AddApiManagement(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ApiManagerSettings>(configuration.GetSection("AndrewM5.DevKit:ApiManager"));

        services.AddSingleton<IApiManager, ApiManager>();

        services.AddHttpClient();

        return services;
    }
}
