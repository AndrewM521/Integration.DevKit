using AndrewM5.DevKit.Core.Results;
using AndrewM5.DevKit.CredentialManagement.Contracts.Interfaces;
using Microsoft.AspNetCore.DataProtection;

namespace AndrewM5.DevKit.CredentialManagement;

/// <summary>
/// Provides a base implementation for secret stores, incorporating <see cref="IDataProtector"/> 
/// to handle encryption and decryption of sensitive values before storage.
/// </summary>
public abstract class SecretStoreBase : ISecretStore
{
    /// <inheritdoc />
    public string StoreName { get; private set; }

    /// <summary>
    /// The protector used to secure strings within the store.
    /// </summary>
    private readonly IDataProtector _protector;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecretStoreBase"/> class.
    /// </summary>
    /// <param name="provider">The provider used to create the data protector.</param>
    /// <param name="purpose">A unique string used to derive the encryption key (e.g., a specific application or module name).</param>
    /// <param name="storeName">The friendly name of this secret store.</param>
    protected SecretStoreBase(IDataProtectionProvider provider, string purpose, string storeName)
    {
        _protector = provider.CreateProtector(purpose);
        StoreName = storeName;
    }

    /// <inheritdoc />
    public abstract NullOperationResult SetKey(string fileName, string key, string value);

    /// <inheritdoc />
    public abstract OperationResult<string> GetKey(string fileName, string key);

    /// <inheritdoc />
    public abstract NullOperationResult DeleteKey(string fileName, string key);

    /// <inheritdoc />
    public abstract NullOperationResult DeleteSecret(string fileName);

    /// <summary>
    /// Protects the provided plaintext string using the configured data protector.
    /// </summary>
    /// <param name="plaintext">The sensitive string to encrypt.</param>
    /// <returns>An encrypted string (ciphertext) safe for storage.</returns>
    protected string Encrypt(string plaintext) => _protector.Protect(plaintext.Trim());

    /// <summary>
    /// Unprotects the provided ciphertext string back into plaintext.
    /// </summary>
    /// <param name="ciphertext">The encrypted string retrieved from storage.</param>
    /// <returns>The original decrypted plaintext string.</returns>
    /// <exception cref="System.Security.Cryptography.CryptographicException">Thrown if the ciphertext is tampered with or the protection key is unavailable.</exception>
    protected string Decrypt(string ciphertext) => _protector.Unprotect(ciphertext.Trim());
}
