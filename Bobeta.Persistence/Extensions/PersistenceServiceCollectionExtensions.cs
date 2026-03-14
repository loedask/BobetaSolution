using Bobeta.Application.Interfaces;
using Bobeta.Persistence.Context;
using Bobeta.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Bobeta.Persistence.Extensions;

/// <summary>Registers Bobeta persistence: DbContext (PostgreSQL) and all repository implementations.</summary>
public static class PersistenceServiceCollectionExtensions
{
    /// <summary>Adds BobetaDbContext and repository implementations; configures PostgreSQL with the given connection string.</summary>
    public static IServiceCollection AddBobetaPersistence(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<BobetaDbContext>(options =>
            options.UseNpgsql(connectionString));
        services.AddScoped<IPlayerRepository, PlayerRepository>();
        services.AddScoped<IWalletRepository, WalletRepository>();
        services.AddScoped<IWalletTransactionRepository, WalletTransactionRepository>();
        services.AddScoped<IGameSessionRepository, GameSessionRepository>();
        services.AddScoped<IGameMoveRepository, GameMoveRepository>();
        services.AddScoped<IGameResultRepository, GameResultRepository>();
        services.AddScoped<IOtpRepository, OtpRepository>();
        return services;
    }
}
