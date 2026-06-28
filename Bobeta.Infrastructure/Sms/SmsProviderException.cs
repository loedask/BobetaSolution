using System.Net;

namespace Bobeta.Infrastructure.Sms;

/// <summary>Thrown when an SMS provider returns an error or the send fails.</summary>
public class SmsProviderException : InvalidOperationException
{
    public HttpStatusCode? StatusCode { get; }
    public string? ResponseBody { get; }

    public SmsProviderException(string message, HttpStatusCode? statusCode = null, string? responseBody = null)
        : base(message)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }

    public SmsProviderException(string message, Exception inner)
        : base(message, inner)
    {
        StatusCode = null;
        ResponseBody = null;
    }

    /// <summary>Whether another provider may succeed (e.g. skip fallback for invalid phone number).</summary>
    public bool AllowProviderFallback => StatusCode is not (HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity);
}

/// <summary>Backward-compatible alias for <see cref="SmsProviderException"/>.</summary>
public class SmsGatewayException : SmsProviderException
{
    public SmsGatewayException(string message, HttpStatusCode? statusCode = null, string? responseBody = null)
        : base(message, statusCode, responseBody)
    {
    }

    public SmsGatewayException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
