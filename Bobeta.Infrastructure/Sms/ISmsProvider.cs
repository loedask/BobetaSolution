using Bobeta.Domain.Enums;

namespace Bobeta.Infrastructure.Sms;

/// <summary>Pluggable SMS gateway (SendSMSGate, SMSPortal, etc.).</summary>
public interface ISmsProvider
{
    /// <summary>Provider identifier matching <see cref="SmsProviderNames"/>.</summary>
    string ProviderName { get; }

    /// <summary>True when required credentials are present in configuration.</summary>
    bool IsConfigured { get; }

    /// <summary>Sends one SMS via this provider.</summary>
    Task<SmsSendResult> SendAsync(SmsSendContext context, CancellationToken cancellationToken = default);

    /// <summary>Maps a provider-specific delivery status string to the platform status, if recognized.</summary>
    SmsMessageStatus? MapDeliveryStatus(string? status);
}

/// <summary>Input for a single outbound SMS attempt.</summary>
public sealed class SmsSendContext
{
    public required Guid RecordId { get; init; }
    public required string NormalizedPhone { get; init; }
    public required string Message { get; init; }
    public required string MessageType { get; init; }
}

/// <summary>Outcome of a single provider send attempt.</summary>
public sealed class SmsSendResult
{
    public bool Success { get; init; }
    public string? ProviderMessageId { get; init; }
    public string? ErrorMessage { get; init; }
    public SmsProviderException? Exception { get; init; }

    public static SmsSendResult Succeeded(string providerMessageId) =>
        new() { Success = true, ProviderMessageId = providerMessageId };

    public static SmsSendResult Failed(string errorMessage, SmsProviderException? exception = null) =>
        new() { Success = false, ErrorMessage = errorMessage, Exception = exception };
}
