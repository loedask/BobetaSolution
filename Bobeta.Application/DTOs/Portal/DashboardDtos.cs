namespace Bobeta.Application.DTOs.Portal;

public sealed class DashboardStatsDto
{
  public DateTime? FromUtc { get; init; }
  public DateTime? ToUtc { get; init; }
  public bool ShowFinancials { get; init; }
  public bool ShowPayments { get; init; }
  public bool ShowPartnerLeaderboard { get; init; }
  public bool ShowInfluencerLeaderboard { get; init; }
  public bool IsInfluencerScoped { get; init; }
  public bool IsPartnerScoped { get; init; }
  public PlayerDashboardStatsDto Players { get; init; } = new();
  public PaymentDashboardStatsDto Payments { get; init; } = new();
  public RevenueDashboardStatsDto Revenue { get; init; } = new();
  public GameDashboardStatsDto Games { get; init; } = new();
  public IReadOnlyList<CountryCountDto> PlayersByCountry { get; init; } = [];
  public IReadOnlyList<PartnerRevenueBreakdownDto> RevenueByPartner { get; init; } = [];
  public IReadOnlyList<PartnerRevenueBreakdownDto> RevenueBySource { get; init; } = [];
  public IReadOnlyList<PartnerRevenueBreakdownDto> RevenueByInfluencer { get; init; } = [];
}

public sealed class PlayerDashboardStatsDto
{
  public int TotalPlayers { get; init; }
  public int NewPlayers { get; init; }
  public int VerifiedPlayers { get; init; }
  public int ActivePlayers { get; init; }
}

public sealed class PaymentDashboardStatsDto
{
  public int SuccessfulDeposits { get; init; }
  public decimal DepositVolume { get; init; }
  public int SuccessfulWithdrawals { get; init; }
  public decimal WithdrawalVolume { get; init; }
}

public sealed class RevenueDashboardStatsDto
{
  public decimal GrossPlatformRevenue { get; init; }
  public decimal PartnerSharePaid { get; init; }
  public decimal InfluencerSharePaid { get; init; }
  public decimal PlatformRetained { get; init; }
  public int AllocationCount { get; init; }
}

public sealed class GameDashboardStatsDto
{
  public int GamesPlayed { get; init; }
  public decimal TotalPot { get; init; }
  public decimal PlatformCommission { get; init; }
}

public sealed class CountryCountDto
{
  public string CountryCode { get; init; } = string.Empty;
  public string CountryName { get; init; } = string.Empty;
  public int Count { get; init; }
}

public sealed class DashboardQuery
{
  public DateTime? FromUtc { get; init; }
  public DateTime? ToUtc { get; init; }
}
