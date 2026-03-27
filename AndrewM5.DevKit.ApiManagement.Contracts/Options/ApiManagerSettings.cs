using System.Collections.Concurrent;

namespace AndrewM5.DevKit.ApiManagement.Abstractions.Options;

public class ApiManagerSettings
{
    public int Default_HttpTimeout_Seconds { get; set; } = 30;

    public ConcurrentDictionary<string, ApiClientSettings> Clients { get; set; } = new ConcurrentDictionary<string, ApiClientSettings>();

    public ApiManagerSettings Clone()
    {
        return new ApiManagerSettings {
            Default_HttpTimeout_Seconds = this.Default_HttpTimeout_Seconds,
            Clients = this.Clients
        };
    }
}
