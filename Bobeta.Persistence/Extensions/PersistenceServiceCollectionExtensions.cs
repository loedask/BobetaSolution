using Bobeta.Application.Interfaces;
using Bobeta.Persistence.Context;
using Bobeta.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Bobeta.Persistence.Extensions;

/// <summary>Registers Bobeta persistence: DbContext (PostgreSQL) and all repository implementations.</summary>
public static class PersistenceServiceCollectionExtensions
{
    /// <summary>Name used by Azure App Service flexible PostgreSQL for the DB connection string.</summary>
    public const string AzurePostgresConnectionStringName = "AZURE_POSTGRESQL_CONNECTIONSTRING";

    /// <summary>Default max pooled connections per app process (API and Portal each have their own pool).</summary>
    private const int DefaultMaxPoolSize = 20;

    /// <summary>Default connection open timeout in seconds (Npgsql default is 15).</summary>
    private const int DefaultConnectionTimeoutSeconds = 30;

    /// <summary>Adds BobetaDbContext and repository implementations; configures PostgreSQL with the given connection string.</summary>
    public static IServiceCollection AddBobetaPersistence(this IServiceCollection services, string connectionString)
    {
        var configuredConnectionString = ConfigureNpgsqlConnectionString(connectionString);

        services.AddDbContext<BobetaDbContext>(options =>
            options.UseNpgsql(configuredConnectionString, npgsql =>
                npgsql.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorCodesToAdd: null)));

        services.AddScoped<IPlayerRepository, PlayerRepository>();
        services.AddScoped<IWalletRepository, WalletRepository>();
        services.AddScoped<IWalletTransactionRepository, WalletTransactionRepository>();
        services.AddScoped<IGameSessionRepository, GameSessionRepository>();
        services.AddScoped<IGameMoveRepository, GameMoveRepository>();
        services.AddScoped<IGameResultRepository, GameResultRepository>();
        services.AddScoped<IOtpRepository, OtpRepository>();
        services.AddScoped<IPaymentTransactionRepository, PaymentTransactionRepository>();
        services.AddScoped<ISmsMessageRepository, SmsMessageRepository>();
        services.AddScoped<IPortalUserRepository, PortalUserRepository>();
        services.AddScoped<ILicensePartnerRepository, LicensePartnerRepository>();
        services.AddScoped<IInfluencerRepository, InfluencerRepository>();
        services.AddScoped<IPlatformSettingsRepository, PlatformSettingsRepository>();
        services.AddScoped<IDashboardStatsRepository, DashboardStatsRepository>();
        services.AddScoped<IPlayerNotificationRepository, PlayerNotificationRepository>();
        services.AddScoped<IPlayerDeviceTokenRepository, PlayerDeviceTokenRepository>();
        return services;
    }

    /// <summary>
    /// Applies safe pool defaults when the connection string does not already set them,
    /// so API and Portal do not each open up to Npgsql's default of 100 connections.
    /// </summary>
    internal static string ConfigureNpgsqlConnectionString(string connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);

        if (!HasConnectionStringKey(connectionString, "Maximum Pool Size", "Max Pool Size", "MaxPoolSize"))
            builder.MaxPoolSize = DefaultMaxPoolSize;

        if (!HasConnectionStringKey(connectionString, "Timeout"))
            builder.Timeout = DefaultConnectionTimeoutSeconds;

        return builder.ConnectionString;
    }

    private static bool HasConnectionStringKey(string connectionString, params string[] keys)
    {
        foreach (var part in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var eq = part.IndexOf('=');
            if (eq <= 0)
                continue;

            var key = part[..eq].Trim();
            foreach (var candidate in keys)
            {
                if (key.Equals(candidate, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        return false;
    }
}
