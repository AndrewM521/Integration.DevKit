/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using Integration.DevKit.Core.Abstractions;
using System.Net;

namespace Integration.DevKit.Core.Results;

/// <summary>
/// Represents the result of an API operation, extending <see cref="NullableOperationResult{T}"/> 
/// with HTTP-specific metadata such as status codes, request URLs, and raw response bodies.
/// </summary>
/// <typeparam name="T">The type of the result data to return within the operation.</typeparam>
public class ApiOperationResult<T> : NullableOperationResult<T>
{
    private string _requestUrl = string.Empty;
    private HttpStatusCode _statusCode = HttpStatusCode.InternalServerError;
    private string? _responseBody = string.Empty;
    private string? _displaySummary = string.Empty;

    /// <summary>
    /// Gets the URL of the API request that was performed.
    /// </summary>
    public string RequestUrl => _requestUrl;

    /// <summary>
    /// Gets the HTTP status code returned by the server. 
    /// </summary>
    public HttpStatusCode StatusCode => _statusCode;

    /// <summary>
    /// Gets the raw string content of the API response body.
    /// </summary>
    public string? ResponseBody => _responseBody;

    /// <summary>
    /// Gets a user-friendly or developer-facing summary of the operation's outcome.
    /// </summary>
    public string? DisplaySummary => _displaySummary;

    /// <summary>
    /// Sets the URL for the request.
    /// </summary>
    /// <param name="url">The request URL string. If null or whitespace, an empty string is stored.</param>
    public void SetRequestUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            _requestUrl = "";
        }
        else
        {
            _requestUrl = url;
        }
    }

    /// <summary>
    /// Sets the state of the result to success, capturing relevant API response data.
    /// </summary>
    /// <param name="result">The deserialized object from the response.</param>
    /// <param name="statusCode">The HTTP status code (e.g., 200 OK).</param>
    /// <param name="responseBody">The raw JSON/XML/Text response body.</param>
    /// <param name="displaySummary">An optional summary message; defaults to "Success".</param>
    /// <returns>The current <see cref="ApiOperationResult{T}"/> instance.</returns>
    public ApiOperationResult<T> SetApiSuccess(T? result, HttpStatusCode statusCode, string? responseBody = null, string? displaySummary = null)
    {
        SetMethodSuccess(result);

        _statusCode = statusCode;
        _responseBody = responseBody;
        _displaySummary = "Success";

        if (!string.IsNullOrWhiteSpace(displaySummary))
        {
            _displaySummary = displaySummary;
        }

        return this;
    }

    /// <summary>
    /// Sets the state of the result to failure, capturing the exception and API context.
    /// </summary>
    /// <param name="statusCode">The HTTP status code (e.g., 404, 500, etc.).</param>
    /// <param name="ex">The exception that occurred during the request or parsing.</param>
    /// <param name="responseBody">The raw response body, which may contain error details.</param>
    /// <param name="displaySummary">An optional summary message; defaults to "Fail".</param>
    /// <returns>The current <see cref="ApiOperationResult{T}"/> instance.</returns>
    public ApiOperationResult<T> SetApiFailure(HttpStatusCode statusCode, Exception ex, string? responseBody = null, string? displaySummary = null)
    {
        SetMethodFailure(ex);

        _statusCode = statusCode;
        _responseBody = responseBody;
        _displaySummary = "Fail";

        if (!string.IsNullOrWhiteSpace(displaySummary))
        {
            _displaySummary = displaySummary;
        }

        return this;
    }

    /// <summary>
    /// Returns a detailed string representation of the API operation result
    /// </summary>
    /// <returns>
    /// A formatted string including Success/Fail status, the Request URL, and the Status Code.
    /// If a response body exists, it is truncated to the first 100 characters.
    /// </returns>
    public override string ToString()
    {
        string retVal = string.Empty;

        if (MethodSuccess)
        {
            retVal = "Result: Success, ";

            if (Result == null)
            {
                retVal += "Result: null";
            }
            else
            {
                retVal += $"Result: {Result.ToString()}";
            }

            retVal += $", RequestUrl: {RequestUrl}, StatusCode: {StatusCode}";

            if (!string.IsNullOrEmpty(ResponseBody))
            {
                retVal += $", ResponseBody: {ResponseBody.Substring(0, Math.Min(ResponseBody.Length, 100))}"; // Truncate to avoid large responses
            }

            if (!string.IsNullOrWhiteSpace(DisplaySummary))
            {
                retVal += $", DisplaySummary: {DisplaySummary}";
            }

            retVal += "}";
        }
        else
        {
            retVal = $"Result: Fail, Exception: {Exception.Message}, RequestUrl: {RequestUrl}, StatusCode: {StatusCode}";

            if (!string.IsNullOrWhiteSpace(DisplaySummary))
            {
                retVal += $", DisplaySummary: {DisplaySummary}";
            }
        }

        return retVal;
    }
}
