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

  Task<byte[]> ExportSummaryExcelAsync(
      DashboardQuery query,
      PortalUserRole role,
      Guid portalUserId,
      CancellationToken cancellationToken = default);

  Task<byte[]> ExportPlayersExcelAsync(
      DashboardQuery query,
      PortalUserRole role,
      Guid portalUserId,
      CancellationToken cancellationToken = default);

  Task<byte[]> ExportRevenueExcelAsync(
      DashboardQuery query,
      PortalUserRole role,
      Guid portalUserId,
      CancellationToken cancellationToken = default);

  Task<byte[]> ExportPaymentsExcelAsync(
      DashboardQuery query,
      PortalUserRole role,
      Guid portalUserId,
      CancellationToken cancellationToken = default);
}
