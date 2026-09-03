using Integration.DevKit.Core;
using Integration.DevKit.CredentialMgmt.Contracts;
using Integration.DevKit.SQLMgmt.Contracts;
using Moq;

namespace Integration.DevKit.SQLMgmt.Tests;

public class SQLClientTests
{
    private static SQLClient CreateClient(string name = "TestClient") =>
        new(name, new SQLClientSettings { ConnectionString = "Server=default;" });

    [Fact]
    public void SetSecretStoreCredentials_WithoutSecretStore_Fails()
    {
        var client = CreateClient();

        var result = client.SetSecretStoreCredentials("Server=whatever;");

        Assert.False(result.MethodSuccess);
        Assert.IsType<ArgumentNullException>(result.Exception);
    }

    [Fact]
    public void SetSecretStoreCredentials_WithSecretStore_DelegatesToStore()
    {
        var client = CreateClient();
        var store = new Mock<ISecretStore>();
        store.Setup(s => s.SetKey(It.IsAny<string>(), "ConnectionString", "Server=from-store;"))
            .Returns(new NullOperationResult().SetMethodSuccess());

        client.SetSecretStore(store.Object);
        var result = client.SetSecretStoreCredentials("Server=from-store;");

        Assert.True(result.MethodSuccess);
        store.Verify(s => s.SetKey(It.IsAny<string>(), "ConnectionString", "Server=from-store;"), Times.Once);
    }

    [Fact]
    public void DeleteCredential_WithoutSecretStore_Fails()
    {
        var client = CreateClient();

        var result = client.DeleteCredential("ConnectionString");

        Assert.False(result.MethodSuccess);
        Assert.IsType<ArgumentNullException>(result.Exception);
    }

    [Fact]
    public void DeleteCredential_WithSecretStore_DelegatesToStore()
    {
        var client = CreateClient();
        var store = new Mock<ISecretStore>();
        store.Setup(s => s.DeleteKey(It.IsAny<string>(), "ConnectionString"))
            .Returns(new NullOperationResult().SetMethodSuccess());

        client.SetSecretStore(store.Object);
        var result = client.DeleteCredential("ConnectionString");

        Assert.True(result.MethodSuccess);
        store.Verify(s => s.DeleteKey(It.IsAny<string>(), "ConnectionString"), Times.Once);
    }

    [Fact]
    public void DeleteAllCredentials_WithoutSecretStore_Fails()
    {
        var client = CreateClient();

        var result = client.DeleteAllCredentials();

        Assert.False(result.MethodSuccess);
        Assert.IsType<ArgumentNullException>(result.Exception);
    }

    [Fact]
    public void DeleteAllCredentials_WithSecretStore_DelegatesToStore()
    {
        var client = CreateClient();
        var store = new Mock<ISecretStore>();
        store.Setup(s => s.DeleteSecret(It.IsAny<string>()))
            .Returns(new NullOperationResult().SetMethodSuccess());

        client.SetSecretStore(store.Object);
        var result = client.DeleteAllCredentials();

        Assert.True(result.MethodSuccess);
        store.Verify(s => s.DeleteSecret(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void ClientName_MatchesConstructorArgument()
    {
        var client = CreateClient("MyClient");

        Assert.Equal("MyClient", client.ClientName);
    }

    [Fact]
    public void RuntimeSettings_MatchesConstructorArgument()
    {
        var settings = new SQLClientSettings { ConnectionString = "Server=x;", UseSingleConnection = true };
        var client = new SQLClient("named", settings);

        Assert.Same(settings, client.RuntimeSettings);
    }

    [Fact]
    public void Dispose_DoesNotThrow_WhenNoConnectionWasEverOpened()
    {
        var client = CreateClient();

        client.Dispose();
    }
}
