namespace AndrewM5.DevKit.Core.Results;

public class NullOperationResult : NullableOperationResult<object?>
{
    public NullOperationResult SetMethodSuccess()
    {
        base.SetMethodSuccess(null);   
        return this;
    }

    public new NullOperationResult SetMethodFailure(Exception ex)
    {
        base.SetMethodFailure(ex);
        return this;
    }

    public override string ToString()
    {
        return MethodSuccess ? "Result: Success, null" : $"Result: Fail, {Exception.Message}";
    }
}
