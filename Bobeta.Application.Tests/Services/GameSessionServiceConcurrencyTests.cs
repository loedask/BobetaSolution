using Bobeta.Application.Interfaces;
using Bobeta.Application.Services;
using Bobeta.Application.Tests.Games;
using Bobeta.Domain.Entities;
using Bobeta.Domain.Enums;
using Xunit;

namespace Bobeta.Application.Tests.Services;

public sealed class GameSessionServiceConcurrencyTests
{
    [Fact]
    public async Task CreateGameAsync_WhenPlayerAlreadyHasWaitingSeat_Throws()
    {
        var playerId = Guid.NewGuid();
        var repo = new ListableGameSessionRepository(NewWaiting(playerId, GameVariant.Makopa, 200m));
        var sut = CreateSut(repo);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.CreateGameAsync(playerId, 300m, GameVariant.Domino));

        Assert.Contains("open table", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateGameAsync_WhenPlayerOnlyHasInProgress_AllowsWaitingSeat()
    {
        var playerId = Guid.NewGuid();
        var live = NewWaiting(playerId, GameVariant.Makopa, 200m);
        live.OpponentPlayerId = Guid.NewGuid();
        live.Status = GameStatus.InProgress;
        var repo = new ListableGameSessionRepository(live);
        var wallet = new RecordingWalletService();
        var sut = CreateSut(repo, wallet);

        var created = await sut.CreateGameAsync(playerId, 200m, GameVariant.Domino);

        Assert.Equal(GameStatus.Waiting, created.Status);
        Assert.Equal(GameVariant.Domino, created.Variant);
        Assert.Single(wallet.Locks);
    }

    [Fact]
    public async Task JoinGameAsync_WhenPlayerAlreadyHasInProgress_Throws()
    {
        var playerId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();
        var live = NewWaiting(playerId, GameVariant.Kopo, 200m);
        live.OpponentPlayerId = Guid.NewGuid();
        live.Status = GameStatus.InProgress;
        var open = NewWaiting(creatorId, GameVariant.Domino, 200m);
        var repo = new ListableGameSessionRepository(live, open);
        var sut = CreateSut(repo);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.JoinGameAsync(playerId, open.Id));

        Assert.Contains("current match", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task JoinGameAsync_WhenPlayerOnlyHasWaitingSeat_AllowsJoin()
    {
        var playerId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();
        var myWaiting = NewWaiting(playerId, GameVariant.Makopa, 200m);
        var open = NewWaiting(creatorId, GameVariant.Domino, 300m);
        var repo = new ListableGameSessionRepository(myWaiting, open);
        var wallet = new RecordingWalletService();
        var sut = CreateSut(repo, wallet);

        var joined = await sut.JoinGameAsync(playerId, open.Id);

        Assert.NotNull(joined);
        Assert.Equal(playerId, joined!.OpponentPlayerId);
        Assert.Contains(wallet.Locks, l => l.PlayerId == playerId && l.Amount == 300m);
    }

    private static GameSessionService CreateSut(
        IGameSessionRepository repo,
        RecordingWalletService? wallet = null) =>
        new(
            repo,
            new InMemoryPlayerRepository(
                new Player { Id = Guid.NewGuid(), PlayerName = "P1" },
                new Player { Id = Guid.NewGuid(), PlayerName = "P2" }),
            wallet ?? new RecordingWalletService(),
            new NoOpGameEngineService(),
            new RecordingGameSessionNotifier(),
            NoOpInfluencerAttributionService.Instance,
            NoOpNotificationService.Instance);

    private static GameSession NewWaiting(Guid creatorId, GameVariant variant, decimal bet) =>
        new()
        {
            Id = Guid.NewGuid(),
            CreatorPlayerId = creatorId,
            BetAmount = bet,
            CreatorChargedAmount = bet,
            Variant = variant,
            Status = GameStatus.Waiting,
            OpponentPlayerId = null,
            CreatedAt = DateTime.UtcNow
        };

    /// <summary>Shared in-memory store for concurrency tests.</summary>
    private sealed class ListableGameSessionRepository : IGameSessionRepository
    {
        private readonly List<GameSession> _sessions;

        public ListableGameSessionRepository(params GameSession[] sessions) =>
            _sessions = sessions.ToList();

        public Task<GameSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_sessions.FirstOrDefault(s => s.Id == id));

        public Task<IReadOnlyList<GameSession>> GetWaitingSessionsAsync(decimal betAmount, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<GameSession>>(
                _sessions
                    .Where(s => s.Status == GameStatus.Waiting && s.BetAmount == betAmount && s.OpponentPlayerId == null)
                    .ToList());

        public Task<IReadOnlyList<GameSession>> GetJoinableWaitingSessionsAsync(
            Guid forPlayerId, int skip, int take, GameVariant? variant = null, CancellationToken cancellationToken = default)
        {
            IEnumerable<GameSession> q = _sessions
                .Where(s => s.Status == GameStatus.Waiting && s.OpponentPlayerId == null && s.CreatorPlayerId != forPlayerId);
            if (variant.HasValue)
                q = q.Where(s => s.Variant == variant.Value);
            return Task.FromResult<IReadOnlyList<GameSession>>(q.Skip(skip).Take(take).ToList());
        }

        public Task<IReadOnlyList<GameSession>> GetMyWaitingSessionsAsync(
            Guid playerId, int skip, int take, GameVariant? variant = null, CancellationToken cancellationToken = default)
        {
            IEnumerable<GameSession> q = _sessions
                .Where(s => s.Status == GameStatus.Waiting && s.OpponentPlayerId == null && s.CreatorPlayerId == playerId);
            if (variant.HasValue)
                q = q.Where(s => s.Variant == variant.Value);
            return Task.FromResult<IReadOnlyList<GameSession>>(q.Skip(skip).Take(take).ToList());
        }

        public Task<IReadOnlyList<GameSession>> GetByPlayerIdAsync(Guid playerId, int skip, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<GameSession>>(
                _sessions
                    .Where(s => s.CreatorPlayerId == playerId || s.OpponentPlayerId == playerId)
                    .Skip(skip).Take(take).ToList());

        public Task<bool> HasOpenWaitingSeatAsync(Guid playerId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_sessions.Any(s =>
                s.CreatorPlayerId == playerId
                && s.Status == GameStatus.Waiting
                && s.OpponentPlayerId == null));

        public Task<bool> HasInProgressGameAsync(Guid playerId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_sessions.Any(s =>
                s.Status == GameStatus.InProgress
                && (s.CreatorPlayerId == playerId || s.OpponentPlayerId == playerId)));

        public Task<GameSession> AddAsync(GameSession session, CancellationToken cancellationToken = default)
        {
            _sessions.Add(session);
            return Task.FromResult(session);
        }

        public Task UpdateAsync(GameSession session, CancellationToken cancellationToken = default)
        {
            var idx = _sessions.FindIndex(s => s.Id == session.Id);
            if (idx >= 0)
                _sessions[idx] = session;
            else
                _sessions.Add(session);
            return Task.CompletedTask;
        }
    }
}
