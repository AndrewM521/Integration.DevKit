using Microsoft.Extensions.Configuration;

namespace Integration.DevKit.Core;

public static class ConfigProtectorExtensions
{
    public static IConfigurationBuilder EncryptJsonOnBuild(this IConfigurationBuilder builder, ConfigProtectorContract contract, Action<EncryptionOptions> configure)
    {
        var options = new EncryptionOptions();
        configure(options);

        var encrypter = new JsonEncryptor(contract, options);
        encrypter.Execute();

        return builder;
    }

    public static IConfigurationBuilder DecryptJsonOnBuild(this IConfigurationBuilder builder, ConfigProtectorContract contract, List<IConfigProtector> protectors)
    {
        // 1. Build the configuration as it stands up to this exact point
        var intermediateConfig = builder.Build();

        var decryptor = new JsonDecryptor(contract, protectors);

        return builder.Add(new DecryptionConfigSource(intermediateConfig, decryptor));
    }
}
