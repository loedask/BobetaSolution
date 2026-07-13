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
    IReadOnlyList<PartnerRevenueBreakdownDto> revenueByInfluencer = [];

    if (scope.ShowFinancials)
    {
      payments = await stats.GetPaymentStatsAsync(filter, cancellationToken);
      revenue = await stats.GetRevenueStatsAsync(filter, cancellationToken);
      games = await stats.GetGameStatsAsync(filter, cancellationToken);
      revenueBySource = await stats.GetRevenueBySourceAsync(filter, cancellationToken);

      if (scope.ShowPartnerLeaderboard)
        revenueByPartner = await stats.GetRevenueByPartnerAsync(filter, cancellationToken);

      if (scope.ShowInfluencerLeaderboard)
        revenueByInfluencer = await stats.GetRevenueByInfluencerAsync(filter, cancellationToken);
    }

    return new DashboardStatsDto
    {
      FromUtc = query.FromUtc,
      ToUtc = query.ToUtc,
      ShowFinancials = scope.ShowFinancials,
      ShowPartnerLeaderboard = scope.ShowPartnerLeaderboard,
      ShowInfluencerLeaderboard = scope.ShowInfluencerLeaderboard,
      Players = players,
      Payments = payments,
      Revenue = revenue,
      Games = games,
      PlayersByCountry = playersByCountry,
      RevenueByPartner = revenueByPartner,
      RevenueBySource = revenueBySource,
      RevenueByInfluencer = revenueByInfluencer
    };
  }

  public async Task<byte[]> ExportSummaryExcelAsync(
      DashboardQuery query,
      PortalUserRole role,
      Guid portalUserId,
      CancellationToken cancellationToken = default)
  {
    var dashboard = await GetDashboardAsync(query, role, portalUserId, cancellationToken);
    var metrics = BuildSummaryMetrics(dashboard);

    return ExcelReportWriter.BuildSummaryWorkbook(
      dashboard.FromUtc,
      dashboard.ToUtc,
      metrics,
      dashboard.PlayersByCountry.Select(r => (r.CountryCode, r.CountryName, r.Count)).ToList(),
      dashboard.ShowFinancials
        ? dashboard.RevenueBySource.Select(r => (r.Label, r.GrossPlatformRevenue, r.PartnerAmount, r.InfluencerAmount, r.TransactionCount)).ToList()
        : null,
      dashboard.ShowPartnerLeaderboard
        ? dashboard.RevenueByPartner.Select(r => (r.Label, r.PartnerAmount, r.InfluencerAmount, r.GrossPlatformRevenue, r.TransactionCount)).ToList()
        : null,
      dashboard.ShowInfluencerLeaderboard
        ? dashboard.RevenueByInfluencer.Select(r => (r.Label, r.InfluencerAmount, r.GrossPlatformRevenue, r.TransactionCount)).ToList()
        : null);
  }

  public async Task<byte[]> ExportPlayersExcelAsync(
      DashboardQuery query,
      PortalUserRole role,
      Guid portalUserId,
      CancellationToken cancellationToken = default)
  {
    var scope = await ResolveScopeAsync(role, portalUserId, cancellationToken);
    var filter = BuildFilter(query, scope);
    var rows = await stats.GetPlayersByCountryAsync(filter, cancellationToken);

    return ExcelReportWriter.Build(
      "Players by country",
      ["Country code", "Country", "Players"],
      rows.Select(r => new object?[] { r.CountryCode, r.CountryName, r.Count }));
  }

  public async Task<byte[]> ExportRevenueExcelAsync(
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

    return ExcelReportWriter.Build(
      "Revenue detail",
      ["Date (UTC)", "Source", "Country", "Gross revenue", "Partner rate %", "Partner amount", "Influencer amount", "Platform retained", "Currency"],
      allocations.Select(a => new object?[]
      {
        a.CreatedAt,
        FormatSourceType(a.SourceType),
        a.CountryName,
        a.GrossPlatformRevenue,
        a.PartnerSharePercent,
        a.PartnerAmount,
        a.InfluencerAmount,
        a.PlatformRetainedAmount,
        a.Currency
      }));
  }

  public async Task<byte[]> ExportPaymentsExcelAsync(
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

    return ExcelReportWriter.Build(
      "Payments",
      ["Type", "Count", "Volume"],
      new[]
      {
        new object?[] { "Deposits", payments.SuccessfulDeposits, payments.DepositVolume },
        new object?[] { "Withdrawals", payments.SuccessfulWithdrawals, payments.WithdrawalVolume }
      });
  }

  private static List<(string Category, string Metric, object? Value)> BuildSummaryMetrics(DashboardStatsDto dashboard)
  {
    var metrics = new List<(string Category, string Metric, object? Value)>
    {
      ("Players", "Total players", dashboard.Players.TotalPlayers),
      ("Players", "New players in period", dashboard.Players.NewPlayers),
      ("Players", "Verified players", dashboard.Players.VerifiedPlayers),
      ("Players", "Active players", dashboard.Players.ActivePlayers)
    };

    if (!dashboard.ShowFinancials)
      return metrics;

    metrics.AddRange(
    [
      ("Payments", "Successful deposits", dashboard.Payments.SuccessfulDeposits),
      ("Payments", "Deposit volume", dashboard.Payments.DepositVolume),
      ("Payments", "Successful withdrawals", dashboard.Payments.SuccessfulWithdrawals),
      ("Payments", "Withdrawal volume", dashboard.Payments.WithdrawalVolume),
      ("Games", "Games played", dashboard.Games.GamesPlayed),
      ("Games", "Total pot", dashboard.Games.TotalPot),
      ("Games", "Platform commission", dashboard.Games.PlatformCommission),
      ("Revenue", "Gross platform revenue", dashboard.Revenue.GrossPlatformRevenue),
      ("Revenue", "Partner share paid", dashboard.Revenue.PartnerSharePaid),
      ("Revenue", "Influencer share paid", dashboard.Revenue.InfluencerSharePaid),
      ("Revenue", "Platform retained", dashboard.Revenue.PlatformRetained),
      ("Revenue", "Revenue allocations", dashboard.Revenue.AllocationCount)
    ]);

    return metrics;
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
        ShowPartnerLeaderboard = true,
        ShowInfluencerLeaderboard = true
      },
      PortalUserRole.Member => new DashboardScope
      {
        ShowFinancials = false,
        ShowPartnerLeaderboard = false,
        ShowInfluencerLeaderboard = false
      },
      PortalUserRole.LicensePartner => new DashboardScope
      {
        ShowFinancials = true,
        ShowPartnerLeaderboard = false,
        ShowInfluencerLeaderboard = false,
        CountryCodes = await access.GetLicensedCountryCodesAsync(portalUserId, cancellationToken),
        LicensePartnerId = (await partners.GetByPortalUserIdAsync(portalUserId, cancellationToken))?.Id
      },
      PortalUserRole.Influencer => new DashboardScope
      {
        ShowFinancials = false,
        ShowPartnerLeaderboard = false,
        ShowInfluencerLeaderboard = false
      },
      _ => throw new UnauthorizedAccessException("Unknown portal role.")
    };
  }

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
    public bool ShowInfluencerLeaderboard { get; init; }
    public IReadOnlyList<string>? CountryCodes { get; init; }
    public Guid? LicensePartnerId { get; init; }
  }
}
