using Bobeta.Application.Configuration;
using Bobeta.Application.Interfaces;
using Bobeta.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bobeta.Application.Extensions;

public static class PortalServiceCollectionExtensions
{
  public static IServiceCollection AddBobetaPortalServices(this IServiceCollection services, IConfiguration configuration)
  {
    services.Configure<PortalSettings>(configuration.GetSection(PortalSettings.SectionName));
    services.Configure<PaymentRevenueSettings>(configuration.GetSection(PaymentRevenueSettings.SectionName));
    services.AddSingleton<PortalPasswordHasher>();
    services.AddScoped<IPortalAuthService, PortalAuthService>();
    services.AddScoped<IPortalUserService, PortalUserService>();
    services.AddScoped<IPlayerQueryService, PlayerQueryService>();
    services.AddScoped<INotificationRealtimePublisher, NullNotificationRealtimePublisher>();
    services.AddSingleton<IPushNotificationSender, NullPushNotificationSender>();
    services.AddScoped<INotificationService, NotificationService>();
    services.AddScoped<IWalletService, WalletService>();
    services.AddScoped<IGameHistoryService, GameHistoryService>();
    services.AddScoped<ILicensePartnerService, LicensePartnerService>();
    services.AddScoped<IInfluencerService, InfluencerService>();
    services.AddScoped<IInfluencerAttributionService, InfluencerAttributionService>();
    services.AddScoped<IInfluencerProgramSettingsService, InfluencerProgramSettingsService>();
    services.AddScoped<IRevenueShareResolver, RevenueShareResolver>();
    services.AddScoped<IPartnerRevenueAllocationService, PartnerRevenueAllocationService>();
    services.AddScoped<IPaymentRevenueService, PaymentRevenueService>();
    services.AddScoped<IPartnerRevenueReportService, PartnerRevenueReportService>();
    services.AddScoped<ILicensePartnerAccessService, LicensePartnerAccessService>();
    services.AddScoped<IDashboardService, DashboardService>();
    services.AddScoped<IDemoAccountGamesResetService, DemoAccountGamesResetService>();
    return services;
  }
}
