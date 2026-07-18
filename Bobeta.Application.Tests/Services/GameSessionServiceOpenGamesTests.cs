using Bobeta.Application.Interfaces;
using Bobeta.Application.Services;
using Bobeta.Application.Tests.Games;
using Bobeta.Domain.Entities;
using Bobeta.Domain.Enums;
using Xunit;

namespace Bobeta.Application.Tests.Services;

/// <summary>
/// Regression: a waiting game created by one player must appear in open/joinable games for another player
/// (including Domino + variant filter), and must be hidden from the creator.
/// </summary>
public sealed class GameSessionServiceOpenGamesTests
{
    [Fact]
    public async Task ListOpenJoinableGamesAsync_ShowsOtherPlayersWaitingDominoGame()
    {
        var creatorId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var joinerId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var sessionId = Guid.Parse("c6b49434-dae9-457a-b95c-fd4535eb4b31");

        var waitingDomino = new GameSession
        {
            Id = sessionId,
            CreatorPlayerId = creatorId,
            BetAmount = 200m,
            CreatorChargedAmount = 200m,
            Variant = GameVariant.Domino,
            Status = GameStatus.Waiting,
            OpponentPlayerId = null,
            CreatedAt = new DateTime(2026, 7, 18, 6, 21, 14, DateTimeKind.Utc)
        };

        var repo = new ListableGameSessionRepository(waitingDomino);
        var sut = CreateSut(repo);

        var forJoiner = await sut.ListOpenJoinableGamesAsync(joinerId, variant: GameVariant.Domino);
        var forCreator = await sut.ListOpenJoinableGamesAsync(creatorId, variant: GameVariant.Domino);

        Assert.Single(forJoiner);
        Assert.Equal(sessionId, forJoiner[0].Id);
        Assert.Equal(GameVariant.Domino, forJoiner[0].Variant);
        Assert.Equal(GameStatus.Waiting, forJoiner[0].Status);
        Assert.Null(forJoiner[0].OpponentPlayerId);
        Assert.Equal(creatorId, forJoiner[0].CreatorPlayerId);

        Assert.Empty(forCreator);
    }

    [Fact]
    public async Task ListOpenJoinableGamesAsync_VariantFilter_HidesOtherVariants()
    {
        var creatorId = Guid.NewGuid();
        var joinerId = Guid.NewGuid();
        var domino = NewWaiting(creatorId, GameVariant.Domino, 200m);
        var makopa = NewWaiting(creatorId, GameVariant.Makopa, 300m);
        var repo = new ListableGameSessionRepository(domino, makopa);
        var sut = CreateSut(repo);

        var onlyDomino = await sut.ListOpenJoinableGamesAsync(joinerId, variant: GameVariant.Domino);
        var all = await sut.ListOpenJoinableGamesAsync(joinerId, variant: null);

        Assert.Single(onlyDomino);
        Assert.Equal(domino.Id, onlyDomino[0].Id);
        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task ListOpenJoinableGamesAsync_ExcludesGamesThatAlreadyHaveOpponent()
    {
        var creatorId = Guid.NewGuid();
        var joinerId = Guid.NewGuid();
        var otherJoiner = Guid.NewGuid();
        var open = NewWaiting(creatorId, GameVariant.Domino, 200m);
        var taken = NewWaiting(creatorId, GameVariant.Domino, 250m);
        taken.OpponentPlayerId = otherJoiner;
        var repo = new ListableGameSessionRepository(open, taken);
        var sut = CreateSut(repo);

        var list = await sut.ListOpenJoinableGamesAsync(joinerId, variant: GameVariant.Domino);

        Assert.Single(list);
        Assert.Equal(open.Id, list[0].Id);
    }

    [Fact]
    public async Task CreateThenList_OtherPlayerSeesWaitingGame()
    {
        var creatorId = Guid.NewGuid();
        var joinerId = Guid.NewGuid();
        var repo = new ListableGameSessionRepository();
        var wallet = new RecordingWalletService();
        var sut = new GameSessionService(
            repo,
            new InMemoryPlayerRepository(
                new Player { Id = creatorId, PlayerName = "Creator" },
                new Player { Id = joinerId, PlayerName = "Joiner" }),
            wallet,
            new NoOpGameEngineService(),
            new RecordingGameSessionNotifier(),
            NoOpInfluencerAttributionService.Instance,
            NoOpNotificationService.Instance);

        var created = await sut.CreateGameAsync(creatorId, 200m, GameVariant.Domino);
        var openForJoiner = await sut.ListOpenJoinableGamesAsync(joinerId, variant: GameVariant.Domino);
        var openForCreator = await sut.ListOpenJoinableGamesAsync(creatorId, variant: GameVariant.Domino);

        Assert.Equal(GameStatus.Waiting, created.Status);
        Assert.Equal(GameVariant.Domino, created.Variant);
        Assert.Single(openForJoiner);
        Assert.Equal(created.Id, openForJoiner[0].Id);
        Assert.Empty(openForCreator);
        Assert.Single(wallet.Locks);
        Assert.Equal((creatorId, 200m), wallet.Locks[0]);
    }

    private static GameSessionService CreateSut(IGameSessionRepository repo) =>
        new(
            repo,
            new InMemoryPlayerRepository(),
            new RecordingWalletService(),
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

    /// <summary>In-memory session store that implements joinable waiting-list queries.</summary>
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
                    .OrderBy(s => s.CreatedAt)
                    .ToList());

        public Task<IReadOnlyList<GameSession>> GetJoinableWaitingSessionsAsync(
            Guid forPlayerId,
            int skip,
            int take,
            GameVariant? variant = null,
            CancellationToken cancellationToken = default)
        {
            IEnumerable<GameSession> q = _sessions
                .Where(s => s.Status == GameStatus.Waiting && s.OpponentPlayerId == null && s.CreatorPlayerId != forPlayerId);
            if (variant.HasValue)
                q = q.Where(s => s.Variant == variant.Value);
            return Task.FromResult<IReadOnlyList<GameSession>>(
                q.OrderByDescending(s => s.CreatedAt).Skip(skip).Take(take).ToList());
        }

        public Task<IReadOnlyList<GameSession>> GetByPlayerIdAsync(Guid playerId, int skip, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<GameSession>>(
                _sessions
                    .Where(s => s.CreatorPlayerId == playerId || s.OpponentPlayerId == playerId)
                    .OrderByDescending(s => s.CreatedAt)
                    .Skip(skip)
                    .Take(take)
                    .ToList());

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
