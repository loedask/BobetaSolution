namespace Bobeta.Client.Models.Notifications;

public class NotificationViewModel
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
