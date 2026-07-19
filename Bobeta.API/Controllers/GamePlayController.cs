using Bobeta.Application.Common;
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
    IGameSessionService gameSessionService,
    ILogger<GamePlayController> logger) : ControllerBase
{
    private readonly IGameEngineService _gameEngineService = gameEngineService;
    private readonly IHubContext<GameHub> _hubContext = hubContext;
    private readonly IGameSessionRepository _sessionRepository = sessionRepository;
    private readonly IGameSessionConnectionTracker _sessionConnectionTracker = sessionConnectionTracker;
    private readonly IGameInactivityCoordinator _gameInactivityCoordinator = gameInactivityCoordinator;
    private readonly IGameSessionService _gameSessionService = gameSessionService;

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
        var move = await _gameEngineService.PlayCardAsync(PlayerId, request.SessionId, card, cancellationToken);
        if (!move.IsSuccess)
            return BadRequest(new { code = move.ErrorCode, message = DescribeMoveError(move.ErrorCode) });

        var state = move.State!;
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
        var move = await _gameEngineService.VoidFollowDrawAsync(PlayerId, sessionId, cancellationToken);
        if (!move.IsSuccess)
            return BadRequest(new { code = move.ErrorCode, message = DescribeMoveError(move.ErrorCode) });

        var state = move.State!;

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

    /// <summary>Applies a Kopo move (path of squares: start, then each landing after a jump or quiet destination).</summary>
    [HttpPost("kopo/move")]
    public async Task<ActionResult<GameStateDto>> KopoMove([FromBody] KopoMoveRequest request, CancellationToken cancellationToken)
    {
        var path = request.Path.Select(p => (p.Row, p.Col)).ToList();
        var move = await _gameEngineService.ApplyKopoMoveAsync(PlayerId, request.SessionId, path, cancellationToken);
        if (!move.IsSuccess)
            return BadRequest(new { code = move.ErrorCode, message = DescribeKopoMoveError(move.ErrorCode) });

        var state = move.State!;
        await _gameInactivityCoordinator.RecordGameplayActivityAsync(request.SessionId, cancellationToken);
        if (state.GameOver)
        {
            _gameInactivityCoordinator.UnregisterSession(request.SessionId);
            var groupName = GameHub.GroupPrefix + request.SessionId;
            await _hubContext.Clients.Group(groupName).SendAsync("GameResult", state.WinnerPlayerId);
        }

        var sessionRow = await _sessionRepository.GetByIdAsync(request.SessionId, cancellationToken);
        await PushGameStateToParticipantsAsync(sessionRow, request.SessionId, state, cancellationToken);
        return Ok(state);
    }

    /// <summary>Sows the seeds from one pit in the current player's Ngola row.</summary>
    [HttpPost("ngola/move")]
    public async Task<ActionResult<GameStateDto>> NgolaMove([FromBody] NgolaMoveRequest request, CancellationToken cancellationToken)
    {
        var move = await _gameEngineService.ApplyNgolaMoveAsync(
            PlayerId, request.SessionId, request.PitIndex, cancellationToken);
        if (!move.IsSuccess)
            return BadRequest(new { code = move.ErrorCode, message = DescribeNgolaMoveError(move.ErrorCode) });

        var state = move.State!;
        await _gameInactivityCoordinator.RecordGameplayActivityAsync(request.SessionId, cancellationToken);
        if (state.GameOver)
        {
            _gameInactivityCoordinator.UnregisterSession(request.SessionId);
            await _hubContext.Clients.Group(GameHub.GroupPrefix + request.SessionId)
                .SendAsync("GameResult", state.WinnerPlayerId);
        }

        var sessionRow = await _sessionRepository.GetByIdAsync(request.SessionId, cancellationToken);
        await PushGameStateToParticipantsAsync(sessionRow, request.SessionId, state, cancellationToken);
        return Ok(state);
    }

    /// <summary>Plays, draws, or passes in a Domino match.</summary>
    [HttpPost("domino/move")]
    public async Task<ActionResult<GameStateDto>> DominoMove([FromBody] DominoMoveRequest request, CancellationToken cancellationToken)
    {
        var move = await _gameEngineService.ApplyDominoMoveAsync(
            PlayerId, request.SessionId, request.Action, request.High, request.Low, request.End, cancellationToken);
        if (!move.IsSuccess)
            return BadRequest(new { code = move.ErrorCode, message = DescribeDominoMoveError(move.ErrorCode) });

        var state = move.State!;
        await _gameInactivityCoordinator.RecordGameplayActivityAsync(request.SessionId, cancellationToken);
        if (state.GameOver)
        {
            _gameInactivityCoordinator.UnregisterSession(request.SessionId);
            await _hubContext.Clients.Group(GameHub.GroupPrefix + request.SessionId)
                .SendAsync("GameResult", state.WinnerPlayerId);
        }

        var sessionRow = await _sessionRepository.GetByIdAsync(request.SessionId, cancellationToken);
        await PushGameStateToParticipantsAsync(sessionRow, request.SessionId, state, cancellationToken);
        return Ok(state);
    }

    /// <summary>Throws Abbia tokens for the current player.</summary>
    [HttpPost("abbia/throw")]
    public async Task<ActionResult<GameStateDto>> AbbiaThrow([FromBody] AbbiaMoveRequest request, CancellationToken cancellationToken)
    {
        var move = await _gameEngineService.ApplyAbbiaThrowAsync(PlayerId, request.SessionId, cancellationToken);
        if (!move.IsSuccess)
            return BadRequest(new { code = move.ErrorCode, message = DescribeAbbiaMoveError(move.ErrorCode) });

        var state = move.State!;
        await _gameInactivityCoordinator.RecordGameplayActivityAsync(request.SessionId, cancellationToken);
        if (state.GameOver)
        {
            _gameInactivityCoordinator.UnregisterSession(request.SessionId);
            await _hubContext.Clients.Group(GameHub.GroupPrefix + request.SessionId)
                .SendAsync("GameResult", state.WinnerPlayerId);
        }

        var sessionRow = await _sessionRepository.GetByIdAsync(request.SessionId, cancellationToken);
        await PushGameStateToParticipantsAsync(sessionRow, request.SessionId, state, cancellationToken);
        return Ok(state);
    }

    /// <summary>Places or moves a stone in Nzengué.</summary>
    [HttpPost("nzengue/move")]
    public async Task<ActionResult<GameStateDto>> NzengueMove([FromBody] NzengueMoveRequest request, CancellationToken cancellationToken)
    {
        var move = await _gameEngineService.ApplyNzengueMoveAsync(
            PlayerId, request.SessionId, request.FromPoint, request.ToPoint, cancellationToken);
        if (!move.IsSuccess)
            return BadRequest(new { code = move.ErrorCode, message = DescribeNzengueMoveError(move.ErrorCode) });

        var state = move.State!;
        await _gameInactivityCoordinator.RecordGameplayActivityAsync(request.SessionId, cancellationToken);
        if (state.GameOver)
        {
            _gameInactivityCoordinator.UnregisterSession(request.SessionId);
            await _hubContext.Clients.Group(GameHub.GroupPrefix + request.SessionId)
                .SendAsync("GameResult", state.WinnerPlayerId);
        }

        var sessionRow = await _sessionRepository.GetByIdAsync(request.SessionId, cancellationToken);
        await PushGameStateToParticipantsAsync(sessionRow, request.SessionId, state, cancellationToken);
        return Ok(state);
    }

    /// <summary>Places, slides, or captures in Yoté.</summary>
    [HttpPost("yote/move")]
    public async Task<ActionResult<GameStateDto>> YoteMove([FromBody] YoteMoveRequest request, CancellationToken cancellationToken)
    {
        var move = await _gameEngineService.ApplyYoteMoveAsync(
            PlayerId, request.SessionId, request.FromCell, request.ToCell, request.ExtraRemoveCell, cancellationToken);
        if (!move.IsSuccess)
            return BadRequest(new { code = move.ErrorCode, message = DescribeYoteMoveError(move.ErrorCode) });

        var state = move.State!;
        await _gameInactivityCoordinator.RecordGameplayActivityAsync(request.SessionId, cancellationToken);
        if (state.GameOver)
        {
            _gameInactivityCoordinator.UnregisterSession(request.SessionId);
            await _hubContext.Clients.Group(GameHub.GroupPrefix + request.SessionId)
                .SendAsync("GameResult", state.WinnerPlayerId);
        }

        var sessionRow = await _sessionRepository.GetByIdAsync(request.SessionId, cancellationToken);
        await PushGameStateToParticipantsAsync(sessionRow, request.SessionId, state, cancellationToken);
        return Ok(state);
    }

    /// <summary>Gets the current game state for the authenticated player (hand, last card, whose turn, game over, winner).</summary>
    [HttpGet("state")]
    public async Task<ActionResult<GameStateDto>> GetGameState([FromQuery] Guid sessionId, CancellationToken cancellationToken)
    {
        var state = await _gameEngineService.GetGameStateAsync(PlayerId, sessionId, cancellationToken);
        if (state == null) return NotFound();
        return Ok(state);
    }

    /// <summary>Dismisses the first AFK warning and resumes play (HTTP fallback when SignalR invoke fails).</summary>
    [HttpPost("inactivity/continue")]
    public async Task<IActionResult> InactivityContinue([FromQuery] Guid sessionId, CancellationToken cancellationToken)
    {
        await _gameInactivityCoordinator.ContinueAsync(sessionId, PlayerId, cancellationToken);
        return Ok();
    }

    /// <summary>Cancels the in-progress match due to inactivity (HTTP fallback when SignalR invoke fails).</summary>
    [HttpPost("inactivity/cancel")]
    public async Task<IActionResult> InactivityCancel([FromQuery] Guid sessionId, CancellationToken cancellationToken)
    {
        await _gameInactivityCoordinator.CancelByPlayerAsync(sessionId, PlayerId, cancellationToken);
        return Ok();
    }

    /// <summary>Player leaves and forfeits: opponent wins the pot (minus platform fee).</summary>
    [HttpPost("forfeit")]
    public async Task<IActionResult> Forfeit([FromQuery] Guid sessionId, CancellationToken cancellationToken)
    {
        var outcome = await _gameSessionService.ForfeitGameAsync(PlayerId, sessionId, cancellationToken);
        if (outcome == null)
            return BadRequest(new { code = "cannot_forfeit", message = "This game cannot be forfeited." });

        _gameInactivityCoordinator.UnregisterSession(sessionId);
        var groupName = GameHub.GroupPrefix + sessionId;
        var payload = new { winnerPlayerId = outcome.WinnerPlayerId, loserPlayerId = outcome.LoserPlayerId, reason = "forfeit" };
        await _hubContext.Clients.Group(groupName).SendAsync("GameEndedByForfeit", payload, cancellationToken);
        await _hubContext.Clients.Group(groupName).SendAsync("GameResult", outcome.WinnerPlayerId, cancellationToken);
        logger.LogInformation(
            "Game forfeited session={SessionId} loser={LoserId} winner={WinnerId}",
            sessionId, outcome.LoserPlayerId, outcome.WinnerPlayerId);
        return Ok(outcome);
    }

    private static string DescribeMoveError(string? code) => code switch
    {
        GameMoveErrorCodes.NotYourTurn => "It is not your turn.",
        GameMoveErrorCodes.MustFollowSuit => "You must follow the led suit.",
        GameMoveErrorCodes.MustTake => "You cannot play a card — use Take when you have no card in the led suit.",
        GameMoveErrorCodes.CardNotInHand => "That card is not in your hand.",
        GameMoveErrorCodes.InvalidTrick => "No valid trick to respond to.",
        _ => "Invalid move or game state."
    };

    private static string DescribeKopoMoveError(string? code) => code switch
    {
        GameMoveErrorCodes.NotYourTurn => "It is not your turn.",
        GameMoveErrorCodes.MustCapture => "You must capture when a capture is available.",
        GameMoveErrorCodes.MustMaxCapture => "You must choose a capture that takes the maximum number of pieces.",
        GameMoveErrorCodes.MustContinueChain => "You must continue capturing with the same piece.",
        GameMoveErrorCodes.InvalidMove => "That move is not legal.",
        _ => "Invalid move or game state."
    };

    private static string DescribeNgolaMoveError(string? code) => code switch
    {
        GameMoveErrorCodes.NotYourTurn => "It is not your turn.",
        GameMoveErrorCodes.InvalidMove => "Choose one of your pits containing at least two seeds.",
        _ => "Invalid move or game state."
    };

    private static string DescribeDominoMoveError(string? code) => code switch
    {
        GameMoveErrorCodes.NotYourTurn => "It is not your turn.",
        GameMoveErrorCodes.InvalidMove => "That Domino action is not legal.",
        _ => "Invalid move or game state."
    };

    private static string DescribeAbbiaMoveError(string? code) => code switch
    {
        GameMoveErrorCodes.NotYourTurn => "It is not your turn.",
        GameMoveErrorCodes.InvalidMove => "You already threw your tokens.",
        _ => "Invalid move or game state."
    };

    private static string DescribeNzengueMoveError(string? code) => code switch
    {
        GameMoveErrorCodes.NotYourTurn => "It is not your turn.",
        GameMoveErrorCodes.InvalidMove => "That Nzengué placement or move is not legal.",
        _ => "Invalid move or game state."
    };

    private static string DescribeYoteMoveError(string? code) => code switch
    {
        GameMoveErrorCodes.NotYourTurn => "It is not your turn.",
        GameMoveErrorCodes.InvalidMove => "That Yoté placement, slide, or capture is not legal.",
        _ => "Invalid move or game state."
    };
}
