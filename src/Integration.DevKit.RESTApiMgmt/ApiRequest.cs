using System.Net;
using System.Net.Http.Headers;
using Integration.DevKit.Core;
using Integration.DevKit.RESTApiMgmt.Interfaces;

namespace Integration.DevKit.RESTApiMgmt;

/// <summary>
/// Provides static helper methods for performing common HTTP operations and wrapping responses in an <see cref="ApiOperationResult{T}"/>.
/// </summary>
public static class ApiRequest
{
    /// <summary>
    /// Sends a GET request to the specified endpoint as an asynchronous operation.
    /// </summary>
    /// <param name="client">The <see cref="HttpClient"/> instance to use for the request.</param>
    /// <param name="endpointUrl">The relative or absolute URL of the API endpoint.</param>
    /// <param name="content">Optional HTTP body content sent to the server.</param>
    /// <param name="headers">Optional dictionary of HTTP headers to include in the request.</param>
    /// <param name="authStrategy">Optional <see cref="IAuthStrategy"/> applied to the request before it is sent.</param>
    /// <returns>A task representing the asynchronous operation, containing an <see cref="ApiOperationResult{T}"/> with the response string.</returns>
    public static async Task<ApiOperationResult<string>> GetAsync(HttpClient client, string endpointUrl, HttpContent? content = null, Dictionary<string, string>? headers = null, IAuthStrategy? authStrategy = null)
    {
        var finalUri = ResolveUri(client, endpointUrl);

        using var request = new HttpRequestMessage(HttpMethod.Get, finalUri);

        if (content != null)
        {
            request.Content = content;
        }

        AddHeadersToRequest(request, headers);

        var applyAuthResult = await ApplyAuthAsync(request, authStrategy);
        if (applyAuthResult != null)
        {
            return applyAuthResult;
        }

        return await SendRequestAsync(client, request);
    }

    /// <summary>
    /// Sends a POST request with optional content to the specified endpoint as an asynchronous operation.
    /// </summary>
    /// <param name="client">The <see cref="HttpClient"/> instance to use for the request.</param>
    /// <param name="endpointUrl">The relative or absolute URL of the API endpoint.</param>
    /// <param name="content">Optional HTTP body content sent to the server.</param>
    /// <param name="headers">Optional dictionary of HTTP headers to include in the request.</param>
    /// <param name="authStrategy">Optional <see cref="IAuthStrategy"/> applied to the request before it is sent.</param>
    /// <returns>A task representing the asynchronous operation, containing an <see cref="ApiOperationResult{T}"/> with the response string.</returns>
    public static async Task<ApiOperationResult<string>> PostAsync(HttpClient client, string endpointUrl, HttpContent? content = null, Dictionary<string, string>? headers = null, IAuthStrategy? authStrategy = null)
    {
        var finalUri = ResolveUri(client, endpointUrl);

        using var request = new HttpRequestMessage(HttpMethod.Post, finalUri);

        if (content != null)
        {
            request.Content = content;
        }

        AddHeadersToRequest(request, headers);

        var applyAuthResult = await ApplyAuthAsync(request, authStrategy);
        if (applyAuthResult != null)
        {
            return applyAuthResult;
        }

        return await SendRequestAsync(client, request);
    }

    /// <summary>
    /// Sends a PUT request with content to the specified endpoint as an asynchronous operation.
    /// </summary>
    /// <param name="client">The <see cref="HttpClient"/> instance to use for the request.</param>
    /// <param name="endpointUrl">The relative or absolute URL of the API endpoint.</param>
    /// <param name="content">The HTTP request content sent to the server.</param>
    /// <param name="headers">Optional dictionary of HTTP headers to include in the request.</param>
    /// <param name="authStrategy">Optional <see cref="IAuthStrategy"/> applied to the request before it is sent.</param>
    /// <returns>A task representing the asynchronous operation, containing an <see cref="ApiOperationResult{T}"/> with the response string.</returns>
    public static async Task<ApiOperationResult<string>> PutAsync(HttpClient client, string endpointUrl, HttpContent content, Dictionary<string, string>? headers = null, IAuthStrategy? authStrategy = null)
    {
        var finalUri = ResolveUri(client, endpointUrl);

        using var request = new HttpRequestMessage(HttpMethod.Put, finalUri) { Content = content };

        AddHeadersToRequest(request, headers);

        var applyAuthResult = await ApplyAuthAsync(request, authStrategy);
        if (applyAuthResult != null)
        {
            return applyAuthResult;
        }

        return await SendRequestAsync(client, request);
    }

    /// <summary>
    /// Sends a DELETE request to the specified endpoint as an asynchronous operation.
    /// </summary>
    /// <param name="client">The <see cref="HttpClient"/> instance to use for the request.</param>
    /// <param name="endpointUrl">The relative or absolute URL of the API endpoint.</param>
    /// <param name="content">Optional HTTP body content sent to the server.</param>
    /// <param name="headers">Optional dictionary of HTTP headers to include in the request.</param>
    /// <param name="authStrategy">Optional <see cref="IAuthStrategy"/> applied to the request before it is sent.</param>
    /// <returns>A task representing the asynchronous operation, containing an <see cref="ApiOperationResult{T}"/> with the response string.</returns>
    public static async Task<ApiOperationResult<string>> DeleteAsync(HttpClient client, string endpointUrl, HttpContent? content = null, Dictionary<string, string>? headers = null, IAuthStrategy? authStrategy = null)
    {
        var finalUri = ResolveUri(client, endpointUrl);

        using var request = new HttpRequestMessage(HttpMethod.Delete, finalUri);

        if (content != null)
        {
            request.Content = content;
        }

        AddHeadersToRequest(request, headers);

        var applyAuthResult = await ApplyAuthAsync(request, authStrategy);
        if (applyAuthResult != null)
        {
            return applyAuthResult;
        }

        return await SendRequestAsync(client, request);
    }

    /// <summary>
    /// Executes an HTTP request
    /// </summary>
    /// <param name="httpClient">The <see cref="HttpClient"/> instance to use.</param>
    /// <param name="request">The <see cref="HttpRequestMessage"/> to execute.</param>
    /// <returns>An <see cref="ApiOperationResult{T}"/> containing the result of the request execution.</returns>
    public static async Task<ApiOperationResult<string>> SendRequestAsync(HttpClient httpClient, HttpRequestMessage request)
    {
        var result = new ApiOperationResult<string>();

        result.SetRequestUrl(request.RequestUri!.ToString());

        try
        {
            using (HttpResponseMessage response = await httpClient.SendAsync(request))
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                HttpStatusCode statusEnum = response.StatusCode;
                int statusCode = (int)statusEnum;

                try
                {
                    response.EnsureSuccessStatusCode();

                    return result.SetApiSuccess(responseBody, statusEnum, $"HTTP {statusCode} {statusEnum}");
                }
                catch (HttpRequestException ex)
                {
                    return result.SetApiFailure(statusEnum, ex, responseBody, $"Request failed with HTTP {statusCode} {statusEnum}");
                }
            }
        }
        catch (HttpRequestException ex)
        {
            return result.SetApiFailure(ex.StatusCode ?? HttpStatusCode.ServiceUnavailable, ex, null, "Network or protocol error");
        }
        catch (Exception ex)
        {
            return result.SetApiFailure(HttpStatusCode.InternalServerError, ex, null, "Unexpected execution error");
        }
    }

    /// <summary>
    /// Resolves the final <see cref="Uri"/> by combining the client's BaseAddress and the provided endpoint, 
    /// ensuring correct trailing slash handling.
    /// </summary>
    /// <param name="client">The client whose BaseAddress may be used.</param>
    /// <param name="endpointUrl">The target endpoint URL.</param>
    /// <returns>A resolved <see cref="Uri"/>.</returns>
    private static Uri ResolveUri(HttpClient client, string endpointUrl)
    {
        // If the client has no BaseAddress, just treat the endpoint as the full URL
        if (client.BaseAddress != null)
        {
            Uri baseUri;

            if (client.BaseAddress.OriginalString.EndsWith("/"))
            {
                baseUri = client.BaseAddress;
            }
            else
            {
                baseUri = new Uri(client.BaseAddress.OriginalString + "/");
            }

            // Ensure endpoint does NOT start with a slash so it's treated as relative to the base
            var relativeUri = endpointUrl.TrimStart('/');

            return new Uri(baseUri, relativeUri);
        }
            
        return new Uri(endpointUrl, UriKind.RelativeOrAbsolute);
    }

    /// <summary>
    /// Applies the given <see cref="IAuthStrategy"/> (if any) to the request.
    /// </summary>
    /// <returns>
    /// <see langword="null"/> if there was no strategy or it applied successfully; otherwise a failed
    /// <see cref="ApiOperationResult{T}"/> the caller should return immediately without sending the request.
    /// </returns>
    private static async Task<ApiOperationResult<string>?> ApplyAuthAsync(HttpRequestMessage request, IAuthStrategy? authStrategy)
    {
        if (authStrategy == null)
        {
            return null;
        }

        var applyResult = await authStrategy.ApplyAsync(request);
        if (!applyResult.MethodSuccess)
        {
            return new ApiOperationResult<string>()
                .SetApiFailure(HttpStatusCode.Unauthorized, applyResult.Exception, null, "Auth strategy failed to apply credentials");
        }

        return null;
    }

    /// <summary>
    /// Adds a collection of headers to the provided <see cref="HttpRequestMessage"/>.
    /// </summary>
    private static OperationResult<bool> AddHeadersToRequest(HttpRequestMessage request, Dictionary<string, string>? requestHeaders = null)
    {
        var result = new OperationResult<bool>();

        try
        {
            if (requestHeaders != null && requestHeaders.Count > 0)
            {
                List<Exception> errors = new List<Exception>();

                foreach (var header in requestHeaders)
                {
                    try
                    {
                        var key = header.Key.Trim().ToLowerInvariant() ?? string.Empty;
                        var value = header.Value?.Trim() ?? string.Empty;

                        switch (key)
                        {
                            case "content-type":
                                if (request.Content == null)
                                {
                                    throw new InvalidOperationException("Cannot set Content-Type when HttpContent is null.");
                                }

                                request.Content.Headers.ContentType = new MediaTypeHeaderValue(value);
                                break;
                            case "authorization":
                                // Check for common schemes: Basic, Bearer, Digest, or custom
                                switch (value)
                                {
                                    case "basic":
                                        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", value.Substring(6));
                                        break;
                                    case "bearer":
                                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", value.Substring(7));
                                        break;
                                    default:
                                        var split = value.IndexOf(' ');
                                        if (split > 0)
                                        {
                                            var scheme = value.Substring(0, split);
                                            var parameter = value.Substring(split + 1);
                                            request.Headers.Authorization = new AuthenticationHeaderValue(scheme, parameter);
                                        }
                                        else
                                        {
                                            // If only a token without scheme, treat as Bearer
                                            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", value);
                                        }
                                        break;
                                }
                                break;
                            case "user-agent":
                                request.Headers.UserAgent.ParseAdd(header.Value);
                                break;
                            default:
                                if (request.Headers.Contains(header.Key))
                                {
                                    request.Headers.Remove(header.Key); // overwrite existing header
                                }

                                request.Headers.Add(header.Key, header.Value);
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add(ex);
                    }
                }

                if (errors.Count > 0)
                {
                    throw new AggregateException(errors);
                }
            }

            return result.SetMethodSuccess(true);
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }
}
