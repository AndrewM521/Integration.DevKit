using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.DevKit.Core.Configuration;

public sealed class JsonDecryptor
{
    private readonly Dictionary<string, IStringCryptoProvider> _providers;
    private readonly CryptoContract _contract;

    public JsonDecryptor(IEnumerable<IStringCryptoProvider> providers, CryptoContract contract)
    {
        if (providers == null)
        {
            throw new ArgumentNullException(nameof(providers));
        }

        if (_contract == null)
        {
            throw new ArgumentNullException(nameof(contract));
        }

        _providers = providers.ToDictionary(p => p.Name);
        _contract = contract;
    }

    public string Decrypt(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        if (!value.StartsWith(_contract.Signature, StringComparison.Ordinal))
        {
            return value;
        }

        string expectedPrefix = _contract.BuildPrefix();

        // If it matches the contract's default provider perfectly, peel off the prefix and decrypt
        if (value.StartsWith(expectedPrefix, StringComparison.Ordinal))
        {
            string payload = value.Substring(expectedPrefix.Length);
            return _contract.Provider.Decrypt(payload);
        }

        // If it starts with "ENC:" but doesn't match the current default provider prefix,
        // it means it was encrypted using an alternate registered provider. We parse it dynamically.
        var parts = value.Split(':', 4);
        if (parts.Length < 4)
        {
            throw new FormatException($"The encrypted configuration value is structurally malformed.");
        }

        var version = parts[1];
        var providerName = parts[2];
        var alternativePayload = parts[3];

        // Validate version compatibility if your contract enforces it
        if (!string.Equals(version, _contract.Version, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Unsupported encryption contract version '{version}'. Expected version '{_contract.Version}'.");
        }

        // Safeguard against missing cryptography engines
        if (!_providers.TryGetValue(providerName, out var provider))
        {
            throw new KeyNotFoundException(
                $"Cryptographic provider '{providerName}' requested by the payload is not registered in the application container.");
        }

        try
        {
            return provider.Decrypt(alternativePayload);
        }
        catch (Exception ex)
        {
            // Wrap crypto provider errors to give clear configuration troubleshooting context
            throw new InvalidOperationException(
                $"Failed to decrypt payload using provider '{providerName}'. See inner exception for details.", ex);
        }
    }
}
