using System.Security.Cryptography;
using System.Text;
using Bobeta.Application.Common;
using Bobeta.Application.Interfaces;
using Bobeta.Domain.Authentication;
using Bobeta.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Bobeta.Identity.Services;

/// <summary>Generates and validates OTP codes for phone-based authentication. OTPs expire after 5 minutes and are single-use. Codes are stored as SHA256 hashes; brute-force protection locks after 5 incorrect attempts.</summary>
public class OtpService
{
    /// <summary>OTP codes expire after this many minutes. Validation rejects expired codes.</summary>
    public const int ExpirationMinutes = 5;

    /// <summary>Maximum incorrect verification attempts before locking the OTP.</summary>
    public const int MaxFailedAttempts = 5;

    /// <summary>Lockout duration in minutes when max failed attempts exceeded.</summary>
    public const int LockoutMinutes = 10;

    private readonly IOtpRepository _otpRepository;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ILogger<OtpService> _logger;

    public OtpService(
        IOtpRepository otpRepository,
        IConfiguration configuration,
        IHostEnvironment hostEnvironment,
        ILogger<OtpService> logger)
    {
        _otpRepository = otpRepository;
        _configuration = configuration;
        _hostEnvironment = hostEnvironment;
        _logger = logger;
    }

    /// <summary>Generates an OTP of <see cref="PhoneAuthConstants.OtpDigitLength"/> digits, hashes it with SHA256, stores the hash with expiration (5 minutes), and returns the plain code (for sending via SMS).</summary>
    public async Task<string> GenerateAndStoreOtpAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        var code = GenerateOtpCode();
        var codeHash = HashCode(code);
        var otp = new OtpCode
        {
            Id = Guid.NewGuid(),
            PhoneNumber = phoneNumber,
            Code = codeHash,
            ExpiresAt = DateTime.UtcNow.AddMinutes(ExpirationMinutes),
            CreatedAt = DateTime.UtcNow,
            FailedAttemptCount = 0,
            LockedUntil = null
        };
        await _otpRepository.AddAsync(otp, cancellationToken);
        _logger.LogInformation("OTP generated: PhoneNumber={PhoneNumber}, ExpiresAt={ExpiresAt}", phoneNumber, otp.ExpiresAt);
        return code;
    }

    /// <summary>Validates the code for the phone number. Rejects expired, locked, or already-used codes. Compares SHA256 hash of input with stored hash. Returns (valid, errorMessage); errorMessage is set for brute-force lockout.</summary>
    public async Task<(bool Valid, string? ErrorMessage)> ValidateOtpAsync(string phoneNumber, string code, CancellationToken cancellationToken = default)
    {
        if (TryValidateStaticDemoOtp(phoneNumber, code))
        {
            _logger.LogInformation("OTP validated (dev/staging demo static code): PhoneNumber={PhoneNumber}", phoneNumber);
            return (true, null);
        }

        var otp = await _otpRepository.GetLatestByPhoneAsync(phoneNumber, cancellationToken);
        var now = DateTime.UtcNow;

        if (otp == null)
        {
            _logger.LogWarning("OTP validation failed: no code for PhoneNumber={PhoneNumber}", phoneNumber);
            return (false, null);
        }
        if (otp.IsUsed)
        {
            _logger.LogWarning("OTP validation failed: already used for PhoneNumber={PhoneNumber}", phoneNumber);
            return (false, null);
        }
        if (otp.ExpiresAt < now)
        {
            _logger.LogWarning("OTP expired: PhoneNumber={PhoneNumber}, ExpiredAt={ExpiresAt}", phoneNumber, otp.ExpiresAt);
            return (false, null);
        }
        if (otp.LockedUntil.HasValue && otp.LockedUntil.Value > now)
        {
            _logger.LogWarning("OTP validation failed: locked until {LockedUntil}, PhoneNumber={PhoneNumber}", otp.LockedUntil, phoneNumber);
            return (false, "Too many incorrect verification attempts.");
        }

        var inputHash = HashCode(code);
        if (otp.Code != inputHash)
        {
            otp.FailedAttemptCount++;
            if (otp.FailedAttemptCount >= MaxFailedAttempts)
            {
                otp.LockedUntil = now.AddMinutes(LockoutMinutes);
                await _otpRepository.UpdateAsync(otp, cancellationToken);
                _logger.LogWarning("OTP brute force detected: PhoneNumber={PhoneNumber}, AttemptCount={AttemptCount}", phoneNumber, otp.FailedAttemptCount);
                return (false, "Too many incorrect verification attempts.");
            }
            await _otpRepository.UpdateAsync(otp, cancellationToken);
            _logger.LogWarning("OTP validation failed: wrong code for PhoneNumber={PhoneNumber}", phoneNumber);
            return (false, null);
        }

        await _otpRepository.InvalidateAsync(otp, cancellationToken);
        _logger.LogInformation("OTP validated: PhoneNumber={PhoneNumber}", phoneNumber);
        return (true, null);
    }

    private static string HashCode(string code)
    {
        var bytes = Encoding.UTF8.GetBytes(code ?? "");
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    private static string GenerateOtpCode()
    {
        var len = PhoneAuthConstants.OtpDigitLength;
        var min = (int)Math.Pow(10, len - 1);
        var max = (int)Math.Pow(10, len);
        return Random.Shared.Next(min, max).ToString();
    }

    /// <summary>Allows a fixed OTP for configured demo numbers in Development or Staging only (never in Production).</summary>
    private bool TryValidateStaticDemoOtp(string phoneNumber, string code)
    {
        if (!DemoEnvironmentHelper.AllowsDemoAuthFeatures(_hostEnvironment))
            return false;
        if (!_configuration.GetValue("DemoAuth:EnableStaticOtp", false))
            return false;

        var expected = _configuration["DemoAuth:StaticOtp"];
        if (string.IsNullOrEmpty(expected) || code != expected)
            return false;

        var normalized = PhoneNumberHelper.Normalize(phoneNumber);
        foreach (var child in _configuration.GetSection("DemoAuth:PhoneNumbers").GetChildren())
        {
            var configured = child.Value;
            if (string.IsNullOrEmpty(configured))
                continue;
            if (PhoneNumberHelper.Normalize(configured) == normalized)
                return true;
        }

        return false;
    }
}
