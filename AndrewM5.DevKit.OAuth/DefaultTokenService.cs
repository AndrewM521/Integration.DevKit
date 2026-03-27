using AndrewM5.DevKit.ApiManagement;
using AndrewM5.DevKit.ApiManagement.Contracts.Interfaces;
using AndrewM5.DevKit.Core.Results;
using AndrewM5.DevKit.OAuth.Abstractions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AndrewM5.DevKit.OAuth;

public class DefaultTokenService : ITokenService
{
    private readonly IApiClient _apiClient;
    private readonly IOAuthProvider _provider;

    public DefaultTokenService(IApiClient apiClient, IOAuthProvider provider)
    {
        _apiClient = apiClient;
        _provider = provider;
    }

    public async Task<OperationResult<TokenResponse>> RequestTokenAsync(string clientId, string clientSecret, string code, string redirectUri)
    {
        var result = new OperationResult<TokenResponse>();

        try
        {
            var endpoint = new ApiEndpoint(_provider.TokenEndpoint);

            var formParams = new Dictionary<string, object?>
            {
                ["grant_type"] = "authorization_code",
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["code"] = code,
                ["redirect_uri"] = redirectUri
            };

            var buildPostUrl = await endpoint.BuildQueryUrlAsync(formParams);
            if (!buildPostUrl.MethodSuccess)
            {
                throw buildPostUrl.Exception;
            }

            var getContent = _apiClient.CreateHttpContent(RESTApiMediaTypes.WWW_UrlEncoded, buildPostUrl.Result);
            if (!getContent.MethodSuccess)
            {
                throw getContent.Exception;
            }

            var apiPostCall = await _apiClient.PostAsync("", getContent.Result);
            if (!apiPostCall.MethodSuccess)
            {
                throw apiPostCall.Exception;
            }

            Debug.WriteLine("<DefaultTokenService> TODO: Allow Deserialize into custom type");
            var token = JsonSerializer.Deserialize<TokenResponse>(apiPostCall.Result!);

            return result.SetMethodSuccess(token!);
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }

    public async Task<OperationResult<TokenResponse>> RefreshTokenAsync(string clientId, string clientSecret, string refreshToken)
    {
        var result = new OperationResult<TokenResponse>();

        try
        {
            var endpoint = new ApiEndpoint(_provider.TokenEndpoint);

            var formParams = new Dictionary<string, object?>
            {
                ["grant_type"] = "refresh_token",
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["refresh_token"] = refreshToken
            };

            var buildPostUrl = await endpoint.BuildQueryUrlAsync(formParams);
            if (!buildPostUrl.MethodSuccess)
            {
                throw buildPostUrl.Exception;
            }

            var getContent = _apiClient.CreateHttpContent(RESTApiMediaTypes.WWW_UrlEncoded, buildPostUrl.Result);
            if (!getContent.MethodSuccess)
            {
                throw getContent.Exception;
            }

            var apiPostCall = await _apiClient.PostAsync("", getContent.Result);
            if (!apiPostCall.MethodSuccess)
            {
                throw apiPostCall.Exception;
            }

            Debug.WriteLine("<DefaultTokenService> TODO: Allow Deserialize into custom type");
            var token = JsonSerializer.Deserialize<TokenResponse>(apiPostCall.Result!);

            return result.SetMethodSuccess(token!);
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }

    //private HttpRequestMessage BuildHttpRequest(TokenRequest request)
    //{
    //    var parameters = request switch
    //    {
    //        AuthorizationCodeTokenRequest auth => new Dictionary<string, string>
    //        {
    //            ["grant_type"] = "authorization_code",
    //            ["code"] = auth.Code,
    //            ["redirect_uri"] = auth.RedirectUri,
    //            ["client_id"] = auth.ClientId,
    //            ["code_verifier"] = auth.CodeVerifier
    //        },

    //        RefreshTokenRequest refresh => new Dictionary<string, string>
    //        {
    //            ["grant_type"] = "refresh_token",
    //            ["refresh_token"] = refresh.RefreshToken,
    //            ["client_id"] = refresh.ClientId
    //        },

    //        ClientCredentialsTokenRequest client => new Dictionary<string, string>
    //        {
    //            ["grant_type"] = "client_credentials",
    //            ["scope"] = client.Scope,
    //            ["client_id"] = client.ClientId,
    //            ["client_secret"] = client.ClientSecret
    //        },

    //        _ => throw new NotSupportedException($"Unsupported token request: {request.GetType().Name}")
    //    };

    //    return new HttpRequestMessage(HttpMethod.Post, _provider.TokenEndpoint)
    //    {
    //        Content = new FormUrlEncodedContent(parameters)
    //    };
    //}

    //private Exception ParseError(string content, HttpStatusCode statusCode)
    //{
    //    try
    //    {
    //        var error = JsonSerializer.Deserialize<TokenErrorResponse>(content);

    //        return new OAuthException(
    //            error?.Error ?? "unknown_error",
    //            error?.ErrorDescription,
    //            statusCode);
    //    }
    //    catch
    //    {
    //        return new OAuthException("invalid_response", content, statusCode);
    //    }
    //}
}

public class OAuthException : Exception
{
    public string Error { get; }
    public HttpStatusCode StatusCode { get; }

    public OAuthException(string error, string? description, HttpStatusCode statusCode)
        : base(description ?? error)
    {
        Error = error;
        StatusCode = statusCode;
    }
}