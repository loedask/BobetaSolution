using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Bobeta.API.Services;

/// <summary>Periodic tick for AFK idle detection and synchronized warning deadlines.</summary>
public sealed class GameInactivityHostedService(IGameInactivityCoordinator coordinator, ILogger<GameInactivityHostedService> logger)
    : BackgroundService
{
    private readonly IGameInactivityCoordinator _coordinator = coordinator;
    private readonly ILogger<GameInactivityHostedService> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
            while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
                await _coordinator.TickAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Game inactivity hosted service failed.");
            throw;
        }
    }
}
