using Integration.DevKit.CredentialMgmt.Contracts;
using Integration.DevKit.CredentialMgmt.Implementations;
using Integration.DevKit.CredentialMgmt.Tests.TestSupport;
using Moq;

namespace Integration.DevKit.CredentialMgmt.Tests;

public class CompositeSecretReaderTests
{
    [Fact]
    public void GetKey_FirstReaderSucceeds_ReturnsItsValue()
    {
        var first = new FakeSecretReader().With("file", "key", "from-first");
        var second = new FakeSecretReader().With("file", "key", "from-second");

        var composite = new CompositeSecretReader(new List<ISecretReader> { first, second });

        var result = composite.GetKey("file", "key");

        Assert.True(result.MethodSuccess);
        Assert.Equal("from-first", result.Result);
    }

    [Fact]
    public void GetKey_FirstReaderFails_FallsBackToSecond()
    {
        var first = new FakeSecretReader();
        var second = new FakeSecretReader().With("file", "key", "from-second");

        var composite = new CompositeSecretReader(new List<ISecretReader> { first, second });

        var result = composite.GetKey("file", "key");

        Assert.True(result.MethodSuccess);
        Assert.Equal("from-second", result.Result);
    }

    [Fact]
    public void GetKey_AllReadersFail_ReturnsKeyNotFoundException()
    {
        var composite = new CompositeSecretReader(new List<ISecretReader> { new FakeSecretReader(), new FakeSecretReader() });

        var result = composite.GetKey("file", "missing");

        Assert.False(result.MethodSuccess);
        Assert.IsType<KeyNotFoundException>(result.Exception);
    }

    [Fact]
    public void GetKey_StopsAtFirstSuccess_DoesNotCallLaterReaders()
    {
        var first = new FakeSecretReader().With("file", "key", "value");
        var secondMock = new Mock<ISecretReader>();

        var composite = new CompositeSecretReader(new List<ISecretReader> { first, secondMock.Object });

        composite.GetKey("file", "key");

        secondMock.Verify(r => r.GetKey(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void Constructor_NullReaders_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new CompositeSecretReader(null!));
    }

    [Fact]
    public void StoreName_DefaultsToCompositeSecretReader()
    {
        var composite = new CompositeSecretReader(new List<ISecretReader>());

        Assert.Equal("CompositeSecretReader", composite.StoreName);
    }

    [Fact]
    public void StoreName_CustomValue_IsUsed()
    {
        var composite = new CompositeSecretReader(new List<ISecretReader>(), "MyReader");

        Assert.Equal("MyReader", composite.StoreName);
    }
}
