namespace Bobeta.Domain.Entities;

/// <summary>
/// One-time password sent to a phone number for verification. Expires after a short period and is single-use.
/// </summary>
public class OtpCode
{
    /// <summary>Unique identifier for the OTP record.</summary>
    public Guid Id { get; set; }

    /// <summary>Phone number this OTP was sent to.</summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>Numeric OTP code (e.g. 6 digits).</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>When this OTP expires and can no longer be used.</summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>When this OTP was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Whether this OTP has already been used for a successful verification.</summary>
    public bool IsUsed { get; set; }
}
