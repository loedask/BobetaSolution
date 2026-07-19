using Bobeta.Application.DTOs.Game;
using Bobeta.Application.DTOs.Notifications;
using Bobeta.Application.DTOs.Wallet;
using Bobeta.Application.Interfaces;
using Bobeta.Domain.Entities;
using Bobeta.Domain.Enums;
using Bobeta.Domain.ValueObjects;

namespace Bobeta.Application.Tests.Games;

internal sealed class InMemoryGameSessionRepository(GameSession session) : IGameSessionRepository
{
    public Task<GameSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(id == session.Id ? session : null);

    public Task<IReadOnlyList<GameSession>> GetWaitingSessionsAsync(decimal betAmount, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<GameSession>>(Array.Empty<GameSession>());

    public Task<IReadOnlyList<GameSession>> GetJoinableWaitingSessionsAsync(Guid forPlayerId, int skip, int take, Domain.Enums.GameVariant? variant = null, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<GameSession>>(Array.Empty<GameSession>());

    public Task<IReadOnlyList<GameSession>> GetMyWaitingSessionsAsync(Guid playerId, int skip, int take, Domain.Enums.GameVariant? variant = null, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<GameSession>>(Array.Empty<GameSession>());

    public Task<IReadOnlyList<GameSession>> GetByPlayerIdAsync(Guid playerId, int skip, int take, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<GameSession>>(Array.Empty<GameSession>());

    public Task<bool> HasOpenWaitingSeatAsync(Guid playerId, CancellationToken cancellationToken = default)
        => Task.FromResult(
            session.CreatorPlayerId == playerId
            && session.Status == GameStatus.Waiting
            && session.OpponentPlayerId == null);

    public Task<int> CountInProgressGamesAsync(Guid playerId, CancellationToken cancellationToken = default)
        => Task.FromResult(
            session.Status == GameStatus.InProgress
            && (session.CreatorPlayerId == playerId || session.OpponentPlayerId == playerId)
                ? 1
                : 0);

    public Task<GameSession> AddAsync(GameSession session, CancellationToken cancellationToken = default)
        => Task.FromResult(session);

    public Task UpdateAsync(GameSession session, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

internal sealed class InMemoryGameMoveRepository : IGameMoveRepository
{
    private readonly List<GameMove> _moves = new();

    public Task<GameMove> AddAsync(GameMove move, CancellationToken cancellationToken = default)
    {
        _moves.Add(move);
        return Task.FromResult(move);
    }

    public Task<IReadOnlyList<GameMove>> GetByGameSessionIdAsync(Guid gameSessionId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<GameMove>>(_moves.Where(m => m.GameSessionId == gameSessionId).ToList());

    public Task<int> GetCountByGameSessionIdAsync(Guid gameSessionId, CancellationToken cancellationToken = default)
        => Task.FromResult(_moves.Count(m => m.GameSessionId == gameSessionId));
}

internal sealed class InMemoryGameResultRepository(Action<GameResult> onAdd) : IGameResultRepository
{
    public Task<GameResult> AddAsync(GameResult result, CancellationToken cancellationToken = default)
    {
        onAdd(result);
        return Task.FromResult(result);
    }
}

internal sealed class NoOpGameRevenueService : IGameRevenueService
{
    public static NoOpGameRevenueService Instance { get; } = new();

    public Task EnrichWithPartnerShareAsync(GameResult result, Guid winnerPlayerId, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

internal sealed class NoOpNotificationService : INotificationService
{
    public static NoOpNotificationService Instance { get; } = new();

    public Task NotifyOpponentJoinedAsync(Guid creatorPlayerId, Guid gameSessionId, string opponentName, decimal betAmount, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task NotifyGameResultAsync(Guid playerId, Guid gameSessionId, bool won, decimal amount, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task NotifyPaymentAsync(Guid playerId, bool isDeposit, bool success, decimal amount, Guid? paymentTransactionId = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task SendGameInviteAsync(Guid recipientPlayerId, Guid gameSessionId, string inviterName, decimal betAmount, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task SendBetProposalAsync(Guid recipientPlayerId, Guid gameSessionId, decimal proposedAmount, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task<IReadOnlyList<NotificationDto>> GetInboxAsync(Guid playerId, int skip = 0, int take = 30, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<NotificationDto>>(Array.Empty<NotificationDto>());
    public Task<int> GetUnreadCountAsync(Guid playerId, CancellationToken cancellationToken = default) => Task.FromResult(0);
    public Task MarkReadAsync(Guid playerId, Guid notificationId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task MarkAllReadAsync(Guid playerId, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

internal sealed class RecordingNotificationService : INotificationService
{
    public List<(Guid CreatorPlayerId, Guid GameSessionId, string OpponentName, decimal BetAmount)> OpponentJoined { get; } = new();
    public List<(Guid PlayerId, Guid GameSessionId, bool Won, decimal Amount)> GameResults { get; } = new();
    public List<(Guid PlayerId, bool IsDeposit, bool Success, decimal Amount, Guid? PaymentTransactionId)> Payments { get; } = new();
    public List<(Guid RecipientPlayerId, Guid GameSessionId, string InviterName, decimal BetAmount)> GameInvites { get; } = new();
    public List<(Guid RecipientPlayerId, Guid GameSessionId, decimal ProposedAmount)> BetProposals { get; } = new();

    public Task NotifyOpponentJoinedAsync(Guid creatorPlayerId, Guid gameSessionId, string opponentName, decimal betAmount, CancellationToken cancellationToken = default)
    {
        OpponentJoined.Add((creatorPlayerId, gameSessionId, opponentName, betAmount));
        return Task.CompletedTask;
    }

    public Task NotifyGameResultAsync(Guid playerId, Guid gameSessionId, bool won, decimal amount, CancellationToken cancellationToken = default)
    {
        GameResults.Add((playerId, gameSessionId, won, amount));
        return Task.CompletedTask;
    }

    public Task NotifyPaymentAsync(Guid playerId, bool isDeposit, bool success, decimal amount, Guid? paymentTransactionId = null, CancellationToken cancellationToken = default)
    {
        Payments.Add((playerId, isDeposit, success, amount, paymentTransactionId));
        return Task.CompletedTask;
    }

    public Task SendGameInviteAsync(Guid recipientPlayerId, Guid gameSessionId, string inviterName, decimal betAmount, CancellationToken cancellationToken = default)
    {
        GameInvites.Add((recipientPlayerId, gameSessionId, inviterName, betAmount));
        return Task.CompletedTask;
    }

    public Task SendBetProposalAsync(Guid recipientPlayerId, Guid gameSessionId, decimal proposedAmount, CancellationToken cancellationToken = default)
    {
        BetProposals.Add((recipientPlayerId, gameSessionId, proposedAmount));
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<NotificationDto>> GetInboxAsync(Guid playerId, int skip = 0, int take = 30, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<NotificationDto>>(Array.Empty<NotificationDto>());

    public Task<int> GetUnreadCountAsync(Guid playerId, CancellationToken cancellationToken = default) => Task.FromResult(0);
    public Task MarkReadAsync(Guid playerId, Guid notificationId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task MarkAllReadAsync(Guid playerId, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

internal sealed class InMemoryPlayerNotificationRepository : IPlayerNotificationRepository
{
    private readonly List<PlayerNotification> _items = new();

    public IReadOnlyList<PlayerNotification> Items => _items;

    public Task<PlayerNotification> AddAsync(PlayerNotification notification, CancellationToken cancellationToken = default)
    {
        _items.Add(notification);
        return Task.FromResult(notification);
    }

    public Task<IReadOnlyList<PlayerNotification>> GetForPlayerAsync(
        Guid playerId,
        int skip,
        int take,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<PlayerNotification>>(
            _items.Where(n => n.PlayerId == playerId)
                .OrderByDescending(n => n.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToList());

    public Task<int> CountUnreadAsync(Guid playerId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.Count(n => n.PlayerId == playerId && !n.IsRead));

    public Task<PlayerNotification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.FirstOrDefault(n => n.Id == id));

    public Task UpdateAsync(PlayerNotification notification, CancellationToken cancellationToken = default)
    {
        var idx = _items.FindIndex(n => n.Id == notification.Id);
        if (idx >= 0)
            _items[idx] = notification;
        return Task.CompletedTask;
    }

    public Task MarkAllReadAsync(Guid playerId, CancellationToken cancellationToken = default)
    {
        foreach (var item in _items.Where(n => n.PlayerId == playerId && !n.IsRead))
            item.IsRead = true;
        return Task.CompletedTask;
    }
}

internal sealed class RecordingNotificationRealtimePublisher : INotificationRealtimePublisher
{
    public List<(Guid PlayerId, NotificationDto Dto)> Published { get; } = new();

    public Task PublishAsync(Guid playerId, NotificationDto notification, CancellationToken cancellationToken = default)
    {
        Published.Add((playerId, notification));
        return Task.CompletedTask;
    }
}

internal sealed class ThrowingPlayerNotificationRepository : IPlayerNotificationRepository
{
    public Task<PlayerNotification> AddAsync(PlayerNotification notification, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("repository failed");

    public Task<IReadOnlyList<PlayerNotification>> GetForPlayerAsync(Guid playerId, int skip, int take, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("repository failed");

    public Task<int> CountUnreadAsync(Guid playerId, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("repository failed");

    public Task<PlayerNotification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("repository failed");

    public Task UpdateAsync(PlayerNotification notification, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("repository failed");

    public Task MarkAllReadAsync(Guid playerId, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("repository failed");
}

internal sealed class InMemoryPlayerDeviceTokenRepository : IPlayerDeviceTokenRepository
{
    public List<PlayerDeviceToken> Items { get; } = new();

    public Task<IReadOnlyList<PlayerDeviceToken>> GetByPlayerIdAsync(Guid playerId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<PlayerDeviceToken>>(Items.Where(t => t.PlayerId == playerId).ToList());

    public Task<PlayerDeviceToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(t => t.Token == token));

    public Task<PlayerDeviceToken> AddAsync(PlayerDeviceToken token, CancellationToken cancellationToken = default)
    {
        Items.Add(token);
        return Task.FromResult(token);
    }

    public Task UpdateAsync(PlayerDeviceToken token, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task DeleteByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        Items.RemoveAll(t => t.Token == token);
        return Task.CompletedTask;
    }

    public Task DeleteByTokensAsync(IReadOnlyList<string> tokens, CancellationToken cancellationToken = default)
    {
        Items.RemoveAll(t => tokens.Contains(t.Token));
        return Task.CompletedTask;
    }
}

internal sealed class InMemoryWalletRepository : IWalletRepository
{
    private readonly Dictionary<Guid, Wallet> _wallets = new();

    public InMemoryWalletRepository(params Wallet[] wallets)
    {
        foreach (var wallet in wallets)
            _wallets[wallet.PlayerId] = wallet;
    }

    public Task<Wallet?> GetByPlayerIdAsync(Guid playerId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_wallets.TryGetValue(playerId, out var wallet) ? wallet : null);

    public Task<Wallet> AddAsync(Wallet wallet, CancellationToken cancellationToken = default)
    {
        _wallets[wallet.PlayerId] = wallet;
        return Task.FromResult(wallet);
    }

    public Task UpdateAsync(Wallet wallet, CancellationToken cancellationToken = default)
    {
        _wallets[wallet.PlayerId] = wallet;
        return Task.CompletedTask;
    }
}

internal sealed class InMemoryWalletTransactionRepository : IWalletTransactionRepository
{
    public List<WalletTransaction> Items { get; } = new();

    public Task<WalletTransaction> AddAsync(WalletTransaction transaction, CancellationToken cancellationToken = default)
    {
        Items.Add(transaction);
        return Task.FromResult(transaction);
    }

    public Task<IReadOnlyList<WalletTransaction>> GetByPlayerIdAsync(Guid playerId, int skip, int take, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<WalletTransaction>>(
            Items.Where(t => t.PlayerId == playerId)
                .OrderByDescending(t => t.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToList());
}

internal sealed class NoOpInfluencerAttributionService : IInfluencerAttributionService
{
    public static NoOpInfluencerAttributionService Instance { get; } = new();

    public Task ApplyCodeAsync(Guid playerId, string code, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task<Bobeta.Application.DTOs.Influencer.InfluencerCodeStatusDto> GetStatusAsync(Guid playerId, CancellationToken cancellationToken = default) =>
        Task.FromResult(new Bobeta.Application.DTOs.Influencer.InfluencerCodeStatusDto(false, null, null, 5));
    public Task<decimal> GetChargeAmountAsync(Guid playerId, decimal betAmount, CancellationToken cancellationToken = default) =>
        Task.FromResult(betAmount);
    public Task AttachPendingCodeToGameAsync(Guid playerId, Guid gameSessionId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task MarkGameRedemptionsConsumedAsync(Guid gameSessionId, DateTime atUtc, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task DetachGameRedemptionsAsync(Guid gameSessionId, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

internal sealed class RecordingWalletService : IWalletService
{
    public List<(Guid WinnerId, Guid LoserId, decimal Bet)> Settlements { get; } = new();
    public List<(Guid PlayerId, decimal Amount)> Releases { get; } = new();

    public Task<WalletBalanceDto> GetBalanceAsync(Guid playerId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<WalletTransactionDto> DepositAsync(Guid playerId, decimal amount, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<WalletTransactionDto> WithdrawAsync(Guid playerId, decimal amount, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public List<(Guid PlayerId, decimal Amount)> Locks { get; } = new();

    public Task LockBetAsync(Guid playerId, decimal amount, CancellationToken cancellationToken = default)
    {
        Locks.Add((playerId, amount));
        return Task.CompletedTask;
    }

    public Task ReleaseBetAsync(Guid playerId, decimal amount, CancellationToken cancellationToken = default)
    {
        Releases.Add((playerId, amount));
        return Task.CompletedTask;
    }
    public Task<IReadOnlyList<WalletTransactionDto>> GetTransactionsAsync(Guid playerId, int skip, int take, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task SettleGameAsync(
        Guid winnerId,
        Guid loserId,
        decimal betAmountPerPlayer,
        decimal winnerChargedAmount,
        decimal loserChargedAmount,
        CancellationToken cancellationToken = default)
    {
        Settlements.Add((winnerId, loserId, betAmountPerPlayer));
        return Task.CompletedTask;
    }
}

internal sealed class InMemoryPlayerRepository(params Player[] players) : IPlayerRepository
{
    private readonly Dictionary<Guid, Player> _players = players.ToDictionary(x => x.Id);

    public Task<Player?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_players.TryGetValue(id, out var player) ? player : null);

    public Task<Player?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default)
        => Task.FromResult(_players.Values.FirstOrDefault(x => x.PhoneNumber == phoneNumber));

    public Task<Player> AddAsync(Player player, CancellationToken cancellationToken = default)
    {
        _players[player.Id] = player;
        return Task.FromResult(player);
    }

    public Task UpdateAsync(Player player, CancellationToken cancellationToken = default)
    {
        _players[player.Id] = player;
        return Task.CompletedTask;
    }

    public Task TouchLastSeenOnlineAsync(Guid playerId, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        if (_players.TryGetValue(playerId, out var player))
            player.LastSeenOnlineUtc = utcNow;
        return Task.CompletedTask;
    }

    public Task<(IReadOnlyList<Player> Items, int TotalCount)> GetPagedAsync(
        int skip,
        int take,
        string? search = null,
        IReadOnlyList<string>? countryCodes = null,
        CancellationToken cancellationToken = default)
    {
        var items = _players.Values
            .Where(p => search == null || p.PlayerName.Contains(search, StringComparison.OrdinalIgnoreCase) || p.PhoneNumber.Contains(search, StringComparison.OrdinalIgnoreCase))
            .Skip(skip)
            .Take(take)
            .ToList();
        return Task.FromResult<(IReadOnlyList<Player>, int)>((items, _players.Count));
    }
}

internal sealed class RecordingGameSessionNotifier : IGameSessionNotifier
{
    public List<Guid> Sessions { get; } = new();

    public Task NotifySessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        Sessions.Add(sessionId);
        return Task.CompletedTask;
    }
}

internal sealed class NoOpGameEngineService : IGameEngineService
{
    public Task StartGameAsync(Guid sessionId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task<GameMoveResult> PlayCardAsync(Guid playerId, Guid sessionId, Card card, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
    public Task<GameMoveResult> VoidFollowDrawAsync(Guid playerId, Guid sessionId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
    public Task<GameMoveResult> ApplyKopoMoveAsync(Guid playerId, Guid sessionId, IReadOnlyList<(int Row, int Col)> path, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
    public Task<GameMoveResult> ApplyNgolaMoveAsync(Guid playerId, Guid sessionId, int pitIndex, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
    public Task<GameMoveResult> ApplyDominoMoveAsync(
        Guid playerId, Guid sessionId, string action, int? high, int? low, string? end, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
    public Task<GameMoveResult> ApplyAbbiaThrowAsync(Guid playerId, Guid sessionId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
    public Task<GameMoveResult> ApplyNzengueMoveAsync(
        Guid playerId, Guid sessionId, int? fromPoint, int toPoint, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
    public Task<GameStateDto?> GetGameStateAsync(Guid playerId, Guid sessionId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}
