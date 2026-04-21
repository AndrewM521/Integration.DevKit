using AndrewM5.DevKit.Core.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace AndrewM5.DevKit.ApiClientManagement;

public static class ApiRequest
{
    public static async Task<ApiOperationResult<string>> GetAsync(HttpClient client, string endpointUrl, Dictionary<string, string>? headers = null)
    {
        var finalUri = ResolveUri(client, endpointUrl);

        using var request = new HttpRequestMessage(HttpMethod.Get, finalUri);

        AddHeaders(request, headers);
        
        return await SendRequestAsync(client, request);
    }

    public static async Task<ApiOperationResult<string>> PostAsync(HttpClient client, string endpointUrl, HttpContent? content = null, Dictionary<string, string>? headers = null)
    {
        var finalUri = ResolveUri(client, endpointUrl);

        using var request = new HttpRequestMessage(HttpMethod.Post, finalUri) { Content = content };
        
        AddHeaders(request, headers);
        
        return await SendRequestAsync(client, request);
    }

    public static async Task<ApiOperationResult<string>> PutAsync(HttpClient client, string endpointUrl, HttpContent content, Dictionary<string, string>? headers = null)
    {
        var finalUri = ResolveUri(client, endpointUrl);

        using var request = new HttpRequestMessage(HttpMethod.Put, finalUri) { Content = content };
        
        AddHeaders(request, headers);
        
        return await SendRequestAsync(client, request);
    }

    public static async Task<ApiOperationResult<string>> DeleteAsync(HttpClient client, string endpointUrl, Dictionary<string, string>? headers = null)
    {
        var finalUri = ResolveUri(client, endpointUrl);

        using var request = new HttpRequestMessage(HttpMethod.Delete, finalUri);
        
        AddHeaders(request, headers);
        
        return await SendRequestAsync(client, request);
    }

    /// <summary>
    /// Executes an HTTP request and safely captures the response or exception into an ApiOperationResult.
    /// </summary>
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

    private static void AddHeaders(HttpRequestMessage request, Dictionary<string, string>? headers)
    {
        if (headers != null)
        {
            foreach (var header in headers)
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }
    }

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
}
