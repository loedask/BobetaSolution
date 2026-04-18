using Bobeta.Application.DTOs.Profile;
using Bobeta.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bobeta.API.Controllers;

/// <summary>API for player profile: get profile, update language. Requires authentication.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfileController(IPlayerRepository playerRepository) : ControllerBase
{
    private readonly IPlayerRepository _playerRepository = playerRepository;

    private Guid PlayerId => Guid.Parse(User.FindFirst("playerId")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());

    /// <summary>Gets the authenticated player's profile.</summary>
    [HttpGet]
    public async Task<ActionResult<PlayerProfileDto>> GetProfile(CancellationToken cancellationToken)
    {
        var player = await _playerRepository.GetByIdAsync(PlayerId, cancellationToken);
        if (player == null) return NotFound();
        return Ok(new PlayerProfileDto(player.Id, player.PhoneNumber, player.PlayerName, player.Language, player.CreatedAt, player.IsVerified));
    }

    /// <summary>Updates the player's preferred language.</summary>
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
