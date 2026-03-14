using Bobeta.Application.DTOs.Game;
using Bobeta.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bobeta.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GameController : ControllerBase
{
    private readonly IGameSessionService _gameSessionService;

    public GameController(IGameSessionService gameSessionService) => _gameSessionService = gameSessionService;

    private Guid PlayerId => Guid.Parse(User.FindFirst("playerId")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());

    [HttpPost("create")]
    public async Task<ActionResult<GameSessionDto>> CreateGame([FromBody] CreateGameRequest request, CancellationToken cancellationToken)
    {
        var session = await _gameSessionService.CreateGameAsync(PlayerId, request.BetAmount, cancellationToken);
        return Ok(session);
    }

    [HttpPost("join")]
    public async Task<ActionResult<GameSessionDto>> JoinGame([FromBody] JoinGameRequest request, CancellationToken cancellationToken)
    {
        var session = await _gameSessionService.JoinGameAsync(PlayerId, request.GameId, cancellationToken);
        if (session == null) return BadRequest("Game not available to join.");
        return Ok(session);
    }

    [HttpPost("propose-bet")]
    public async Task<IActionResult> ProposeBet([FromBody] ProposeBetRequest request, CancellationToken cancellationToken)
    {
        await _gameSessionService.ProposeNewBetAsync(PlayerId, request.GameId, request.Amount, cancellationToken);
        return Accepted();
    }

    [HttpPost("accept-bet")]
    public async Task<IActionResult> AcceptBet([FromQuery] Guid gameId, CancellationToken cancellationToken)
    {
        await _gameSessionService.AcceptBetChangeAsync(gameId, cancellationToken);
        return Accepted();
    }
}
