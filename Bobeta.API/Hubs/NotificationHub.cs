using System.Security.Claims;
using Bobeta.API.App.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Bobeta.API.Hubs;

/// <summary>SignalR hub for player inbox notifications and authenticated presence pings.</summary>
[Authorize]
public class NotificationHub(IPlayerOnlinePresenceTracker presenceTracker) : Hub
{
    public override async Task OnConnectedAsync()
    {
        await presenceTracker.TouchAsync(GetPlayerId(Context), Context.ConnectionAborted);
        await base.OnConnectedAsync();
    }

    /// <summary>Client heartbeat so the player stays in the admin "online now" window.</summary>
    public Task Heartbeat() =>
        presenceTracker.TouchAsync(GetPlayerId(Context), Context.ConnectionAborted);

    private static Guid GetPlayerId(HubCallerContext context)
    {
        var v = context.User?.FindFirst("playerId")?.Value
            ?? context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return v == null ? throw new HubException("Missing player id.") : Guid.Parse(v);
    }
}
