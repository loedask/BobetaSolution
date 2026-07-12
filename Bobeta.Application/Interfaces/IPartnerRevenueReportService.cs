using Bobeta.Application.DTOs.Portal;
using Bobeta.Domain.Enums;

namespace Bobeta.Application.Interfaces;

public interface IPartnerRevenueReportService
{
  Task<PartnerRevenueReportDto> GetReportAsync(
      Guid licensePartnerId,
      DateTime? fromUtc = null,
      DateTime? toUtc = null,
      string? countryCode = null,
      RevenueAllocationSourceType? sourceType = null,
      CancellationToken cancellationToken = default);
}
