namespace Bobeta.Application.Interfaces;

/// <summary>Sends native phone push (FCM / APNs via FCM) to device tokens.</summary>
public interface IPushNotificationSender
{
    /// <summary>Sends a notification to the given FCM tokens. Returns tokens that should be removed (invalid/expired).</summary>
    Task<IReadOnlyList<string>> SendAsync(
        IReadOnlyList<string> tokens,
        string title,
        string body,
        IReadOnlyDictionary<string, string>? data = null,
        CancellationToken cancellationToken = default);
}
