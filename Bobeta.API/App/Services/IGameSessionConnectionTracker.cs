namespace Bobeta.API.App.Services;

/// <summary>Maps SignalR connection ids to a game session seat so gameplay can push per-player state without relying solely on IUserId routing.</summary>
public interface IGameSessionConnectionTracker
{
    void AddConnection(Guid sessionId, Guid playerId, string connectionId);

    void RemoveConnection(string connectionId);

    IReadOnlyList<string> GetConnectionIds(Guid sessionId, Guid playerId);
}
