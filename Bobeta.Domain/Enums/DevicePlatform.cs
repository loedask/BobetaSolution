namespace Bobeta.Domain.Enums;

/// <summary>Client OS for push device tokens (FCM covers Android; APNs via FCM for iOS).</summary>
public enum DevicePlatform
{
    Android = 0,
    Ios = 1
}
