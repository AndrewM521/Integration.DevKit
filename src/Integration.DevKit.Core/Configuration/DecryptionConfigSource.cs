using Microsoft.Extensions.Configuration;

namespace Integration.DevKit.Core;

public sealed class DecryptionConfigSource : IConfigurationSource
{
    private readonly IConfiguration _intermediateConfig;
    private readonly JsonDecryptor _decryptor;

    public DecryptionConfigSource(IConfiguration intermediateConfig, JsonDecryptor decryptor)
    {
        _intermediateConfig = intermediateConfig ?? throw new ArgumentNullException(nameof(intermediateConfig));
        _decryptor = decryptor ?? throw new ArgumentNullException(nameof(decryptor));
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new DecryptionConfigProvider(_intermediateConfig, _decryptor);
    }
}
