using Bobeta.Application.DTOs.Notifications;

namespace Bobeta.Application.Interfaces;

/// <summary>Pushes a persisted notification to a connected player (e.g. SignalR).</summary>
public interface INotificationRealtimePublisher
{
    Task PublishAsync(Guid playerId, NotificationDto notification, CancellationToken cancellationToken = default);
}
