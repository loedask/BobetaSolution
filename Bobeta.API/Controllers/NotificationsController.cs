using Bobeta.Application.DTOs.Notifications;
using Bobeta.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Bobeta.API.Controllers;

/// <summary>Player inbox: list, unread count, mark read.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController(INotificationService notificationService) : ControllerBase
{
    private Guid PlayerId => Guid.Parse(
        User.FindFirst("playerId")?.Value
        ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? throw new UnauthorizedAccessException());

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<NotificationDto>>> GetInbox(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 30,
        CancellationToken cancellationToken = default)
    {
        var items = await notificationService.GetInboxAsync(PlayerId, skip, take, cancellationToken);
        return Ok(items);
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<int>> GetUnreadCount(CancellationToken cancellationToken = default)
    {
        var count = await notificationService.GetUnreadCountAsync(PlayerId, cancellationToken);
        return Ok(count);
    }

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken cancellationToken = default)
    {
        await notificationService.MarkReadAsync(PlayerId, id, cancellationToken);
        return NoContent();
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken = default)
    {
        await notificationService.MarkAllReadAsync(PlayerId, cancellationToken);
        return NoContent();
    }
}
