using Bobeta.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Bobeta.Infrastructure.Payments;

/// <summary>Background worker that polls MoMo for pending payment transaction status every 60 seconds and updates wallet when completed.</summary>
public class PaymentStatusWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PaymentStatusWorker> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(60);

    public PaymentStatusWorker(IServiceScopeFactory scopeFactory, ILogger<PaymentStatusWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PaymentStatusWorker started; polling every {Seconds} seconds.", Interval.TotalSeconds);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollPendingPaymentsAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PaymentStatusWorker error during poll.");
            }

            await Task.Delay(Interval, stoppingToken);
        }
        _logger.LogInformation("PaymentStatusWorker stopped.");
    }

    private async Task PollPendingPaymentsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var paymentRepository = scope.ServiceProvider.GetRequiredService<IPaymentTransactionRepository>();
        var paymentService = scope.ServiceProvider.GetRequiredService<IPaymentService>();

        var pendingIds = await paymentRepository.GetPendingTransactionIdsAsync(cancellationToken);
        if (pendingIds.Count == 0)
            return;

        _logger.LogDebug("PaymentStatusWorker: checking {Count} pending transaction(s).", pendingIds.Count);
        foreach (var id in pendingIds)
        {
            try
            {
                await paymentService.CheckTransactionStatusAsync(id, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "PaymentStatusWorker: failed to check status for TransactionId={TransactionId}.", id);
            }
        }
    }
}
