using Bobeta.API.Hubs;
using Bobeta.Application.DTOs.Notifications;
using Bobeta.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace Bobeta.API.App.Services;

/// <summary>Publishes inbox notifications to the authenticated player's SignalR user channel.</summary>
public sealed class SignalRNotificationRealtimePublisher(IHubContext<NotificationHub> hubContext) : INotificationRealtimePublisher
{
    public const string EventName = "NotificationReceived";

    public Task PublishAsync(Guid playerId, NotificationDto notification, CancellationToken cancellationToken = default) =>
        hubContext.Clients.User(playerId.ToString()).SendAsync(EventName, notification, cancellationToken);
}
