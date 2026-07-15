using Bobeta.Application.DTOs.Notifications;
using Bobeta.Application.Interfaces;

namespace Bobeta.Application.Services;

/// <summary>No-op publisher used when SignalR is not registered (e.g. tests).</summary>
public sealed class NullNotificationRealtimePublisher : INotificationRealtimePublisher
{
    public Task PublishAsync(Guid playerId, NotificationDto notification, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
