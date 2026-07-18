namespace Bobeta.Mobile.Services.Push;

/// <summary>Obtains an FCM registration token from the native SDK (when configured).</summary>
public interface IPushTokenProvider
{
    /// <summary>Returns the current FCM token, or null if push is unavailable on this platform/build.</summary>
    Task<string?> GetTokenAsync(CancellationToken cancellationToken = default);

    Bobeta.Domain.Enums.DevicePlatform Platform { get; }
}
