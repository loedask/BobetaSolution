namespace Bobeta.Application.DTOs.Portal;

public sealed class DemoAccountGamesResetPreviewDto
{
  public IReadOnlyList<DemoAccountSummaryDto> Accounts { get; init; } = [];
  public int GameSessionCount { get; init; }
  public int GameWalletTransactionCount { get; init; }
  public int GameNotificationCount { get; init; }
}

public sealed class DemoAccountSummaryDto
{
  public Guid PlayerId { get; init; }
  public string PhoneNumber { get; init; } = string.Empty;
  public string PlayerName { get; init; } = string.Empty;
  public decimal Balance { get; init; }
  public decimal LockedBalance { get; init; }
}

public sealed class DemoAccountGamesResetResultDto
{
  public int AccountsFound { get; init; }
  public int GameSessionsDeleted { get; init; }
  public int GameWalletTransactionsDeleted { get; init; }
  public int GameNotificationsDeleted { get; init; }
  public int InfluencerCommissionAllocationsDeleted { get; init; }
  public int RevenueAllocationsDeleted { get; init; }
  public int InfluencerCodeRedemptionsDeleted { get; init; }
  public int WalletsReset { get; init; }
}
