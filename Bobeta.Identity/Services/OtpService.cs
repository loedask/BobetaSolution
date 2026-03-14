using Bobeta.Application.Interfaces;
using Bobeta.Domain.Entities;
using Microsoft.Extensions.Configuration;

namespace Bobeta.Identity.Services;

/// <summary>Generates and validates OTP codes for phone-based authentication. OTPs expire after a short period and are single-use.</summary>
public class OtpService
{
    private const int ExpirationMinutes = 5;
    private readonly IOtpRepository _otpRepository;
    private readonly IConfiguration _configuration;

    public OtpService(IOtpRepository otpRepository, IConfiguration configuration)
    {
        _otpRepository = otpRepository;
        _configuration = configuration;
    }

    /// <summary>Generates a 6-digit OTP, stores it for the phone number with expiration, and returns the code (for sending via SMS in production).</summary>
    public async Task<string> GenerateAndStoreOtpAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        var code = GenerateOtpCode();
        var otp = new OtpCode
        {
            Id = Guid.NewGuid(),
            PhoneNumber = phoneNumber,
            Code = code,
            ExpiresAt = DateTime.UtcNow.AddMinutes(ExpirationMinutes),
            CreatedAt = DateTime.UtcNow
        };
        await _otpRepository.AddAsync(otp, cancellationToken);
        return code;
    }

    /// <summary>Validates the code for the phone number; if valid, marks the OTP as used and returns true.</summary>
    public async Task<bool> ValidateOtpAsync(string phoneNumber, string code, CancellationToken cancellationToken = default)
    {
        var otp = await _otpRepository.GetLatestByPhoneAsync(phoneNumber, cancellationToken);
        if (otp == null || otp.IsUsed || otp.ExpiresAt < DateTime.UtcNow || otp.Code != code)
            return false;
        await _otpRepository.InvalidateAsync(otp, cancellationToken);
        return true;
    }

    private static string GenerateOtpCode()
    {
        var rng = new Random();
        return rng.Next(100000, 999999).ToString();
    }
}
