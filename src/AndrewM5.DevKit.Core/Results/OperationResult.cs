using AndrewM5.DevKit.Core.Abstractions;

namespace AndrewM5.DevKit.Core.Results;

public class OperationResult<T> : IOperationResult<T>
{
    private bool _methodSuccess = false;
    private T _result = default!;
    private Exception? _exception;

    public bool MethodSuccess => _methodSuccess;
    public T Result => _result;
    public Exception Exception { 
        get { 
            if (_exception == null)
            {
                _exception = new Exception("No Error");
            }

            return _exception;
        }
    } 
    

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

    public OperationResult<T> SetMethodFailure(Exception ex)
    {
        _methodSuccess = false;
        _result = default!;
        _exception = ex;

        return this;
    }

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
