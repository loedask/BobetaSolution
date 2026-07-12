using Bobeta.Application.Games.Kopo;
using Bobeta.Application.Games.Makopa;
using Bobeta.Application.Interfaces;
using Bobeta.Application.Services;
using Bobeta.Application.Validators;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Bobeta.Application.Extensions;

/// <summary>Registers application services (wallet, game session, engine, history, notifications) and FluentValidation validators.</summary>
public static class ApplicationServiceCollectionExtensions
{
    /// <summary>Adds Bobeta application layer services and validators to the container.</summary>
    public static IServiceCollection AddBobetaApplication(this IServiceCollection services)
    {
        services.AddScoped<IWalletService, WalletService>();
        services.AddScoped<IGameSessionService, GameSessionService>();
        services.AddScoped<MakopaGameEngine>();
        services.AddScoped<KopoGameEngine>();
        services.AddScoped<IGameEngineService, GameEngineService>();
        services.AddScoped<IGameHistoryService, GameHistoryService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IGameRevenueService, GameRevenueService>();
        services.AddScoped<IRevenueShareResolver, RevenueShareResolver>();
        services.AddScoped<IPartnerRevenueAllocationService, PartnerRevenueAllocationService>();
        services.AddScoped<IPaymentRevenueService, PaymentRevenueService>();
        services.AddScoped<IInfluencerAttributionService, InfluencerAttributionService>();
        services.AddScoped<IInfluencerProgramSettingsService, InfluencerProgramSettingsService>();
        services.AddValidatorsFromAssemblyContaining<CreateGameRequestValidator>();
        return services;
    }
}
