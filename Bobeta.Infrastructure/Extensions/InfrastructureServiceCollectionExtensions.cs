using Bobeta.Application.Interfaces;
using Bobeta.Infrastructure.External;
using Bobeta.Infrastructure.MoMo;
using Bobeta.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bobeta.Infrastructure.Extensions;

/// <summary>Registers infrastructure services (Mobile Money placeholder and MTN MoMo payment service).</summary>
public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>Adds infrastructure services and MTN MoMo payment integration (requires configuration for MoMo settings).</summary>
    public static IServiceCollection AddBobetaInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IMobileMoneyService, PlaceholderMobileMoneyService>();
        services.Configure<MoMoSettings>(configuration.GetSection(MoMoSettings.SectionName));
        services.AddHttpClient();
        services.AddScoped<IPaymentService, MoMoPaymentService>();
        return services;
    }
}
