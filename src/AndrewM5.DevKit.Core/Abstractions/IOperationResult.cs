namespace AndrewM5.DevKit.Core.Abstractions;

public interface IOperationResult<T>
{
    public bool MethodSuccess { get; }
    public Exception Exception { get; }
    public T Result { get; }
}