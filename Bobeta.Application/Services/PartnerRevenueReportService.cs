using Bobeta.Application.Common;
using Bobeta.Application.DTOs.Portal;
using Bobeta.Application.Interfaces;
using Bobeta.Domain.Enums;

namespace Bobeta.Application.Services;

public sealed class PartnerRevenueReportService(
    ILicensePartnerRepository partners) : IPartnerRevenueReportService
{
  public async Task<PartnerRevenueReportDto> GetReportAsync(
      Guid licensePartnerId,
      DateTime? fromUtc = null,
      DateTime? toUtc = null,
      string? countryCode = null,
      RevenueAllocationSourceType? sourceType = null,
      CancellationToken cancellationToken = default)
  {
    var partner = await partners.GetByIdAsync(licensePartnerId, cancellationToken)
      ?? throw new InvalidOperationException("License partner not found.");

    var allocations = await partners.GetAllocationsAsync(
        licensePartnerId, fromUtc, toUtc, countryCode, sourceType, skip: 0, take: 10_000, cancellationToken);

    var byCountry = allocations
      .GroupBy(a => a.CountryCode)
      .Select(g => new PartnerRevenueBreakdownDto
      {
        Key = g.Key,
        Label = CountryCatalog.GetByCode(g.Key)?.Name ?? g.Key,
        PartnerAmount = g.Sum(x => x.PartnerAmount),
        InfluencerAmount = g.Sum(x => x.InfluencerAmount),
        GrossPlatformRevenue = g.Sum(x => x.GrossPlatformRevenue),
        TransactionCount = g.Count()
      })
      .OrderByDescending(x => x.PartnerAmount)
      .ToList();

    var bySource = allocations
      .GroupBy(a => a.SourceType)
      .Select(g => new PartnerRevenueBreakdownDto
      {
        Key = g.Key.ToString(),
        Label = FormatSourceType(g.Key),
        PartnerAmount = g.Sum(x => x.PartnerAmount),
        InfluencerAmount = g.Sum(x => x.InfluencerAmount),
        GrossPlatformRevenue = g.Sum(x => x.GrossPlatformRevenue),
        TransactionCount = g.Count()
      })
      .OrderByDescending(x => x.PartnerAmount)
      .ToList();

    var recent = allocations
      .OrderByDescending(a => a.CreatedAt)
      .Take(50)
      .Select(MapItem)
      .ToList();

    return new PartnerRevenueReportDto
    {
      LicensePartnerId = partner.Id,
      LegalName = partner.LegalName,
      From = fromUtc,
      To = toUtc,
      TotalPartnerAmount = allocations.Sum(a => a.PartnerAmount),
      TotalGrossPlatformRevenue = allocations.Sum(a => a.GrossPlatformRevenue),
      TransactionCount = allocations.Count,
      ByCountry = byCountry,
      BySource = bySource,
      RecentAllocations = recent
    };
  }

  private static PartnerRevenueAllocationItemDto MapItem(Domain.Entities.RevenueAllocation a) => new()
  {
    Id = a.Id,
    SourceType = a.SourceType,
    SourceId = a.SourceId,
    CountryCode = a.CountryCode,
    CountryName = CountryCatalog.GetByCode(a.CountryCode)?.Name ?? a.CountryCode,
    GrossPlatformRevenue = a.GrossPlatformRevenue,
    PartnerSharePercent = a.PartnerSharePercent,
    PartnerAmount = a.PartnerAmount,
    InfluencerAmount = a.InfluencerAmount,
    PlatformRetainedAmount = a.PlatformRetainedAmount,
    Currency = a.Currency,
    CreatedAt = a.CreatedAt
  };

  private static string FormatSourceType(RevenueAllocationSourceType type) => type switch
  {
    RevenueAllocationSourceType.GameCommission => "Game commission",
    RevenueAllocationSourceType.MoMoDeposit => "MoMo deposit",
    RevenueAllocationSourceType.MoMoWithdrawal => "MoMo withdrawal",
    _ => type.ToString()
  };
}
