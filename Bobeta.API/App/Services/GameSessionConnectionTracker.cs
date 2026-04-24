using System.Collections.Concurrent;

namespace Bobeta.API.App.Services;

/// <summary>Tracks hub connections per (session, player) so <see cref="Bobeta.API.Controllers.GamePlayController"/> can deliver seat-specific <c>GameState</c> to the correct clients.</summary>
public sealed class GameSessionConnectionTracker : IGameSessionConnectionTracker
{
    private readonly ConcurrentDictionary<string, (Guid SessionId, Guid PlayerId)> _connectionToSeat = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<(Guid SessionId, Guid PlayerId), ConcurrentDictionary<string, byte>> _seatToConnections = new();

    public void AddConnection(Guid sessionId, Guid playerId, string connectionId)
    {
        _connectionToSeat[connectionId] = (sessionId, playerId);
        var seat = (sessionId, playerId);
        var set = _seatToConnections.GetOrAdd(seat, _ => new ConcurrentDictionary<string, byte>(StringComparer.Ordinal));
        set[connectionId] = 0;
    }

    public void RemoveConnection(string connectionId)
    {
        if (!_connectionToSeat.TryRemove(connectionId, out var seat))
            return;
        if (_seatToConnections.TryGetValue(seat, out var set))
        {
            set.TryRemove(connectionId, out _);
            if (set.IsEmpty)
                _seatToConnections.TryRemove(seat, out _);
        }
    }

    public IReadOnlyList<string> GetConnectionIds(Guid sessionId, Guid playerId)
    {
        if (!_seatToConnections.TryGetValue((sessionId, playerId), out var set) || set.IsEmpty)
            return Array.Empty<string>();
        return set.Keys.ToArray();
    }
}
