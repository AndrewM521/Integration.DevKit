using AndrewM5.DevKit.ApiManagement.Contracts.Interfaces;

namespace AndrewM5.DevKit.ApiManagement;

/// <summary>
/// Implementation of <see cref="IApiClientMetrics"/> that tracks HTTP request statistics 
/// using internal increment methods.
/// </summary>
public class ApiClientMetrics : IApiClientMetrics
{
    /// <summary>
    /// Gets the total number of requests by summing all individual HTTP method counters.
    /// </summary>
    public int TotalRequests
    {
        get
        {
            return GetCount + PostCount + PutCount + DeleteCount + OtherCount;
        }
    }

    /// <summary>
    /// Gets the number of successful requests by subtracting <see cref="FailureCount"/> 
    /// from <see cref="TotalRequests"/>.
    /// </summary>
    public int SuccessCount
    {
        get
        {
            return TotalRequests - FailureCount;
        }
    }

    /// <inheritdoc/>
    public int FailureCount { get; private set; } = 0;

    /// <inheritdoc/>
    public int GetCount { get; private set; } = 0;

    /// <inheritdoc/>
    public int PostCount { get; private set; } = 0;

    /// <inheritdoc/>
    public int PutCount { get; private set; } = 0;

    /// <inheritdoc/>
    public int DeleteCount { get; private set; } = 0;

    /// <inheritdoc/>
    public int OtherCount { get; private set; } = 0;

    /// <summary>
    /// Increments the specific counter associated with the provided <see cref="HttpMetricNames"/>.
    /// </summary>
    /// <param name="httpMethod">The HTTP method verb to increment.</param>
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

    /// <summary>
    /// Increments the cumulative failure counter.
    /// </summary>
    internal void IncrementFailure()
    {
        FailureCount++;
    }

    /// <summary>
    /// Returns a formatted string representing the current state of all tracked metrics.
    /// </summary>
    /// <returns>A string summary of requests, successes, failures, and method counts.</returns>
    public override string ToString()
    {
        return
            $"Requests={TotalRequests} | Success={SuccessCount} | Failure={FailureCount} | " +
            $"GET={GetCount} | POST={PostCount} | PUT={PutCount} | DELETE={DeleteCount} | OTHER={OtherCount}";
    }
}
