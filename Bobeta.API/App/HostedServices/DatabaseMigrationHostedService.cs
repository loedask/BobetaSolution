using Bobeta.API.App.Extensions;

namespace Bobeta.API.App.HostedServices;

/// <summary>
/// Applies EF Core migrations after Kestrel starts so OPTIONS/CORS and health checks are not blocked by DB connect timeouts.
/// </summary>
internal sealed class DatabaseMigrationHostedService(IHost host, ILogger<DatabaseMigrationHostedService> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = RunMigrationsAsync(cancellationToken);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task RunMigrationsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await host.ApplyMigrationsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Database migration failed on startup");
        }
    }
}
