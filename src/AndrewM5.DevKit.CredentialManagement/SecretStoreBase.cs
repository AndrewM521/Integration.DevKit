using AndrewM5.DevKit.Core.Results;
using AndrewM5.DevKit.CredentialManagement.Contracts.Interfaces;
using Microsoft.AspNetCore.DataProtection;

namespace AndrewM5.DevKit.CredentialManagement;

/// <summary>
/// Concrete Implementation of <see cref="ISecretStore"/> incorporating <see cref="IDataProtector"/> 
/// to handle encryption and decryption of sensitive values before they are persisted.
/// </summary>
/// <remarks>
/// Inheriting classes are responsible for implementing the storage mechanism (e.g., File System, Database),
/// while this base class ensures all secrets are cryptographically protected using the ASP.NET Core Data Protection stack.
/// </remarks>
public abstract class SecretStoreBase : ISecretStore
{
    /// <inheritdoc />
    /// <value>The descriptive name used to identify this specific store instance.</value>
    public string StoreName { get; private set; }

    /// <summary>
    /// The protector used to secure strings within the store.
    /// </summary>
    private readonly IDataProtector _protector;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecretStoreBase"/> class.
    /// </summary>
    /// <param name="provider">The <see cref="IDataProtectionProvider"/> used to create the data protector.</param>
    /// <param name="purpose">
    /// A unique string used for cryptographic isolation. Data protected with one purpose string 
    /// cannot be unprotected by a provider using a different purpose.
    /// </param>
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
    /// Encrypts a plaintext string using the configured <see cref="IDataProtector"/>.
    /// </summary>
    /// <param name="plaintext">The sensitive string to encrypt. The string is trimmed before encryption.</param>
    /// <returns>A Base64-encoded ciphertext string safe for storage.</returns>
    /// <remarks>
    /// Leading and trailing whitespace is removed via <see cref="string.Trim()"/> to ensure 
    /// consistent encryption results.
    /// </remarks>
    protected string Encrypt(string plaintext) => _protector.Protect(plaintext.Trim());

    /// <summary>
    /// Decrypts a ciphertext string back into its original plaintext format.
    /// </summary>
    /// <param name="ciphertext">The encrypted string retrieved from storage.</param>
    /// <returns>The original decrypted plaintext string.</returns>
    /// <exception cref="System.Security.Cryptography.CryptographicException">
    /// Thrown if the ciphertext is corrupted, tampered with, or if the decryption keys have expired/changed.
    /// </exception>
    protected string Decrypt(string ciphertext) => _protector.Unprotect(ciphertext.Trim());
}
