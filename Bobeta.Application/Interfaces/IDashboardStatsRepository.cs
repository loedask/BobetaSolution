using Bobeta.Application.DTOs.Portal;

namespace Bobeta.Application.Interfaces;

public sealed class DashboardStatsFilter
{
  public DateTime? FromUtc { get; init; }
  public DateTime? ToUtc { get; init; }
  public IReadOnlyList<string>? CountryCodes { get; init; }
  public Guid? LicensePartnerId { get; init; }
}

public interface IDashboardStatsRepository
{
  Task<PlayerDashboardStatsDto> GetPlayerStatsAsync(DashboardStatsFilter filter, CancellationToken cancellationToken = default);
  Task<IReadOnlyList<CountryCountDto>> GetPlayersByCountryAsync(DashboardStatsFilter filter, CancellationToken cancellationToken = default);
  Task<PaymentDashboardStatsDto> GetPaymentStatsAsync(DashboardStatsFilter filter, CancellationToken cancellationToken = default);
  Task<RevenueDashboardStatsDto> GetRevenueStatsAsync(DashboardStatsFilter filter, CancellationToken cancellationToken = default);
  Task<GameDashboardStatsDto> GetGameStatsAsync(DashboardStatsFilter filter, CancellationToken cancellationToken = default);
  Task<IReadOnlyList<PartnerRevenueBreakdownDto>> GetRevenueByPartnerAsync(DashboardStatsFilter filter, CancellationToken cancellationToken = default);
  Task<IReadOnlyList<PartnerRevenueBreakdownDto>> GetRevenueBySourceAsync(DashboardStatsFilter filter, CancellationToken cancellationToken = default);
  Task<IReadOnlyList<PartnerRevenueAllocationItemDto>> GetRevenueAllocationsAsync(DashboardStatsFilter filter, int take, CancellationToken cancellationToken = default);
}
