using AndrewM5.DevKit.Core.Abstractions;
using System.Net;

namespace AndrewM5.DevKit.Core.Results;

public class ApiOperationResult<T> : NullableOperationResult<T>
{
    private string _requestUrl = string.Empty;
    private HttpStatusCode _statusCode = HttpStatusCode.InternalServerError;
    private string? _responseBody = string.Empty;
    private string? _displaySummary = string.Empty;

    public string RequestUrl => _requestUrl;
    public HttpStatusCode StatusCode => _statusCode;
    public string? ResponseBody => _responseBody;
    public string? DisplaySummary => _displaySummary;

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
