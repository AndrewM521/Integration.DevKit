using Integration.DevKit.CredentialMgmt.Implementations;
using Integration.DevKit.ThreadLocks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Integration.DevKit.CredentialMgmt.Tests;

public class Service_CredentialMgmtTests : IDisposable
{
    private readonly string _tempDir;

    public Service_CredentialMgmtTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "devkit-svc-credmgmt-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    private static IConfiguration BuildConfig(string provider) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Integration.DevKit:CredentialManagement:Provider"] = provider
            })
            .Build();

    [Fact]
    public void AddCredentialMgmt_BuiltInFileProvider_RegistersWorkingFileSecretStore()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Integration.DevKit:CredentialManagement:Provider"] = "File",
                ["Integration.DevKit:CredentialManagement:Options:ApplicationName"] = "TestApp",
                ["Integration.DevKit:CredentialManagement:Options:SecretsFolder"] = Path.Combine(_tempDir, "secrets"),
                ["Integration.DevKit:CredentialManagement:Options:KeysFolder"] = Path.Combine(_tempDir, "keys")
            })
            .Build();

        var services = new ServiceCollection();
        services.AddCredentialMgmt(config);
        var provider = services.BuildServiceProvider();

        var store = provider.GetRequiredService<FileSecretStore>();

        Assert.Equal("TestApp", store.StoreName);
    }

    [Fact]
    public void AddCredentialMgmt_BuiltInFileProvider_AlsoRegistersThreadLockManager()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Integration.DevKit:CredentialManagement:Provider"] = "File",
                ["Integration.DevKit:CredentialManagement:Options:ApplicationName"] = "TestApp",
                ["Integration.DevKit:CredentialManagement:Options:SecretsFolder"] = Path.Combine(_tempDir, "secrets"),
                ["Integration.DevKit:CredentialManagement:Options:KeysFolder"] = Path.Combine(_tempDir, "keys")
            })
            .Build();

        var services = new ServiceCollection();
        services.AddCredentialMgmt(config);
        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<ThreadLockManager>());
    }

    [Fact]
    public void AddCredentialMgmt_BuiltInFileProvider_MissingRequiredOption_Throws()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Integration.DevKit:CredentialManagement:Provider"] = "File",
                ["Integration.DevKit:CredentialManagement:Options:ApplicationName"] = "TestApp"
                // SecretsFolder/KeysFolder intentionally omitted
            })
            .Build();

        var services = new ServiceCollection();

        var ex = Assert.Throws<InvalidOperationException>(() => services.AddCredentialMgmt(config));

        Assert.Contains("SecretsFolder", ex.Message);
        Assert.Contains("File", ex.Message);
    }

    [Fact]
    public void RegisterProvider_ThenSelectItViaConfig_InvokesRegisteredDelegate()
    {
        var providerName = "Custom-" + Guid.NewGuid();
        var invoked = false;

        Service_CredentialMgmt.RegisterProvider(providerName, (services, options, enableLogging, configuration) =>
        {
            invoked = true;
            services.AddSingleton(new object());
        });

        var config = BuildConfig(providerName);
        var services = new ServiceCollection();

        services.AddCredentialMgmt(config);

        Assert.True(invoked);
    }

    [Fact]
    public void RegisterProvider_CustomProvider_ResolvesRegisteredService()
    {
        var providerName = "Custom-" + Guid.NewGuid();
        var marker = new object();

        Service_CredentialMgmt.RegisterProvider(providerName, (services, options, enableLogging, configuration) =>
        {
            services.AddSingleton(marker);
        });

        var services = new ServiceCollection();
        services.AddCredentialMgmt(BuildConfig(providerName));
        var provider = services.BuildServiceProvider();

        Assert.Same(marker, provider.GetRequiredService<object>());
    }

    [Fact]
    public void RegisterProvider_CustomProvider_ReceivesBoundOptionsValues()
    {
        var providerName = "Custom-" + Guid.NewGuid();
        Dictionary<string, object>? received = null;

        Service_CredentialMgmt.RegisterProvider(providerName, (services, options, enableLogging, configuration) =>
        {
            received = options;
        });

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Integration.DevKit:CredentialManagement:Provider"] = providerName,
                ["Integration.DevKit:CredentialManagement:Options:Region"] = "us-east-1",
                ["Integration.DevKit:CredentialManagement:Options:SecretPrefix"] = "myapp/"
            })
            .Build();

        new ServiceCollection().AddCredentialMgmt(config);

        Assert.NotNull(received);
        Assert.Equal("us-east-1", received!.GetRequiredOption<string>("Region", providerName));
        Assert.Equal("myapp/", received!.GetRequiredOption<string>("SecretPrefix", providerName));
    }

    [Fact]
    public void AddCredentialMgmt_UnregisteredProvider_ThrowsWithHelpfulMessage()
    {
        var unknownProvider = "Unknown-" + Guid.NewGuid();
        var services = new ServiceCollection();

        var ex = Assert.Throws<NotSupportedException>(() => services.AddCredentialMgmt(BuildConfig(unknownProvider)));

        Assert.Contains(unknownProvider, ex.Message);
        Assert.Contains("File", ex.Message);
        Assert.Contains(nameof(Service_CredentialMgmt.RegisterProvider), ex.Message);
    }

    [Fact]
    public void RegisterProvider_SameNameTwice_SecondRegistrationWins()
    {
        var providerName = "Custom-" + Guid.NewGuid();

        Service_CredentialMgmt.RegisterProvider(providerName, (services, options, enableLogging, configuration) => services.AddSingleton("first"));
        Service_CredentialMgmt.RegisterProvider(providerName, (services, options, enableLogging, configuration) => services.AddSingleton("second"));

        var services = new ServiceCollection();
        services.AddCredentialMgmt(BuildConfig(providerName));
        var provider = services.BuildServiceProvider();

        Assert.Equal("second", provider.GetRequiredService<string>());
    }

    [Fact]
    public void RegisterProvider_NullOrWhitespaceName_Throws()
    {
        Assert.Throws<ArgumentException>(() => Service_CredentialMgmt.RegisterProvider("", (s, o, e, c) => { }));
        Assert.Throws<ArgumentException>(() => Service_CredentialMgmt.RegisterProvider("   ", (s, o, e, c) => { }));
        Assert.Throws<ArgumentException>(() => Service_CredentialMgmt.RegisterProvider(null!, (s, o, e, c) => { }));
    }

    [Fact]
    public void RegisterProvider_NullDelegate_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => Service_CredentialMgmt.RegisterProvider("Custom-" + Guid.NewGuid(), null!));
    }
}

public class CredentialManagementOptionsExtensionsTests
{
    [Fact]
    public void GetRequiredOption_PresentValueOfExactType_ReturnsIt()
    {
        var options = new Dictionary<string, object> { ["Region"] = "us-east-1" };

        var value = options.GetRequiredOption<string>("Region", "Aws");

        Assert.Equal("us-east-1", value);
    }

    [Fact]
    public void GetRequiredOption_ConvertibleType_ConvertsValue()
    {
        var options = new Dictionary<string, object> { ["MaxRetries"] = "3" };

        var value = options.GetRequiredOption<int>("MaxRetries", "Aws");

        Assert.Equal(3, value);
    }

    [Fact]
    public void GetRequiredOption_MissingKey_ThrowsWithKeyAndProviderName()
    {
        var options = new Dictionary<string, object>();

        var ex = Assert.Throws<InvalidOperationException>(() => options.GetRequiredOption<string>("Region", "Aws"));

        Assert.Contains("Region", ex.Message);
        Assert.Contains("Aws", ex.Message);
    }

    [Fact]
    public void GetRequiredOption_ValueNotConvertible_ThrowsWithKeyAndProviderName()
    {
        var options = new Dictionary<string, object> { ["MaxRetries"] = "not-a-number" };

        var ex = Assert.Throws<InvalidOperationException>(() => options.GetRequiredOption<int>("MaxRetries", "Aws"));

        Assert.Contains("MaxRetries", ex.Message);
        Assert.Contains("Aws", ex.Message);
    }
}
