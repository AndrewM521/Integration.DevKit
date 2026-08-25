using System.Text;

namespace Integration.DevKit.Core;

public sealed class Base64ConfigProtector : IConfigProtector
{
    /// <summary>
    /// Gets the identifier for the base-64 encoding strategy implemented by this protector.
    /// </summary>
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
