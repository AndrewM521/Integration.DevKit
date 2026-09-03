using Integration.DevKit.Core;
using Integration.DevKit.CredentialMgmt.Contracts;
using Moq;

namespace Integration.DevKit.CredentialMgmt.Tests;

public class SecretStoreExtensionsTests
{
    [Fact]
    public void ImportFrom_ReadsFromSourceAndWritesToTarget()
    {
        var source = new Mock<ISecretReader>();
        source.Setup(s => s.GetKey("file", "key")).Returns(new OperationResult<string>().SetMethodSuccess("plaintext"));

        var target = new Mock<ISecretStore>();
        target.Setup(s => s.SetKey("file", "key", "plaintext")).Returns(new NullOperationResult().SetMethodSuccess());

        var result = target.Object.ImportFrom(source.Object, "file", "key");

        Assert.True(result.MethodSuccess);
        target.Verify(s => s.SetKey("file", "key", "plaintext"), Times.Once);
    }

    [Fact]
    public void ImportFrom_SourceReadFails_PropagatesFailureWithoutWriting()
    {
        var source = new Mock<ISecretReader>();
        source.Setup(s => s.GetKey("file", "key"))
            .Returns(new OperationResult<string>().SetMethodFailure(new KeyNotFoundException("nope")));

        var target = new Mock<ISecretStore>();

        var result = target.Object.ImportFrom(source.Object, "file", "key");

        Assert.False(result.MethodSuccess);
        Assert.IsType<KeyNotFoundException>(result.Exception);
        target.Verify(s => s.SetKey(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void ImportFrom_TargetWriteFails_PropagatesFailure()
    {
        var source = new Mock<ISecretReader>();
        source.Setup(s => s.GetKey("file", "key")).Returns(new OperationResult<string>().SetMethodSuccess("plaintext"));

        var target = new Mock<ISecretStore>();
        target.Setup(s => s.SetKey("file", "key", "plaintext"))
            .Returns(new NullOperationResult().SetMethodFailure(new IOException("disk full")));

        var result = target.Object.ImportFrom(source.Object, "file", "key");

        Assert.False(result.MethodSuccess);
        Assert.IsType<IOException>(result.Exception);
    }

    [Fact]
    public void ImportFrom_NullTargetOrSource_Fails()
    {
        var target = new Mock<ISecretStore>();
        var source = new Mock<ISecretReader>();

        var nullTargetResult = ((ISecretStore)null!).ImportFrom(source.Object, "file", "key");
        var nullSourceResult = target.Object.ImportFrom(null!, "file", "key");

        Assert.False(nullTargetResult.MethodSuccess);
        Assert.IsType<ArgumentNullException>(nullTargetResult.Exception);
        Assert.False(nullSourceResult.MethodSuccess);
        Assert.IsType<ArgumentNullException>(nullSourceResult.Exception);
    }
}
