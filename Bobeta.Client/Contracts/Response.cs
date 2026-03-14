namespace Bobeta.Client.Contracts;

/// <summary>Standard wrapper for API responses (success/failure with optional data and error details).</summary>
public class Response<T>
{
    public bool IsSuccess { get; init; }
    public T? Data { get; init; }
    public string? ErrorMessage { get; init; }
    public int? StatusCode { get; init; }

    public static Response<T> Success(T data) => new()
    {
        IsSuccess = true,
        Data = data
    };

    public static Response<T> Failure(string message, int? statusCode = null) => new()
    {
        IsSuccess = false,
        ErrorMessage = message,
        StatusCode = statusCode
    };
}
