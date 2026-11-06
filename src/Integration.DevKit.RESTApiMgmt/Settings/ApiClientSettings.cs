/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

namespace Integration.DevKit.RESTApiMgmt.Settings;

/// <summary>
/// Represents the configuration for an <see cref="ApiClient"/>.
/// </summary>
public class ApiClientSettings
{
    /// <summary>
    /// Gets or sets the username used for authenticating requests. 
    /// Default is <see cref="string.Empty"/>.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password or secret key used for authenticating requests. 
    /// Default is <see cref="string.Empty"/>.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the base URL for the API service. 
    /// All relative paths provided to request methods will be appended to this value.
    /// Default is "https://example.com".
    /// </summary>
    public string BaseUrl { get; set; } = "https://example.com";

    /// <summary>
    /// Gets or sets the threshold for the number of requests allowed before rate limiting is engaged. 
    /// Default is <see cref="int.MaxValue"/>.
    /// </summary>
    public int MaxConcurrentRequests { get; set; } = int.MaxValue;

    /// <summary>
    /// Gets the collection of HTTP headers that are automatically included in every request.
    /// </summary>
    /// <value>
    /// A <see cref="Dictionary{TKey, TValue}"/> containing header names and their corresponding values.
    /// </value>
    public Dictionary<string, string> DefaultHeaders { get; init; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets or sets the maximum time, in seconds, to wait for an HTTP response before timing out.
    /// If <see langword="null"/>, the underlying system default timeout is used.
    /// </summary>
    public int? HttpTimeout_Seconds { get; set; } = null;

    /// <summary>
    /// Gets or sets how many times a request is retried after a transient failure
    /// (network error, timeout, HTTP 408, 429, 500, 502, 503 or 504).
    /// If <see langword="null"/>, <see cref="ApiManagerSettings.Default_MaxRetries"/> is used. Negative values are coerced to 0.
    /// </summary>
    /// <remarks>Retries apply to every HTTP method, including POST, so only enable them for endpoints that are safe to repeat.</remarks>
    public int? MaxRetries { get; set; } = null;

    /// <summary>
    /// Gets or sets the delay, in milliseconds, before the first retry. Each later retry waits twice as long, with up to
    /// 20% random jitter, capped at 30 seconds.
    /// If <see langword="null"/>, <see cref="ApiManagerSettings.Default_RetryBaseDelay_Milliseconds"/> is used. Negative values are coerced to 0.
    /// </summary>
    public int? RetryBaseDelay_Milliseconds { get; set; } = null;
}
