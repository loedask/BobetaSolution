using Bobeta.Application.Interfaces;
using Bobeta.Domain.Entities;
using Bobeta.Domain.Enums;

namespace Bobeta.Application.Services;

/// <summary>Upserts FCM device tokens per player (tokens can move between accounts on shared devices).</summary>
public class DeviceTokenService(IPlayerDeviceTokenRepository repository) : IDeviceTokenService
{
    public async Task RegisterAsync(Guid playerId, string token, DevicePlatform platform, CancellationToken cancellationToken = default)
    {
        token = token.Trim();
        if (string.IsNullOrEmpty(token) || token.Length > 512)
            throw new InvalidOperationException("Invalid device token.");

        var now = DateTime.UtcNow;
        var existing = await repository.GetByTokenAsync(token, cancellationToken);
        if (existing is not null)
        {
            existing.PlayerId = playerId;
            existing.Platform = platform;
            existing.UpdatedAt = now;
            await repository.UpdateAsync(existing, cancellationToken);
            return;
        }

        await repository.AddAsync(new PlayerDeviceToken
        {
            Id = Guid.NewGuid(),
            PlayerId = playerId,
            Token = token,
            Platform = platform,
            CreatedAt = now,
            UpdatedAt = now
        }, cancellationToken);
    }

    public async Task UnregisterAsync(Guid playerId, string token, CancellationToken cancellationToken = default)
    {
        token = token.Trim();
        if (string.IsNullOrEmpty(token))
            return;

        var existing = await repository.GetByTokenAsync(token, cancellationToken);
        if (existing is null || existing.PlayerId != playerId)
            return;

        await repository.DeleteByTokenAsync(token, cancellationToken);
    }
}
