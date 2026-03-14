using Bobeta.Domain.Entities;

namespace Bobeta.Application.Interfaces;

/// <summary>Persistence contract for OTP codes (send, validate, invalidate after use).</summary>
public interface IOtpRepository
{
    /// <summary>Gets the latest unused OTP for the phone number, or null if none.</summary>
    Task<OtpCode?> GetLatestByPhoneAsync(string phoneNumber, CancellationToken cancellationToken = default);

    /// <summary>Stores a new OTP (e.g. after sending to user).</summary>
    Task<OtpCode> AddAsync(OtpCode otp, CancellationToken cancellationToken = default);

    /// <summary>Marks an OTP as used so it cannot be reused.</summary>
    Task InvalidateAsync(OtpCode otp, CancellationToken cancellationToken = default);
}
