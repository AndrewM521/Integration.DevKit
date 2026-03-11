using Microsoft.Extensions.DependencyInjection;

namespace AndrewM5.DevKit.ApiManagement.Services;

public static class ApiManagementHost
{
    private const string NoInit = "ApiManagementHost has not been initialized.";

    private static IApiManager? _apiManager;

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
