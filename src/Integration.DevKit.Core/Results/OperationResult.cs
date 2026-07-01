/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

namespace Integration.DevKit.Core;

/// <summary>
/// A concrete implementation of <see cref="IOperationResult{T}"/> that does not allow a null result.
/// </summary>
/// <typeparam name="T">The type of the result data to return within the operation.</typeparam>
public class OperationResult<T> : IOperationResult<T>
{
    private bool _methodSuccess = false;
    private T _result = default!;
    private Exception? _exception;

    /// <inheritdoc />
    public bool MethodSuccess => _methodSuccess;

    /// <inheritdoc />
    public T Result => _result;

    /// <inheritdoc />
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
    /// <param name="result">The successful output of the operation. Must not be null.</param>
    /// <returns>The current <see cref="OperationResult{T}"/> instance for method chaining.</returns>
    /// <remarks>
    /// If <paramref name="result"/> is null, this method will automatically transition 
    /// to a failure state, as <see cref="OperationResult{T}"/> does not support null results.
    /// </remarks>
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
    /// <param name="defaultVal">Optional default value for <see cref="Result"/> on failure. Defaults to <typeparamref name="T"/> default.</param>
    /// <returns>The current <see cref="OperationResult{T}"/> instance for method chaining.</returns>
    public OperationResult<T> SetMethodFailure(Exception ex, T defaultVal = default!)
    {
        _methodSuccess = false;
        _result = defaultVal;
        _exception = ex;

        return this;
    }

    /// <summary>
    /// Returns a string representation of the operation result.
    /// </summary>
    /// <returns>
    /// A formatted string indicating "Success" with the result data, 
    /// or "Fail" with the exception message.
    /// </returns>
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
