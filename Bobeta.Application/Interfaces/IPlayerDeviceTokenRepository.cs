using Bobeta.Domain.Entities;
using Bobeta.Domain.Enums;

namespace Bobeta.Application.Interfaces;

/// <summary>Persistence for FCM/APNs device tokens.</summary>
public interface IPlayerDeviceTokenRepository
{
    Task<IReadOnlyList<PlayerDeviceToken>> GetByPlayerIdAsync(Guid playerId, CancellationToken cancellationToken = default);

    Task<PlayerDeviceToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);

    Task<PlayerDeviceToken> AddAsync(PlayerDeviceToken token, CancellationToken cancellationToken = default);

    Task UpdateAsync(PlayerDeviceToken token, CancellationToken cancellationToken = default);

    Task DeleteByTokenAsync(string token, CancellationToken cancellationToken = default);

    Task DeleteByTokensAsync(IReadOnlyList<string> tokens, CancellationToken cancellationToken = default);
}
