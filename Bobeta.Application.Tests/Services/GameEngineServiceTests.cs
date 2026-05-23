using System.Text.Json;
using Bobeta.Application.Common;
using Bobeta.Application.Interfaces;
using Bobeta.Application.Services;
using Bobeta.Application.DTOs.Wallet;
using Bobeta.Domain.Entities;
using Bobeta.Domain.Enums;
using Bobeta.Domain.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Bobeta.Application.Tests.Services;

public class GameEngineServiceTests
{
    [Fact]
    public async Task PlayCardAsync_WhenTrickWinnerHasOneCardToLead_EndsGameInSameCall()
    {
        var sessionId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();
        var opponentId = Guid.NewGuid();
        var pendingState = new MakopaGameState
        {
            // Creator led; opponent wins the trick and keeps one card — wins immediately when it is their turn to lead.
            CreatorHand = new List<string>(),
            OpponentHand = new List<string> { "Spade_12", "Heart_10" },
            LeadPlayerId = creatorId,
            CurrentTurnPlayerId = opponentId,
            TrickSuit = "Spade",
            TrickPlays = new List<PlayedInTrick>
            {
                new() { PlayerId = creatorId, Card = "Spade_5" }
            }
        };

        var session = new GameSession
        {
            Id = sessionId,
            CreatorPlayerId = creatorId,
            OpponentPlayerId = opponentId,
            BetAmount = 100m,
            Status = GameStatus.InProgress,
            CreatedAt = DateTime.UtcNow,
            GameStateJson = JsonSerializer.Serialize(pendingState, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
        };

        var sessionRepo = new InMemoryGameSessionRepository(session);
        var moveRepo = new InMemoryGameMoveRepository();
        var resultRepo = new InMemoryGameResultRepository(result => session.GameResult = result);
        var wallet = new RecordingWalletService();
        var players = new InMemoryPlayerRepository(
            new Player { Id = creatorId, PlayerName = "Creator" },
            new Player { Id = opponentId, PlayerName = "Opponent" });

        var sut = new GameEngineService(
            sessionRepo,
            moveRepo,
            resultRepo,
            wallet,
            players,
            NullLogger<GameEngineService>.Instance);

        var state = await sut.PlayCardAsync(opponentId, sessionId, new Card(CardSuit.Spade, CardRank.Queen));

        Assert.NotNull(state);
        Assert.True(state!.GameOver);
        Assert.Equal(opponentId, state.WinnerPlayerId);
        Assert.Equal(GameStatus.Finished, session.Status);
        Assert.Null(session.GameStateJson);
        Assert.NotNull(session.GameResult);
        Assert.Equal(opponentId, session.GameResult!.WinnerPlayerId);
        Assert.Equal(creatorId, session.GameResult.LoserPlayerId);
        Assert.Single(wallet.Settlements);
    }

    [Fact]
    public async Task VoidFollowDrawAsync_WhenLeaderHasOneCardToLead_EndsGameInSameCall()
    {
        var sessionId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();
        var opponentId = Guid.NewGuid();
        var pendingState = new MakopaGameState
        {
            CreatorHand = new List<string> { "Spade_2" },
            OpponentHand = new List<string> { "Club_10", "Diamond_3" },
            LeadPlayerId = creatorId,
            CurrentTurnPlayerId = opponentId,
            TrickSuit = "Heart",
            TrickPlays = new List<PlayedInTrick>
            {
                new() { PlayerId = creatorId, Card = "Heart_5" }
            }
        };

        var session = new GameSession
        {
            Id = sessionId,
            CreatorPlayerId = creatorId,
            OpponentPlayerId = opponentId,
            BetAmount = 100m,
            Status = GameStatus.InProgress,
            CreatedAt = DateTime.UtcNow,
            GameStateJson = JsonSerializer.Serialize(pendingState, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
        };

        var sessionRepo = new InMemoryGameSessionRepository(session);
        var moveRepo = new InMemoryGameMoveRepository();
        var resultRepo = new InMemoryGameResultRepository(result => session.GameResult = result);
        var wallet = new RecordingWalletService();
        var players = new InMemoryPlayerRepository(
            new Player { Id = creatorId, PlayerName = "Creator" },
            new Player { Id = opponentId, PlayerName = "Opponent" });

        var sut = new GameEngineService(
            sessionRepo,
            moveRepo,
            resultRepo,
            wallet,
            players,
            NullLogger<GameEngineService>.Instance);

        var state = await sut.VoidFollowDrawAsync(opponentId, sessionId);

        Assert.NotNull(state);
        Assert.True(state!.GameOver);
        Assert.Equal(creatorId, state.WinnerPlayerId);
        Assert.Equal(GameStatus.Finished, session.Status);
        Assert.Single(wallet.Settlements);
    }

    [Fact]
    public async Task PlayCardAsync_WhenTrickWinnerStillHasCards_GameContinues()
    {
        var sessionId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();
        var opponentId = Guid.NewGuid();
        var pendingState = new MakopaGameState
        {
            CreatorHand = new List<string> { "Club_9" },
            OpponentHand = new List<string> { "Heart_10", "Club_2", "Spade_3" },
            LeadPlayerId = creatorId,
            CurrentTurnPlayerId = opponentId,
            TrickSuit = "Heart",
            TrickPlays = new List<PlayedInTrick>
            {
                new() { PlayerId = creatorId, Card = "Heart_5" }
            }
        };

        var session = new GameSession
        {
            Id = sessionId,
            CreatorPlayerId = creatorId,
            OpponentPlayerId = opponentId,
            BetAmount = 100m,
            Status = GameStatus.InProgress,
            CreatedAt = DateTime.UtcNow,
            GameStateJson = JsonSerializer.Serialize(pendingState, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
        };

        var sessionRepo = new InMemoryGameSessionRepository(session);
        var moveRepo = new InMemoryGameMoveRepository();
        var resultRepo = new InMemoryGameResultRepository(result => session.GameResult = result);
        var wallet = new RecordingWalletService();
        var players = new InMemoryPlayerRepository(
            new Player { Id = creatorId, PlayerName = "Creator" },
            new Player { Id = opponentId, PlayerName = "Opponent" });

        var sut = new GameEngineService(
            sessionRepo,
            moveRepo,
            resultRepo,
            wallet,
            players,
            NullLogger<GameEngineService>.Instance);

        var state = await sut.PlayCardAsync(opponentId, sessionId, new Card(CardSuit.Heart, CardRank.Ten));

        Assert.NotNull(state);
        Assert.False(state!.GameOver);
        Assert.Null(state.WinnerPlayerId);
        Assert.Equal(GameStatus.InProgress, session.Status);
        Assert.Empty(wallet.Settlements);
    }

    private sealed class InMemoryGameSessionRepository : IGameSessionRepository
    {
        private readonly GameSession _session;

        public InMemoryGameSessionRepository(GameSession session) => _session = session;

        public Task<GameSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(id == _session.Id ? _session : null);

        public Task<IReadOnlyList<GameSession>> GetWaitingSessionsAsync(decimal betAmount, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<GameSession>>(Array.Empty<GameSession>());

        public Task<IReadOnlyList<GameSession>> GetJoinableWaitingSessionsAsync(Guid forPlayerId, int skip, int take, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<GameSession>>(Array.Empty<GameSession>());

        public Task<IReadOnlyList<GameSession>> GetByPlayerIdAsync(Guid playerId, int skip, int take, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<GameSession>>(Array.Empty<GameSession>());

        public Task<GameSession> AddAsync(GameSession session, CancellationToken cancellationToken = default)
            => Task.FromResult(session);

        public Task UpdateAsync(GameSession session, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class InMemoryGameMoveRepository : IGameMoveRepository
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

    private sealed class InMemoryGameResultRepository(Action<GameResult> onAdd) : IGameResultRepository
    {
        private readonly Action<GameResult> _onAdd = onAdd;

        public Task<GameResult> AddAsync(GameResult result, CancellationToken cancellationToken = default)
        {
            _onAdd(result);
            return Task.FromResult(result);
        }
    }

    private sealed class RecordingWalletService : IWalletService
    {
        public List<(Guid WinnerId, Guid LoserId, decimal Bet)> Settlements { get; } = new();

        public Task<WalletBalanceDto> GetBalanceAsync(Guid playerId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<WalletTransactionDto> DepositAsync(Guid playerId, decimal amount, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<WalletTransactionDto> WithdrawAsync(Guid playerId, decimal amount, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task LockBetAsync(Guid playerId, decimal amount, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task ReleaseBetAsync(Guid playerId, decimal amount, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<WalletTransactionDto>> GetTransactionsAsync(Guid playerId, int skip, int take, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task SettleGameAsync(Guid winnerId, Guid loserId, decimal betAmountPerPlayer, CancellationToken cancellationToken = default)
        {
            Settlements.Add((winnerId, loserId, betAmountPerPlayer));
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryPlayerRepository(params Player[] players) : IPlayerRepository
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
    }
}
