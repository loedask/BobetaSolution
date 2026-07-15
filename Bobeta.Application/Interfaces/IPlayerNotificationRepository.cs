using Bobeta.Domain.Entities;

namespace Bobeta.Application.Interfaces;

/// <summary>Persistence for player inbox notifications.</summary>
public interface IPlayerNotificationRepository
{
    Task<PlayerNotification> AddAsync(PlayerNotification notification, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PlayerNotification>> GetForPlayerAsync(
        Guid playerId,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<int> CountUnreadAsync(Guid playerId, CancellationToken cancellationToken = default);

    Task<PlayerNotification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task UpdateAsync(PlayerNotification notification, CancellationToken cancellationToken = default);

    Task MarkAllReadAsync(Guid playerId, CancellationToken cancellationToken = default);
}
