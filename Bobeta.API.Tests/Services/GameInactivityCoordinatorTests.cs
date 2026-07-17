using Bobeta.API.App.Services;
using Bobeta.API.Hubs;
using Bobeta.API.Services;
using Bobeta.Application.DTOs.Game;
using Bobeta.Application.Interfaces;
using Bobeta.Domain.Entities;
using Bobeta.Domain.Enums;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Bobeta.API.Tests.Services;

public class GameInactivityCoordinatorTests
{
    private readonly Guid _sessionId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private readonly Guid _creatorId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private readonly Guid _opponentId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    [Fact]
    public async Task TickAsync_After60SecondsIdle_OpensFirstWarningWithButtons()
    {
        var hub = new RecordingGameHubContext();
        var sessions = new InMemoryGameSessionRepository(CreateInProgressSession());
        var coordinator = CreateCoordinator(hub, sessions);

        await coordinator.NotifyGameReadyAsync(_sessionId, _creatorId);
        BackdateActivity(coordinator, _sessionId, TimeSpan.FromSeconds(GameInactivityCoordinator.FirstIdleSeconds + 1));

        await coordinator.TickAsync();

        var warning = Assert.Single(hub.GroupMessages, m => m.Method == "InactivityWarning");
        Assert.Equal(_sessionId, warning.SessionId);
        Assert.Equal(1, warning.Phase);
        Assert.True(warning.ShowButtons);
    }

    [Fact]
    public async Task ContinueAsync_AfterFirstWarning_Uses40SecondIdleThreshold()
    {
        var hub = new RecordingGameHubContext();
        var sessions = new InMemoryGameSessionRepository(CreateInProgressSession());
        var coordinator = CreateCoordinator(hub, sessions);

        await coordinator.NotifyGameReadyAsync(_sessionId, _creatorId);
        BackdateActivity(coordinator, _sessionId, TimeSpan.FromSeconds(GameInactivityCoordinator.FirstIdleSeconds + 1));
        await coordinator.TickAsync();
        hub.GroupMessages.Clear();

        await coordinator.ContinueAsync(_sessionId, _creatorId);
        BackdateActivity(coordinator, _sessionId, TimeSpan.FromSeconds(GameInactivityCoordinator.SecondIdleSeconds + 1));

        await coordinator.TickAsync();

        var warning = Assert.Single(hub.GroupMessages, m => m.Method == "InactivityWarning");
        Assert.Equal(2, warning.Phase);
        Assert.False(warning.ShowButtons);
    }

    [Fact]
    public async Task TickAsync_WhenDecisionDeadlineExpires_CancelsGameAndNotifiesClients()
    {
        var hub = new RecordingGameHubContext();
        var sessions = new InMemoryGameSessionRepository(CreateInProgressSession());
        var cancels = new RecordingGameSessionService();
        var coordinator = CreateCoordinator(hub, sessions, cancels);

        await coordinator.NotifyGameReadyAsync(_sessionId, _creatorId);
        BackdateActivity(coordinator, _sessionId, TimeSpan.FromSeconds(GameInactivityCoordinator.FirstIdleSeconds + 1));
        await coordinator.TickAsync();
        BackdateDecisionDeadline(coordinator, _sessionId, TimeSpan.FromSeconds(1));

        await coordinator.TickAsync();

        Assert.Contains(_sessionId, cancels.CancelledSessions);
        Assert.Contains(hub.GroupMessages, m => m.Method == "GameEndedByInactivity");
    }

    [Fact]
    public async Task RecordGameplayActivityAsync_DismissesOpenWarning()
    {
        var hub = new RecordingGameHubContext();
        var sessions = new InMemoryGameSessionRepository(CreateInProgressSession());
        var coordinator = CreateCoordinator(hub, sessions);

        await coordinator.NotifyGameReadyAsync(_sessionId, _creatorId);
        BackdateActivity(coordinator, _sessionId, TimeSpan.FromSeconds(GameInactivityCoordinator.FirstIdleSeconds + 1));
        await coordinator.TickAsync();
        hub.GroupMessages.Clear();

        await coordinator.RecordGameplayActivityAsync(_sessionId);

        var dismiss = Assert.Single(hub.GroupMessages);
        Assert.Equal("InactivityWarningDismissed", dismiss.Method);
    }

    private GameSession CreateInProgressSession() => new()
    {
        Id = _sessionId,
        CreatorPlayerId = _creatorId,
        OpponentPlayerId = _opponentId,
        BetAmount = 200m,
        Status = GameStatus.InProgress,
        CreatedAt = DateTime.UtcNow
    };

    private static GameInactivityCoordinator CreateCoordinator(
        RecordingGameHubContext hub,
        InMemoryGameSessionRepository sessions,
        RecordingGameSessionService? cancelService = null)
    {
        cancelService ??= new RecordingGameSessionService();
        var scopeFactory = new SingleScopeFactory(sessions, cancelService);
        return new GameInactivityCoordinator(
            scopeFactory,
            hub,
            new EmptyConnectionTracker(),
            NullLogger<GameInactivityCoordinator>.Instance);
    }

    private static void BackdateActivity(GameInactivityCoordinator coordinator, Guid sessionId, TimeSpan by)
    {
        var sessionsField = typeof(GameInactivityCoordinator).GetField("_sessions", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        var syncField = typeof(GameInactivityCoordinator).GetField("_sync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        var dict = (Dictionary<Guid, SessionInactivityState>)sessionsField.GetValue(coordinator)!;
        lock (syncField.GetValue(coordinator)!)
            dict[sessionId].LastActivityUtc = DateTime.UtcNow - by;
    }

    private static void BackdateDecisionDeadline(GameInactivityCoordinator coordinator, Guid sessionId, TimeSpan by)
    {
        var sessionsField = typeof(GameInactivityCoordinator).GetField("_sessions", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        var syncField = typeof(GameInactivityCoordinator).GetField("_sync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        var dict = (Dictionary<Guid, SessionInactivityState>)sessionsField.GetValue(coordinator)!;
        lock (syncField.GetValue(coordinator)!)
            dict[sessionId].DecisionDeadlineUtc = DateTime.UtcNow - by;
    }

    private sealed class SingleScopeFactory(InMemoryGameSessionRepository sessions, RecordingGameSessionService cancelService) : IServiceScopeFactory
    {
        public IServiceScope CreateScope() => new SingleScope(sessions, cancelService);
    }

    private sealed class SingleScope(InMemoryGameSessionRepository sessions, RecordingGameSessionService cancelService) : IServiceScope
    {
        public IServiceProvider ServiceProvider { get; } = new SingleProvider(sessions, cancelService);
        public void Dispose() { }
    }

    private sealed class SingleProvider(InMemoryGameSessionRepository sessions, RecordingGameSessionService cancelService) : IServiceProvider
    {
        public object? GetService(Type serviceType)
        {
            if (serviceType == typeof(IGameSessionRepository))
                return sessions;
            if (serviceType == typeof(IGameSessionService))
                return cancelService;
            return null;
        }
    }

    private sealed class InMemoryGameSessionRepository(GameSession session) : IGameSessionRepository
    {
        public Task<GameSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult<GameSession?>(id == session.Id ? session : null);

        public Task<IReadOnlyList<GameSession>> GetWaitingSessionsAsync(decimal betAmount, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<GameSession>>(Array.Empty<GameSession>());

        public Task<IReadOnlyList<GameSession>> GetJoinableWaitingSessionsAsync(Guid forPlayerId, int skip, int take, GameVariant? variant = null, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<GameSession>>(Array.Empty<GameSession>());

        public Task<IReadOnlyList<GameSession>> GetByPlayerIdAsync(Guid playerId, int skip, int take, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<GameSession>>(Array.Empty<GameSession>());

        public Task<GameSession> AddAsync(GameSession session, CancellationToken cancellationToken = default)
            => Task.FromResult(session);

        public Task UpdateAsync(GameSession session, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class RecordingGameSessionService : IGameSessionService
    {
        public HashSet<Guid> CancelledSessions { get; } = new();

        public Task<GameSessionDto> CreateGameAsync(Guid playerId, decimal betAmount, GameVariant variant = GameVariant.Makopa, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<GameSessionDto?> JoinGameAsync(Guid playerId, Guid gameId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<GameSessionDto>> ListOpenJoinableGamesAsync(Guid playerId, int skip = 0, int take = 50, GameVariant? variant = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task ProposeNewBetAsync(Guid playerId, Guid gameId, decimal amount, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task AcceptBetChangeAsync(Guid gameId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<bool> CancelInProgressGameAsync(Guid sessionId, CancellationToken cancellationToken = default)
        {
            CancelledSessions.Add(sessionId);
            return Task.FromResult(true);
        }

        public Task<bool> CancelWaitingGameAsync(Guid playerId, Guid sessionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
    }

    private sealed class EmptyConnectionTracker : IGameSessionConnectionTracker
    {
        public void AddConnection(Guid sessionId, Guid playerId, string connectionId) { }
        public void RemoveConnection(string connectionId) { }
        public IReadOnlyList<string> GetConnectionIds(Guid sessionId, Guid playerId) => Array.Empty<string>();
    }

    private sealed class RecordingGameHubContext : IHubContext<GameHub>
    {
        public List<(Guid SessionId, string Method, int Phase, bool ShowButtons)> GroupMessages { get; } = new();

        IHubClients IHubContext<GameHub>.Clients => new RecordingHubClients(this);
        IGroupManager IHubContext<GameHub>.Groups => throw new NotSupportedException();
    }

    private sealed class RecordingHubClients(RecordingGameHubContext context) : IHubClients
    {
        public IClientProxy All => throw new NotSupportedException();
        public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => throw new NotSupportedException();
        public IClientProxy Client(string connectionId) => throw new NotSupportedException();
        public IClientProxy Clients(IReadOnlyList<string> connectionIds) => throw new NotSupportedException();
        public IClientProxy Group(string groupName) => new RecordingGroupProxy(context, ParseSessionId(groupName));
        public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => throw new NotSupportedException();
        public IClientProxy Groups(IReadOnlyList<string> groupNames) => throw new NotSupportedException();
        public IClientProxy User(string userId) => new RecordingGroupProxy(context, Guid.Empty);
        public IClientProxy Users(IReadOnlyList<string> userIds) => throw new NotSupportedException();
        public IClientProxy UsersExcept(IReadOnlyList<string> userIds) => throw new NotSupportedException();

        private static Guid ParseSessionId(string groupName) =>
            Guid.Parse(groupName.AsSpan(GameHub.GroupPrefix.Length));
    }

    private sealed class RecordingGroupProxy(RecordingGameHubContext context, Guid sessionId) : IClientProxy
    {
        public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
        {
            if (method == "InactivityWarning" && args[0] is { } payload)
            {
                var phase = (int)payload.GetType().GetProperty("phase")!.GetValue(payload)!;
                var showButtons = (bool)payload.GetType().GetProperty("showButtons")!.GetValue(payload)!;
                context.GroupMessages.Add((sessionId, method, phase, showButtons));
            }
            else
            {
                context.GroupMessages.Add((sessionId, method, 0, false));
            }

            return Task.CompletedTask;
        }
    }
}
