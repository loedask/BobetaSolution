using Bobeta.Application.DTOs.Game;
using Bobeta.Application.Interfaces;
using Bobeta.API.Hubs;
using Bobeta.Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace Bobeta.API.Controllers;

/// <summary>API for Makopa gameplay: start game (deal), play card, get game state. Server broadcasts updates via SignalR.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GamePlayController(IGameEngineService gameEngineService, IHubContext<GameHub> hubContext) : ControllerBase
{
    private readonly IGameEngineService _gameEngineService = gameEngineService;
    private readonly IHubContext<GameHub> _hubContext = hubContext;

    private Guid PlayerId => Guid.Parse(User.FindFirst("playerId")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());

    /// <summary>Starts the game: deals 4 cards to each player. Session must have two players.</summary>
    [HttpPost("start")]
    public async Task<IActionResult> StartGame([FromQuery] Guid sessionId, CancellationToken cancellationToken)
    {
        await _gameEngineService.StartGameAsync(sessionId, cancellationToken);
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

        await _hubContext.Clients.Group(groupName).SendAsync("NotifyOpponentMove", cardSuitRank);
        await _hubContext.Clients.Group(groupName).SendAsync("GameState", state);

        if (state.GameOver)
            await _hubContext.Clients.Group(groupName).SendAsync("GameResult", state.WinnerPlayerId);

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
}
