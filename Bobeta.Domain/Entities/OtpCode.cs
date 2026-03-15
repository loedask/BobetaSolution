namespace Bobeta.Domain.Entities;

/// <summary>
/// One-time password sent to a phone number for verification. Expires after a short period and is single-use.
/// Stores SHA256 hash of the code; supports brute-force protection (max attempts, lockout).
/// </summary>
public class OtpCode
{
    /// <summary>Unique identifier for the OTP record.</summary>
    public Guid Id { get; set; }

    /// <summary>Phone number this OTP was sent to.</summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>SHA256 hash of the OTP code (hex string), not the plain code.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>When this OTP expires and can no longer be used.</summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>When this OTP was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Whether this OTP has already been used for a successful verification.</summary>
    public bool IsUsed { get; set; }

    /// <summary>Number of incorrect verification attempts. OTP is locked after max attempts.</summary>
    public int FailedAttemptCount { get; set; }

    /// <summary>When the OTP is locked until (e.g. after too many failed attempts). Null if not locked.</summary>
    public DateTime? LockedUntil { get; set; }
}
