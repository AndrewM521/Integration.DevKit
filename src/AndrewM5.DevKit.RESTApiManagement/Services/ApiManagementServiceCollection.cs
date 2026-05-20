/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using AndrewM5.DevKit.RESTApiManagement.Contracts.Interfaces;
using AndrewM5.DevKit.RESTApiManagement.Contracts.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AndrewM5.DevKit.RESTApiManagement.Services;

/// <summary>
/// Extension methods for setting up the APIManagement module in an <see cref="IServiceCollection"/>.
/// </summary>
public static class ApiManagementServiceCollection
{
    /// <summary>
    /// Adds the API management infrastructure to the service collection
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configuration">The application configuration used to bind <see cref="ApiManagerSettings"/>.</param>
    /// <returns>The original <see cref="IServiceCollection"/> for chaining calls.</returns>
    public static IServiceCollection AddApiManagement(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ApiManagerSettings>(configuration.GetSection("AndrewM5.DevKit:ApiClientManagement"));

        services.AddSingleton<IApiManager, ApiManager>();

        services.AddHttpClient();

        return services;
    }
}
