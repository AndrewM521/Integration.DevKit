using AndrewM5.DevKit.Core.Results;
using System.Net;

namespace AndrewM5.DevKit.ApiClientManagement.Contracts.Models;

/// <summary>
/// A utility class for constructing formatted endpoint URLs using various routing styles 
/// (Query String, Keyed Slashes, or Positional Slashes) based on a persistent base route.
/// </summary>
public class ApiEndpoint
{
    private string _route;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiEndpoint"/> class.
    /// </summary>
    /// <param name="route">The base route or path for the endpoint (e.g., "users" or "api/v1/orders").</param>
    public ApiEndpoint(string route)
    {
        _route = route.TrimEnd('/');
    }

    #region Asyncronous Methods
    /// <summary>
    /// Asynchronously constructs a URL with a standard query string appended (e.g., "route?key=value").
    /// </summary>
    /// <param name="queryParams">A dictionary of key-value pairs to include in the query string.</param>
    /// <returns>An <see cref="OperationResult{T}"/> containing the formatted URL string.</returns>
    /// <example>
    /// Base Route: "users/search"
    /// Params: { name = "John Doe", age = 30 }
    /// Result: "users/search?name=John+Doe&amp;age=30"
    /// </example>
    public async Task<OperationResult<string>> BuildQueryUrlAsync(Dictionary<string, object> queryParams)
    {
        var result = new OperationResult<string>();

        try
        {
            string url = _route;

            if (queryParams == null || queryParams.Count == 0)
            {
                return result.SetMethodSuccess(url);
            }

            var items = new List<KeyValuePair<string, string>>();
            foreach (var kvp in queryParams)
            {
                if (kvp.Value != null)
                {
                    items.Add(new KeyValuePair<string, string>(kvp.Key, kvp.Value.ToString()!));
                }
            }

            string queryStr = await new FormUrlEncodedContent(items).ReadAsStringAsync();
            url += $"?{queryStr}";

            return result.SetMethodSuccess(url);
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }
    #endregion

    #region Syncronous Methods
    /// <summary>
    /// Returns the base route string provided during initialization.
    /// </summary>
    /// <returns>The unformatted base route.</returns>
    public string BuildUrl()
    {
        return _route;
    }

    /// <summary>
    /// Synchronously constructs a URL with a standard query string.
    /// </summary>
    /// <param name="queryParams">A dictionary of key-value pairs to include in the query string.</param>
    /// <returns>An <see cref="OperationResult{T}"/> containing the formatted URL string.</returns>
    /// <example>
    /// Base Route: "users/search"
    /// Params: { name = "John Doe", age = 30 }
    /// Result: "users/search?name=John+Doe&amp;age=30"
    /// </example>
    public OperationResult<string> BuildQueryUrl(Dictionary<string, object> queryParams)
    {
        return BuildQueryUrlAsync(queryParams).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Synchronously constructs a "Slash" style URL where keys and values are paired as path segments.
    /// </summary>
    /// <param name="queryParams">The dictionary of parameters to append as path segments.</param>
    /// <returns>An <see cref="OperationResult{T}"/> containing the formatted URL string.</returns>
    /// <example>
    /// Base Route: "products/filter"
    /// Params: { category = "Laptops" }
    /// Result: "products/filter/category/Laptops"
    /// </example>
    public OperationResult<string> BuildSlashUrl(Dictionary<string, object> queryParams)
    {
        var result = new OperationResult<string>();

        try
        {
            string url = _route;

            foreach (var kvp in queryParams)
            {
                if (kvp.Value != null)
                {
                    url += $"/{WebUtility.UrlEncode(kvp.Key)}/{WebUtility.UrlEncode(kvp.Value.ToString()!)}";
                }
            }

            return result.SetMethodSuccess(url);
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }

    /// <summary>
    /// Synchronously constructs a "Positional" style URL where only values are appended as path segments.
    /// </summary>
    /// <param name="queryParams">The ordered list of values to append to the path.</param>
    /// <returns>An <see cref="OperationResult{T}"/> containing the formatted URL string.</returns>
    /// <example>
    /// Base Route: "orders/details"
    /// Params: [ 12345, "full" ]
    /// Result: "orders/details/12345/full"
    /// </example>
    public OperationResult<string> BuildPositionalUrl(List<object> queryParams)
    {
        var result = new OperationResult<string>();

        try
        {
            string url = _route;

            foreach (var val in queryParams)
            {
                if (val != null)
                {
                    url += $"/{WebUtility.UrlEncode(val.ToString()!)}";
                }
            }

            return result.SetMethodSuccess(url);
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }
    #endregion
}

/// <summary>
/// Defines the supported styles for constructing endpoint URLs.
/// </summary>
public enum EndpointUrlStyle
{
    /// <summary> Standard query string format</summary>
    Query,
    /// <summary> Key-Value path segment format</summary>
    Slash,
    /// <summary> Value-only path segment format</summary>
    Positional
}