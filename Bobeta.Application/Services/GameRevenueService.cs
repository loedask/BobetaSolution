using Bobeta.Application.Common;
using Bobeta.Application.Interfaces;
using Bobeta.Domain.Entities;
using Bobeta.Domain.Enums;

namespace Bobeta.Application.Services;

/// <summary>Applies license partner revenue share to a finished game and records the allocation ledger entry.</summary>
public sealed class GameRevenueService(
    IPlayerRepository players,
    IRevenueShareResolver revenueShare,
    ILicensePartnerRepository licensePartners) : IGameRevenueService
{
  public async Task EnrichWithPartnerShareAsync(GameResult result, Guid winnerPlayerId, CancellationToken cancellationToken = default)
  {
    var player = await players.GetByIdAsync(winnerPlayerId, cancellationToken);
    var countryCode = player?.CountryCode ?? CountryCatalog.ResolveCountryCodeFromPhone(player?.PhoneNumber);

    var split = await revenueShare.ResolveAsync(countryCode, result.PlatformCommission, result.CreatedAt, cancellationToken);
    result.PartnerCommission = split.PartnerAmount;
    result.LicensePartnerId = split.LicensePartnerId;

    if (split.LicensePartnerId is null || split.PartnerAmount <= 0)
      return;

    await licensePartners.AddAllocationAsync(new RevenueAllocation
    {
      Id = Guid.NewGuid(),
      SourceType = RevenueAllocationSourceType.GameCommission,
      SourceId = result.GameSessionId,
      CountryCode = split.CountryCode ?? countryCode ?? string.Empty,
      LicensePartnerId = split.LicensePartnerId.Value,
      GrossPlatformRevenue = result.PlatformCommission,
      PartnerSharePercent = split.PartnerSharePercent,
      PartnerAmount = split.PartnerAmount,
      PlatformRetainedAmount = split.PlatformRetainedAmount,
      Currency = "XAF",
      CreatedAt = result.CreatedAt
    }, cancellationToken);
  }
}
