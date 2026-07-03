namespace Integration.DevKit.Core;

public interface IConfigProtector
{
    public string Name { get; }
    public string Encrypt(string plainText);
    public string Decrypt(string cipherText);
}
