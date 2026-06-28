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
    services.AddSingleton<PortalPasswordHasher>();
    services.AddScoped<IPortalAuthService, PortalAuthService>();
    services.AddScoped<IPortalUserService, PortalUserService>();
    services.AddScoped<IPlayerQueryService, PlayerQueryService>();
    services.AddScoped<IWalletService, WalletService>();
    services.AddScoped<IGameHistoryService, GameHistoryService>();
    return services;
  }
}
