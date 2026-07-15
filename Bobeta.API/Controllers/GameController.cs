using Bobeta.Application.DTOs.Game;
using Bobeta.Application.Interfaces;
using Bobeta.Domain.Enums;
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
        try
        {
            var session = await _gameSessionService.CreateGameAsync(PlayerId, request.BetAmount, request.Variant, cancellationToken);
            return Ok(session);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>Joins an existing waiting game by id.</summary>
    [HttpPost("join")]
    public async Task<ActionResult<GameSessionDto>> JoinGame([FromBody] JoinGameRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var session = await _gameSessionService.JoinGameAsync(PlayerId, request.GameId, cancellationToken);
            if (session == null) return BadRequest("Game not available to join.");
            return Ok(session);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>Lists waiting games you can join (excludes games you created).</summary>
    [HttpGet("open")]
    public async Task<ActionResult<IReadOnlyList<GameSessionDto>>> GetOpenGames([FromQuery] int skip = 0, [FromQuery] int take = 50, [FromQuery] GameVariant? variant = null, CancellationToken cancellationToken = default)
    {
        var list = await _gameSessionService.ListOpenJoinableGamesAsync(PlayerId, skip, take, variant, cancellationToken);
        return Ok(list);
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

    /// <summary>Cancels a waiting game you created (releases stake and unused invite code).</summary>
    [HttpPost("cancel-waiting")]
    public async Task<IActionResult> CancelWaiting([FromQuery] Guid gameId, CancellationToken cancellationToken)
    {
        var ok = await _gameSessionService.CancelWaitingGameAsync(PlayerId, gameId, cancellationToken);
        if (!ok) return BadRequest("Game cannot be cancelled.");
        return NoContent();
    }
}
