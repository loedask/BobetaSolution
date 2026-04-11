using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
        using var cfgStream = assembly.GetManifestResourceStream("Bobeta.Mobile.appsettings.json");
        if (cfgStream != null)
            builder.Configuration.AddJsonStream(cfgStream);

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

        var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:5001";
        builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

        builder.Services.AddSingleton<PreferencesStorageService>();
        builder.Services.AddSingleton<AppStateService>();
        builder.Services.AddSingleton<IAccessTokenProvider, MobileAccessTokenProvider>();
        builder.Services.AddSingleton<I18nService>();
        builder.Services.AddSingleton<INavigationService, ShellNavigationService>();
        builder.Services.AddSingleton<GameHubClient>();
        builder.Services.AddSingleton<GamePlayTestService>();

        builder.Services.AddBobetaClient(http => http.BaseAddress = new Uri(apiBaseUrl), useBearerToken: true);

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

        builder.Services.AddSingleton<AppShell>();

        var app = builder.Build();
        Services = app.Services;
        return app;
    }
}
