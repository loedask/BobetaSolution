using Bobeta.Application.DTOs.Portal;
using Bobeta.Application.Interfaces;
using Bobeta.Domain.Enums;
using Bobeta.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Bobeta.Persistence.Repositories;

/// <summary>
/// Clears game-related rows for hard-coded demo phone numbers only.
/// Player accounts and MoMo payment history are left intact.
/// Games that involve a non-demo player are never deleted.
/// </summary>
public sealed class DemoAccountGamesResetRepository(BobetaDbContext db) : IDemoAccountGamesResetRepository
{
  private static readonly TransactionType[] GameTransactionTypes =
  [
    TransactionType.BetLock,
    TransactionType.BetRelease,
    TransactionType.Win,
    TransactionType.Commission
  ];

  private static readonly NotificationType[] GameNotificationTypes =
  [
    NotificationType.OpponentJoined,
    NotificationType.GameWon,
    NotificationType.GameLost,
    NotificationType.GameInvite,
    NotificationType.BetProposal
  ];

  public async Task<DemoAccountGamesResetPreviewDto> GetPreviewAsync(
      IReadOnlyList<string> demoPhoneNumbers,
      CancellationToken cancellationToken = default)
  {
    var accounts = await LoadDemoAccountsAsync(demoPhoneNumbers, cancellationToken);
    if (accounts.Count == 0)
      return new DemoAccountGamesResetPreviewDto();

    var demoIds = accounts.Select(a => a.PlayerId).ToList();
    var sessionCount = await CountDemoOnlySessionsAsync(demoIds, cancellationToken);
    var txCount = await CountGameWalletTransactionsAsync(demoIds, cancellationToken);
    var notificationCount = await CountGameNotificationsAsync(demoIds, cancellationToken);

    return new DemoAccountGamesResetPreviewDto
    {
      Accounts = accounts,
      GameSessionCount = sessionCount,
      GameWalletTransactionCount = txCount,
      GameNotificationCount = notificationCount
    };
  }

  public async Task<DemoAccountGamesResetResultDto> ClearGamesDataAsync(
      IReadOnlyList<string> demoPhoneNumbers,
      decimal resetWalletBalance,
      CancellationToken cancellationToken = default)
  {
    if (demoPhoneNumbers.Count == 0)
      throw new InvalidOperationException("No demo phone numbers were provided.");

    var strategy = db.Database.CreateExecutionStrategy();
    return await strategy.ExecuteAsync(async () =>
    {
      await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

      var accounts = await LoadDemoAccountsAsync(demoPhoneNumbers, cancellationToken);
      if (accounts.Count == 0)
      {
        await transaction.CommitAsync(cancellationToken);
        return new DemoAccountGamesResetResultDto();
      }

      var demoIds = accounts.Select(a => a.PlayerId).ToList();
      var gameTxTypes = GameTransactionTypes;
      var gameNotificationTypes = GameNotificationTypes;

      var openMixedCount = await db.GameSessions.CountAsync(
          s => (s.Status == GameStatus.Waiting || s.Status == GameStatus.InProgress)
               && (
                   (demoIds.Contains(s.CreatorPlayerId)
                    && s.OpponentPlayerId != null
                    && !demoIds.Contains(s.OpponentPlayerId.Value))
                   || (s.OpponentPlayerId != null
                       && demoIds.Contains(s.OpponentPlayerId.Value)
                       && !demoIds.Contains(s.CreatorPlayerId))),
          cancellationToken);

      if (openMixedCount > 0)
      {
        throw new InvalidOperationException(
            "A demo account has an open game with a non-demo player. Finish or cancel that game first, then try again.");
      }

      // Only sessions that never involve a non-demo player.
      var sessionIds = await db.GameSessions
          .Where(s => demoIds.Contains(s.CreatorPlayerId)
                      && (s.OpponentPlayerId == null || demoIds.Contains(s.OpponentPlayerId.Value)))
          .Select(s => s.Id)
          .ToListAsync(cancellationToken);

      var influencerAllocationsDeleted = 0;
      var revenueAllocationsDeleted = 0;
      var sessionsDeleted = 0;

      if (sessionIds.Count > 0)
      {
        influencerAllocationsDeleted = await db.InfluencerCommissionAllocations
            .Where(a => sessionIds.Contains(a.GameSessionId))
            .ExecuteDeleteAsync(cancellationToken);

        revenueAllocationsDeleted = await db.RevenueAllocations
            .Where(a => a.SourceType == RevenueAllocationSourceType.GameCommission
                        && sessionIds.Contains(a.SourceId))
            .ExecuteDeleteAsync(cancellationToken);

        // Cascade deletes moves + results. Redemption GameSessionId is SetNull.
        sessionsDeleted = await db.GameSessions
            .Where(s => sessionIds.Contains(s.Id))
            .ExecuteDeleteAsync(cancellationToken);
      }

      var redemptionsDeleted = await db.InfluencerCodeRedemptions
          .Where(r => demoIds.Contains(r.PlayerId))
          .ExecuteDeleteAsync(cancellationToken);

      var walletTxDeleted = await db.WalletTransactions
          .Where(t => demoIds.Contains(t.PlayerId) && gameTxTypes.Contains(t.Type))
          .ExecuteDeleteAsync(cancellationToken);

      var notificationsDeleted = await db.PlayerNotifications
          .Where(n => demoIds.Contains(n.PlayerId) && gameNotificationTypes.Contains(n.Type))
          .ExecuteDeleteAsync(cancellationToken);

      var now = DateTime.UtcNow;
      var walletsReset = await db.Wallets
          .Where(w => demoIds.Contains(w.PlayerId))
          .ExecuteUpdateAsync(
              setters => setters
                  .SetProperty(w => w.Balance, resetWalletBalance)
                  .SetProperty(w => w.LockedBalance, 0m)
                  .SetProperty(w => w.UpdatedAt, now),
              cancellationToken);

      await transaction.CommitAsync(cancellationToken);

      return new DemoAccountGamesResetResultDto
      {
        AccountsFound = accounts.Count,
        GameSessionsDeleted = sessionsDeleted,
        GameWalletTransactionsDeleted = walletTxDeleted,
        GameNotificationsDeleted = notificationsDeleted,
        InfluencerCommissionAllocationsDeleted = influencerAllocationsDeleted,
        RevenueAllocationsDeleted = revenueAllocationsDeleted,
        InfluencerCodeRedemptionsDeleted = redemptionsDeleted,
        WalletsReset = walletsReset
      };
    });
  }

  private async Task<List<DemoAccountSummaryDto>> LoadDemoAccountsAsync(
      IReadOnlyList<string> demoPhoneNumbers,
      CancellationToken cancellationToken)
  {
    var phones = demoPhoneNumbers
        .Where(p => !string.IsNullOrWhiteSpace(p))
        .Select(p => p.Trim())
        .Distinct(StringComparer.Ordinal)
        .ToList();

    if (phones.Count == 0)
      return [];

    var players = await db.Players
        .AsNoTracking()
        .Where(p => phones.Contains(p.PhoneNumber))
        .OrderBy(p => p.PhoneNumber)
        .Select(p => new { p.Id, p.PhoneNumber, p.PlayerName })
        .ToListAsync(cancellationToken);

    if (players.Count == 0)
      return [];

    var playerIds = players.Select(p => p.Id).ToList();
    var wallets = await db.Wallets
        .AsNoTracking()
        .Where(w => playerIds.Contains(w.PlayerId))
        .Select(w => new { w.PlayerId, w.Balance, w.LockedBalance })
        .ToListAsync(cancellationToken);

    var walletByPlayerId = wallets.ToDictionary(w => w.PlayerId);

    return players
        .Select(p =>
        {
          walletByPlayerId.TryGetValue(p.Id, out var wallet);
          return new DemoAccountSummaryDto
          {
            PlayerId = p.Id,
            PhoneNumber = p.PhoneNumber,
            PlayerName = p.PlayerName,
            Balance = wallet?.Balance ?? 0m,
            LockedBalance = wallet?.LockedBalance ?? 0m
          };
        })
        .ToList();
  }

  private Task<int> CountDemoOnlySessionsAsync(IReadOnlyList<Guid> demoIds, CancellationToken cancellationToken)
  {
    var ids = demoIds.ToList();
    return db.GameSessions.CountAsync(
        s => ids.Contains(s.CreatorPlayerId)
             && (s.OpponentPlayerId == null || ids.Contains(s.OpponentPlayerId.Value)),
        cancellationToken);
  }

  private Task<int> CountGameWalletTransactionsAsync(IReadOnlyList<Guid> demoIds, CancellationToken cancellationToken)
  {
    var ids = demoIds.ToList();
    var types = GameTransactionTypes;
    return db.WalletTransactions.CountAsync(
        t => ids.Contains(t.PlayerId) && types.Contains(t.Type),
        cancellationToken);
  }

  private Task<int> CountGameNotificationsAsync(IReadOnlyList<Guid> demoIds, CancellationToken cancellationToken)
  {
    var ids = demoIds.ToList();
    var types = GameNotificationTypes;
    return db.PlayerNotifications.CountAsync(
        n => ids.Contains(n.PlayerId) && types.Contains(n.Type),
        cancellationToken);
  }
}
