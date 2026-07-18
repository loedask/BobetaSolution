using Bobeta.Domain.Enums;

namespace Bobeta.Domain.Entities;

/// <summary>FCM registration token for a player device (phone push when app is backgrounded or closed).</summary>
public class PlayerDeviceToken
{
    public Guid Id { get; set; }
    public Guid PlayerId { get; set; }
    /// <summary>FCM device token (also used for iOS when APNs is linked in Firebase).</summary>
    public string Token { get; set; } = string.Empty;
    public DevicePlatform Platform { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
