using AndrewM5.DevKit.OAuth.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AndrewM5.DevKit.OAuth;

public class SystemTextJsonTokenResponseParser : ITokenResponseParser
{
    public TokenResponse Parse(string raw)
    {
        using var doc = JsonDocument.Parse(raw);
        var root = doc.RootElement;

        var accessToken = root.GetProperty("access_token").GetString();

        int? expiresIn = null;
        if (root.TryGetProperty("expires_in", out var exp))
        {
            expiresIn = exp.GetInt32();
        }

        return new TokenResponse
        {
            AccessToken = accessToken,
            TokenType = root.TryGetProperty("token_type", out var type)
                ? type.GetString()
                : "Bearer",

            ExpiresIn = expiresIn,
            ExpiresAt = expiresIn.HasValue
                ? DateTimeOffset.UtcNow.AddSeconds(expiresIn.Value)
                : null,

            RefreshToken = root.TryGetProperty("refresh_token", out var refresh)
                ? refresh.GetString()
                : null,

            Scope = root.TryGetProperty("scope", out var scope)
                ? scope.GetString()
                : null,

            IdToken = root.TryGetProperty("id_token", out var id)
                ? id.GetString()
                : null
        };
    }
}
