namespace Integration.DevKit.Core.Tests;

public class OperationResultTests
{
    [Fact]
    public void OperationResult_SetMethodSuccess_SetsResultAndSuccess()
    {
        var result = new OperationResult<string>().SetMethodSuccess("value");

        Assert.True(result.MethodSuccess);
        Assert.Equal("value", result.Result);
    }

    [Fact]
    public void OperationResult_SetMethodSuccess_WithNull_RedirectsToFailure()
    {
        var result = new OperationResult<string>().SetMethodSuccess(null!);

        Assert.False(result.MethodSuccess);
        Assert.IsType<ArgumentException>(result.Exception);
    }

    [Fact]
    public void OperationResult_SetMethodFailure_SetsExceptionAndDefault()
    {
        var ex = new InvalidOperationException("boom");
        var result = new OperationResult<string>().SetMethodFailure(ex, "fallback");

        Assert.False(result.MethodSuccess);
        Assert.Equal("fallback", result.Result);
        Assert.Same(ex, result.Exception);
    }

    [Fact]
    public void OperationResult_Exception_WhenNeverFailed_ReturnsNoErrorPlaceholder()
    {
        var result = new OperationResult<string>();

        Assert.Equal("No Error", result.Exception.Message);
    }

    [Fact]
    public void OperationResult_ToString_ReflectsSuccessAndFailure()
    {
        var success = new OperationResult<string>().SetMethodSuccess("ok");
        var failure = new OperationResult<string>().SetMethodFailure(new Exception("bad"));

        Assert.Contains("Success", success.ToString());
        Assert.Contains("bad", failure.ToString());
    }

    [Fact]
    public void NullOperationResult_SetMethodSuccess_IsSuccessWithNullResult()
    {
        var result = new NullOperationResult().SetMethodSuccess();

        Assert.True(result.MethodSuccess);
        Assert.Null(result.Result);
    }

    [Fact]
    public void NullOperationResult_SetMethodFailure_IsFailure()
    {
        var ex = new Exception("failed");
        var result = new NullOperationResult().SetMethodFailure(ex);

        Assert.False(result.MethodSuccess);
        Assert.Same(ex, result.Exception);
    }

    [Fact]
    public void NullableOperationResult_SetMethodSuccess_AllowsNullResult()
    {
        var result = new NullableOperationResult<string>().SetMethodSuccess(null);

        Assert.True(result.MethodSuccess);
        Assert.Null(result.Result);
    }

    [Fact]
    public void NullableOperationResult_SetMethodSuccess_WithValue_Succeeds()
    {
        var result = new NullableOperationResult<string>().SetMethodSuccess("hi");

        Assert.True(result.MethodSuccess);
        Assert.Equal("hi", result.Result);
    }

    [Fact]
    public void NullableOperationResult_SetMethodFailure_UsesDefaultValue()
    {
        var result = new NullableOperationResult<string>().SetMethodFailure(new Exception("x"), "fallback");

        Assert.False(result.MethodSuccess);
        Assert.Equal("fallback", result.Result);
    }
}
