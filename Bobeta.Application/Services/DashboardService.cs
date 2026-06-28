using Bobeta.Application.Common;
using Bobeta.Application.DTOs.Portal;
using Bobeta.Application.Interfaces;
using Bobeta.Domain.Enums;

namespace Bobeta.Application.Services;

public sealed class DashboardService(
    IDashboardStatsRepository stats,
    ILicensePartnerRepository partners,
    ILicensePartnerAccessService access) : IDashboardService
{
  public async Task<DashboardStatsDto> GetDashboardAsync(
      DashboardQuery query,
      PortalUserRole role,
      Guid portalUserId,
      CancellationToken cancellationToken = default)
  {
    var scope = await ResolveScopeAsync(role, portalUserId, cancellationToken);
    var filter = BuildFilter(query, scope);

    var players = await stats.GetPlayerStatsAsync(filter, cancellationToken);
    var playersByCountry = await stats.GetPlayersByCountryAsync(filter, cancellationToken);

    PaymentDashboardStatsDto payments = new();
    RevenueDashboardStatsDto revenue = new();
    GameDashboardStatsDto games = new();
    IReadOnlyList<PartnerRevenueBreakdownDto> revenueByPartner = [];
    IReadOnlyList<PartnerRevenueBreakdownDto> revenueBySource = [];

    if (scope.ShowFinancials)
    {
      payments = await stats.GetPaymentStatsAsync(filter, cancellationToken);
      revenue = await stats.GetRevenueStatsAsync(filter, cancellationToken);
      games = await stats.GetGameStatsAsync(filter, cancellationToken);
      revenueBySource = await stats.GetRevenueBySourceAsync(filter, cancellationToken);

      if (scope.ShowPartnerLeaderboard)
        revenueByPartner = await stats.GetRevenueByPartnerAsync(filter, cancellationToken);
    }

    return new DashboardStatsDto
    {
      FromUtc = query.FromUtc,
      ToUtc = query.ToUtc,
      ShowFinancials = scope.ShowFinancials,
      ShowPartnerLeaderboard = scope.ShowPartnerLeaderboard,
      Players = players,
      Payments = payments,
      Revenue = revenue,
      Games = games,
      PlayersByCountry = playersByCountry,
      RevenueByPartner = revenueByPartner,
      RevenueBySource = revenueBySource
    };
  }

  public async Task<string> ExportSummaryCsvAsync(
      DashboardQuery query,
      PortalUserRole role,
      Guid portalUserId,
      CancellationToken cancellationToken = default)
  {
    var dashboard = await GetDashboardAsync(query, role, portalUserId, cancellationToken);
    var rows = new List<string[]>
    {
      new[] { "Metric", "Value" },
      new[] { "Period from (UTC)", FormatDate(dashboard.FromUtc) },
      new[] { "Period to (UTC)", FormatDate(dashboard.ToUtc) },
      new[] { "Total players", dashboard.Players.TotalPlayers.ToString() },
      new[] { "New players", dashboard.Players.NewPlayers.ToString() },
      new[] { "Verified players", dashboard.Players.VerifiedPlayers.ToString() },
      new[] { "Active players", dashboard.Players.ActivePlayers.ToString() }
    };

    if (dashboard.ShowFinancials)
    {
      rows.AddRange(new[]
      {
        new[] { "Successful deposits", dashboard.Payments.SuccessfulDeposits.ToString() },
        new[] { "Deposit volume", dashboard.Payments.DepositVolume.ToString("N2") },
        new[] { "Successful withdrawals", dashboard.Payments.SuccessfulWithdrawals.ToString() },
        new[] { "Withdrawal volume", dashboard.Payments.WithdrawalVolume.ToString("N2") },
        new[] { "Games played", dashboard.Games.GamesPlayed.ToString() },
        new[] { "Total pot", dashboard.Games.TotalPot.ToString("N2") },
        new[] { "Platform commission", dashboard.Games.PlatformCommission.ToString("N2") },
        new[] { "Gross platform revenue", dashboard.Revenue.GrossPlatformRevenue.ToString("N2") },
        new[] { "Partner share paid", dashboard.Revenue.PartnerSharePaid.ToString("N2") },
        new[] { "Platform retained", dashboard.Revenue.PlatformRetained.ToString("N2") },
        new[] { "Revenue allocations", dashboard.Revenue.AllocationCount.ToString() }
      });
    }

    return CsvBuilder.Build(rows);
  }

  public async Task<string> ExportPlayersCsvAsync(
      DashboardQuery query,
      PortalUserRole role,
      Guid portalUserId,
      CancellationToken cancellationToken = default)
  {
    var scope = await ResolveScopeAsync(role, portalUserId, cancellationToken);
    var filter = BuildFilter(query, scope);
    var rows = await stats.GetPlayersByCountryAsync(filter, cancellationToken);

    var csvRows = new List<string[]> { new[] { "Country code", "Country", "Players" } };
    csvRows.AddRange(rows.Select(r => new[] { r.CountryCode, r.CountryName, r.Count.ToString() }));
    return CsvBuilder.Build(csvRows);
  }

  public async Task<string> ExportRevenueCsvAsync(
      DashboardQuery query,
      PortalUserRole role,
      Guid portalUserId,
      CancellationToken cancellationToken = default)
  {
    var scope = await ResolveScopeAsync(role, portalUserId, cancellationToken);
    if (!scope.ShowFinancials)
      throw new UnauthorizedAccessException("Revenue reports are not available for this role.");

    var filter = BuildFilter(query, scope);
    var allocations = await stats.GetRevenueAllocationsAsync(filter, take: 10_000, cancellationToken);

    var csvRows = new List<string[]>
    {
      new[] { "Date (UTC)", "Source", "Country", "Gross revenue", "Partner rate %", "Partner amount", "Currency" }
    };

    csvRows.AddRange(allocations.Select(a => new[]
    {
      a.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
      FormatSourceType(a.SourceType),
      a.CountryName,
      a.GrossPlatformRevenue.ToString("N2"),
      a.PartnerSharePercent.ToString("N2"),
      a.PartnerAmount.ToString("N2"),
      a.Currency
    }));

    return CsvBuilder.Build(csvRows);
  }

  public async Task<string> ExportPaymentsCsvAsync(
      DashboardQuery query,
      PortalUserRole role,
      Guid portalUserId,
      CancellationToken cancellationToken = default)
  {
    var scope = await ResolveScopeAsync(role, portalUserId, cancellationToken);
    if (!scope.ShowFinancials)
      throw new UnauthorizedAccessException("Payment reports are not available for this role.");

    var filter = BuildFilter(query, scope);
    var payments = await stats.GetPaymentStatsAsync(filter, cancellationToken);

    return CsvBuilder.Build(new[]
    {
      new[] { "Type", "Count", "Volume" },
      new[] { "Deposits", payments.SuccessfulDeposits.ToString(), payments.DepositVolume.ToString("N2") },
      new[] { "Withdrawals", payments.SuccessfulWithdrawals.ToString(), payments.WithdrawalVolume.ToString("N2") }
    });
  }

  private static DashboardStatsFilter BuildFilter(DashboardQuery query, DashboardScope scope) => new()
  {
    FromUtc = query.FromUtc,
    ToUtc = query.ToUtc,
    CountryCodes = scope.CountryCodes,
    LicensePartnerId = scope.LicensePartnerId
  };

  private async Task<DashboardScope> ResolveScopeAsync(
      PortalUserRole role,
      Guid portalUserId,
      CancellationToken cancellationToken)
  {
    return role switch
    {
      PortalUserRole.PlatformOwner => new DashboardScope
      {
        ShowFinancials = true,
        ShowPartnerLeaderboard = true
      },
      PortalUserRole.Member => new DashboardScope
      {
        ShowFinancials = false,
        ShowPartnerLeaderboard = false
      },
      PortalUserRole.LicensePartner => new DashboardScope
      {
        ShowFinancials = true,
        ShowPartnerLeaderboard = false,
        CountryCodes = await access.GetLicensedCountryCodesAsync(portalUserId, cancellationToken),
        LicensePartnerId = (await partners.GetByPortalUserIdAsync(portalUserId, cancellationToken))?.Id
      },
      _ => throw new UnauthorizedAccessException("Unknown portal role.")
    };
  }

  private static string FormatDate(DateTime? value) =>
    value?.ToString("yyyy-MM-dd") ?? string.Empty;

  private static string FormatSourceType(RevenueAllocationSourceType type) => type switch
  {
    RevenueAllocationSourceType.GameCommission => "Game commission",
    RevenueAllocationSourceType.MoMoDeposit => "MoMo deposit",
    RevenueAllocationSourceType.MoMoWithdrawal => "MoMo withdrawal",
    _ => type.ToString()
  };

  private sealed class DashboardScope
  {
    public bool ShowFinancials { get; init; }
    public bool ShowPartnerLeaderboard { get; init; }
    public IReadOnlyList<string>? CountryCodes { get; init; }
    public Guid? LicensePartnerId { get; init; }
  }
}
