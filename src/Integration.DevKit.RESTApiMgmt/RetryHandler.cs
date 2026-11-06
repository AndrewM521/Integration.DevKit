/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using System.Net;

namespace Integration.DevKit.RESTApiMgmt;

/// <summary>
/// Retries transient HTTP failures (network errors, 408, 429, 500, 502, 503, 504) with exponential backoff and jitter,
/// honoring a server's Retry-After header. Reads <see cref="ApiManager.RuntimeSettings"/> on every request.
/// </summary>
/// <remarks>
/// POST and PATCH are never retried because they are not idempotent and a retry could duplicate a write.
/// The owning <see cref="HttpClient.Timeout"/> covers all attempts together, not each one.
/// </remarks>
internal sealed class RetryHandler : DelegatingHandler
{
    private readonly ApiManager _apiManager;

    internal RetryHandler(ApiManager apiManager)
    {
        _apiManager = apiManager;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var settings = _apiManager.RuntimeSettings;

        int maxRetries = 0;
        if (request.Method != HttpMethod.Post && request.Method != HttpMethod.Patch)
        {
            maxRetries = Math.Max(0, settings.MaxRetries);
        }

        for (int attempt = 0; ; attempt++)
        {
            HttpResponseMessage? response = null;

            try
            {
                response = await base.SendAsync(request, cancellationToken);

                if (attempt >= maxRetries || !IsTransient(response.StatusCode))
                {
                    return response;
                }
            }
            catch (HttpRequestException) when (attempt < maxRetries)
            {
            }

            double backoffMs = settings.RetryBaseDelay_Milliseconds * Math.Pow(2, attempt);
            TimeSpan delay = response?.Headers.RetryAfter?.Delta
                ?? TimeSpan.FromMilliseconds(backoffMs + Random.Shared.NextDouble() * backoffMs);

            response?.Dispose();

            await Task.Delay(delay, cancellationToken);
        }
    }

    private static bool IsTransient(HttpStatusCode statusCode)
    {
        return statusCode is HttpStatusCode.RequestTimeout
            or HttpStatusCode.TooManyRequests
            or HttpStatusCode.InternalServerError
            or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout;
    }
}
