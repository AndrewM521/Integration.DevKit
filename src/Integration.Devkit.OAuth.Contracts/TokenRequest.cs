
namespace AndrewM5.DevKit.OAuth.Abstractions;

public class TokenRequest
{
    public string ClientId { get; init; }
    public string ClientSecret { get; init; }
    public string Code { get; init; }
    public string RedirectUri { get; init; }
}
