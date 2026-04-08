using AndrewM5.DevKit.ApiClientManagement.Contracts.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace AndrewM5.DevKit.ApiClientManagement.Services;

/// <summary>
/// Provides a static entry point to access the <see cref="IApiManager"/> 
/// This host must be initialized during application startup after the DI container is built.
/// </summary>
public static class ApiManagementHost
{
    private const string NoInit = "ApiManagementHost has not been initialized.";

    private static IApiManager? _apiManager;

    /// <summary>
    /// Initializes the static host with the <see cref="IApiManager"/> resolved from the service provider.
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
