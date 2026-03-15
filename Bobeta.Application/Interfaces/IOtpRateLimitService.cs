namespace Bobeta.Application.Interfaces;

/// <summary>Checks and records OTP request attempts for rate limiting (per phone and per IP).</summary>
public interface IOtpRateLimitService
{
    /// <summary>Checks rate limits, records this attempt, and returns whether the request is allowed.</summary>
    /// <param name="phoneNumber">Phone number (normalized or raw).</param>
    /// <param name="clientIp">Client IP address (optional; if null, only phone limit is applied).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Allowed = true if within limits; otherwise ErrorMessage is "Too many OTP requests. Please try again later."</returns>
    Task<(bool Allowed, string? ErrorMessage)> CheckAndRecordAsync(string phoneNumber, string? clientIp, CancellationToken cancellationToken = default);
}
