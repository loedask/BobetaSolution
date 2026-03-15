using Bobeta.Application.Interfaces;
using Bobeta.Infrastructure.External;
using Bobeta.Infrastructure.MoMo;
using Bobeta.Infrastructure.Payments;
using Bobeta.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bobeta.Infrastructure.Extensions;

/// <summary>Registers infrastructure services (Mobile Money placeholder, MTN MoMo payment service, retry policy, status worker).</summary>
public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>Adds infrastructure services and MTN MoMo payment integration (requires configuration for MoMo settings).</summary>
    public static IServiceCollection AddBobetaInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IMobileMoneyService, PlaceholderMobileMoneyService>();
        services.Configure<MoMoSettings>(configuration.GetSection(MoMoSettings.SectionName));

        // Part 4 — Retry: MoMoPaymentService uses named client; retry policy applied inside service (Polly, up to 3 times)
        services.AddHttpClient(MoMoPaymentService.MoMoHttpClientName);

        services.AddScoped<IPaymentService, MoMoPaymentService>();

        // Part 3 — Payment status worker: poll pending transactions every 60 seconds
        services.AddHostedService<PaymentStatusWorker>();

        return services;
    }
}
