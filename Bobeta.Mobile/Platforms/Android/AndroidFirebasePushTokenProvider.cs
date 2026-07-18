#if ANDROID
using Android.Gms.Extensions;
using Bobeta.Mobile.Services.Push;
using Firebase.Messaging;
using Microsoft.Extensions.Logging;

namespace Bobeta.Mobile.Platforms.Android;

/// <summary>Reads the FCM token from Firebase Messaging (requires Platforms/Android/google-services.json).</summary>
public sealed class AndroidFirebasePushTokenProvider(ILogger<AndroidFirebasePushTokenProvider> logger) : IPushTokenProvider
{
    public Bobeta.Domain.Enums.DevicePlatform Platform => Bobeta.Domain.Enums.DevicePlatform.Android;

    public async Task<string?> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var task = FirebaseMessaging.Instance.GetToken();
            var result = await task.AsAsync<Java.Lang.String>();
            var token = result?.ToString();
            return string.IsNullOrWhiteSpace(token) ? null : token;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "FirebaseMessaging.GetToken failed. Replace google-services.json with a real Firebase Android app config.");
            return null;
        }
    }
}
#endif
