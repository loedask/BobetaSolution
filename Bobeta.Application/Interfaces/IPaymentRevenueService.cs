using Bobeta.Domain.Enums;

namespace Bobeta.Application.Interfaces;

public interface IPaymentRevenueService
{
  Task RecordSuccessfulPaymentAsync(
      PaymentTransactionType paymentType,
      Guid paymentTransactionId,
      Guid playerId,
      decimal amount,
      string currency,
      DateTime atUtc,
      CancellationToken cancellationToken = default);
}
