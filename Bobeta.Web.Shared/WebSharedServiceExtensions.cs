using Bobeta.Web.Shared.Services;
using Bobeta.Web.Shared.Services.Realtime;
using Bobeta.Web.Shared.ViewModels.Auth;
using Bobeta.Web.Shared.ViewModels.Dashboard;
using Bobeta.Web.Shared.ViewModels.Games;
using Bobeta.Web.Shared.ViewModels.Notifications;
using Bobeta.Web.Shared.ViewModels.Profile;
using Bobeta.Web.Shared.ViewModels.Wallet;
using Microsoft.Extensions.DependencyInjection;

namespace Bobeta.Web.Shared;

/// <summary>DI registrations shared by the main WASM host and the lazy-loaded assembly.</summary>
public static class WebSharedServiceExtensions
{
    public static IServiceCollection AddBobetaWebShared(this IServiceCollection services)
    {
        services.AddScoped<LocalStorageService>();
        services.AddScoped<AppStateService>();
        services.AddScoped<I18nService>();
        services.AddScoped<GameHubClient>();
        services.AddScoped<NotificationHubClient>();

        services.AddScoped<PhoneLoginViewModel>();
        services.AddScoped<OtpVerificationViewModel>();
        services.AddScoped<CreatePlayerViewModel>();
        services.AddScoped<DashboardViewModel>();
        services.AddScoped<DepositViewModel>();
        services.AddScoped<WithdrawViewModel>();
        services.AddScoped<JoinGameViewModel>();
        services.AddScoped<CreateGameViewModel>();
        services.AddScoped<GameHistoryViewModel>();
        services.AddScoped<ProfileViewModel>();
        services.AddScoped<NotificationInboxViewModel>();

        return services;
    }
}
