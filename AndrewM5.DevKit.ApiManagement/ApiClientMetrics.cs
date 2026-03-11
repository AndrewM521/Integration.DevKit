using AndrewM5.DevKit.ApiManagement.Abstractions;

namespace AndrewM5.DevKit.ApiManagement;

public class ApiClientMetrics : IApiClientMetrics
{
    public int TotalRequests
    {
        get
        {
            return GetCount + PostCount + PutCount + DeleteCount + OtherCount;
        }
    }

    public int SuccessCount
    {
        get
        {
            return TotalRequests - FailureCount;
        }
    }
    
    public int FailureCount { get; private set; } = 0;
    
    public int GetCount { get; private set; } = 0;
    
    public int PostCount { get; private set; } = 0;
    
    public int PutCount { get; private set; } = 0;
    
    public int DeleteCount { get; private set; } = 0;
    
    public int OtherCount { get; private set; } = 0;

    internal void IncrementMetric(HttpMetricNames httpMethod)
    {
        switch (httpMethod)
        {
            case HttpMetricNames.Get:
                GetCount++;
                break;
            case HttpMetricNames.Put:
                PutCount++;
                break;
            case HttpMetricNames.Post:
                PostCount++;
                break;
            case HttpMetricNames.Delete:
                DeleteCount++;
                break;
            default:
                OtherCount++;
                break;
        }
    }

    internal void IncrementFailure()
    {
        FailureCount++;
    }

    public override string ToString()
    {
        return
            $"Requests={TotalRequests} | Success={SuccessCount} | Failure={FailureCount} | " +
            $"GET={GetCount} | POST={PostCount} | PUT={PutCount} | DELETE={DeleteCount} | OTHER={OtherCount}";
    }
}
