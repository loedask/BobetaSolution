using Bobeta.Application.Interfaces;

namespace Bobeta.Application.Services;

/// <summary>No-op push sender used when FCM is not configured (local/tests).</summary>
public sealed class NullPushNotificationSender : IPushNotificationSender
{
    public static NullPushNotificationSender Instance { get; } = new();

    public Task<IReadOnlyList<string>> SendAsync(
        IReadOnlyList<string> tokens,
        string title,
        string body,
        IReadOnlyDictionary<string, string>? data = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
}
