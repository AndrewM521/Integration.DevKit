using AndrewM5.DevKit.Core.Abstractions;

namespace AndrewM5.DevKit.Core.Results;

/// <summary>
/// A concrete implementation of <see cref="IOperationResult{T}"/> that enforces 
/// a non-null result upon success.
/// </summary>
/// <typeparam name="T">The type of the result data.</typeparam>
public class OperationResult<T> : IOperationResult<T>
{
    private bool _methodSuccess = false;
    private T _result = default!;
    private Exception? _exception;

    /// <inheritdoc />
    public bool MethodSuccess => _methodSuccess;

    /// <inheritdoc />
    public T Result => _result;

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
    /// Sets the state of the result to success and assigns the provided result data.
    /// </summary>
    /// <remarks>
    /// If <paramref name="result"/> is null, this method will automatically transition 
    /// to a failure state, as <see cref="OperationResult{T}"/> does not support null results.
    /// </remarks>
    /// <param name="result">The successful output of the operation.</param>
    /// <returns>The current <see cref="OperationResult{T}"/> instance for method chaining.</returns>
    public OperationResult<T> SetMethodSuccess(T result)
    {
        if (result == null)
        {
            return SetMethodFailure(new ArgumentException($"Result cannot be null for {nameof(OperationResult<T>)}. Use {nameof(NullableOperationResult<T>)} or {nameof(NullOperationResult)} instead"));
        }

        _methodSuccess = true;
        _result = result;
        _exception = null;

        return this;
    }

    /// <summary>
    /// Sets the state of the result to failure and assigns the provided exception.
    /// </summary>
    /// <param name="ex">The exception that caused the operation to fail.</param>
    /// <returns>The current <see cref="OperationResult{T}"/> instance for method chaining.</returns>
    public OperationResult<T> SetMethodFailure(Exception ex)
    {
        _methodSuccess = false;
        _result = default!;
        _exception = ex;

        return this;
    }

    /// <summary>
    /// Returns a string representation of the operation result, 
    /// including success status and either the result data or the error message.
    /// </summary>
    /// <returns>A formatted string describing the outcome.</returns>
    public override string ToString()
    {
        string retVal = string.Empty;

        if (MethodSuccess == true)
        {
            retVal = $"Result: Success, {Result!.ToString()!}";
        }
        else
        {
            retVal = $"Result: Fail, {Exception.Message}";
        }

        return retVal;
    }
}
