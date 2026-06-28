namespace Bobeta.Application.Interfaces;

public sealed record RevenueShareSplit(
    Guid? LicensePartnerId,
    string? CountryCode,
    decimal PartnerSharePercent,
    decimal PartnerAmount,
    decimal PlatformRetainedAmount);

public interface IRevenueShareResolver
{
  Task<RevenueShareSplit> ResolveAsync(
      string? countryCode,
      decimal grossPlatformRevenue,
      DateTime atUtc,
      CancellationToken cancellationToken = default);
}
