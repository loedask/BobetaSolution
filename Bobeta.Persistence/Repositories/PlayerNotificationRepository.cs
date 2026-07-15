using Bobeta.Application.Interfaces;
using Bobeta.Domain.Entities;
using Bobeta.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Bobeta.Persistence.Repositories;

/// <summary>Repository for player inbox notifications.</summary>
public class PlayerNotificationRepository(BobetaDbContext db) : IPlayerNotificationRepository
{
    public async Task<PlayerNotification> AddAsync(PlayerNotification notification, CancellationToken cancellationToken = default)
    {
        db.PlayerNotifications.Add(notification);
        await db.SaveChangesAsync(cancellationToken);
        return notification;
    }

    public async Task<IReadOnlyList<PlayerNotification>> GetForPlayerAsync(
        Guid playerId,
        int skip,
        int take,
        CancellationToken cancellationToken = default) =>
        await db.PlayerNotifications
            .AsNoTracking()
            .Where(n => n.PlayerId == playerId)
            .OrderByDescending(n => n.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

    public Task<int> CountUnreadAsync(Guid playerId, CancellationToken cancellationToken = default) =>
        db.PlayerNotifications.CountAsync(n => n.PlayerId == playerId && !n.IsRead, cancellationToken);

    public Task<PlayerNotification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.PlayerNotifications.FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

    public async Task UpdateAsync(PlayerNotification notification, CancellationToken cancellationToken = default)
    {
        db.PlayerNotifications.Update(notification);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkAllReadAsync(Guid playerId, CancellationToken cancellationToken = default)
    {
        await db.PlayerNotifications
            .Where(n => n.PlayerId == playerId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), cancellationToken);
    }
}
