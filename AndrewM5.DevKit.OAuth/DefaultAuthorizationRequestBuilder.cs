using AndrewM5.DevKit.OAuth.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AndrewM5.DevKit.OAuth;

internal class DefaultAuthorizationRequestBuilder : IAuthorizationRequestBuilder
{
    private readonly IOAuthProvider _provider;

    public DefaultAuthorizationRequestBuilder(IOAuthProvider provider)
    {
        _provider = provider;
    }

    public string BuildAuthorizationUrl(AuthorizationRequest request)
    {
        var query = new Dictionary<string, string>
        {
            ["response_type"] = "code",
            ["client_id"] = request.ClientId,
            ["redirect_uri"] = request.RedirectUri,
            ["scope"] = request.Scope,
            ["state"] = request.State,
            ["code_challenge"] = request.CodeChallenge,
            ["code_challenge_method"] = request.CodeChallengeMethod
        };

        return QueryHelpers.AddQueryString(_provider.AuthorizationEndpoint, query);
    }
}
