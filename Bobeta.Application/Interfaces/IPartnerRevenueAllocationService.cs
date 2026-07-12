using Bobeta.Domain.Enums;

namespace Bobeta.Application.Interfaces;

public interface IPartnerRevenueAllocationService
{
  Task TryAllocateForPlayerAsync(
      RevenueAllocationSourceType sourceType,
      Guid sourceId,
      Guid playerId,
      decimal grossPlatformRevenue,
      string currency,
      DateTime atUtc,
      CancellationToken cancellationToken = default);
}
