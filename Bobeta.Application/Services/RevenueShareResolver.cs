using Bobeta.Application.Interfaces;

namespace Bobeta.Application.Services;

/// <summary>Resolves license partner revenue share for a country at a point in time.</summary>
public sealed class RevenueShareResolver(ILicensePartnerRepository partners) : IRevenueShareResolver
{
  public async Task<RevenueShareSplit> ResolveAsync(
      string? countryCode,
      decimal grossPlatformRevenue,
      DateTime atUtc,
      CancellationToken cancellationToken = default)
  {
    if (string.IsNullOrWhiteSpace(countryCode) || grossPlatformRevenue <= 0)
      return new RevenueShareSplit(null, countryCode, 0, 0, grossPlatformRevenue);

    var assignment = await partners.GetActiveAssignmentForCountryAsync(countryCode, cancellationToken);
    if (assignment is null)
      return new RevenueShareSplit(null, countryCode.Trim().ToUpperInvariant(), 0, 0, grossPlatformRevenue);

    var rate = await partners.GetEffectiveRateAsync(assignment.Id, atUtc, cancellationToken);
    if (rate is null || rate.RevenueSharePercent <= 0)
      return new RevenueShareSplit(assignment.LicensePartnerId, assignment.CountryCode, 0, 0, grossPlatformRevenue);

    var percent = rate.RevenueSharePercent;
    var partnerAmount = Math.Round(grossPlatformRevenue * percent / 100m, 2, MidpointRounding.AwayFromZero);
    var platformRetained = grossPlatformRevenue - partnerAmount;

    return new RevenueShareSplit(
        assignment.LicensePartnerId,
        assignment.CountryCode,
        percent,
        partnerAmount,
        platformRetained);
  }
}
