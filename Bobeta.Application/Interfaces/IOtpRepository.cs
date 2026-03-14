using Bobeta.Domain.Entities;

namespace Bobeta.Application.Interfaces;

public interface IOtpRepository
{
    Task<OtpCode?> GetLatestByPhoneAsync(string phoneNumber, CancellationToken cancellationToken = default);
    Task<OtpCode> AddAsync(OtpCode otp, CancellationToken cancellationToken = default);
    Task InvalidateAsync(OtpCode otp, CancellationToken cancellationToken = default);
}
