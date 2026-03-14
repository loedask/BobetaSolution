using Bobeta.Application.DTOs.Game;
using Bobeta.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bobeta.API.Controllers;

/// <summary>API for game session lifecycle: create game, join game, propose/accept bet. Requires authentication.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GameController(IGameSessionService gameSessionService) : ControllerBase
{
    private readonly IGameSessionService _gameSessionService = gameSessionService;

    private Guid PlayerId => Guid.Parse(User.FindFirst("playerId")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());

    /// <summary>Creates a new game with the specified bet amount (validated 200–500).</summary>
    [HttpPost("create")]
    public async Task<ActionResult<GameSessionDto>> CreateGame([FromBody] CreateGameRequest request, CancellationToken cancellationToken)
    {
        var session = await _gameSessionService.CreateGameAsync(PlayerId, request.BetAmount, cancellationToken);
        return Ok(session);
    }

    /// <summary>Joins an existing waiting game by id.</summary>
    [HttpPost("join")]
    public async Task<ActionResult<GameSessionDto>> JoinGame([FromBody] JoinGameRequest request, CancellationToken cancellationToken)
    {
        var session = await _gameSessionService.JoinGameAsync(PlayerId, request.GameId, cancellationToken);
        if (session == null) return BadRequest("Game not available to join.");
        return Ok(session);
    }

    /// <summary>Proposes a new bet amount for the game (opponent must accept).</summary>
    [HttpPost("propose-bet")]
    public async Task<IActionResult> ProposeBet([FromBody] ProposeBetRequest request, CancellationToken cancellationToken)
    {
        await _gameSessionService.ProposeNewBetAsync(PlayerId, request.GameId, request.Amount, cancellationToken);
        return Accepted();
    }

    /// <summary>Accepts a pending bet change for the game.</summary>
    [HttpPost("accept-bet")]
    public async Task<IActionResult> AcceptBet([FromQuery] Guid gameId, CancellationToken cancellationToken)
    {
        await _gameSessionService.AcceptBetChangeAsync(gameId, cancellationToken);
        return Accepted();
    }
}
