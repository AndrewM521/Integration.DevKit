using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AndrewM5.DevKit.OAuth.Abstractions;

public class AuthorizationRequest
{
    public string ClientId { get; init; }
    public string RedirectUri { get; init; }
    public string Scope { get; init; }
    public string State { get; init; }

    // PKCE
    public string CodeChallenge { get; init; }
    public string CodeChallengeMethod { get; init; } = "S256";
}
