using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.DevKit.Core.Configuration;

public sealed class Base64CryptoProvider : IStringCryptoProvider
{
    public string Name => "BASE64";

    public string Encrypt(string plainText)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(plainText));
    }

    public string Decrypt(string cipherText)
    {
        return Encoding.UTF8.GetString(Convert.FromBase64String(cipherText));
    }
}
