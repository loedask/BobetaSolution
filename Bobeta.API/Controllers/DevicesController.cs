using Bobeta.Application.DTOs.Devices;
using Bobeta.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Bobeta.API.Controllers;

/// <summary>Registers FCM device tokens for phone push when the app is backgrounded or closed.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DevicesController(IDeviceTokenService deviceTokenService) : ControllerBase
{
    private Guid PlayerId => Guid.Parse(
        User.FindFirst("playerId")?.Value
        ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? throw new UnauthorizedAccessException());

    /// <summary>Upserts the current device FCM token for this player.</summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDeviceTokenRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await deviceTokenService.RegisterAsync(PlayerId, request.Token, request.Platform, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>Removes a device token (logout or permission revoked).</summary>
    [HttpPost("unregister")]
    public async Task<IActionResult> Unregister([FromBody] UnregisterDeviceTokenRequest request, CancellationToken cancellationToken)
    {
        await deviceTokenService.UnregisterAsync(PlayerId, request.Token, cancellationToken);
        return NoContent();
    }
}
