using Bobeta.Application.Common;
using Bobeta.Persistence.Context;
using Bobeta.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;

namespace Bobeta.API.App.Extensions;

/// <summary>
/// Applies pending EF Core migrations at host startup (Development and Staging only).
/// </summary>
public static class DatabaseMigrationExtensions
{
    /// <summary>
    /// Applies pending migrations and demo seed data. Runs only in Development or Staging.
    /// </summary>
    public static async Task ApplyMigrationsAsync(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;

        var env = services.GetRequiredService<IWebHostEnvironment>();

        if (!DemoEnvironmentHelper.AllowsDemoAuthFeatures(env))
            return;

        var context = services.GetRequiredService<BobetaDbContext>();
        await context.Database.MigrateAsync();
        await DemoAccountsSeeder.SeedAsync(context);
    }
}
