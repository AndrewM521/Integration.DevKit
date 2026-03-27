using AndrewM5.DevKit.Core.Results;

namespace AndrewM5.DevKit.OAuth.Abstractions;

public interface ITokenService
{
    Task<OperationResult<TokenResponse>> RequestTokenAsync(string clientId, string clientSecret, string code, string redirectUri);
    Task<OperationResult<TokenResponse>> RefreshTokenAsync(string clientId, string clientSecret, string refreshToken);
}
