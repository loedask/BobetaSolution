using Bobeta.Domain.Enums;

namespace Bobeta.Application.DTOs.Notifications;

/// <summary>Player inbox notification returned by the API and pushed over SignalR.</summary>
public record NotificationDto(
    Guid Id,
    NotificationType Type,
    string? ActorName,
    decimal? Amount,
    Guid? RelatedEntityId,
    string? DeepLink,
    bool IsRead,
    DateTime CreatedAt);
