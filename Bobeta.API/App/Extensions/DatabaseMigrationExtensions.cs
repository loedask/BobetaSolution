using Bobeta.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Bobeta.API.App.Extensions;

/// <summary>
/// Applies pending EF Core migrations at host startup (Development only).
/// </summary>
public static class DatabaseMigrationExtensions
{
    /// <summary>
    /// Applies pending migrations for all registered DbContexts. Runs only in Development.
    /// </summary>
    public static async Task ApplyMigrationsAsync(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;

        var env = services.GetRequiredService<IWebHostEnvironment>();

        if (!env.IsDevelopment())
            return;

        var context = services.GetRequiredService<BobetaDbContext>();
        await context.Database.MigrateAsync();
    }
}
