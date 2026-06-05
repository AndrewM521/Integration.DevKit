/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

namespace Integration.DevKit.RESTApiMgmt.Contracts;

/// <summary>
/// Provides a read-only snapshot of HTTP request metrics and execution statistics 
/// for an <see cref="IApiClient"/> instance.
/// </summary>
public interface IApiClientMetrics
{
    /// <summary>
    /// Gets the total number of HTTP requests attempted by the client.
    /// </summary>
    public int TotalRequests { get; }

    /// <summary>
    /// Gets the cumulative count of requests that resulted in a successful operation.
    /// </summary>
    public int SuccessCount{ get; }

    /// <summary>
    /// Gets the cumulative count of requests that resulted in a failure or error.
    /// </summary>
    public int FailureCount { get; }

    /// <summary>
    /// Gets the total count of HTTP GET requests performed.
    /// </summary>
    public int GetCount { get; }

    /// <summary>
    /// Gets the total count of HTTP POST requests performed.
    /// </summary>
    public int PostCount { get; }

    /// <summary>
    /// Gets the total count of HTTP PUT requests performed.
    /// </summary>
    public int PutCount { get; }

    /// <summary>
    /// Gets the total count of HTTP DELETE requests performed.
    /// </summary>
    public int DeleteCount { get; }

    /// <summary>
    /// Gets the total count of requests using methods not explicitly tracked by individual properties 
    /// (e.g., PATCH, HEAD, or custom verbs).
    /// </summary>
    public int OtherCount { get; }
}

/// <summary>
/// Defines the specific HTTP methods tracked within the reporting system.
/// </summary>
public enum HttpMetricNames
{
    /// <summary> Represents an HTTP GET operation. </summary>
    Get,
    /// <summary> Represents an HTTP POST operation. </summary>
    Post,
    /// <summary> Represents an HTTP PUT operation. </summary>
    Put,
    /// <summary> Represents an HTTP DELETE operation. </summary>
    Delete,
    /// <summary> Represents an HTTP PATCH operation. </summary>
    Patch,
    /// <summary> Represents an HTTP HEAD operation. </summary>
    Head,
    /// <summary> Represents an HTTP OPTIONS operation. </summary>
    Options,
    /// <summary> Represents any other non-standard or untracked HTTP operation. </summary>
    Other
}