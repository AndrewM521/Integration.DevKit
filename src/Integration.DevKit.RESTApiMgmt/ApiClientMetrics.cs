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
    public long TotalRequests
    {
        get
        {
            return GetCount + PostCount + PutCount + DeleteCount + OtherCount;
        }
    }

    /// <summary>
    /// Gets the cumulative count of requests that resulted in a successful operation.
    /// </summary>
    public long SuccessCount
    {
        get
        {
            return TotalRequests - FailureCount;
        }
    }

    private long _failureCount;
    private long _getCount;
    private long _postCount;
    private long _putCount;
    private long _deleteCount;
    private long _otherCount;

    /// <summary>
    /// Gets the cumulative count of requests that resulted in a failure or error.
    /// </summary>
    public long FailureCount => Volatile.Read(ref _failureCount);

    /// <summary>
    /// Gets the total count of HTTP GET requests performed.
    /// </summary>
    public long GetCount => Volatile.Read(ref _getCount);

    /// <summary>
    /// Gets the total count of HTTP POST requests performed.
    /// </summary>
    public long PostCount => Volatile.Read(ref _postCount);

    /// <summary>
    /// Gets the total count of HTTP PUT requests performed.
    /// </summary>
    public long PutCount => Volatile.Read(ref _putCount);

    /// <summary>
    /// Gets the total count of HTTP DELETE requests performed.
    /// </summary>
    public long DeleteCount => Volatile.Read(ref _deleteCount);

    /// <summary>
    /// Gets the total count of requests using methods not explicitly tracked by individual properties
    /// (e.g., PATCH, HEAD, or custom verbs).
    /// </summary>
    public long OtherCount => Volatile.Read(ref _otherCount);

    /// <summary>
    /// Increments the specific counter associated with the provided <see cref="HttpMetricNames"/>.
    /// </summary>
    /// <param name="httpMethod">The HTTP method verb to increment.</param>
    internal void IncrementMetric(HttpMetricNames httpMethod)
    {
        switch (httpMethod)
        {
            case HttpMetricNames.Get:
                Interlocked.Increment(ref _getCount);
                break;
            case HttpMetricNames.Put:
                Interlocked.Increment(ref _putCount);
                break;
            case HttpMetricNames.Post:
                Interlocked.Increment(ref _postCount);
                break;
            case HttpMetricNames.Delete:
                Interlocked.Increment(ref _deleteCount);
                break;
            default:
                Interlocked.Increment(ref _otherCount);
                break;
        }
    }

    /// <summary>
    /// Increments the cumulative failure counter.
    /// </summary>
    internal void IncrementFailure()
    {
        Interlocked.Increment(ref _failureCount);
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
