#if ANDROID
using Android.App;
using Android.Content;
using Firebase.Messaging;
using Bobeta.Mobile.Services.Push;

namespace Bobeta.Mobile.Platforms.Android;

/// <summary>Receives FCM token refresh and notification messages while the app process is alive.</summary>
[Service(Exported = false)]
[IntentFilter(["com.google.firebase.MESSAGING_EVENT"])]
public sealed class BobetaFirebaseMessagingService : FirebaseMessagingService
{
    public override void OnNewToken(string token)
    {
        base.OnNewToken(token);
        if (string.IsNullOrWhiteSpace(token))
            return;

        Preferences.Default.Set("fcm_token", token);
        _ = RegisterRefreshedTokenAsync(token);
    }

    private static async Task RegisterRefreshedTokenAsync(string token)
    {
        try
        {
            var push = MauiProgram.Services.GetService<PushRegistrationService>();
            if (push is not null)
                await push.RegisterIfPossibleAsync();
        }
        catch
        {
            // Best-effort; next login/dashboard open will retry.
        }
    }

    public override void OnMessageReceived(RemoteMessage message)
    {
        base.OnMessageReceived(message);
        // System tray display is handled by FCM when the app is backgrounded and a notification payload is present.
    }
}
#endif
