using AndrewM5.DevKit.ApiManagement.Abstractions.Settings;
using AndrewM5.DevKit.Core.Results;
using AndrewM5.DevKit.CredentialManagement.Contracts.Interfaces;
using System.Text;

namespace AndrewM5.DevKit.ApiManagement.Contracts.Interfaces;

public interface IApiClient : IAsyncDisposable
{
    public ApiClientSettings RuntimeSettings { get; }

    public IApiClientMetrics ClientMetrics { get; }

    public string ClientName { get; }

    #region Asynchronous HTTP Methods
    public Task<ApiOperationResult<string>> GetAsync(string endpointUrl, Dictionary<string, string>? requestHeaders = null);

    public Task<ApiOperationResult<string>> PostAsync(string endpointUrl, Dictionary<string, string>? requestHeaders = null);

    public Task<ApiOperationResult<string>> PostAsync(string endpointUrl, HttpContent httpContent, Dictionary<string, string>? requestHeaders = null);

    public Task<ApiOperationResult<string>> PutAsync(string endpointUrl, HttpContent httpContent, Dictionary<string, string>? requestHeaders = null);

    public Task<ApiOperationResult<string>> DeleteAsync(string endpointUrl, Dictionary<string, string>? requestHeaders = null);
    #endregion

    #region Synchronous HTTP Methods
    public ApiOperationResult<string> Get(string endpointUrl, Dictionary<string, string>? requestHeaders = null);

    public ApiOperationResult<string> Post(string endpointUrl, Dictionary<string, string>? requestHeaders = null);

    public ApiOperationResult<string> Post(string endpointUrl, HttpContent httpContent, Dictionary<string, string>? requestHeaders = null);

    public ApiOperationResult<string> Put(string endpointUrl, HttpContent httpContent, Dictionary<string, string>? requestHeaders = null);
    
    public ApiOperationResult<string> Delete(string endpointUrl, Dictionary<string, string>? requestHeaders = null);
    #endregion

    #region Credentials
    public void SetSecretStore(ISecretStore secretStore);
    
    public NullOperationResult SetCredentials(string username, string password);

    public OperationResult<string> GetUsername();

    public OperationResult<string> GetPassword();

    public NullOperationResult DeleteCredential(string key);

    public NullOperationResult DeleteAllCredentials();
    #endregion

    #region Helper Methods
    public OperationResult<HttpContent> CreateHttpContent(RESTApiMediaTypes mediaType, string data, Encoding? encoding = null);

    public NullOperationResult AddDefaultHeader(string key, string value);
    #endregion

    public void OutputRuntimeSettings(bool calledFromManager = false);
}

public enum RESTApiMediaTypes
{
    Json,
    Xml,
    PlainText,
    WWW_UrlEncoded
}