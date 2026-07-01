using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.DevKit.Core.Configuration;

public sealed class CryptoContract
{
    private readonly string _signature = "ENC:";

    public string Signature
    {
        get { return _signature; }
        init
        {
            if (!value.EndsWith(":"))
            {
                _signature = value + ":";
            }

            _signature = value;
        }
    }
    public string Version { get; init; } = "v1";
    public IStringCryptoProvider Provider { get; init; } = new Base64CryptoProvider();

    public string BuildPrefix() => $"{Signature}{Version}:{Provider.Name}:";
}
