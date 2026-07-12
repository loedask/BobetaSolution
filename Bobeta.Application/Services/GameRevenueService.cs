using Bobeta.Application.Interfaces;
using Bobeta.Domain.Entities;
using Bobeta.Domain.Enums;

namespace Bobeta.Application.Services;

public sealed class GameRevenueService(
    IPlayerRepository players,
    IRevenueShareResolver revenueShare,
    IPartnerRevenueAllocationService allocations,
    IInfluencerRepository influencers,
    IInfluencerAttributionService attribution) : IGameRevenueService
{
  public async Task EnrichWithPartnerShareAsync(GameResult result, Guid winnerPlayerId, CancellationToken cancellationToken = default)
  {
    var gross = result.PlatformCommission;
    var player = await players.GetByIdAsync(winnerPlayerId, cancellationToken);
    var countryCode = player?.CountryCode ?? Common.CountryCatalog.ResolveCountryCodeFromPhone(player?.PhoneNumber);
    var partnerSplit = await revenueShare.ResolveAsync(countryCode, gross, result.CreatedAt, cancellationToken);

    var influencerTotal = await AllocateInfluencerSharesAsync(result.GameSessionId, gross, result.CreatedAt, cancellationToken);

    result.PartnerCommission = partnerSplit.PartnerAmount;
    result.LicensePartnerId = partnerSplit.LicensePartnerId;
    result.InfluencerCommission = influencerTotal;

    var platformRetained = Math.Max(0, gross - partnerSplit.PartnerAmount - influencerTotal);

    await allocations.TryAllocateGameAsync(
        result.GameSessionId,
        winnerPlayerId,
        gross,
        partnerSplit,
        influencerTotal,
        platformRetained,
        countryCode,
        "XAF",
        result.CreatedAt,
        cancellationToken);

    await attribution.MarkGameRedemptionsConsumedAsync(result.GameSessionId, result.CreatedAt, cancellationToken);
  }

  private async Task<decimal> AllocateInfluencerSharesAsync(
      Guid gameSessionId,
      decimal grossPlatformRevenue,
      DateTime atUtc,
      CancellationToken cancellationToken)
  {
    if (await influencers.CommissionAllocationExistsAsync(gameSessionId, cancellationToken))
      return 0;

    var redemptions = await influencers.GetRedemptionsForGameAsync(gameSessionId, cancellationToken);
    if (redemptions.Count == 0)
      return 0;

    var attributionBase = Math.Round(grossPlatformRevenue / 2m, 2, MidpointRounding.AwayFromZero);
    decimal total = 0;

    foreach (var redemption in redemptions)
    {
      var influencer = redemption.Influencer
        ?? await influencers.GetByIdAsync(redemption.InfluencerId, cancellationToken);
      if (influencer is null || !influencer.IsActive || influencer.CommissionPercent <= 0)
        continue;

      var amount = Math.Round(
          attributionBase * influencer.CommissionPercent / 100m,
          2,
          MidpointRounding.AwayFromZero);

      if (amount <= 0)
        continue;

      await influencers.AddCommissionAllocationAsync(new InfluencerCommissionAllocation
      {
        Id = Guid.NewGuid(),
        GameSessionId = gameSessionId,
        InfluencerId = influencer.Id,
        PlayerId = redemption.PlayerId,
        GrossPlatformRevenue = grossPlatformRevenue,
        AttributionBase = attributionBase,
        CommissionPercent = influencer.CommissionPercent,
        InfluencerAmount = amount,
        Currency = "XAF",
        CreatedAt = atUtc
      }, cancellationToken);

      total += amount;
    }

    return total;
  }
}
