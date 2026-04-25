namespace AndrewM5.DevKit.Core.Abstractions;

/// <summary>
/// Defines a standardized structure for the output of a method, 
/// encapsulating the success status, any resulting data, and error information.
/// </summary>
/// <typeparam name="T">The type of the result data to return within the operation.</typeparam>
public interface IOperationResult<T>
{
    /// <summary>
    /// Gets a value indicating whether the operation completed successfully.
    /// </summary>
    public bool MethodSuccess { get; }

    /// <summary>
    /// Gets the exception associated with a failed operation. If <see cref="MethodSuccess"/> is true, this will be populated with a "No Error" exception
    /// </summary>
    public Exception Exception { get; }

    /// <summary>
    /// Gets the data produced by the operation. 
    /// If <see cref="MethodSuccess"/> is false, this value may be the default for type <typeparamref name="T"/>.
    /// </summary>
    public T Result { get; }
}