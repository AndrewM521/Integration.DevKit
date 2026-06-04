using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AndrewM5.DevKit.OAuth.Abstractions;

public interface ITokenStore
{
    Task StoreAsync(TokenResponse token, CancellationToken ct = default);
    Task<TokenResponse?> GetAsync(string key, CancellationToken ct = default);
}
