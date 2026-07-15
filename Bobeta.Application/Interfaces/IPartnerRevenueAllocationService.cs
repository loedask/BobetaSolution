using Bobeta.Domain.Enums;

namespace Bobeta.Application.Interfaces;

public interface IPartnerRevenueAllocationService
{
  /// <summary>Allocates partner share for MoMo fees (no influencer cut).</summary>
  Task TryAllocateForPlayerAsync(
      RevenueAllocationSourceType sourceType,
      Guid sourceId,
      Guid playerId,
      decimal grossPlatformRevenue,
      string currency,
      DateTime atUtc,
      CancellationToken cancellationToken = default);

  /// <summary>Allocates partner share for a finished game, including influencer total in retained amount.</summary>
  Task TryAllocateGameAsync(
      Guid gameSessionId,
      Guid winnerPlayerId,
      decimal grossPlatformRevenue,
      RevenueShareSplit partnerSplit,
      decimal influencerAmount,
      decimal platformRetainedAmount,
      string? fallbackCountryCode,
      string currency,
      DateTime atUtc,
      CancellationToken cancellationToken = default);
}
