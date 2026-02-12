using System.Collections.Concurrent;

namespace AndrewM5.DevKit.ApiManagement.Abstractions.Settings;

public class ApiManagerSettings
{
    public int DefaultTimeoutSeconds { get; set; } = 30;

    public int MaxHttpClients { get; set; } = 1;

    public ConcurrentDictionary<string, ApiClientSettings> Clients { get; set; } = new ConcurrentDictionary<string, ApiClientSettings>();

    public ApiManagerSettings Clone()
    {
        return new ApiManagerSettings { 
            DefaultTimeoutSeconds = this.DefaultTimeoutSeconds,
            MaxHttpClients = this.MaxHttpClients,
            Clients = this.Clients
        };
    }
}
