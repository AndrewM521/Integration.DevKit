namespace Integration.DevKit.Core;

/// <summary>
/// Defines the contract for components that can protect and unprotect configuration values.
/// </summary>
public interface IConfigProtector
{
    public string Name { get; }

    /// <summary>
    /// Encrypts a plaintext value into its protected form for storage or transmission.
    /// </summary>
    /// <param name="plainText">The plaintext value to protect.</param>
    /// <returns>The protected representation of the supplied value.</returns>
    public string Encrypt(string plainText);

    /// <summary>
    /// Decrypts a protected value back into its original plaintext form.
    /// </summary>
    /// <param name="cipherText">The protected value to decode.</param>
    /// <returns>The original plaintext value.</returns>
    public string Decrypt(string cipherText);
}
