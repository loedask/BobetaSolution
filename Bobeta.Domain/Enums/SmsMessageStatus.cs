namespace Bobeta.Domain.Enums;

/// <summary>Status of an SMS message in the pipeline (pending, sent to provider, delivered, or failed).</summary>
public enum SmsMessageStatus
{
    Pending = 0,
    Sent = 1,
    Delivered = 2,
    Failed = 3
}
