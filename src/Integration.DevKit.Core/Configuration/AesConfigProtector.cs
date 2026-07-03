using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Integration.DevKit.Core.Configuration;

public sealed class AesConfigProtector : IConfigProtector
{
    private readonly byte[] _key;
    private readonly byte[] _iv;

    public string Name => "AES256";

    /// <summary>
    /// Initializes the protector with a secure 32-byte key and a 16-byte initialization vector (IV).
    /// </summary>
    public AesConfigProtector(string encryptionKey, string iv)
    {
        if (string.IsNullOrWhiteSpace(encryptionKey)) throw new ArgumentNullException(nameof(encryptionKey));
        if (string.IsNullOrWhiteSpace(iv)) throw new ArgumentNullException(nameof(iv));

        // AES-256 requires a 32-byte key and a 16-byte IV
        _key = RandomNumberGenerator.GetBytes(32); // In production, derive these deterministically from your inputs
        _iv = RandomNumberGenerator.GetBytes(16);

        // Alternatively, pad/truncate your string inputs for testing:
        _key = Encoding.UTF8.GetBytes(encryptionKey.PadRight(32).Substring(0, 32));
        _iv = Encoding.UTF8.GetBytes(iv.PadRight(16).Substring(0, 16));
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return plainText;

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var memoryStream = new MemoryStream();
        using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
        using (var writer = new StreamWriter(cryptoStream))
        {
            writer.Write(plainText);
        }

        return Convert.ToBase64String(memoryStream.ToArray());
    }

    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText)) return cipherText;

        var buffer = Convert.FromBase64String(cipherText);

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;

        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var memoryStream = new MemoryStream(buffer);
        using var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
        using var reader = new StreamReader(cryptoStream);

        return reader.ReadToEnd();
    }
}
