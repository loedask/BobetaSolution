using Bobeta.Application.DTOs.Influencer;
using Bobeta.Application.Interfaces;
using Bobeta.Domain.Entities;

namespace Bobeta.Application.Services;

public sealed class InfluencerAttributionService(
    IInfluencerRepository influencers,
    IInfluencerProgramSettingsService settings) : IInfluencerAttributionService
{
  public async Task ApplyCodeAsync(Guid playerId, string code, CancellationToken cancellationToken = default)
  {
    if (string.IsNullOrWhiteSpace(code))
      throw new InvalidOperationException("Invite code is required.");

    var influencer = await influencers.GetByCodeAsync(code, cancellationToken)
      ?? throw new InvalidOperationException("Invalid or inactive invite code.");

    var existing = await influencers.GetRedemptionAsync(influencer.Id, playerId, cancellationToken);
    if (existing is not null)
      throw new InvalidOperationException("You have already used this invite code.");

    var pending = await influencers.GetPendingRedemptionForPlayerAsync(playerId, cancellationToken);
    if (pending is not null)
      throw new InvalidOperationException("You already have an unused invite code. Play a game with it first.");

    await influencers.AddRedemptionAsync(new InfluencerCodeRedemption
    {
      Id = Guid.NewGuid(),
      InfluencerId = influencer.Id,
      PlayerId = playerId,
      Code = influencer.Code,
      AppliedAt = DateTime.UtcNow
    }, cancellationToken);
  }

  public async Task<InfluencerCodeStatusDto> GetStatusAsync(Guid playerId, CancellationToken cancellationToken = default)
  {
    var pending = await influencers.GetPendingRedemptionForPlayerAsync(playerId, cancellationToken);
    var discount = await settings.GetPlayerDiscountPercentAsync(cancellationToken);
    if (pending is null)
      return new InfluencerCodeStatusDto(false, null, null, discount);

    return new InfluencerCodeStatusDto(
        true,
        pending.Code,
        pending.Influencer?.DisplayName,
        discount);
  }

  public async Task<decimal> GetChargeAmountAsync(Guid playerId, decimal betAmount, CancellationToken cancellationToken = default)
  {
    var pending = await influencers.GetPendingRedemptionForPlayerAsync(playerId, cancellationToken);
    if (pending is null)
      return betAmount;

    var discountPercent = await settings.GetPlayerDiscountPercentAsync(cancellationToken);
    if (discountPercent <= 0)
      return betAmount;

    return Math.Round(betAmount * (100m - discountPercent) / 100m, 2, MidpointRounding.AwayFromZero);
  }

  public async Task AttachPendingCodeToGameAsync(Guid playerId, Guid gameSessionId, CancellationToken cancellationToken = default)
  {
    var pending = await influencers.GetPendingRedemptionForPlayerAsync(playerId, cancellationToken);
    if (pending is null)
      return;

    pending.GameSessionId = gameSessionId;
    pending.AttachedAt = DateTime.UtcNow;
    await influencers.UpdateRedemptionAsync(pending, cancellationToken);
  }

  public async Task MarkGameRedemptionsConsumedAsync(Guid gameSessionId, DateTime atUtc, CancellationToken cancellationToken = default)
  {
    var redemptions = await influencers.GetRedemptionsForGameAsync(gameSessionId, cancellationToken);
    foreach (var redemption in redemptions.Where(r => r.ConsumedAt is null))
    {
      redemption.ConsumedAt = atUtc;
      await influencers.UpdateRedemptionAsync(redemption, cancellationToken);
    }
  }

  public Task DetachGameRedemptionsAsync(Guid gameSessionId, CancellationToken cancellationToken = default) =>
    influencers.DetachRedemptionsFromGameAsync(gameSessionId, cancellationToken);
}
