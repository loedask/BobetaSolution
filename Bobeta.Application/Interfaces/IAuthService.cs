using Bobeta.Application.DTOs.Auth;

namespace Bobeta.Application.Interfaces;

public interface IAuthService
{
    Task SendOtpAsync(string phoneNumber, CancellationToken cancellationToken = default);
    Task<VerifyOtpResult> VerifyOtpAsync(string phoneNumber, string code, CancellationToken cancellationToken = default);
    Task<AuthResponse> RegisterPlayerAsync(string phoneNumber, string playerName, CancellationToken cancellationToken = default);
}

public record VerifyOtpResult(bool Valid, string? Token);
