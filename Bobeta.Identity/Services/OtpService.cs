using Bobeta.Application.Interfaces;
using Bobeta.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Bobeta.Identity.Services;

/// <summary>Generates and validates OTP codes for phone-based authentication. OTPs expire after 5 minutes and are single-use.</summary>
public class OtpService
{
    /// <summary>OTP codes expire after this many minutes. Validation rejects expired codes.</summary>
    public const int ExpirationMinutes = 5;

    private readonly IOtpRepository _otpRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OtpService> _logger;

    public OtpService(IOtpRepository otpRepository, IConfiguration configuration, ILogger<OtpService> logger)
    {
        _otpRepository = otpRepository;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>Generates a 6-digit OTP, stores it for the phone number with expiration (5 minutes), and returns the code (for sending via SMS in production).</summary>
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
        _logger.LogInformation("OTP generated: PhoneNumber={PhoneNumber}, ExpiresAt={ExpiresAt}", phoneNumber, otp.ExpiresAt);
        return code;
    }

    /// <summary>Validates the code for the phone number. Rejects expired or already-used codes. Returns true only if valid and not expired.</summary>
    public async Task<bool> ValidateOtpAsync(string phoneNumber, string code, CancellationToken cancellationToken = default)
    {
        var otp = await _otpRepository.GetLatestByPhoneAsync(phoneNumber, cancellationToken);
        var now = DateTime.UtcNow;

        if (otp == null)
        {
            _logger.LogWarning("OTP validation failed: no code for PhoneNumber={PhoneNumber}", phoneNumber);
            return false;
        }
        if (otp.IsUsed)
        {
            _logger.LogWarning("OTP validation failed: already used for PhoneNumber={PhoneNumber}", phoneNumber);
            return false;
        }
        if (otp.ExpiresAt < now)
        {
            _logger.LogWarning("OTP expired: PhoneNumber={PhoneNumber}, ExpiredAt={ExpiresAt}", phoneNumber, otp.ExpiresAt);
            return false;
        }
        if (otp.Code != code)
        {
            _logger.LogWarning("OTP validation failed: wrong code for PhoneNumber={PhoneNumber}", phoneNumber);
            return false;
        }

        await _otpRepository.InvalidateAsync(otp, cancellationToken);
        _logger.LogInformation("OTP validated: PhoneNumber={PhoneNumber}", phoneNumber);
        return true;
    }

    private static string GenerateOtpCode()
    {
        var rng = new Random();
        return rng.Next(100000, 999999).ToString();
    }
}
