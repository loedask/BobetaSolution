using System.Collections.Concurrent;
using Bobeta.Application.Common;
using Bobeta.Application.Interfaces;

namespace Bobeta.API.App.Services;

/// <summary>
/// Throttles presence writes so hub connect/heartbeat storms do not hammer the database.
/// A player counts as online while <c>LastSeenOnlineUtc</c> is within <see cref="PlayerPresenceWindows.Online"/>.
/// </summary>
public sealed class PlayerOnlinePresenceTracker(IServiceScopeFactory scopeFactory) : IPlayerOnlinePresenceTracker
{
    private readonly ConcurrentDictionary<Guid, DateTime> _lastWriteUtc = new();

    public async Task TouchAsync(Guid playerId, CancellationToken cancellationToken = default)
    {
        if (playerId == Guid.Empty)
            return;

        var now = DateTime.UtcNow;
        if (_lastWriteUtc.TryGetValue(playerId, out var last) && now - last < PlayerPresenceWindows.MinWriteInterval)
            return;

        _lastWriteUtc[playerId] = now;

        await using var scope = scopeFactory.CreateAsyncScope();
        var players = scope.ServiceProvider.GetRequiredService<IPlayerRepository>();
        await players.TouchLastSeenOnlineAsync(playerId, now, cancellationToken);
    }
}
