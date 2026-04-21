using Bobeta.Application.Interfaces;
using Bobeta.Domain.Enums;
using Bobeta.Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bobeta.API.Controllers;

/// <summary>Test-only endpoint: simulate AI opponent move after a delay. For local single-browser testing.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GamePlayTestController(
    IGameEngineService gameEngineService,
    IGameSessionRepository sessionRepository) : ControllerBase
{
    private readonly IGameEngineService _gameEngineService = gameEngineService;
    private readonly IGameSessionRepository _sessionRepository = sessionRepository;

    private Guid PlayerId => Guid.Parse(User.FindFirst("playerId")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());

    /// <summary>If it is the opponent's turn, plays a random valid card for the opponent after a 1s delay. For local testing when no second browser is available.</summary>
    [HttpPost("simulate-ai")]
    public async Task<IActionResult> SimulateAiMove([FromQuery] Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId, cancellationToken);
        if (session == null || session.OpponentPlayerId == null)
            return BadRequest("Session not found or has no opponent.");
        var me = PlayerId;
        var opponentId = session.CreatorPlayerId == me ? session.OpponentPlayerId.Value : session.CreatorPlayerId;
        var state = await _gameEngineService.GetGameStateAsync(opponentId, sessionId, cancellationToken);
        if (state == null || state.MyCards == null || state.MyCards.Count == 0)
            return BadRequest("No valid game state or hand for opponent.");
        if (state.CurrentTurnPlayerId != opponentId)
            return BadRequest("Not opponent's turn.");
        var hand = state.MyCards.ToList();
        var random = new Random();
        var cardStr = hand[random.Next(hand.Count)];
        if (!TryParseCard(cardStr, out var card) || card == null)
            return BadRequest("Could not parse card.");
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            var newState = await _gameEngineService.PlayCardAsync(opponentId, sessionId, card!, cancellationToken);
            if (newState == null)
                return BadRequest("Invalid move.");
            return Ok(newState);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Client disconnected or aborted the request (e.g. Blazor navigation) during the delay or play.
            return StatusCode(StatusCodes.Status499ClientClosedRequest);
        }
    }

    private static bool TryParseCard(string value, out Card? card)
    {
        card = null;
        var parts = value.Split('_');
        if (parts.Length != 2) return false;
        if (!Enum.TryParse<CardSuit>(parts[0], ignoreCase: true, out var suit)) return false;
        if (!int.TryParse(parts[1], System.Globalization.NumberStyles.Integer, null, out var rankNum)) return false;
        if (rankNum is < 2 or > 14) return false;
        var rank = (CardRank)rankNum;
        card = new Card(suit, rank);
        return true;
    }
}
