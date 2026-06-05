/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

namespace Integration.DevKit.Core;

/// <summary>
/// A specialized <see cref="NullableOperationResult{T}"/> for operations that do not 
/// return a value. This effectively acts as a "Void" operation result wrapper.
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
    /// Returns a string representation of the operation result.
    /// </summary>
    /// <returns>
    /// A string indicating "Success" or "Fail" along with the exception message if the operation failed.
    /// </returns>
    public override string ToString()
    {
        return MethodSuccess ? "Result: Success, null" : $"Result: Fail, {Exception.Message}";
    }
}
