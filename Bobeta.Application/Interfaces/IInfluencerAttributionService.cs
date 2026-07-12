using Bobeta.Application.DTOs.Influencer;

namespace Bobeta.Application.Interfaces;

public interface IInfluencerAttributionService
{
  Task ApplyCodeAsync(Guid playerId, string code, CancellationToken cancellationToken = default);
  Task<InfluencerCodeStatusDto> GetStatusAsync(Guid playerId, CancellationToken cancellationToken = default);
  /// <summary>Returns the wallet lock amount (discounted when a pending code exists).</summary>
  Task<decimal> GetChargeAmountAsync(Guid playerId, decimal betAmount, CancellationToken cancellationToken = default);
  /// <summary>Attaches a pending code to a persisted game session.</summary>
  Task AttachPendingCodeToGameAsync(Guid playerId, Guid gameSessionId, CancellationToken cancellationToken = default);
  Task MarkGameRedemptionsConsumedAsync(Guid gameSessionId, DateTime atUtc, CancellationToken cancellationToken = default);
  Task DetachGameRedemptionsAsync(Guid gameSessionId, CancellationToken cancellationToken = default);
}
