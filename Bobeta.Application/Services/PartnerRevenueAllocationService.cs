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
      PlatformRetainedAmount = split.PlatformRetainedAmount,
      Currency = currency,
      CreatedAt = atUtc
    }, cancellationToken);
  }
}
