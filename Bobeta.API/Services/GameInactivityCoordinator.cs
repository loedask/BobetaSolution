using System.Collections.Generic;
using Bobeta.API.App.Services;
using Bobeta.API.Hubs;
using Bobeta.Application.Interfaces;
using Bobeta.Domain.Enums;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bobeta.API.Services;

/// <summary>Server-side AFK tracking for live games: idle thresholds, synchronized warning deadlines, cancel/suspend.</summary>
public interface IGameInactivityCoordinator
{
    Task NotifyGameReadyAsync(Guid sessionId, Guid playerId, CancellationToken cancellationToken = default);
    void UnregisterSession(Guid sessionId);
    Task RecordGameplayActivityAsync(Guid sessionId, CancellationToken cancellationToken = default);
    void Pause(Guid sessionId);
    void Resume(Guid sessionId);
    Task ContinueAsync(Guid sessionId, Guid playerId, CancellationToken cancellationToken = default);
    Task CancelByPlayerAsync(Guid sessionId, Guid playerId, CancellationToken cancellationToken = default);
    Task TickAsync(CancellationToken cancellationToken = default);
}

internal enum InactivityPopupPhase
{
    None = 0,
    First = 1,
    Second = 2
}

internal sealed class SessionInactivityState
{
    public DateTime LastActivityUtc { get; set; }
    /// <summary>After first Continue, idle threshold drops from 60s to 40s until the next real move.</summary>
    public bool ShortIdleAfterContinue { get; set; }
    public InactivityPopupPhase Phase { get; set; }
    public DateTime? DecisionDeadlineUtc { get; set; }
    public int PauseRefCount { get; set; }
    public DateTime? PauseStartedUtc { get; set; }
}

/// <inheritdoc />
public sealed class GameInactivityCoordinator(
    IServiceScopeFactory scopeFactory,
    IHubContext<GameHub> hubContext,
    IGameSessionConnectionTracker sessionConnectionTracker,
    ILogger<GameInactivityCoordinator> logger) : IGameInactivityCoordinator
{
    public const int FirstIdleSeconds = 60;
    public const int SecondIdleSeconds = 40;
    public const int DecisionSeconds = 10;

    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly IHubContext<GameHub> _hubContext = hubContext;
    private readonly IGameSessionConnectionTracker _sessionConnectionTracker = sessionConnectionTracker;
    private readonly ILogger<GameInactivityCoordinator> _logger = logger;

    private readonly object _sync = new();
    private readonly Dictionary<Guid, SessionInactivityState> _sessions = new();

    /// <inheritdoc />
    public async Task NotifyGameReadyAsync(Guid sessionId, Guid playerId, CancellationToken cancellationToken = default)
    {
        if (!await IsParticipantAsync(sessionId, playerId, cancellationToken))
            return;

        lock (_sync)
        {
            var now = DateTime.UtcNow;
            if (!_sessions.TryGetValue(sessionId, out var state))
            {
                state = new SessionInactivityState();
                _sessions[sessionId] = state;
            }

            state.LastActivityUtc = now;
        }
    }

    /// <inheritdoc />
    public void UnregisterSession(Guid sessionId)
    {
        lock (_sync)
            _sessions.Remove(sessionId);
    }

    /// <inheritdoc />
    public async Task RecordGameplayActivityAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        bool dismissPopup;
        lock (_sync)
        {
            dismissPopup = false;
            if (!_sessions.TryGetValue(sessionId, out var state))
            {
                state = new SessionInactivityState();
                _sessions[sessionId] = state;
            }

            var now = DateTime.UtcNow;
            state.LastActivityUtc = now;
            state.ShortIdleAfterContinue = false;
            if (state.Phase != InactivityPopupPhase.None)
            {
                state.Phase = InactivityPopupPhase.None;
                state.DecisionDeadlineUtc = null;
                state.PauseStartedUtc = null;
                dismissPopup = true;
            }
        }

        if (dismissPopup)
        {
            var group = GameHub.GroupPrefix + sessionId;
            await _hubContext.Clients.Group(group).SendAsync("InactivityWarningDismissed", cancellationToken);
        }
    }

    /// <inheritdoc />
    public void Pause(Guid sessionId)
    {
        lock (_sync)
        {
            if (!_sessions.TryGetValue(sessionId, out var state))
                return;
            state.PauseRefCount++;
            if (state.PauseRefCount == 1 && state.Phase != InactivityPopupPhase.None && state.DecisionDeadlineUtc.HasValue)
                state.PauseStartedUtc = DateTime.UtcNow;
        }
    }

    /// <inheritdoc />
    public void Resume(Guid sessionId)
    {
        lock (_sync)
        {
            if (!_sessions.TryGetValue(sessionId, out var state))
                return;
            state.PauseRefCount = Math.Max(0, state.PauseRefCount - 1);
            var now = DateTime.UtcNow;
            if (state.PauseRefCount == 0 && state.PauseStartedUtc is { } pauseStart && state.DecisionDeadlineUtc is { } deadline)
            {
                var delta = now - pauseStart;
                state.DecisionDeadlineUtc = deadline + delta;
                state.PauseStartedUtc = null;
            }

            if (state.PauseRefCount == 0 && state.Phase == InactivityPopupPhase.None)
                state.LastActivityUtc = now;
        }
    }

    /// <inheritdoc />
    public async Task ContinueAsync(Guid sessionId, Guid playerId, CancellationToken cancellationToken = default)
    {
        if (!await IsParticipantAsync(sessionId, playerId, cancellationToken))
            return;

        lock (_sync)
        {
            if (!_sessions.TryGetValue(sessionId, out var state) || state.Phase != InactivityPopupPhase.First)
                return;
            state.Phase = InactivityPopupPhase.None;
            state.DecisionDeadlineUtc = null;
            state.ShortIdleAfterContinue = true;
            state.LastActivityUtc = DateTime.UtcNow;
            state.PauseStartedUtc = null;
        }

        var group = GameHub.GroupPrefix + sessionId;
        await _hubContext.Clients.Group(group).SendAsync("InactivityWarningDismissed", cancellationToken);
    }

    /// <inheritdoc />
    public async Task CancelByPlayerAsync(Guid sessionId, Guid playerId, CancellationToken cancellationToken = default)
    {
        if (!await IsParticipantAsync(sessionId, playerId, cancellationToken))
            return;
        await EndGameDueToInactivityAsync(sessionId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task TickAsync(CancellationToken cancellationToken = default)
    {
        List<(Guid SessionId, bool SecondStrike)> idleWarnings = new();
        List<Guid> deadlineTimeouts = new();

        var now = DateTime.UtcNow;
        lock (_sync)
        {
            foreach (var (sessionId, state) in _sessions)
            {
                if (state.PauseRefCount > 0)
                    continue;

                if (state.Phase != InactivityPopupPhase.None && state.DecisionDeadlineUtc is { } deadline)
                {
                    if (now >= deadline)
                        deadlineTimeouts.Add(sessionId);
                    continue;
                }

                var idleSec = (now - state.LastActivityUtc).TotalSeconds;
                if (state.ShortIdleAfterContinue)
                {
                    if (idleSec >= SecondIdleSeconds)
                        idleWarnings.Add((sessionId, SecondStrike: true));
                }
                else
                {
                    if (idleSec >= FirstIdleSeconds)
                        idleWarnings.Add((sessionId, SecondStrike: false));
                }
            }
        }

        foreach (var sessionId in deadlineTimeouts)
            await EndGameDueToInactivityAsync(sessionId, cancellationToken);

        foreach (var (sessionId, secondStrike) in idleWarnings)
            await TryOpenWarningAsync(sessionId, secondStrike, cancellationToken);
    }

    private async Task TryOpenWarningAsync(Guid sessionId, bool secondStrike, CancellationToken cancellationToken)
    {
        if (!await IsSessionInProgressAsync(sessionId, cancellationToken))
        {
            UnregisterSession(sessionId);
            return;
        }

        var deadline = DateTime.UtcNow.AddSeconds(DecisionSeconds);
        var phase = secondStrike ? InactivityPopupPhase.Second : InactivityPopupPhase.First;
        var showButtons = !secondStrike;

        lock (_sync)
        {
            if (!_sessions.TryGetValue(sessionId, out var state))
                return;
            if (state.Phase != InactivityPopupPhase.None)
                return;
            if (state.PauseRefCount > 0)
                return;
            if (secondStrike && !state.ShortIdleAfterContinue)
                return;
            if (!secondStrike && state.ShortIdleAfterContinue)
                return;

            state.Phase = phase;
            state.DecisionDeadlineUtc = deadline;
        }

        var group = GameHub.GroupPrefix + sessionId;
        await _hubContext.Clients.Group(group).SendAsync(
            "InactivityWarning",
            new { phase = (int)phase, decisionDeadlineUtc = deadline, showButtons },
            cancellationToken);
        _logger.LogInformation("Inactivity warning phase={Phase} session={SessionId} deadline={Deadline:o}", phase, sessionId,
            deadline);
    }

    private async Task EndGameDueToInactivityAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        lock (_sync)
            _sessions.Remove(sessionId);

        Guid? creatorId = null;
        Guid? opponentId = null;
        await using (var scope = _scopeFactory.CreateAsyncScope())
        {
            var session = await scope.ServiceProvider
                .GetRequiredService<IGameSessionRepository>()
                .GetByIdAsync(sessionId, cancellationToken);
            if (session?.OpponentPlayerId is { } opp)
            {
                creatorId = session.CreatorPlayerId;
                opponentId = opp;
            }
        }

        var ok = await CancelSessionInScopeAsync(sessionId, cancellationToken);

        // Always notify clients so overlays dismiss even when the session was already cancelled (e.g. deadline + manual cancel).
        var payload = new { reason = "inactivity" };
        var group = GameHub.GroupPrefix + sessionId;
        await _hubContext.Clients.Group(group).SendAsync("GameEndedByInactivity", payload, cancellationToken);
        if (creatorId is { } creator && opponentId is { } opponent)
        {
            await SendGameEndedByInactivityToPlayerAsync(sessionId, creator, payload, cancellationToken);
            await SendGameEndedByInactivityToPlayerAsync(sessionId, opponent, payload, cancellationToken);
        }

        if (ok)
            _logger.LogInformation("Game ended due to inactivity session={SessionId}", sessionId);
    }

    private async Task SendGameEndedByInactivityToPlayerAsync(
        Guid sessionId,
        Guid playerId,
        object payload,
        CancellationToken cancellationToken)
    {
        var connectionIds = _sessionConnectionTracker.GetConnectionIds(sessionId, playerId);
        if (connectionIds.Count > 0)
        {
            foreach (var connectionId in connectionIds)
                await _hubContext.Clients.Client(connectionId).SendAsync("GameEndedByInactivity", payload, cancellationToken);
            return;
        }

        await _hubContext.Clients.User(playerId.ToString()).SendAsync("GameEndedByInactivity", payload, cancellationToken);
    }

    private async Task<bool> IsParticipantAsync(Guid sessionId, Guid playerId, CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var session = await scope.ServiceProvider
            .GetRequiredService<IGameSessionRepository>()
            .GetByIdAsync(sessionId, cancellationToken);
        if (session == null || session.Status != GameStatus.InProgress || session.OpponentPlayerId == null)
            return false;
        return playerId == session.CreatorPlayerId || playerId == session.OpponentPlayerId.Value;
    }

    private async Task<bool> IsSessionInProgressAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var session = await scope.ServiceProvider
            .GetRequiredService<IGameSessionRepository>()
            .GetByIdAsync(sessionId, cancellationToken);
        return session != null && session.Status == GameStatus.InProgress;
    }

    private async Task<bool> CancelSessionInScopeAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        return await scope.ServiceProvider
            .GetRequiredService<IGameSessionService>()
            .CancelInProgressGameAsync(sessionId, cancellationToken);
    }
}
