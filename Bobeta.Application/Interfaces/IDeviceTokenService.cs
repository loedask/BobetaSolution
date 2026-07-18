using Bobeta.Domain.Enums;

namespace Bobeta.Application.Interfaces;

/// <summary>Registers and removes phone push device tokens for a player.</summary>
public interface IDeviceTokenService
{
    Task RegisterAsync(Guid playerId, string token, DevicePlatform platform, CancellationToken cancellationToken = default);

    Task UnregisterAsync(Guid playerId, string token, CancellationToken cancellationToken = default);
}
