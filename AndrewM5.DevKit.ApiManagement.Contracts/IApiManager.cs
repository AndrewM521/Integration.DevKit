using AndrewM5.DevKit.ApiManagement.Abstractions.Settings;

namespace AndrewM5.DevKit.ApiManagement.Services;

public interface IApiManager : IAsyncDisposable
{
    public ApiManagerSettings RuntimeSettings { get; set; }

    public IApiClient GetClient(string clientName);

    public void OutputRuntimeSettings();
}
