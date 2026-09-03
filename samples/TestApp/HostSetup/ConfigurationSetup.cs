/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using Integration.DevKit.Core;
using Integration.DevKit.Core.Configuration;
using Microsoft.Extensions.Configuration;

namespace TestApp.HostSetup;

/// <summary>
/// Builds the app's <see cref="IConfiguration"/> through the encrypt/decrypt-on-build pipeline.
/// </summary>
public class ConfigurationSetup
{
    public IConfiguration BuildDecryptedConfiguration()
    {
        var cryptoContract = new ConfigProtectorContract('|')
        {
            Signature = "ENC",
            Version = "v1"
        };
        var base64Protector = new Base64ConfigProtector();
        var aesProtector = new AesConfigProtector("my-super-secret-32-byte-long-key!!", "1234567890123456");

        var configBuilder = new ConfigurationBuilder().AddJsonFile("appsettings.json", false);

        // Encrypts any newly-added plaintext values at these paths in appsettings.json, in place,
        // the first time this runs against values that aren't encrypted yet.
        configBuilder.EncryptJsonOnBuild(
            cryptoContract,
            (options) =>
            {
                options.Encrypt("Integration.DevKit:RnadomManagement");
                options.Encrypt("Integration.DevKit:SQLManagement:Clients:TestClient:ConnectionString", aesProtector);
            }
        ).Build();

        return configBuilder.DecryptJsonOnBuild(cryptoContract, new List<IConfigProtector> { base64Protector, aesProtector }).Build();
    }
}
