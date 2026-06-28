using Bobeta.Application.Configuration;
using Bobeta.Application.Interfaces;
using Bobeta.Infrastructure.External;
using Bobeta.Infrastructure.MoMo;
using Bobeta.Infrastructure.Payments;
using Bobeta.Infrastructure.Services;
using Bobeta.Infrastructure.Sms;
using Bobeta.Infrastructure.Sms.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bobeta.Infrastructure.Extensions;

/// <summary>Registers infrastructure services (Mobile Money placeholder, MTN MoMo payment service, SMS gateway, retry policy, status worker).</summary>
public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>Named HttpClient for SendSMSGate.</summary>
    public const string SendSmsGateHttpClientName = "SendSmsGate";

    /// <summary>Named HttpClient for SMSPortal REST API.</summary>
    public const string SmsPortalHttpClientName = "SmsPortal";

    /// <summary>Backward-compatible alias for <see cref="SendSmsGateHttpClientName"/>.</summary>
    public const string SmsGatewayHttpClientName = SendSmsGateHttpClientName;

    /// <summary>Adds infrastructure services and MTN MoMo payment integration (requires configuration for MoMo settings).</summary>
    public static IServiceCollection AddBobetaInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IMobileMoneyService, PlaceholderMobileMoneyService>();
        services.Configure<CountrySettings>(configuration.GetSection(CountrySettings.SectionName));
        services.Configure<PaymentRevenueSettings>(configuration.GetSection(PaymentRevenueSettings.SectionName));
        services.Configure<MoMoSettings>(configuration.GetSection(MoMoSettings.SectionName));
        services.Configure<SmsOptions>(configuration.GetSection(SmsOptions.SectionName));
        services.Configure<SmsGatewaySettings>(configuration.GetSection(SmsGatewaySettings.SectionName));
        services.Configure<SmsPortalSettings>(configuration.GetSection(SmsPortalSettings.SectionName));

        services.AddHttpClient(MoMoPaymentService.MoMoHttpClientName);
        services.AddHttpClient(SendSmsGateHttpClientName);
        services.AddHttpClient(SmsPortalHttpClientName);

        services.AddMemoryCache();
        services.AddScoped<IOtpRateLimitService, OtpRateLimitService>();
        services.AddScoped<ISmsTemplateProvider, SmsTemplateProvider>();

        services.AddScoped<IPaymentService, MoMoPaymentService>();
        services.AddScoped<ISmsProvider, SendSmsGateSmsProvider>();
        services.AddScoped<ISmsProvider, SmsPortalSmsProvider>();
        services.AddScoped<ISmsService, MultiProviderSmsService>();

        services.AddHostedService<PaymentStatusWorker>();

        return services;
    }
}
