using AndrewM5.DevKit.Core.Abstractions;

namespace AndrewM5.DevKit.Core.Results;

/// <summary>
/// A concrete implementation of <see cref="IOperationResult{T}"/> that allows 
/// for a null result even when the operation is marked as successful.
/// </summary>
/// <typeparam name="T">The type of the result data.</typeparam>
public class NullableOperationResult<T> : IOperationResult<T?>
{
    private bool _methodSuccess = false;
    private T? _result;
    private Exception? _exception;

    /// <inheritdoc />
    public bool MethodSuccess => _methodSuccess;

    /// <inheritdoc />
    public T? Result => _result;

    /// <summary>
    /// Gets the exception associated with the failure. 
    /// If no exception was provided, returns a default "No Error" exception.
    /// </summary>
    public Exception Exception { 
        get { 
            if (_exception == null)
            {
                _exception = new Exception("No Error");
            }

            return _exception;
        }
    }

    /// <summary>
    /// Sets the state of the result to success and assigns the provided result data, 
    /// which may be null.
    /// </summary>
    /// <param name="result">The output of the operation (can be null).</param>
    /// <returns>The current <see cref="NullableOperationResult{T}"/> instance for method chaining.</returns>
    public NullableOperationResult<T?> SetMethodSuccess(T? result)
    {
        _methodSuccess = true;
        _result = result;
        _exception = null;

        return this;
    }

    /// <summary>
    /// Sets the state of the result to failure and assigns the provided exception.
    /// </summary>
    /// <param name="ex">The exception that caused the operation to fail.</param>
    /// <returns>The current <see cref="NullableOperationResult{T}"/> instance for method chaining.</returns>
    public NullableOperationResult<T?> SetMethodFailure(Exception ex)
    {
        _methodSuccess = false;
        _result = default;
        _exception = ex;

        return this;
    }

    /// <summary>
    /// Returns a string representation of the operation result.
    /// </summary>
    /// <returns>A formatted string describing the outcome.</returns>
    public override string ToString()
    {
        string retVal = string.Empty;

        if (MethodSuccess == true)
        {
            string resultStr = "null";

            if (Result != null)
            {
                resultStr = Result.ToString()!;
            }

            retVal = $"Result: Success, {resultStr}";
        }
        else
        {
            retVal = $"Result: Fail, {Exception.Message}";
        }

        return retVal;
    }
}
