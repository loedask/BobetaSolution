using Bobeta.Application.DTOs.Portal;
using Bobeta.Domain.Enums;

namespace Bobeta.Application.Interfaces;

public interface IDashboardService
{
  Task<DashboardStatsDto> GetDashboardAsync(
      DashboardQuery query,
      PortalUserRole role,
      Guid portalUserId,
      CancellationToken cancellationToken = default);

  Task<string> ExportSummaryCsvAsync(
      DashboardQuery query,
      PortalUserRole role,
      Guid portalUserId,
      CancellationToken cancellationToken = default);

  Task<string> ExportPlayersCsvAsync(
      DashboardQuery query,
      PortalUserRole role,
      Guid portalUserId,
      CancellationToken cancellationToken = default);

  Task<string> ExportRevenueCsvAsync(
      DashboardQuery query,
      PortalUserRole role,
      Guid portalUserId,
      CancellationToken cancellationToken = default);

  Task<string> ExportPaymentsCsvAsync(
      DashboardQuery query,
      PortalUserRole role,
      Guid portalUserId,
      CancellationToken cancellationToken = default);
}
