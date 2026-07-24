namespace Riaya.Api.Common;

public class ServiceResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? ErrorCode { get; init; }
    public ErrorType ErrorType { get; init; } = ErrorType.None;

    public static ServiceResult Ok(string message = "Request completed successfully.")
    {
        return new ServiceResult
        {
            Success = true,
            Message = message
        };
    }

    public static ServiceResult Fail(
        string message,
        ErrorType errorType = ErrorType.Validation,
        string? errorCode = null)
    {
        return new ServiceResult
        {
            Success = false,
            Message = message,
            ErrorType = errorType,
            ErrorCode = errorCode
        };
    }
}

public sealed class ServiceResult<T> : ServiceResult
{
    public T? Data { get; init; }

    public static ServiceResult<T> Ok(
        T? data,
        string message = "Request completed successfully.")
    {
        return new ServiceResult<T>
        {
            Success = true,
            Data = data,
            Message = message
        };
    }

    public static new ServiceResult<T> Fail(
        string message,
        ErrorType errorType = ErrorType.Validation,
        string? errorCode = null)
    {
        return new ServiceResult<T>
        {
            Success = false,
            Message = message,
            ErrorType = errorType,
            ErrorCode = errorCode
        };
    }
}
