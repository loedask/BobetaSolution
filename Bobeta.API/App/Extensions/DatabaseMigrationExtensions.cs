using Bobeta.Application.Common;
using Bobeta.Persistence.Context;
using Bobeta.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;

namespace Bobeta.API.App.Extensions;

/// <summary>
/// Applies pending EF Core migrations at host startup in all environments.
/// </summary>
public static class DatabaseMigrationExtensions
{
    /// <summary>
    /// Applies pending migrations on every startup (including Production).
    /// Demo seed data runs only in Development or Staging.
    /// </summary>
    public static async Task ApplyMigrationsAsync(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;

        var env = services.GetRequiredService<IWebHostEnvironment>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseMigration");

        var context = services.GetRequiredService<BobetaDbContext>();

        logger.LogInformation("Applying pending database migrations (Environment={Environment})", env.EnvironmentName);
        await context.Database.MigrateAsync();
        logger.LogInformation("Database migrations applied successfully");

        if (DemoEnvironmentHelper.AllowsDemoAuthFeatures(env))
        {
            logger.LogInformation("Seeding demo accounts (Development/Staging only)");
            await DemoAccountsSeeder.SeedAsync(context);
        }
    }
}
