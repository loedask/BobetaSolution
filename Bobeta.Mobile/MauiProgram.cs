using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
#if ANDROID
using Xamarin.Android.Net;
#endif
using Bobeta.Client;
using Bobeta.Client.Contracts;
using Bobeta.Mobile.Pages;
using Bobeta.Mobile.Services;
using Bobeta.Mobile.Services.Realtime;
using Bobeta.Mobile.ViewModels.Auth;
using Bobeta.Mobile.ViewModels.Dashboard;
using Bobeta.Mobile.ViewModels.Games;
using Bobeta.Mobile.ViewModels.Profile;
using Bobeta.Mobile.ViewModels.Wallet;

namespace Bobeta.Mobile;

public static class MauiProgram
{
    public static IServiceProvider Services { get; private set; } = null!;

    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        var assembly = Assembly.GetExecutingAssembly();
        AddConfigJsonStream(assembly, builder, "Bobeta.Mobile.appsettings.json");
#if DEBUG
        // Debug builds: optional override (e.g. Android emulator host, LAN IP, or https://localhost:7029 for Windows).
        AddConfigJsonStream(assembly, builder, "Bobeta.Mobile.appsettings.Development.json");
#endif
#if !DEBUG
        // Release / store: stable API URL from appsettings.Production.json.
        AddConfigJsonStream(assembly, builder, "Bobeta.Mobile.appsettings.Production.json");
#endif

        static void AddConfigJsonStream(Assembly asm, MauiAppBuilder b, string manifestName)
        {
            using var stream = asm.GetManifestResourceStream(manifestName);
            if (stream != null)
                b.Configuration.AddJsonStream(stream);
        }

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        var apiBaseUrl = (builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7029").Trim();
        // Paste/line-wrap errors can insert a space inside the host (DNS then fails with "No address associated with hostname").
        apiBaseUrl = apiBaseUrl.Replace(" ", "").Replace("\u00A0", "");
#if DEBUG && !ANDROID
        // appsettings.Development.json uses 10.0.2.2 for the Android emulator (host loopback alias).
        // On Windows, iOS simulator, and Mac Catalyst that address is wrong — use the actual machine loopback.
        if (apiBaseUrl.Contains("10.0.2.2", StringComparison.OrdinalIgnoreCase))
            apiBaseUrl = apiBaseUrl.Replace("10.0.2.2", "localhost", StringComparison.OrdinalIgnoreCase);
#endif
        builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

        builder.Services.AddSingleton<PreferencesStorageService>();
        builder.Services.AddSingleton<AppStateService>();
        builder.Services.AddSingleton<IAccessTokenProvider, MobileAccessTokenProvider>();
        builder.Services.AddSingleton<I18nService>();
        builder.Services.AddSingleton<INavigationService, ShellNavigationService>();
        builder.Services.AddSingleton<GameHubClient>();
        builder.Services.AddSingleton<NotificationHubClient>();
        builder.Services.AddSingleton<GamePlayTestService>();

#if ANDROID
        // HttpClientFactory defaults to SocketsHttpHandler on Android; use the Java stack for reliable DNS (emulator "hostname nor servname").
        builder.Services.AddBobetaClient(
            http => http.BaseAddress = new Uri(apiBaseUrl),
            useBearerToken: true,
            b => b.ConfigurePrimaryHttpMessageHandler(() => new AndroidMessageHandler()));
#else
        builder.Services.AddBobetaClient(http => http.BaseAddress = new Uri(apiBaseUrl), useBearerToken: true);
#endif

        builder.Services.AddTransient<PhoneLoginViewModel>();
        builder.Services.AddTransient<OtpVerificationViewModel>();
        builder.Services.AddTransient<CreatePlayerViewModel>();
        builder.Services.AddTransient<DashboardViewModel>();
        builder.Services.AddTransient<DepositViewModel>();
        builder.Services.AddTransient<WithdrawViewModel>();
        builder.Services.AddTransient<JoinGameViewModel>();
        builder.Services.AddTransient<CreateGameViewModel>();
        builder.Services.AddTransient<GameHistoryViewModel>();
        builder.Services.AddTransient<ProfileViewModel>();
        builder.Services.AddTransient<GamePlayViewModel>();
        builder.Services.AddSingleton<Bobeta.Mobile.ViewModels.Notifications.NotificationInboxViewModel>();

        builder.Services.AddSingleton<AppShell>();

        var app = builder.Build();
        Services = app.Services;
        return app;
    }
}
