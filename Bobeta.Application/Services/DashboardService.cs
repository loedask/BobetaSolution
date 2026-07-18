using Bobeta.Application.Common;
using Bobeta.Application.DTOs.Portal;
using Bobeta.Application.Interfaces;
using Bobeta.Domain.Enums;

namespace Bobeta.Application.Services;

public sealed class DashboardService(
    IDashboardStatsRepository stats,
    ILicensePartnerRepository partners,
    IInfluencerRepository influencers,
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
      if (scope.ShowPayments)
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
      ShowPayments = scope.ShowPayments,
      ShowPartnerLeaderboard = scope.ShowPartnerLeaderboard,
      ShowInfluencerLeaderboard = scope.ShowInfluencerLeaderboard,
      IsInfluencerScoped = scope.IsInfluencerScoped,
      IsPartnerScoped = scope.IsPartnerScoped,
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

    if (scope.IsInfluencerScoped)
    {
      return ExcelReportWriter.Build(
        "Revenue detail",
        ["Date (UTC)", "Source", "Country", "Gross game fee", "Your share %", "Your amount", "Currency"],
        allocations.Select(a => new object?[]
        {
          a.CreatedAt,
          FormatSourceType(a.SourceType),
          a.CountryName,
          a.GrossPlatformRevenue,
          a.PartnerSharePercent,
          a.InfluencerAmount,
          a.Currency
        }));
    }

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
    if (!scope.ShowPayments)
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

  public Task<PresenceStatsDto> GetPresenceAsync(CancellationToken cancellationToken = default) =>
    stats.GetPresenceStatsAsync(cancellationToken);

  private static List<(string Category, string Metric, object? Value)> BuildSummaryMetrics(DashboardStatsDto dashboard)
  {
    var playerLabel = dashboard.IsInfluencerScoped
      ? "Players who used your code"
      : dashboard.IsPartnerScoped
        ? "Players in licensed countries"
        : "Players";

    var metrics = new List<(string Category, string Metric, object? Value)>
    {
      (playerLabel, "Total players", dashboard.Players.TotalPlayers),
      (playerLabel, "New players in period", dashboard.Players.NewPlayers),
      (playerLabel, "Verified players", dashboard.Players.VerifiedPlayers),
      (playerLabel, "Active accounts", dashboard.Players.ActivePlayers)
    };

    if (!dashboard.ShowFinancials)
      return metrics;

    if (dashboard.ShowPayments)
    {
      metrics.AddRange(
      [
        ("Payments", "Successful deposits", dashboard.Payments.SuccessfulDeposits),
        ("Payments", "Deposit volume", dashboard.Payments.DepositVolume),
        ("Payments", "Successful withdrawals", dashboard.Payments.SuccessfulWithdrawals),
        ("Payments", "Withdrawal volume", dashboard.Payments.WithdrawalVolume)
      ]);
    }

    metrics.AddRange(
    [
      ("Games", "Games played", dashboard.Games.GamesPlayed),
      ("Games", "Total pot", dashboard.Games.TotalPot),
      ("Games", dashboard.IsInfluencerScoped ? "Attributed game fees" : "Platform commission", dashboard.Games.PlatformCommission)
    ]);

    if (dashboard.IsInfluencerScoped)
    {
      metrics.AddRange(
      [
        ("Revenue", "Attributed gross game fees", dashboard.Revenue.GrossPlatformRevenue),
        ("Revenue", "Your influencer share", dashboard.Revenue.InfluencerSharePaid),
        ("Revenue", "Commission rows", dashboard.Revenue.AllocationCount)
      ]);
      return metrics;
    }

    metrics.AddRange(
    [
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
    LicensePartnerId = scope.LicensePartnerId,
    InfluencerId = scope.InfluencerId
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
        ShowPayments = true,
        ShowPartnerLeaderboard = true,
        ShowInfluencerLeaderboard = true
      },
      PortalUserRole.Member => new DashboardScope
      {
        ShowFinancials = false,
        ShowPayments = false,
        ShowPartnerLeaderboard = false,
        ShowInfluencerLeaderboard = false
      },
      PortalUserRole.LicensePartner => await ResolvePartnerScopeAsync(portalUserId, cancellationToken),
      PortalUserRole.Influencer => await ResolveInfluencerScopeAsync(portalUserId, cancellationToken),
      _ => throw new UnauthorizedAccessException("Unknown portal role.")
    };
  }

  private async Task<DashboardScope> ResolvePartnerScopeAsync(Guid portalUserId, CancellationToken cancellationToken)
  {
    var partner = await partners.GetByPortalUserIdAsync(portalUserId, cancellationToken);
    var countries = await access.GetLicensedCountryCodesAsync(portalUserId, cancellationToken);

    return new DashboardScope
    {
      ShowFinancials = true,
      ShowPayments = true,
      ShowPartnerLeaderboard = false,
      ShowInfluencerLeaderboard = false,
      IsPartnerScoped = true,
      // Empty country list must not mean "all countries".
      CountryCodes = countries.Count > 0 ? countries : ["__NONE__"],
      LicensePartnerId = partner?.Id ?? Guid.Empty
    };
  }

  private async Task<DashboardScope> ResolveInfluencerScopeAsync(Guid portalUserId, CancellationToken cancellationToken)
  {
    var influencer = await influencers.GetByPortalUserIdAsync(portalUserId, cancellationToken);

    return new DashboardScope
    {
      ShowFinancials = true,
      ShowPayments = false,
      ShowPartnerLeaderboard = false,
      ShowInfluencerLeaderboard = false,
      IsInfluencerScoped = true,
      // Always set an id so unlinked accounts never see platform-wide data.
      InfluencerId = influencer?.Id ?? Guid.Empty
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
    public bool ShowPayments { get; init; }
    public bool ShowPartnerLeaderboard { get; init; }
    public bool ShowInfluencerLeaderboard { get; init; }
    public bool IsInfluencerScoped { get; init; }
    public bool IsPartnerScoped { get; init; }
    public IReadOnlyList<string>? CountryCodes { get; init; }
    public Guid? LicensePartnerId { get; init; }
    public Guid? InfluencerId { get; init; }
  }
}
