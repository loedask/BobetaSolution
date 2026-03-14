using Bobeta.Application.DTOs.Auth;

namespace Bobeta.Application.Interfaces;

/// <summary>Application service for phone-based auth: send OTP, verify OTP, register player.</summary>
public interface IAuthService
{
    /// <summary>Sends an OTP to the given phone number (stored for verification; actual SMS is integration-dependent).</summary>
    Task SendOtpAsync(string phoneNumber, CancellationToken cancellationToken = default);

    /// <summary>Verifies the code for the phone number. If valid, returns a JWT token if the player already exists.</summary>
    Task<VerifyOtpResult> VerifyOtpAsync(string phoneNumber, string code, CancellationToken cancellationToken = default);

    /// <summary>Registers a new player with the given phone and name, creates wallet, returns JWT and profile.</summary>
    Task<AuthResponse> RegisterPlayerAsync(string phoneNumber, string playerName, CancellationToken cancellationToken = default);
}

/// <summary>Result of OTP verification: whether the code was valid and an optional JWT if the player exists.</summary>
/// <param name="Valid">True if the OTP matched and was not expired.</param>
/// <param name="Token">JWT token if the player is already registered; null otherwise.</param>
public record VerifyOtpResult(bool Valid, string? Token);
