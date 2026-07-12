using Bobeta.Mobile.Services;

namespace Bobeta.Mobile;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(MauiProgram.Services.GetRequiredService<AppShell>());
    }

    protected override void OnAppLinkRequestReceived(Uri uri)
    {
        base.OnAppLinkRequestReceived(uri);
        TryCaptureInviteCode(uri);
    }

    /// <summary>Supports bobeta://invite/CODE or https://host/invite/CODE.</summary>
    public static void TryCaptureInviteCode(Uri uri)
    {
        try
        {
            var code = ExtractInviteCode(uri);
            if (string.IsNullOrWhiteSpace(code))
                return;

            var appState = MauiProgram.Services.GetRequiredService<AppStateService>();
            appState.SetPendingInviteCode(code);
            _ = appState.PersistAsync();
        }
        catch
        {
            /* best-effort deep link capture */
        }
    }

    private static string? ExtractInviteCode(Uri uri)
    {
        var segments = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < segments.Length - 1; i++)
        {
            if (segments[i].Equals("invite", StringComparison.OrdinalIgnoreCase))
                return segments[i + 1];
        }

        if (uri.Scheme.Equals("bobeta", StringComparison.OrdinalIgnoreCase)
            && segments.Length >= 1
            && !segments[0].Equals("invite", StringComparison.OrdinalIgnoreCase))
            return segments[^1];

        return GetQueryValue(uri.Query, "code") ?? GetQueryValue(uri.Query, "invite");
    }

    private static string? GetQueryValue(string query, string key)
    {
        if (string.IsNullOrEmpty(query)) return null;
        var trimmed = query.TrimStart('?');
        foreach (var part in trimmed.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = part.Split('=', 2);
            if (kv.Length == 2 && kv[0].Equals(key, StringComparison.OrdinalIgnoreCase))
                return Uri.UnescapeDataString(kv[1]);
        }
        return null;
    }
}
