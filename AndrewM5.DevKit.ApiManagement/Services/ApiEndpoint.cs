using AndrewM5.DevKit.Core.Results;
using System.Net;

namespace AndrewM5.DevKit.ApiManagement.Services;

public class ApiEndpoint
{
    private string _route;

    public ApiEndpoint(string route)
    {
        _route = route.TrimEnd('/');
    }

    #region Asyncronous Methods
    public async Task<OperationResult<string>> BuildQueryUrlAsync(Dictionary<string, object?>? queryParams = null)
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
    public string BuildUrl()
    {
        return _route;
    }
    public OperationResult<string> BuildQueryUrl(Dictionary<string, object?>? queryParams = null)
    {
        return BuildQueryUrlAsync(queryParams).GetAwaiter().GetResult();
    }
    
    public OperationResult<string> BuildSlashUrl(Dictionary<string, object?>? queryParams = null)
    {
        var result = new OperationResult<string>();

        try
        {
            string url = _route;

            if (queryParams != null && queryParams.Count > 0)
            {
                foreach (var kvp in queryParams)
                {
                    if (kvp.Value != null)
                    {
                        url += $"/{WebUtility.UrlEncode(kvp.Key)}/{WebUtility.UrlEncode(kvp.Value.ToString()!)}";
                    }
                }
            }

            return result.SetMethodSuccess(url);
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }
    
    public OperationResult<string> BuildPositionalUrl(List<object?>? queryParams = null)
    {
        var result = new OperationResult<string>();

        try
        {
            string url = _route;

            if (queryParams != null)
            {
                foreach (var val in queryParams)
                {
                    if (val != null)
                    {
                        url += $"/{WebUtility.UrlEncode(val.ToString()!)}";
                    }
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

public enum EndpointUrlStyle
{
    Query,
    Slash,
    Positional
}