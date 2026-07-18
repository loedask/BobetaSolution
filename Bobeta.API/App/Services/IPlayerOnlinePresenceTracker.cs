namespace Bobeta.API.App.Services;

/// <summary>Records authenticated player presence for admin "online now" metrics.</summary>
public interface IPlayerOnlinePresenceTracker
{
    /// <summary>Persists a presence ping, throttled per player to avoid excess DB writes.</summary>
    Task TouchAsync(Guid playerId, CancellationToken cancellationToken = default);
}
