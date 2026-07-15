namespace Bobeta.Client.Models.Notifications;

/// <summary>Wire DTO for api/Notifications.</summary>
internal sealed class NotificationApiDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = "";
    public string? ActorName { get; set; }
    public decimal? Amount { get; set; }
    public Guid? RelatedEntityId { get; set; }
    public string? DeepLink { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}
