using Bobeta.Domain.Enums;

namespace Bobeta.Domain.Entities;

/// <summary>
/// SMS message sent via a gateway provider. Tracks provider name, provider ID, and status for delivery reports.
/// </summary>
public class SmsMessage
{
    /// <summary>Unique identifier for the SMS record.</summary>
    public Guid Id { get; set; }

    /// <summary>Destination phone number (E.164 or normalized, e.g. 24267123456).</summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>Message body sent.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Provider that sent the message (e.g. SmsPortal, SendSmsGate).</summary>
    public string? Provider { get; set; }

    /// <summary>Provider message ID for DLR correlation.</summary>
    public string? ProviderMessageId { get; set; }

    /// <summary>Current status: Pending, Sent, Delivered, Failed.</summary>
    public SmsMessageStatus Status { get; set; }

    /// <summary>When the record was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>When the record was last updated (e.g. DLR received).</summary>
    public DateTime UpdatedAt { get; set; }
}
