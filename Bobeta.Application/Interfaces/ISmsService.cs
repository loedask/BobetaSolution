namespace Bobeta.Application.Interfaces;

/// <summary>Application service for sending SMS (OTP, notifications) and checking delivery status.</summary>
public interface ISmsService
{
    /// <summary>Sends an OTP or verification SMS to the given phone number. Phone number should be normalized (e.g. 24267123456).</summary>
    /// <returns>Provider message ID for tracking, or null if send failed.</returns>
    Task<string?> SendOtpAsync(string phoneNumber, string message, CancellationToken cancellationToken = default);

    /// <summary>Sends a notification SMS to the given phone number. Phone number should be normalized.</summary>
    /// <returns>Provider message ID for tracking, or null if send failed.</returns>
    Task<string?> SendNotificationAsync(string phoneNumber, string message, CancellationToken cancellationToken = default);

    /// <summary>Checks delivery status for a message by provider message ID (e.g. from SendSMSGate DLR or status API).</summary>
    /// <returns>Status string if known (e.g. "deliver", "not_deliver", "expired", "send"), or null if unknown.</returns>
    Task<string?> CheckDeliveryStatusAsync(string providerMessageId, CancellationToken cancellationToken = default);
}
