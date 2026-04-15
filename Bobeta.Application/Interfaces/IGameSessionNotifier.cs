namespace Bobeta.Application.Interfaces;

/// <summary>Lets the host notify real-time clients about session lifecycle (e.g. SignalR after the table is ready).</summary>
public interface IGameSessionNotifier
{
    /// <summary>Invoked after the session is updated in a way clients should refresh (e.g. game dealt).</summary>
    Task NotifySessionAsync(Guid sessionId, CancellationToken cancellationToken = default);
}
