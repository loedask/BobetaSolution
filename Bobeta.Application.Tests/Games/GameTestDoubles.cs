using Bobeta.Application.DTOs.Wallet;
using Bobeta.Application.Interfaces;
using Bobeta.Domain.Entities;

namespace Bobeta.Application.Tests.Games;

internal sealed class InMemoryGameSessionRepository(GameSession session) : IGameSessionRepository
{
    public Task<GameSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(id == session.Id ? session : null);

    public Task<IReadOnlyList<GameSession>> GetWaitingSessionsAsync(decimal betAmount, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<GameSession>>(Array.Empty<GameSession>());

    public Task<IReadOnlyList<GameSession>> GetJoinableWaitingSessionsAsync(Guid forPlayerId, int skip, int take, Domain.Enums.GameVariant? variant = null, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<GameSession>>(Array.Empty<GameSession>());

    public Task<IReadOnlyList<GameSession>> GetByPlayerIdAsync(Guid playerId, int skip, int take, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<GameSession>>(Array.Empty<GameSession>());

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
    public Task LockBetAsync(Guid playerId, decimal amount, CancellationToken cancellationToken = default) => throw new NotSupportedException();
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
