using Integration.DevKit.Core.Configuration;
using Microsoft.Extensions.Configuration;

namespace Integration.DevKit.Core.Tests;

public class Base64ConfigProtectorTests
{
    [Fact]
    public void Encrypt_Decrypt_RoundTrips()
    {
        var protector = new Base64ConfigProtector();

        var cipher = protector.Encrypt("hello world");
        var plain = protector.Decrypt(cipher);

        Assert.Equal("hello world", plain);
        Assert.Equal("BASE64", protector.Name);
    }
}

public class AesConfigProtectorTests
{
    [Fact]
    public void Encrypt_Decrypt_RoundTrips_GivenFixedKeyAndIv()
    {
        var protector = new AesConfigProtector("my-secret-key", "my-iv-value");

        var cipher = protector.Encrypt("sensitive data");
        var plain = protector.Decrypt(cipher);

        Assert.Equal("sensitive data", plain);
        Assert.Equal("AES256", protector.Name);
    }

    [Fact]
    public void Encrypt_EmptyString_ReturnsEmptyString()
    {
        var protector = new AesConfigProtector("key", "iv");

        Assert.Equal(string.Empty, protector.Encrypt(string.Empty));
    }

    [Fact]
    public void Constructor_NullOrWhitespaceArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new AesConfigProtector("", "iv"));
        Assert.Throws<ArgumentNullException>(() => new AesConfigProtector("key", " "));
    }

    [Fact]
    public void TwoInstances_WithSameKeyAndIv_ProduceInteroperableCiphertext()
    {
        var a = new AesConfigProtector("key", "iv");
        var b = new AesConfigProtector("key", "iv");

        var cipher = a.Encrypt("data");

        Assert.Equal("data", b.Decrypt(cipher));
    }
}

public class ConfigProtectorContractTests
{
    [Fact]
    public void BuildPrefix_CombinesSignatureVersionAndProviderName()
    {
        var contract = new ConfigProtectorContract();
        var protector = new Base64ConfigProtector();

        var prefix = contract.BuildPrefix(protector);

        Assert.Equal("ENCv1:BASE64:", prefix);
    }

    [Fact]
    public void Signature_ContainingDelimiter_Throws()
    {
        Assert.Throws<ArgumentException>(() => new ConfigProtectorContract { Signature = "EN:C" });
    }

    [Fact]
    public void Version_ContainingDelimiter_Throws()
    {
        Assert.Throws<ArgumentException>(() => new ConfigProtectorContract { Version = "v:1" });
    }

    [Fact]
    public void CustomDelimiter_IsRespected()
    {
        var contract = new ConfigProtectorContract('|');
        var protector = new Base64ConfigProtector();

        Assert.Equal("ENCv1|BASE64|", contract.BuildPrefix(protector));
    }
}

public class EncryptionOptionsTests
{
    [Fact]
    public void Encrypt_NoProviderGiven_DefaultsToBase64()
    {
        var options = new EncryptionOptions();

        options.Encrypt("Some.Path");

        Assert.IsType<Base64ConfigProtector>(options.TargetPaths["Some.Path"]);
    }

    [Fact]
    public void Encrypt_ExplicitProvider_IsUsed()
    {
        var options = new EncryptionOptions();
        var protector = new AesConfigProtector("key", "iv");

        options.Encrypt("Some.Path", protector);

        Assert.Same(protector, options.TargetPaths["Some.Path"]);
    }
}

public class JsonDecryptorTests
{
    private static readonly ConfigProtectorContract Contract = new();

    private class FakeProtector : IConfigProtector
    {
        public string Name { get; init; } = "FAKE";
        public string Encrypt(string plainText) => $"enc({plainText})";
        public string Decrypt(string cipherText) => $"dec({cipherText})";
    }

    [Fact]
    public void Decrypt_ValueWithoutSignature_ReturnsUnchanged()
    {
        var decryptor = new JsonDecryptor(Contract, new List<IConfigProtector> { new FakeProtector() });

        Assert.Equal("plain-value", decryptor.Decrypt("plain-value"));
    }

    [Fact]
    public void Decrypt_ValidEncryptedValue_DelegatesToRegisteredProvider()
    {
        var decryptor = new JsonDecryptor(Contract, new List<IConfigProtector> { new FakeProtector() });

        var result = decryptor.Decrypt("ENCv1:FAKE:payload");

        Assert.Equal("dec(payload)", result);
    }

    [Fact]
    public void Decrypt_MalformedValue_ThrowsFormatException()
    {
        var decryptor = new JsonDecryptor(Contract, new List<IConfigProtector> { new FakeProtector() });

        Assert.Throws<FormatException>(() => decryptor.Decrypt("ENCv1:onlytwoparts"));
    }

    [Fact]
    public void Decrypt_UnregisteredProvider_ThrowsKeyNotFoundException()
    {
        var decryptor = new JsonDecryptor(Contract, new List<IConfigProtector> { new FakeProtector() });

        Assert.Throws<KeyNotFoundException>(() => decryptor.Decrypt("ENCv1:UNKNOWN:payload"));
    }

    [Fact]
    public void Constructor_DuplicateProviderNames_Throws()
    {
        var providers = new List<IConfigProtector> { new FakeProtector(), new FakeProtector() };

        Assert.ThrowsAny<Exception>(() => new JsonDecryptor(Contract, providers));
    }

    [Fact]
    public void Constructor_NullArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new JsonDecryptor(null!, new List<IConfigProtector>()));
        Assert.Throws<ArgumentNullException>(() => new JsonDecryptor(Contract, null!));
    }
}

public class DecryptionConfigProviderTests
{
    private class FakeProtector : IConfigProtector
    {
        public string Name => "FAKE";
        public string Encrypt(string plainText) => $"enc({plainText})";
        public string Decrypt(string cipherText) => $"dec({cipherText})";
    }

    [Fact]
    public void Load_DecryptsEncryptedLeafValues_AndLeavesPlainValuesUnchanged()
    {
        var contract = new ConfigProtectorContract();
        var decryptor = new JsonDecryptor(contract, new List<IConfigProtector> { new FakeProtector() });

        var intermediate = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Plain"] = "hello",
                ["Secret"] = "ENCv1:FAKE:payload",
                ["Nested:Value"] = "ENCv1:FAKE:nested-payload"
            })
            .Build();

        var config = new ConfigurationBuilder()
            .Add(new DecryptionConfigSource(intermediate, decryptor))
            .Build();

        Assert.Equal("hello", config["Plain"]);
        Assert.Equal("dec(payload)", config["Secret"]);
        Assert.Equal("dec(nested-payload)", config["Nested:Value"]);
    }
}
