using Bobeta.Application.DTOs.Game;
using Bobeta.Application.Interfaces;
using Bobeta.API.App.Services;
using Bobeta.API.Hubs;
using Bobeta.API.Services;
using Bobeta.Domain.Entities;
using Bobeta.Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Bobeta.API.Controllers;

/// <summary>API for Makopa gameplay: start game (deal), play card, get game state. Server broadcasts updates via SignalR.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GamePlayController(
    IGameEngineService gameEngineService,
    IHubContext<GameHub> hubContext,
    IGameSessionRepository sessionRepository,
    IGameSessionConnectionTracker sessionConnectionTracker,
    IGameInactivityCoordinator gameInactivityCoordinator,
    ILogger<GamePlayController> logger) : ControllerBase
{
    private readonly IGameEngineService _gameEngineService = gameEngineService;
    private readonly IHubContext<GameHub> _hubContext = hubContext;
    private readonly IGameSessionRepository _sessionRepository = sessionRepository;
    private readonly IGameSessionConnectionTracker _sessionConnectionTracker = sessionConnectionTracker;
    private readonly IGameInactivityCoordinator _gameInactivityCoordinator = gameInactivityCoordinator;

    private Guid PlayerId => Guid.Parse(User.FindFirst("playerId")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());

    /// <summary>Starts a match: deals 4 cards each from shuffled 52; unused cards stay out of play; random first leader.</summary>
    [HttpPost("start")]
    public async Task<IActionResult> StartGame([FromQuery] Guid sessionId, CancellationToken cancellationToken)
    {
        await _gameEngineService.StartGameAsync(sessionId, cancellationToken);
        await _gameInactivityCoordinator.RecordGameplayActivityAsync(sessionId, cancellationToken);
        return Ok();
    }

    /// <summary>Plays a card in the current turn. Server validates turn and follow-suit rule, then broadcasts state and move via SignalR.</summary>
    [HttpPost("play-card")]
    public async Task<ActionResult<GameStateDto>> PlayCard([FromBody] PlayCardRequest request, CancellationToken cancellationToken)
    {
        var card = new Card(request.Card.Suit, request.Card.Rank);
        var state = await _gameEngineService.PlayCardAsync(PlayerId, request.SessionId, card, cancellationToken);
        if (state == null) return BadRequest("Invalid move or game state.");

        var groupName = GameHub.GroupPrefix + request.SessionId;
        var cardSuitRank = $"{request.Card.Suit}_{(int)request.Card.Rank}";

        // Include mover id so WASM/mobile clients can ignore their own play (IHubContext has no "caller" for OthersInGroup).
        await _hubContext.Clients.Group(groupName).SendAsync("NotifyOpponentMove", PlayerId, cardSuitRank);

        await _gameInactivityCoordinator.RecordGameplayActivityAsync(request.SessionId, cancellationToken);
        if (state.GameOver)
        {
            _gameInactivityCoordinator.UnregisterSession(request.SessionId);
            // GameResult first so clients show the winner screen before any seat-specific GameState that might still be in flight.
            await _hubContext.Clients.Group(groupName).SendAsync("GameResult", state.WinnerPlayerId);
        }

        // GameStateDto is per-viewer (myCards, opponent name). Broadcasting the mover's DTO to the whole group
        // overwrote the other client's hand and could desync turn UI — send each seat their own state via JWT user id.
        var sessionRow = await _sessionRepository.GetByIdAsync(request.SessionId, cancellationToken);
        await PushGameStateToParticipantsAsync(sessionRow, request.SessionId, state, cancellationToken);

        return Ok(state);
    }

    /// <summary>Responder holds no cards of the led suit: the lead card is added to the responder&apos;s hand; the leader opens again.</summary>
    [HttpPost("void-follow")]
    public async Task<ActionResult<GameStateDto>> VoidFollowDraw([FromQuery] Guid sessionId, CancellationToken cancellationToken)
    {
        var state = await _gameEngineService.VoidFollowDrawAsync(PlayerId, sessionId, cancellationToken);
        if (state == null) return BadRequest("Invalid move or game state.");

        await _gameInactivityCoordinator.RecordGameplayActivityAsync(sessionId, cancellationToken);
        if (state.GameOver)
        {
            _gameInactivityCoordinator.UnregisterSession(sessionId);
            var groupName = GameHub.GroupPrefix + sessionId;
            await _hubContext.Clients.Group(groupName).SendAsync("GameResult", state.WinnerPlayerId);
        }

        var sessionRow = await _sessionRepository.GetByIdAsync(sessionId, cancellationToken);
        await PushGameStateToParticipantsAsync(sessionRow, sessionId, state, cancellationToken);

        return Ok(state);
    }

    private async Task PushGameStateToParticipantsAsync(
        GameSession? session,
        Guid sessionId,
        GameStateDto moverFallbackState,
        CancellationToken cancellationToken)
    {
        if (session?.OpponentPlayerId is { } opponentId)
        {
            var creatorState = await _gameEngineService.GetGameStateAsync(session.CreatorPlayerId, sessionId, cancellationToken);
            var opponentState = await _gameEngineService.GetGameStateAsync(opponentId, sessionId, cancellationToken);
            await SendGameStateToSeatAsync(sessionId, session.CreatorPlayerId, creatorState, cancellationToken);
            await SendGameStateToSeatAsync(sessionId, opponentId, opponentState, cancellationToken);
            return;
        }

        var groupName = GameHub.GroupPrefix + sessionId;
        await _hubContext.Clients.Group(groupName).SendAsync("GameState", moverFallbackState, cancellationToken);
    }

    /// <summary>Delivers seat-specific state to connections that joined the session hub; falls back to <see cref="IHubClients{T}.User(string)"/> if none are registered yet.</summary>
    private async Task SendGameStateToSeatAsync(
        Guid sessionId,
        Guid playerId,
        GameStateDto? state,
        CancellationToken cancellationToken)
    {
        if (state == null)
            return;
        var connectionIds = _sessionConnectionTracker.GetConnectionIds(sessionId, playerId);
        var delivery = connectionIds.Count > 0 ? $"connectionIds={connectionIds.Count}" : "user-fallback(UserIdProvider)";
        logger.LogInformation(
            "SignalR GameState emit session={SessionId} player={PlayerId} delivery={Delivery} gameOver={GameOver} turn={Turn}",
            sessionId, playerId, delivery, state.GameOver, state.CurrentTurnPlayerId);

        if (connectionIds.Count > 0)
        {
            foreach (var connectionId in connectionIds)
                await _hubContext.Clients.Client(connectionId).SendAsync("GameState", state, cancellationToken);
            return;
        }

        logger.LogWarning(
            "SignalR GameState session={SessionId} player={PlayerId}: no tracked hub connections — using IUserIdProvider fallback.",
            sessionId, playerId);
        await _hubContext.Clients.User(playerId.ToString()).SendAsync("GameState", state, cancellationToken);
    }

    /// <summary>Gets the current game state for the authenticated player (hand, last card, whose turn, game over, winner).</summary>
    [HttpGet("state")]
    public async Task<ActionResult<GameStateDto>> GetGameState([FromQuery] Guid sessionId, CancellationToken cancellationToken)
    {
        var state = await _gameEngineService.GetGameStateAsync(PlayerId, sessionId, cancellationToken);
        if (state == null) return NotFound();
        return Ok(state);
    }
}
