namespace Integration.DevKit.Core;

public interface IStringCryptoProvider
{
    string Name { get; }
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
}
