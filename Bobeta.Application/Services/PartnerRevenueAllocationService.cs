using Bobeta.Application.Common;
using Bobeta.Application.Interfaces;
using Bobeta.Domain.Entities;
using Bobeta.Domain.Enums;

namespace Bobeta.Application.Services;

public sealed class PartnerRevenueAllocationService(
    IPlayerRepository players,
    IRevenueShareResolver revenueShare,
    ILicensePartnerRepository licensePartners) : IPartnerRevenueAllocationService
{
  public async Task TryAllocateForPlayerAsync(
      RevenueAllocationSourceType sourceType,
      Guid sourceId,
      Guid playerId,
      decimal grossPlatformRevenue,
      string currency,
      DateTime atUtc,
      CancellationToken cancellationToken = default)
  {
    if (grossPlatformRevenue <= 0)
      return;

    if (await licensePartners.AllocationExistsAsync(sourceType, sourceId, cancellationToken))
      return;

    var player = await players.GetByIdAsync(playerId, cancellationToken);
    var countryCode = player?.CountryCode ?? CountryCatalog.ResolveCountryCodeFromPhone(player?.PhoneNumber);

    var split = await revenueShare.ResolveAsync(countryCode, grossPlatformRevenue, atUtc, cancellationToken);
    if (split.LicensePartnerId is null || split.PartnerAmount <= 0)
      return;

    await licensePartners.AddAllocationAsync(new RevenueAllocation
    {
      Id = Guid.NewGuid(),
      SourceType = sourceType,
      SourceId = sourceId,
      CountryCode = split.CountryCode ?? countryCode ?? string.Empty,
      LicensePartnerId = split.LicensePartnerId.Value,
      GrossPlatformRevenue = grossPlatformRevenue,
      PartnerSharePercent = split.PartnerSharePercent,
      PartnerAmount = split.PartnerAmount,
      InfluencerAmount = 0,
      PlatformRetainedAmount = split.PlatformRetainedAmount,
      Currency = currency,
      CreatedAt = atUtc
    }, cancellationToken);
  }

  public async Task TryAllocateGameAsync(
      Guid gameSessionId,
      Guid winnerPlayerId,
      decimal grossPlatformRevenue,
      RevenueShareSplit partnerSplit,
      decimal influencerAmount,
      decimal platformRetainedAmount,
      string? fallbackCountryCode,
      string currency,
      DateTime atUtc,
      CancellationToken cancellationToken = default)
  {
    if (grossPlatformRevenue <= 0)
      return;

    if (await licensePartners.AllocationExistsAsync(RevenueAllocationSourceType.GameCommission, gameSessionId, cancellationToken))
      return;

    if (partnerSplit.LicensePartnerId is null || partnerSplit.PartnerAmount <= 0)
      return;

    await licensePartners.AddAllocationAsync(new RevenueAllocation
    {
      Id = Guid.NewGuid(),
      SourceType = RevenueAllocationSourceType.GameCommission,
      SourceId = gameSessionId,
      CountryCode = partnerSplit.CountryCode ?? fallbackCountryCode ?? string.Empty,
      LicensePartnerId = partnerSplit.LicensePartnerId.Value,
      GrossPlatformRevenue = grossPlatformRevenue,
      PartnerSharePercent = partnerSplit.PartnerSharePercent,
      PartnerAmount = partnerSplit.PartnerAmount,
      InfluencerAmount = influencerAmount,
      PlatformRetainedAmount = platformRetainedAmount,
      Currency = currency,
      CreatedAt = atUtc
    }, cancellationToken);
  }
}
