using AndrewM5.DevKit.Core.Abstractions;

namespace AndrewM5.DevKit.Core.Results;

public class NullableOperationResult<T> : IOperationResult<T?>
{
    private bool _methodSuccess = false;
    private T? _result;
    private Exception? _exception;

    public bool MethodSuccess => _methodSuccess;
    public T? Result => _result;
    public Exception Exception { 
        get { 
            if (_exception == null)
            {
                _exception = new Exception("No Error");
            }

            return _exception;
        }
    } 
    
    public NullableOperationResult<T?> SetMethodSuccess(T? result)
    {
        _methodSuccess = true;
        _result = result;
        _exception = null;

        return this;
    }

    public NullableOperationResult<T?> SetMethodFailure(Exception ex)
    {
        _methodSuccess = false;
        _result = default;
        _exception = ex;

        return this;
    }

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
