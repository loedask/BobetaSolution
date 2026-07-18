using System.Security.Claims;
using Bobeta.API.App.Services;
using Bobeta.API.Services;
using Bobeta.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Bobeta.API.Hubs;

/// <summary>SignalR hub for real-time game events: join/leave session, play card, broadcast state and result. Requires JWT (query or header).</summary>
[Authorize]
public class GameHub(
    IGameSessionConnectionTracker sessionConnectionTracker,
    IGameInactivityCoordinator gameInactivityCoordinator,
    IGameSessionService gameSessionService) : Hub
{
    private readonly IGameSessionConnectionTracker _sessionConnectionTracker = sessionConnectionTracker;
    private readonly IGameInactivityCoordinator _gameInactivityCoordinator = gameInactivityCoordinator;
    private readonly IGameSessionService _gameSessionService = gameSessionService;
    /// <summary>Prefix for group names; group id is GroupPrefix + sessionId.</summary>
    public const string GroupPrefix = "Game_";

    /// <summary>Adds the connection to the group for the given game session (receives broadcasts for that game).</summary>
    public async Task JoinGameSession(Guid sessionId)
    {
        var playerId = GetPlayerId(Context);
        _sessionConnectionTracker.AddConnection(sessionId, playerId, Context.ConnectionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupPrefix + sessionId);
    }

    /// <summary>Removes the connection from the game session group.</summary>
    public async Task LeaveGameSession(Guid sessionId)
    {
        _sessionConnectionTracker.RemoveConnection(Context.ConnectionId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupPrefix + sessionId);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _sessionConnectionTracker.RemoveConnection(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>Notifies others in the session that a card was played (same payload shape as HTTP play-card).</summary>
    public async Task PlayCard(Guid sessionId, string cardSuitRank)
    {
        var moverId = GetPlayerId(Context);
        await Clients.OthersInGroup(GroupPrefix + sessionId).SendAsync("NotifyOpponentMove", moverId, cardSuitRank);
    }

    /// <summary>Client finished loading the table; starts inactivity tracking for an active match.</summary>
    public Task NotifyGameReadyForInactivity(Guid sessionId) =>
        _gameInactivityCoordinator.NotifyGameReadyAsync(sessionId, GetPlayerId(Context), Context.ConnectionAborted);

    public Task PauseInactivity(Guid sessionId)
    {
        _gameInactivityCoordinator.Pause(sessionId);
        return Task.CompletedTask;
    }

    public Task ResumeInactivity(Guid sessionId)
    {
        _gameInactivityCoordinator.Resume(sessionId);
        return Task.CompletedTask;
    }

    public Task InactivityContinue(Guid sessionId) =>
        _gameInactivityCoordinator.ContinueAsync(sessionId, GetPlayerId(Context), Context.ConnectionAborted);

    public Task InactivityCancelGame(Guid sessionId) =>
        _gameInactivityCoordinator.CancelByPlayerAsync(sessionId, GetPlayerId(Context), Context.ConnectionAborted);

    /// <summary>Caller forfeits; opponent wins. Broadcasts forfeit + GameResult to the session group.</summary>
    public async Task ForfeitGame(Guid sessionId)
    {
        var loserId = GetPlayerId(Context);
        var outcome = await _gameSessionService.ForfeitGameAsync(loserId, sessionId, Context.ConnectionAborted);
        if (outcome == null)
            throw new HubException("This game cannot be forfeited.");

        _gameInactivityCoordinator.UnregisterSession(sessionId);
        var group = GroupPrefix + sessionId;
        var payload = new { winnerPlayerId = outcome.WinnerPlayerId, loserPlayerId = outcome.LoserPlayerId, reason = "forfeit" };
        await Clients.Group(group).SendAsync("GameEndedByForfeit", payload);
        await Clients.Group(group).SendAsync("GameResult", outcome.WinnerPlayerId);
    }

    private static Guid GetPlayerId(HubCallerContext context)
    {
        var v = context.User?.FindFirst("playerId")?.Value
            ?? context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return v == null ? throw new HubException("Missing player id.") : Guid.Parse(v);
    }

    /// <summary>Broadcasts the current game state to all connections in the session.</summary>
    public async Task BroadcastGameState(Guid sessionId, object gameState)
    {
        await Clients.Group(GroupPrefix + sessionId).SendAsync("GameState", gameState);
    }

    /// <summary>Sends the game result to all connections in the session.</summary>
    public async Task SendGameResult(Guid sessionId, object result)
    {
        await Clients.Group(GroupPrefix + sessionId).SendAsync("GameResult", result);
    }
}
