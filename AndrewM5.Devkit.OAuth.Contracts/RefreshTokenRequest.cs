using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AndrewM5.DevKit.OAuth.Abstractions;

public class RefreshTokenRequest : TokenRequest
{
    public string RefreshToken { get; init; }
}
