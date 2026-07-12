using Bobeta.Application.Configuration;
using Bobeta.Application.Interfaces;
using Bobeta.Domain.Enums;
using Microsoft.Extensions.Options;

namespace Bobeta.Application.Services;

public sealed class PaymentRevenueService(
    IOptions<PaymentRevenueSettings> settings,
    IPartnerRevenueAllocationService allocations) : IPaymentRevenueService
{
  public async Task RecordSuccessfulPaymentAsync(
      PaymentTransactionType paymentType,
      Guid paymentTransactionId,
      Guid playerId,
      decimal amount,
      string currency,
      DateTime atUtc,
      CancellationToken cancellationToken = default)
  {
    var config = settings.Value;
    var feePercent = paymentType == PaymentTransactionType.Deposit
        ? config.DepositFeePercent
        : config.WithdrawalFeePercent;

    if (feePercent <= 0)
      return;

    var grossRevenue = Math.Round(amount * feePercent / 100m, 2, MidpointRounding.AwayFromZero);
    var sourceType = paymentType == PaymentTransactionType.Deposit
        ? RevenueAllocationSourceType.MoMoDeposit
        : RevenueAllocationSourceType.MoMoWithdrawal;

    await allocations.TryAllocateForPlayerAsync(
        sourceType,
        paymentTransactionId,
        playerId,
        grossRevenue,
        currency,
        atUtc,
        cancellationToken);
  }
}
