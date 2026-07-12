using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;

namespace Bobeta.Mobile
{
    [Activity(
        Theme = "@style/Maui.SplashTheme",
        MainLauncher = true,
        LaunchMode = LaunchMode.SingleTop,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    [IntentFilter(
        new[] { Intent.ActionView },
        Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
        DataScheme = "bobeta",
        DataHost = "invite",
        AutoVerify = false)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            TryHandleIntent(Intent);
        }

        protected override void OnNewIntent(Intent? intent)
        {
            base.OnNewIntent(intent);
            TryHandleIntent(intent);
        }

        private static void TryHandleIntent(Intent? intent)
        {
            var data = intent?.DataString;
            if (string.IsNullOrWhiteSpace(data))
                return;
            if (Uri.TryCreate(data, UriKind.Absolute, out var uri))
                App.TryCaptureInviteCode(uri);
        }
    }
}
