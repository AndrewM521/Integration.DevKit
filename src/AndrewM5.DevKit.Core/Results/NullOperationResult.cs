namespace AndrewM5.DevKit.Core.Results;

/// <summary>
/// A specialized <see cref="NullableOperationResult{T}"/> for operations that do not 
/// return a value. This effectively acts as a "Void" operation result.
/// </summary>
public class NullOperationResult : NullableOperationResult<object?>
{
    /// <summary>
    /// Sets the state of the result to success with a null value.
    /// </summary>
    /// <returns>The current <see cref="NullOperationResult"/> instance for method chaining.</returns>
    public NullOperationResult SetMethodSuccess()
    {
        base.SetMethodSuccess(null);   
        return this;
    }

    /// <summary>
    /// Sets the state of the result to failure and assigns the provided exception.
    /// </summary>
    /// <param name="ex">The exception that caused the operation to fail.</param>
    /// <returns>The current <see cref="NullOperationResult"/> instance for method chaining.</returns>
    public new NullOperationResult SetMethodFailure(Exception ex)
    {
        base.SetMethodFailure(ex);
        return this;
    }

    /// <summary>
    /// Returns a string representation of the null operation result.
    /// </summary>
    /// <returns>A formatted string describing the outcome.</returns>
    public override string ToString()
    {
        return MethodSuccess ? "Result: Success, null" : $"Result: Fail, {Exception.Message}";
    }
}
