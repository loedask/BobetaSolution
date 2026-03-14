using Bobeta.Application.DTOs.Profile;
using Bobeta.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bobeta.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IPlayerRepository _playerRepository;

    public ProfileController(IPlayerRepository playerRepository) => _playerRepository = playerRepository;

    private Guid PlayerId => Guid.Parse(User.FindFirst("playerId")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());

    [HttpGet]
    public async Task<ActionResult<PlayerProfileDto>> GetProfile(CancellationToken cancellationToken)
    {
        var player = await _playerRepository.GetByIdAsync(PlayerId, cancellationToken);
        if (player == null) return NotFound();
        return Ok(new PlayerProfileDto(player.Id, player.PhoneNumber, player.PlayerName, player.Language, player.CreatedAt, player.IsVerified));
    }

    [HttpPatch("language")]
    public async Task<IActionResult> UpdateLanguage([FromBody] UpdateLanguageRequest request, CancellationToken cancellationToken)
    {
        var player = await _playerRepository.GetByIdAsync(PlayerId, cancellationToken);
        if (player == null) return NotFound();
        player.Language = request.Language;
        await _playerRepository.UpdateAsync(player, cancellationToken);
        return NoContent();
    }
}
