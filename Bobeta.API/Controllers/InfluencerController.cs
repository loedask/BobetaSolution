using Bobeta.Application.DTOs.Influencer;
using Bobeta.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bobeta.API.Controllers;

/// <summary>Player influencer invite code apply/status.</summary>
[ApiController]
[Route("api/influencer")]
[Authorize]
public class InfluencerController(IInfluencerAttributionService attribution) : ControllerBase
{
  private Guid PlayerId => Guid.Parse(
      User.FindFirst("playerId")?.Value
      ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
      ?? throw new UnauthorizedAccessException());

  /// <summary>Applies an influencer invite code for the next game (one-time per code/player).</summary>
  [HttpPost("code")]
  public async Task<ActionResult<InfluencerCodeStatusDto>> ApplyCode(
      [FromBody] ApplyInfluencerCodeRequest request,
      CancellationToken cancellationToken)
  {
    try
    {
      await attribution.ApplyCodeAsync(PlayerId, request.Code, cancellationToken);
      var status = await attribution.GetStatusAsync(PlayerId, cancellationToken);
      return Ok(status);
    }
    catch (InvalidOperationException ex)
    {
      return BadRequest(new { error = ex.Message });
    }
  }

  /// <summary>Returns whether the player has a pending unused invite code and the current discount %.</summary>
  [HttpGet("code")]
  public async Task<ActionResult<InfluencerCodeStatusDto>> GetCodeStatus(CancellationToken cancellationToken)
  {
    var status = await attribution.GetStatusAsync(PlayerId, cancellationToken);
    return Ok(status);
  }
}
