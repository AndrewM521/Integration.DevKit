using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AndrewM5.DevKit.OAuth.Abstractions;

public class AuthorizationCodeTokenRequest : TokenRequest
{
    public string Code { get; init; }
    public string RedirectUri { get; init; }
    public string CodeVerifier { get; init; }
}
