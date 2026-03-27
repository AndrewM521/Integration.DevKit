using AndrewM5.DevKit.Core.Results;
using AndrewM5.DevKit.CredentialManagement.Contracts.Interfaces;
using Microsoft.AspNetCore.DataProtection;

namespace AndrewM5.DevKit.CredentialManagement;

public abstract class SecretStoreBase : ISecretStore
{
    public string StoreName { get; private set; }

    private readonly IDataProtector _protector;

    protected SecretStoreBase(IDataProtectionProvider provider, string purpose, string storeName)
    {
        _protector = provider.CreateProtector(purpose);
        StoreName = storeName;
    }

    public abstract NullOperationResult SetKey(string fileName, string key, string value);
    public abstract OperationResult<string> GetKey(string fileName, string key);
    public abstract NullOperationResult DeleteKey(string fileName, string key);
    public abstract NullOperationResult DeleteSecret(string fileName);

    protected string Encrypt(string plaintext) => _protector.Protect(plaintext.Trim());
    protected string Decrypt(string ciphertext) => _protector.Unprotect(ciphertext.Trim());
}
