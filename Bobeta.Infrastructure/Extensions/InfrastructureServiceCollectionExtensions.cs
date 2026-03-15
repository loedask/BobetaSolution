using Bobeta.Application.Configuration;
using Bobeta.Application.Interfaces;
using Bobeta.Infrastructure.External;
using Bobeta.Infrastructure.MoMo;
using Bobeta.Infrastructure.Payments;
using Bobeta.Infrastructure.Services;
using Bobeta.Infrastructure.Sms;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bobeta.Infrastructure.Extensions;

/// <summary>Registers infrastructure services (Mobile Money placeholder, MTN MoMo payment service, SMS gateway, retry policy, status worker).</summary>
public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>Named HttpClient for SendSMSGate.</summary>
    public const string SmsGatewayHttpClientName = "SmsGateway";

    /// <summary>Adds infrastructure services and MTN MoMo payment integration (requires configuration for MoMo settings).</summary>
    public static IServiceCollection AddBobetaInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IMobileMoneyService, PlaceholderMobileMoneyService>();
        services.Configure<CountrySettings>(configuration.GetSection(CountrySettings.SectionName));
        services.Configure<MoMoSettings>(configuration.GetSection(MoMoSettings.SectionName));
        services.Configure<SmsGatewaySettings>(configuration.GetSection(SmsGatewaySettings.SectionName));

        services.AddHttpClient(MoMoPaymentService.MoMoHttpClientName);
        services.AddHttpClient(SmsGatewayHttpClientName);

        services.AddScoped<IPaymentService, MoMoPaymentService>();
        services.AddScoped<ISmsService, SmsGatewayService>();

        services.AddHostedService<PaymentStatusWorker>();

        return services;
    }
}
