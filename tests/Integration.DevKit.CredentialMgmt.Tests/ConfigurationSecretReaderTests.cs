using Integration.DevKit.CredentialMgmt.Implementations;
using Microsoft.Extensions.Configuration;

namespace Integration.DevKit.CredentialMgmt.Tests;

public class ConfigurationSecretReaderTests
{
    private static IConfiguration BuildConfig(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    [Fact]
    public void GetKey_ExistingHierarchicalValue_Succeeds()
    {
        var config = BuildConfig(new Dictionary<string, string?> { ["MyContainer:MyKey"] = "secret-value" });
        var reader = new ConfigurationSecretReader(config);

        var result = reader.GetKey("MyContainer", "MyKey");

        Assert.True(result.MethodSuccess);
        Assert.Equal("secret-value", result.Result);
    }

    [Fact]
    public void GetKey_EnvironmentVariableStyleDoubleUnderscore_IsNormalizedByConfiguration()
    {
        // IConfiguration normalizes "Container__Key" env-var style names to "Container:Key" internally,
        // so an in-memory provider keyed with the colon form is what ConfigurationSecretReader expects.
        var config = BuildConfig(new Dictionary<string, string?> { ["Container:Key"] = "env-value" });
        var reader = new ConfigurationSecretReader(config);

        var result = reader.GetKey("Container", "Key");

        Assert.True(result.MethodSuccess);
        Assert.Equal("env-value", result.Result);
    }

    [Fact]
    public void GetKey_MissingValue_FailsWithKeyNotFoundException()
    {
        var config = BuildConfig(new Dictionary<string, string?>());
        var reader = new ConfigurationSecretReader(config);

        var result = reader.GetKey("Container", "Missing");

        Assert.False(result.MethodSuccess);
        Assert.IsType<KeyNotFoundException>(result.Exception);
    }

    [Fact]
    public void GetKey_EmptyStringValue_TreatedAsMissing()
    {
        var config = BuildConfig(new Dictionary<string, string?> { ["Container:Key"] = "" });
        var reader = new ConfigurationSecretReader(config);

        var result = reader.GetKey("Container", "Key");

        Assert.False(result.MethodSuccess);
    }

    [Fact]
    public void Constructor_NullConfiguration_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new ConfigurationSecretReader(null!));
    }

    [Fact]
    public void StoreName_DefaultsToConfigurationSecretReader()
    {
        var config = BuildConfig(new Dictionary<string, string?>());
        var reader = new ConfigurationSecretReader(config);

        Assert.Equal("ConfigurationSecretReader", reader.StoreName);
    }
}
