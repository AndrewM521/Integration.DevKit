/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using Integration.DevKit.RESTApiMgmt.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Integration.DevKit.RESTApiMgmt;

/// <summary>
/// Provides a static entry point to access the APIManagement module 
/// </summary>
/// <remarks>
/// This host acts as a static wrapper for services resolved from the Dependency Injection container. 
/// It must be registered and initialized during application startup (e.g., in Program.cs or Startup.cs)
/// </remarks>
public static class Service_RESTApiMgmt
{
    private const string NoInit = "Service_RESTApiMgmt has not been initialized.";

    private static IApiManager? _apiManager;

    /// <summary>
    /// Adds the API management infrastructure to the service collection
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configuration">The application configuration used to bind <see cref="ApiManagerSettings"/>.</param>
    /// <returns>The original <see cref="IServiceCollection"/> for chaining calls.</returns>
    public static IServiceCollection AddRESTApiMgmt(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ApiManagerSettings>(configuration.GetSection("Integration.DevKit:ApiClientManagement"));

        services.TryAddSingleton<IApiManager, ApiManager>();

        services.AddHttpClient();

        return services;
    }

    /// <summary>
    /// Initializes the static <see cref="ApiManager"/>.
    /// </summary>
    /// <param name="sp">The <see cref="IServiceProvider"/> containing the registered API management services.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="sp"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if <see cref="IApiManager"/> has not been registered in the service collection.
    /// </exception>
    public static void Initialize(IServiceProvider sp)
    {
        if (sp == null)
        {
            throw new ArgumentNullException(nameof(sp));
        }

        _apiManager = sp.GetService<IApiManager>();
        if (_apiManager == null)
        {
            throw new InvalidOperationException($"{nameof(IApiManager)} is not registered, make sure to call AddApiManagement() when configuring services.");
        }
    }

    /// <summary>
    /// Gets the globally accessible instance of the <see cref="IApiManager"/>.
    /// </summary>
    /// <value> The initialized <see cref="IApiManager"/> instance. </value>
    /// <exception cref="InvalidOperationException">
    /// Thrown if <see cref="Initialize"/> was not called prior to accessing this property.
    /// </exception>
    public static IApiManager ApiManager
    {
        get
        {
            if (_apiManager == null)
            {
                throw new InvalidOperationException(NoInit);
            }

            return _apiManager;
        }
    }
}
