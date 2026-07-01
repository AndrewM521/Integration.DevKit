using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.DevKit.Core.Configuration;

public static class CryptoExtension
{
    public static IConfigurationBuilder EncryptJsonOnBuild(this IConfigurationBuilder builder, CryptoContract contract, Action<EncryptionOptions> configure)
    {
        var options = new EncryptionOptions();
        configure(options);

        var encrypter = new JsonEncryptor(contract, options);
        encrypter.Execute();

        return builder;
    }

    //public static IConfigurationBuilder DecryptJsonOnBuild(this IConfigurationBuilder builder, CryptoContract contract, IEnumerable<IStringCryptoProvider> providers)
    //{
    //    // 1. Build the configuration as it stands up to this exact point
    //    var intermediateConfig = builder.Build();

    //    // 2. Instantiate your self-validating dynamic decryptor
    //    var decryptor = new JsonDecryptor(providers, contract);

    //    // 3. Inject our custom decryption provider layer over top of it
    //    return builder.Add(new CryptoConfigurationSource(intermediateConfig, decryptor));
    //}
}
