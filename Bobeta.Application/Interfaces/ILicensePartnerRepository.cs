using Bobeta.Domain.Entities;
using Bobeta.Domain.Enums;

namespace Bobeta.Application.Interfaces;

public interface ILicensePartnerRepository
{
  Task<LicensePartner?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
  Task<LicensePartner?> GetByPortalUserIdAsync(Guid portalUserId, CancellationToken cancellationToken = default);
  Task<IReadOnlyList<LicensePartner>> GetAllAsync(CancellationToken cancellationToken = default);
  Task<LicensePartner> AddAsync(LicensePartner partner, CancellationToken cancellationToken = default);
  Task UpdateAsync(LicensePartner partner, CancellationToken cancellationToken = default);
  Task<LicensePartnerCountryAssignment?> GetActiveAssignmentForCountryAsync(string countryCode, CancellationToken cancellationToken = default);
  Task<LicensePartnerCountryAssignment?> GetAssignmentAsync(Guid partnerId, string countryCode, CancellationToken cancellationToken = default);
  Task<LicensePartnerRevenueShareRate?> GetEffectiveRateAsync(Guid assignmentId, DateTime atUtc, CancellationToken cancellationToken = default);
  Task<LicensePartnerCountryAssignment> AddAssignmentAsync(LicensePartnerCountryAssignment assignment, CancellationToken cancellationToken = default);
  Task<LicensePartnerRevenueShareRate> AddRateAsync(LicensePartnerRevenueShareRate rate, CancellationToken cancellationToken = default);
  Task CloseOpenRatesAsync(Guid assignmentId, DateTime effectiveTo, CancellationToken cancellationToken = default);
  Task<RevenueAllocation> AddAllocationAsync(RevenueAllocation allocation, CancellationToken cancellationToken = default);
  Task<bool> AllocationExistsAsync(RevenueAllocationSourceType sourceType, Guid sourceId, CancellationToken cancellationToken = default);
  Task<IReadOnlyList<RevenueAllocation>> GetAllocationsAsync(
      Guid licensePartnerId,
      DateTime? fromUtc,
      DateTime? toUtc,
      string? countryCode,
      RevenueAllocationSourceType? sourceType,
      int skip,
      int take,
      CancellationToken cancellationToken = default);
  Task<decimal> GetMaxActiveRevenueSharePercentAsync(CancellationToken cancellationToken = default);
}
