using Bobeta.Application.DTOs.Game;
using Bobeta.Application.Interfaces;
using Bobeta.Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bobeta.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GamePlayController(IGameEngineService gameEngineService) : ControllerBase
{
    private readonly IGameEngineService _gameEngineService = gameEngineService;

    private Guid PlayerId => Guid.Parse(User.FindFirst("playerId")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());

    [HttpPost("start")]
    public async Task<IActionResult> StartGame([FromQuery] Guid sessionId, CancellationToken cancellationToken)
    {
        await _gameEngineService.StartGameAsync(sessionId, cancellationToken);
        return Accepted();
    }

    [HttpPost("play-card")]
    public async Task<ActionResult<GameStateDto>> PlayCard([FromBody] PlayCardRequest request, CancellationToken cancellationToken)
    {
        var card = new Card(request.Card.Suit, request.Card.Rank);
        var state = await _gameEngineService.PlayCardAsync(PlayerId, request.SessionId, card, cancellationToken);
        if (state == null) return BadRequest("Invalid move or game state.");
        return Ok(state);
    }

    [HttpGet("state")]
    public async Task<ActionResult<GameStateDto>> GetGameState([FromQuery] Guid sessionId, CancellationToken cancellationToken)
    {
        var state = await _gameEngineService.GetGameStateAsync(PlayerId, sessionId, cancellationToken);
        if (state == null) return NotFound();
        return Ok(state);
    }
}
