using Bobeta.Application.Interfaces;
using Bobeta.Application.Services;
using Bobeta.Application.Validators;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Bobeta.Application.Extensions;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddBobetaApplication(this IServiceCollection services)
    {
        services.AddScoped<IWalletService, WalletService>();
        services.AddScoped<IGameSessionService, GameSessionService>();
        services.AddScoped<IGameEngineService, GameEngineService>();
        services.AddScoped<IGameHistoryService, GameHistoryService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddValidatorsFromAssemblyContaining<CreateGameRequestValidator>();
        return services;
    }
}
