using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AndrewM5.DevKit.OAuth.Abstractions;

public class TokenResponse
{
    public string AccessToken { get; init; } = string.Empty;
    public string? RefreshToken { get; init; }
    public string TokenType { get; init; } = "Bearer";
    public int? ExpiresIn { get; init; }
    //public DateTimeOffset? ExpiresAt { get; init; }
    //public string? Scope { get; init; }
    //public string? IdToken { get; init; }
    //public IReadOnlyDictionary<string, string>? AdditionalParameters { get; init; }
}
