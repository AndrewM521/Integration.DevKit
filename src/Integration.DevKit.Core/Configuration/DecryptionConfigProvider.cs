using Microsoft.Extensions.Configuration;

namespace Integration.DevKit.Core;

public sealed class DecryptionConfigProvider : ConfigurationProvider
{
    private readonly IConfiguration _intermediateConfig;
    private readonly JsonDecryptor _decryptor;

    public DecryptionConfigProvider(IConfiguration intermediateConfig, JsonDecryptor decryptor)
    {
        if (intermediateConfig == null)
        {
            throw new ArgumentNullException(nameof(intermediateConfig));
        }

        if (decryptor == null)
        {
            throw new ArgumentNullException(nameof(decryptor));
        }

        _intermediateConfig = intermediateConfig;
        _decryptor = decryptor;
    }

    public override void Load()
    {
        // Recursively process the configuration tree
        DecryptAndLoadChildren(_intermediateConfig.GetChildren());
    }

    private void DecryptAndLoadChildren(IEnumerable<IConfigurationSection> sections)
    {
        foreach (var section in sections)
        {
            // If the section has a value, run it through the decryptor
            if (section.Value != null)
            {
                // section.Path uses standard .NET configuration keys (e.g., "Parent:Child:Property")
                Data[section.Path] = _decryptor.Decrypt(section.Value);
            }

            // Recurse deeper into the hierarchy for nested objects/arrays
            DecryptAndLoadChildren(section.GetChildren());
        }
    }
}
