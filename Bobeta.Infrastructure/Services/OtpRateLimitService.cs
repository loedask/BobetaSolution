using System.Collections.Concurrent;
using Bobeta.Application.Common;
using Bobeta.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace Bobeta.Infrastructure.Services;

/// <summary>In-memory rate limiting: max 3 OTP requests per phone per 10 minutes, max 10 per IP per 1 hour.</summary>
public class OtpRateLimitService : IOtpRateLimitService
{
    private const int MaxPerPhone = 3;
    private const int PhoneWindowMinutes = 10;
    private const int MaxPerIp = 10;
    private const int IpWindowMinutes = 60;
    private const string ErrorMessage = "Too many OTP requests. Please try again later.";

    private readonly IMemoryCache _cache;
    private static readonly ConcurrentDictionary<string, object> Locks = new();

    public OtpRateLimitService(IMemoryCache cache)
    {
        _cache = cache;
    }

    /// <inheritdoc />
    public Task<(bool Allowed, string? ErrorMessage)> CheckAndRecordAsync(string phoneNumber, string? clientIp, CancellationToken cancellationToken = default)
    {
        var normalized = PhoneNumberHelper.Normalize(phoneNumber);
        var now = DateTime.UtcNow;
        var phoneKey = "otp:phone:" + normalized;
        var ipKey = string.IsNullOrWhiteSpace(clientIp) ? null : "otp:ip:" + clientIp.Trim();

        var phoneLock = Locks.GetOrAdd(phoneKey, _ => new object());
        lock (phoneLock)
        {
            var phoneTimestamps = _cache.GetOrCreate(phoneKey, e =>
            {
                e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(PhoneWindowMinutes + 1);
                return new List<DateTime>();
            })!;
            var sincePhone = now.AddMinutes(-PhoneWindowMinutes);
            phoneTimestamps.RemoveAll(t => t < sincePhone);
            if (phoneTimestamps.Count >= MaxPerPhone)
                return Task.FromResult((false, (string?)ErrorMessage));
            phoneTimestamps.Add(now);
        }

        if (ipKey != null)
        {
            var ipLock = Locks.GetOrAdd(ipKey, _ => new object());
            lock (ipLock)
            {
                var ipTimestamps = _cache.GetOrCreate(ipKey, e =>
                {
                    e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(IpWindowMinutes + 1);
                    return new List<DateTime>();
                })!;
                var sinceIp = now.AddMinutes(-IpWindowMinutes);
                ipTimestamps.RemoveAll(t => t < sinceIp);
                if (ipTimestamps.Count >= MaxPerIp)
                    return Task.FromResult((false, (string?)ErrorMessage));
                ipTimestamps.Add(now);
            }
        }

        return Task.FromResult((true, (string?)null));
    }
}
