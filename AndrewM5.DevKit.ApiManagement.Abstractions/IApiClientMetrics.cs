
namespace AndrewM5.DevKit.ApiManagement.Abstractions;

public interface IApiClientMetrics
{
    public int TotalRequests { get; }

    public int SuccessCount{ get; }

    public int FailureCount { get; }

    public int GetCount { get; }

    public int PostCount { get; }

    public int PutCount { get; }

    public int DeleteCount { get; }

    public int OtherCount { get; }
}
public enum HttpMetricNames
{
    Get,
    Post,
    Put,
    Delete,
    Patch,
    Head,
    Options,
    Other
}