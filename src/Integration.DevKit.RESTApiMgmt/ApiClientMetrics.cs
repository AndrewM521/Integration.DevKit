/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

namespace Integration.DevKit.RESTApiMgmt;

/// <summary>
/// Read-only snapshot of HTTP request metrics and execution statistics
/// for an <see cref="ApiClient"/> instance. Tracks HTTP request statistics
/// using internal increment methods.
/// </summary>
public class ApiClientMetrics
{
    /// <summary>
    /// Gets the total number of HTTP requests attempted by the client.
    /// </summary>
    public int TotalRequests
    {
        get
        {
            return GetCount + PostCount + PutCount + DeleteCount + OtherCount;
        }
    }

    /// <summary>
    /// Gets the cumulative count of requests that resulted in a successful operation.
    /// </summary>
    public int SuccessCount
    {
        get
        {
            return TotalRequests - FailureCount;
        }
    }

    /// <summary>
    /// Gets the cumulative count of requests that resulted in a failure or error.
    /// </summary>
    public int FailureCount { get; private set; } = 0;

    /// <summary>
    /// Gets the total count of HTTP GET requests performed.
    /// </summary>
    public int GetCount { get; private set; } = 0;

    /// <summary>
    /// Gets the total count of HTTP POST requests performed.
    /// </summary>
    public int PostCount { get; private set; } = 0;

    /// <summary>
    /// Gets the total count of HTTP PUT requests performed.
    /// </summary>
    public int PutCount { get; private set; } = 0;

    /// <summary>
    /// Gets the total count of HTTP DELETE requests performed.
    /// </summary>
    public int DeleteCount { get; private set; } = 0;

    /// <summary>
    /// Gets the total count of requests using methods not explicitly tracked by individual properties
    /// (e.g., PATCH, HEAD, or custom verbs).
    /// </summary>
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
