using Bobeta.Domain.Enums;

namespace Bobeta.Domain.Entities;

/// <summary>Persisted in-app notification for a player inbox.</summary>
public class PlayerNotification
{
    public Guid Id { get; set; }
    public Guid PlayerId { get; set; }
    public NotificationType Type { get; set; }
    /// <summary>Optional display name used in message templates (e.g. opponent).</summary>
    public string? ActorName { get; set; }
    public decimal? Amount { get; set; }
    public Guid? RelatedEntityId { get; set; }
    /// <summary>Client route hint, e.g. /history or /game/{id}.</summary>
    public string? DeepLink { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}
