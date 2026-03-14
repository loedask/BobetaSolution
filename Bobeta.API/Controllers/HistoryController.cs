using Bobeta.Application.DTOs.History;
using Bobeta.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bobeta.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class HistoryController(IGameHistoryService gameHistoryService) : ControllerBase
{
    private readonly IGameHistoryService _gameHistoryService = gameHistoryService;

    private Guid PlayerId => Guid.Parse(User.FindFirst("playerId")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());

    [HttpGet("player")]
    public async Task<ActionResult<IReadOnlyList<GameHistoryItemDto>>> GetPlayerHistory([FromQuery] int skip = 0, [FromQuery] int take = 20, CancellationToken cancellationToken = default)
    {
        var history = await _gameHistoryService.GetPlayerHistoryAsync(PlayerId, skip, take, cancellationToken);
        return Ok(history);
    }
}
