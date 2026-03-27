
namespace AndrewM5.DevKit.OAuth.Abstractions;

public interface IOAuthProvider
{
    string AuthorizationEndpoint { get; }
    string TokenEndpoint { get; }
    string? RevocationEndpoint { get; }
    bool SupportsPKCE { get; }
}
