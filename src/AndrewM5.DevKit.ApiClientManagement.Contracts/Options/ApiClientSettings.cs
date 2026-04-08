namespace AndrewM5.DevKit.ApiManagement.Abstractions.Options;

/// <summary>
/// Represents the configuration schema and runtime settings for an <see cref="IApiClient"/>.
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
    /// Gets or sets the threshold for the number of requests allowed before client-side 
    /// rate limiting logic is engaged. 
    /// Default is <see cref="int.MaxValue"/>.
    /// </summary>
    public int RequestCountBeforeRateLimiting { get; set; } = int.MaxValue;

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
}
