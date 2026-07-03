namespace Integration.DevKit.Core;

public sealed class JsonDecryptor
{
    private readonly Dictionary<string, IConfigProtector> _providers;
    private readonly ConfigProtectorContract _contract;

    public JsonDecryptor(ConfigProtectorContract contract, List<IConfigProtector> providers)
    {
        if (contract == null)
        {
            throw new ArgumentNullException(nameof(contract));
        }

        if (providers == null)
        {
            throw new ArgumentNullException(nameof(providers));
        }

        _providers = providers.ToDictionary(p => p.Name);
        _contract = contract;
    }

    public string Decrypt(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || !value.StartsWith(_contract.Signature, StringComparison.Ordinal))
        {
            return value;
        }

        var parts = value.Split(_contract.Delimiter, 3);
        if (parts.Length < 3)
        {
            throw new FormatException($"The encrypted configuration value is structurally malformed.");
        }

        var providerName = parts[1];
        var alternativePayload = parts[2];

        if (!_providers.TryGetValue(providerName, out var provider))
        {
            throw new KeyNotFoundException($"Cryptographic provider '{providerName}' is not registered.");
        }

        return provider.Decrypt(alternativePayload);
    }
}
