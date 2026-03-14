namespace Bobeta.Client.Contracts;

/// <summary>Exception thrown when an API request fails (e.g. non-success status code).</summary>
public class ApiException(string message, int? statusCode = null, string? responseBody = null, Exception? inner = null) : Exception(message, inner)
{
    public int? StatusCode { get; } = statusCode;
    public string? ResponseBody { get; } = responseBody;
}
